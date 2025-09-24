# План реализации QueryChildren/DescendantsBatchAsync (ОБНОВЛЕН ДЛЯ МОДУЛЬНОЙ АРХИТЕКТУРЫ)

## Цель
Добавить перегрузки для `QueryChildrenAsync` и `QueryDescendantsAsync`, принимающие **список родительских объектов** (`IEnumerable<IRedbObject>`) вместо одного, для массового поиска детей/потомков с максимальной эффективностью.

## Проблема производительности
Текущий подход требует **N отдельных вызовов** для поиска детей/потомков множества объектов.
Новые методы должны использовать **один SQL запрос** с `parent_ids bigint[]` → критично быстрее.

## Архитектурное решение (МОДУЛЬНАЯ АРХИТЕКТУРА)
**Использовать существующую модульную архитектуру** с добавлением batch перегрузок:
- ✅ **Модульная база**: переиспользование `_build_facet_conditions`, `_build_order_conditions`, `_build_search_result`
- ✅ **Чистый API**: отдельная функция для каждого случая (одиночный/массовый)
- ✅ **Максимальная производительность**: каждая перегрузка оптимизирована под свой случай
- ✅ **Полная обратная совместимость**: существующие вызовы остаются нетронутыми
- ✅ **Элегантная архитектура**: модули + специализированные функции
- ✅ Один SQL запрос вместо N отдельных вызовов
- ✅ Полная поддержка LINQ-фильтрации для результатов

## Задачи

### Рекомендуемый порядок выполнения (снизу вверх):

### 1. Добавление batch перегрузок (ИСПОЛЬЗУЯ МОДУЛЬНУЮ АРХИТЕКТУРУ)
- [ ] В `redb.Core.Postgres/sql/redbPostgre.sql` добавить batch перегрузки:
  - **✅ Существующая база**: У нас уже есть:
    - Модули: `_build_facet_conditions`, `_build_order_conditions`, `_build_search_result`
    - 3 перегрузки: базовая (6 параметров), children (7), descendants (8)
  
  - **Шаг 1**: Добавить batch перегрузку для children (7 параметров + массив):
    ```sql
    CREATE OR REPLACE FUNCTION search_objects_with_facets(
        scheme_id bigint,
        facet_filters jsonb,
        limit_count integer,
        offset_count integer,
        distinct_mode boolean,
        order_by jsonb,
        parent_ids bigint[]  -- массив вместо одиночного parent_id
    ) RETURNS jsonb
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
    AS $BODY$
    DECLARE
        objects_result jsonb;
        total_count integer;
        where_conditions text := _build_facet_conditions(facet_filters);
        order_conditions text := _build_order_conditions(order_by, false);
        query_text text;
    BEGIN
        -- Добавляем фильтрацию по МАССИВУ родительских объектов
        IF parent_ids IS NOT NULL AND array_length(parent_ids, 1) > 0 THEN
            where_conditions := where_conditions || format(' AND o._id_parent = ANY(%L)', parent_ids);
        END IF;
        
        -- Используем существующие запросы, заменив parent_id на ANY(parent_ids)
        -- ... остальная логика идентична children функции
        RETURN _build_search_result(objects_result, total_count, limit_count, offset_count, scheme_id);
    END;
    $BODY$;
    ```
  
  - **Шаг 2**: Добавить batch перегрузку для descendants (8 параметров + массив):
    ```sql
    CREATE OR REPLACE FUNCTION search_objects_with_facets(
        scheme_id bigint,
        facet_filters jsonb,
        limit_count integer,
        offset_count integer,
        distinct_mode boolean,
        order_by jsonb,
        parent_ids bigint[],  -- массив вместо одиночного parent_id
        max_depth integer
    ) RETURNS jsonb
    -- Аналогично, но с рекурсивной логикой: d._id = ANY(parent_ids)
    ```

- [ ] **Тест**: Проверить в БД:
  - `SELECT search_objects_with_facets(1, null, 10, 0, false, null, 123, 1);` (одиночный - работает как раньше)
  - `SELECT search_objects_with_facets(1, null, 10, 0, false, null, ARRAY[123, 124, 125], 1);` (массив - children batch)
  - `SELECT search_objects_with_facets(1, null, 10, 0, false, null, ARRAY[123, 124, 125], 3);` (массив - descendants batch)

### 2. Обновление PostgresQueryProvider (МИНИМАЛЬНЫЕ ИЗМЕНЕНИЯ)
- [ ] **✅ Существующая архитектура**: У нас уже есть отличная логика маршрутизации:
  ```csharp
  // Текущая логика в ExecuteToListAsync:
  if (context.MaxDepth.HasValue && context.ParentId.HasValue)
      // 8-параметровая функция (descendants)
  else if (context.ParentId.HasValue) 
      // 7-параметровая функция (children)
  else
      // 6-параметровая функция (базовая)
  ```
- [ ] **Добавить batch поддержку** в существующие методы:
  ```csharp
  // Расширить логику на batch массивы
  if (context.MaxDepth.HasValue && (context.ParentId.HasValue || context.ParentIds != null))
      // Batch descendants (8 параметров + массив) ИЛИ одиночная descendants
  else if (context.ParentId.HasValue || context.ParentIds != null) 
      // Batch children (7 параметров + массив) ИЛИ одиночная children
  else
      // Базовая функция (6 параметров)
  ```
- [ ] **Добавить в QueryContext**: `public long[]? ParentIds { get; set; }`

### 3. Расширение существующих методов (ЭЛЕГАНТНО И ПРОСТО)
- [ ] **Расширить существующие методы**: Добавить поддержку `context.ParentIds` в `ExecuteToListAsync` и `ExecuteCountAsync`
- [ ] **Обновить логику маршрутизации** SQL вызовов:
  ```csharp
  // В ExecuteToListAsync добавить поддержку ParentIds:
  if (context.MaxDepth.HasValue && (context.ParentId.HasValue || context.ParentIds?.Length > 0))
  {
      // Descendants: одиночный ИЛИ batch
      var parentParam = context.ParentIds?.Length > 0 ? context.ParentIds : new[] { context.ParentId!.Value };
      var sql = "SELECT search_objects_with_facets({0}, {1}::jsonb, {2}, {3}, {4}, {5}::jsonb, {6}, {7}) as result";
      result = await _context.Database.SqlQueryRaw<SearchJsonResult>(sql, 
          context.SchemeId, facetFilters, context.Limit ?? 100, context.Offset ?? 0,
          context.CheckPermissions, orderByJson ?? "null", parentParam, context.MaxDepth)
          .FirstOrDefaultAsync();
  }
  else if (context.ParentId.HasValue || context.ParentIds?.Length > 0)
  {
      // Children: одиночный ИЛИ batch
      var parentParam = context.ParentIds?.Length > 0 ? context.ParentIds : new[] { context.ParentId!.Value };
      var sql = "SELECT search_objects_with_facets({0}, {1}::jsonb, {2}, {3}, {4}, {5}::jsonb, {6}) as result";
      result = await _context.Database.SqlQueryRaw<SearchJsonResult>(sql,
          context.SchemeId, facetFilters, context.Limit ?? 100, context.Offset ?? 0,
          context.CheckPermissions, orderByJson ?? "null", parentParam)
          .FirstOrDefaultAsync();
  }
  // ... остальная логика
  ```

### 4. Использование существующего RedbQueryable (НИКАКИХ НОВЫХ КЛАССОВ!)
- [ ] **✅ Нет нужды в новых классах**: Используем существующий `RedbQueryable<TProps>`
- [ ] **Просто создавать QueryContext с ParentIds**:
  ```csharp
  // Фабричные методы в PostgresQueryProvider (ПРОСТЫЕ):
  public IRedbQueryable<TProps> CreateChildrenBatchQuery<TProps>(long schemeId, long[] parentIds, long? userId = null, bool checkPermissions = false) 
      where TProps : class, new()
  {
      var context = new QueryContext<TProps>(schemeId, userId, checkPermissions)
      {
          ParentIds = parentIds  // Единственное отличие!
      };
      return new RedbQueryable<TProps>(this, context, _filterParser, _orderingParser);
  }
      
  public IRedbQueryable<TProps> CreateDescendantsBatchQuery<TProps>(long schemeId, long[] parentIds, int maxDepth, long? userId = null, bool checkPermissions = false)
      where TProps : class, new()
  {
      var context = new QueryContext<TProps>(schemeId, userId, checkPermissions)
      {
          ParentIds = parentIds,  // Batch массив
          MaxDepth = maxDepth     // Глубина рекурсии
      };
      return new RedbQueryable<TProps>(this, context, _filterParser, _orderingParser);
  }
  ```
- [ ] **Все остальное работает автоматически**: Where, OrderBy, Skip, Take, ToListAsync, CountAsync

### 5. Реализация в PostgresQueryableProvider
- [ ] В `redb.Core.Postgres/Providers/PostgresQueryableProvider.cs` добавить методы:
  ```csharp
  // ===== МЕТОДЫ ДЛЯ РАБОТЫ С ДЕТЬМИ НЕСКОЛЬКИХ ОБЪЕКТОВ =====
  
  /// <summary>
  /// Создать типобезопасный запрос для дочерних объектов нескольких родителей (автоматически определит схему по типу)
  /// </summary>
  public async Task<IRedbQueryable<TProps>> QueryChildrenAsync<TProps>(IEnumerable<IRedbObject> parentObjs) where TProps : class, new()
  
  /// <summary>
  /// Создать типобезопасный запрос для дочерних объектов нескольких родителей с указанным пользователем
  /// </summary>
  public async Task<IRedbQueryable<TProps>> QueryChildrenAsync<TProps>(IEnumerable<IRedbObject> parentObjs, IRedbUser user) where TProps : class, new()
  
  /// <summary>
  /// Синхронная версия запроса дочерних объектов нескольких родителей
  /// </summary>
  public IRedbQueryable<TProps> QueryChildren<TProps>(IEnumerable<IRedbObject> parentObjs) where TProps : class, new()
  
  // ===== МЕТОДЫ ДЛЯ РАБОТЫ С ПОТОМКАМИ НЕСКОЛЬКИХ ОБЪЕКТОВ =====
  
  /// <summary>
  /// Создать типобезопасный запрос для всех потомков нескольких родителей (автоматически определит схему по типу)
  /// </summary>
  public async Task<IRedbQueryable<TProps>> QueryDescendantsAsync<TProps>(IEnumerable<IRedbObject> parentObjs, int? maxDepth = null) where TProps : class, new()
  
  /// <summary>
  /// Создать типобезопасный запрос для всех потомков нескольких родителей с указанным пользователем
  /// </summary>
  public async Task<IRedbQueryable<TProps>> QueryDescendantsAsync<TProps>(IEnumerable<IRedbObject> parentObjs, IRedbUser user, int? maxDepth = null) where TProps : class, new()
  
  /// <summary>
  /// Синхронная версия запроса потомков нескольких родителей
  /// </summary>
  public IRedbQueryable<TProps> QueryDescendants<TProps>(IEnumerable<IRedbObject> parentObjs, int? maxDepth = null) where TProps : class, new()
  ```
- [ ] Добавить приватные методы по аналогии с `QueryChildrenPrivate` и `QueryDescendantsPrivate`:
  ```csharp
  private IRedbQueryable<TProps> QueryChildrenBatchPrivate<TProps>(long schemeId, long[] parentIds, long? userId = null, bool checkPermissions = false) where TProps : class, new()
  {
      var queryProvider = new PostgresQueryProvider(_context, _serializer, _logger);
      return queryProvider.CreateChildrenBatchQuery<TProps>(schemeId, parentIds, userId, checkPermissions);
  }
  
  private IRedbQueryable<TProps> QueryDescendantsBatchPrivate<TProps>(long schemeId, long[] parentIds, int? maxDepth = null, long? userId = null, bool checkPermissions = false) where TProps : class, new()
  {
      var actualMaxDepth = maxDepth ?? _configuration.DefaultLoadDepth;
      var queryProvider = new PostgresQueryProvider(_context, _serializer, _logger);
      return queryProvider.CreateDescendantsBatchQuery<TProps>(schemeId, parentIds, actualMaxDepth, userId, checkPermissions);
  }
  
  // Реализация публичных методов:
  public async Task<IRedbQueryable<TProps>> QueryChildrenAsync<TProps>(IEnumerable<IRedbObject> parentObjs) where TProps : class, new()
  {
      if (parentObjs == null) throw new ArgumentNullException(nameof(parentObjs));
      var parentIds = parentObjs.Where(obj => obj?.Id > 0).Select(obj => obj.Id).ToArray();
      if (parentIds.Length == 0) throw new ArgumentException("Collection must contain at least one valid parent object", nameof(parentObjs));
      
      var schemeId = await GetSchemeIdAsync<TProps>();
      var currentUser = _securityContext.GetCurrentUser();
      return QueryChildrenBatchPrivate<TProps>(schemeId, parentIds, currentUser?.Id, _configuration.AutoCheckPermissions);
  }
  
  public async Task<IRedbQueryable<TProps>> QueryDescendantsAsync<TProps>(IEnumerable<IRedbObject> parentObjs, int? maxDepth = null) where TProps : class, new()
  {
      if (parentObjs == null) throw new ArgumentNullException(nameof(parentObjs));
      var parentIds = parentObjs.Where(obj => obj?.Id > 0).Select(obj => obj.Id).ToArray();
      if (parentIds.Length == 0) throw new ArgumentException("Collection must contain at least one valid parent object", nameof(parentObjs));
      
      var schemeId = await GetSchemeIdAsync<TProps>();
      var currentUser = _securityContext.GetCurrentUser();
      return QueryDescendantsBatchPrivate<TProps>(schemeId, parentIds, maxDepth, currentUser?.Id, _configuration.AutoCheckPermissions);
  }
  ```

### 6. Расширение интерфейса IQueryableProvider
- [ ] Добавить в `redb.Core/Providers/IQueryProvider.cs`:
  ```csharp
  /// <summary>
  /// Создать типобезопасный запрос для дочерних объектов нескольких родителей
  /// </summary>
  Task<IRedbQueryable<TProps>> QueryChildrenAsync<TProps>(IEnumerable<IRedbObject> parentObjs) where TProps : class, new();
  
  /// <summary>
  /// Создать типобезопасный запрос для дочерних объектов нескольких родителей с указанным пользователем
  /// </summary>
  Task<IRedbQueryable<TProps>> QueryChildrenAsync<TProps>(IEnumerable<IRedbObject> parentObjs, IRedbUser user) where TProps : class, new();
  
  /// <summary>
  /// Синхронная версия запроса дочерних объектов нескольких родителей
  /// </summary>
  IRedbQueryable<TProps> QueryChildren<TProps>(IEnumerable<IRedbObject> parentObjs) where TProps : class, new();
  
  /// <summary>
  /// Создать типобезопасный запрос для всех потомков нескольких родителей
  /// </summary>
  Task<IRedbQueryable<TProps>> QueryDescendantsAsync<TProps>(IEnumerable<IRedbObject> parentObjs, int? maxDepth = null) where TProps : class, new();
  
  /// <summary>
  /// Создать типобезопасный запрос для всех потомков нескольких родителей с указанным пользователем
  /// </summary>
  Task<IRedbQueryable<TProps>> QueryDescendantsAsync<TProps>(IEnumerable<IRedbObject> parentObjs, IRedbUser user, int? maxDepth = null) where TProps : class, new();
  
  /// <summary>
  /// Синхронная версия запроса потомков нескольких родителей
  /// </summary>
  IRedbQueryable<TProps> QueryDescendants<TProps>(IEnumerable<IRedbObject> parentObjs, int? maxDepth = null) where TProps : class, new();
  ```

### 7. Реализация в RedbService
- [ ] Добавить в `redb.Core.Postgres/RedbService.cs` методы для делегирования в `_queryProvider`:
  ```csharp
  // ===== МЕТОДЫ ДЛЯ РАБОТЫ С ДЕТЬМИ НЕСКОЛЬКИХ ОБЪЕКТОВ =====
  public Task<IRedbQueryable<TProps>> QueryChildrenAsync<TProps>(IEnumerable<IRedbObject> parentObjs) where TProps : class, new() 
      => _queryProvider.QueryChildrenAsync<TProps>(parentObjs);
      
  public Task<IRedbQueryable<TProps>> QueryChildrenAsync<TProps>(IEnumerable<IRedbObject> parentObjs, IRedbUser user) where TProps : class, new() 
      => _queryProvider.QueryChildrenAsync<TProps>(parentObjs, user);
      
  public IRedbQueryable<TProps> QueryChildren<TProps>(IEnumerable<IRedbObject> parentObjs) where TProps : class, new() 
      => _queryProvider.QueryChildren<TProps>(parentObjs);
  
  // ===== МЕТОДЫ ДЛЯ РАБОТЫ С ПОТОМКАМИ НЕСКОЛЬКИХ ОБЪЕКТОВ =====
  public Task<IRedbQueryable<TProps>> QueryDescendantsAsync<TProps>(IEnumerable<IRedbObject> parentObjs, int? maxDepth = null) where TProps : class, new() 
      => _queryProvider.QueryDescendantsAsync<TProps>(parentObjs, maxDepth);
      
  public Task<IRedbQueryable<TProps>> QueryDescendantsAsync<TProps>(IEnumerable<IRedbObject> parentObjs, IRedbUser user, int? maxDepth = null) where TProps : class, new() 
      => _queryProvider.QueryDescendantsAsync<TProps>(parentObjs, user, maxDepth);
      
  public IRedbQueryable<TProps> QueryDescendants<TProps>(IEnumerable<IRedbObject> parentObjs, int? maxDepth = null) where TProps : class, new() 
      => _queryProvider.QueryDescendants<TProps>(parentObjs, maxDepth);
  ```

### 8. Тестирование и проверка
- [ ] Компиляция проекта без ошибок
- [ ] Тест базового вызова `QueryChildrenAsync<TProps>(parentObjs)`
- [ ] Тест `QueryDescendantsAsync<TProps>(parentObjs, maxDepth)`
- [ ] Тест с LINQ: `QueryChildrenAsync<Employee>(departments).Where(e => e.IsActive)`
- [ ] Тест пустой коллекции и null-проверок
- [ ] Тест с одним объектом в коллекции (совместимость с одиночными методами)
- [ ] Сравнение производительности с множественными одиночными вызовами

## Ключевые принципы консистентности

### 🔄 **Точная аналогия с существующими методами:**
- Та же структура методов (3 варианта: async, async с user, sync)
- Те же приватные методы `*Private<TProps>()` и `*BatchPrivate<TProps>()`
- Аналогичная обработка схем через `GetSchemeIdAsync<TProps>()`
- Консистентные исключения (`ArgumentNullException`, схема не найдена)
- Те же комментарии и документация

### 🏗️ **SQL архитектура (МОДУЛЬНАЯ + PostgreSQL overloading):**
- **✅ Модульная база**: `_build_facet_conditions`, `_build_order_conditions`, `_build_search_result`
- **✅ Существующие перегрузки**: базовая (6), children (7), descendants (8)
- **Добавить batch перегрузки**:
  1. `search_objects_with_facets(..., parent_ids bigint[])` - children batch (7 параметров + массив)
  2. `search_objects_with_facets(..., parent_ids bigint[], max_depth)` - descendants batch (8 параметров + массив)
- **PostgreSQL автовыбор**: компилятор сам выберет нужную перегрузку по типу параметра
- **Модули переиспользуются**: никакого дублирования кода, элегантность
- **Полная изоляция**: каждая функция делает одну вещь идеально

### ⚙️ **Обработка входных данных:**
- **Валидация коллекции:**
  ```csharp
  if (parentObjs == null) throw new ArgumentNullException(nameof(parentObjs));
  
  var parentIds = parentObjs.Where(obj => obj != null && obj.Id > 0)
                           .Select(obj => obj.Id)
                           .ToArray();
                           
  if (parentIds.Length == 0) 
      throw new ArgumentException("Collection must contain at least one valid parent object", nameof(parentObjs));
  ```
- **Схема из первого объекта**: `var schemeId = await GetSchemeIdAsync<TProps>();`
- Взаимоисключающие параметры: `parentId` и `parentIds`

### 🔧 **Обработка maxDepth:**
- Использовать `_configuration.DefaultLoadDepth` (консистентно с QueryDescendantsAsync)
- Паттерн: `var actualMaxDepth = maxDepth ?? _configuration.DefaultLoadDepth`
- NULL защищает от бесконечных циклов в рекурсивном CTE

## Примечания (МОДУЛЬНАЯ АРХИТЕКТУРА)
- **✅ Модульная база готова**: `_build_facet_conditions`, `_build_order_conditions`, `_build_search_result`
- **Критично**: Один SQL запрос с `parent_ids bigint[]` вместо N отдельных вызовов
- **Архитектурное превосходство**: Модульная архитектура + PostgreSQL overloading:
  - 🎯 **Переиспользование модулей**: никакого дублирования кода
  - ⚡ **Максимальная производительность**: каждая перегрузка оптимизирована
  - 🚫 **Чистая архитектура**: PostgreSQL сам выберет нужную функцию по параметрам
  - 🧹 **Элегантный C# код**: просто добавить `ParentIds` в `QueryContext`
- **Минимальные изменения**: существующие структуры почти НЕ ТРОНУТЫ
- **Никаких новых классов**: используем существующий `RedbQueryable<TProps>`
- **Простота реализации**: модульная архитектура значительно упростила задачу
- Сохранить полную обратную совместимость
- **Инвестиция окупилась**: модульная архитектура сделала batch версии тривиальными

## Результат
После выполнения можно будет использовать:
```csharp
var departments = await service.QueryAsync<Department>()
    .Where(d => d.IsActive).ToListAsync();

// Все активные сотрудники из множества департаментов одним запросом
var allEmployees = await service.QueryChildrenAsync<Employee>(departments)
    .Where(e => e.IsActive == true)
    .Where(e => e.Salary > 50000)
    .OrderBy(e => e.Name)
    .ToListAsync();

// Все потомки из нескольких корневых объектов (рекурсивно)
var categories = new[] { electronics, clothing, books };
var allProducts = await service.QueryDescendantsAsync<Product>(categories, maxDepth: 3)
    .Where(p => p.Price > 50)
    .OrderBy(p => p.Category)
    .ThenBy(p => p.Name)
    .Skip(page * pageSize)
    .Take(pageSize)
    .ToListAsync();

// Пагинация всех дочерних объектов из множества родителей
var companiesBatch = await service.LoadBatchAsync<Company>(companyIds);
var allEmployeesPage = await service.QueryChildrenAsync<Employee>(companiesBatch)
    .Where(e => e.Department == "IT")
    .OrderBy(e => e.LastName)
    .ThenBy(e => e.FirstName)
    .Skip(pageNumber * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

### 🚀 **Преимущества модульной архитектуры + batch:**
- **🎯 Модульное совершенство**: переиспользование `_build_*` модулей без дублирования
- **⚡ Максимальная производительность**: 
  - PostgreSQL автоматически выбирает оптимальную перегрузку
  - Модули обеспечивают консистентную логику
  - 1 SQL запрос вместо N отдельных вызовов
- **🧹 Элегантная простота**:
  - Минимальные изменения: добавить `ParentIds` в `QueryContext`
  - Никаких новых классов: используем существующий `RedbQueryable<TProps>`
  - Существующие методы автоматически поддержат batch режим
- **🔧 Техническое превосходство**:
  - Полная LINQ поддержка автоматически работает для batch
  - Интеграция с системой прав пользователей сохранена
  - Полиморфный поиск по схеме типа работает из коробки
- **📈 Долгосрочная ценность**:
  - **Инвестиция окупилась**: модульная архитектура сделала batch тривиальным
  - Легкая расширяемость через новые модули
  - Максимальная консистентность и качество кода
  - **Доказательство правильности решения**: сложная задача стала простой
