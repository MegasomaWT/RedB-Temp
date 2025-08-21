# План реализации QueryChildren/DescendantsBatchAsync

## Цель
Добавить перегрузки для `QueryChildrenAsync` и `QueryDescendantsAsync`, принимающие **список родительских объектов** (`IEnumerable<IRedbObject>`) вместо одного, для массового поиска детей/потомков с максимальной эффективностью.

## Проблема производительности
Текущий подход требует **N отдельных вызовов** для поиска детей/потомков множества объектов.
Новые методы должны использовать **один SQL запрос** с `parent_ids bigint[]` → критично быстрее.

## Архитектурное решение
**Создать перегрузки функции** `search_objects_with_facets` через PostgreSQL function overloading:
- ✅ **Чистый API**: отдельная функция для каждого случая (одиночный/массовый)
- ✅ **Максимальная производительность**: каждая перегрузка оптимизирована под свой случай
- ✅ **Полная обратная совместимость**: существующие вызовы остаются нетронутыми
- ✅ **Элегантная архитектура**: общая логика в приватной функции, тонкие публичные обертки
- ✅ Один SQL запрос вместо N отдельных вызовов
- ✅ Полная поддержка LINQ-фильтрации для результатов

## Задачи

### Рекомендуемый порядок выполнения (снизу вверх):

### 1. Рефакторинг SQL функций (PostgreSQL overloading)
- [ ] В `redb.Core.Postgres/sql/redbPostgre.sql` создать архитектуру overloading:
  - **Шаг 1**: Создать приватную функцию `_search_objects_with_facets_internal` с `parent_ids bigint[]`:
    ```sql
    CREATE OR REPLACE FUNCTION _search_objects_with_facets_internal(
        scheme_id bigint,
        facet_filters jsonb DEFAULT NULL,
        limit_count integer DEFAULT 100,
        offset_count integer DEFAULT 0,
        distinct_mode boolean DEFAULT false,
        order_by jsonb DEFAULT NULL,
        parent_ids bigint[] DEFAULT NULL,  -- всегда массив внутри
        max_depth integer DEFAULT 1
    ) RETURNS jsonb
    ```
    - Перенести ВСЮ логику из существующей функции
    - Заменить `WHERE o._id_parent = parent_id` на `WHERE o._id_parent = ANY(parent_ids)`
    - Для рекурсивного CTE: `d._id IN (SELECT unnest(parent_ids))`
  
  - **Шаг 2**: Превратить существующую функцию в тонкую обертку:
    ```sql
    CREATE OR REPLACE FUNCTION search_objects_with_facets(
        scheme_id bigint,
        facet_filters jsonb DEFAULT NULL,
        limit_count integer DEFAULT 100,
        offset_count integer DEFAULT 0,
        distinct_mode boolean DEFAULT false,
        order_by jsonb DEFAULT NULL,
        parent_id bigint DEFAULT NULL,
        max_depth integer DEFAULT 1
    ) RETURNS jsonb
    AS $$
    BEGIN
        RETURN _search_objects_with_facets_internal(
            scheme_id, facet_filters, limit_count, offset_count,
            distinct_mode, order_by,
            CASE WHEN parent_id IS NULL THEN NULL ELSE ARRAY[parent_id] END,
            max_depth
        );
    END;
    $$ LANGUAGE plpgsql;
    ```
  
  - **Шаг 3**: Создать новую перегрузку для массивов:
    ```sql
    CREATE OR REPLACE FUNCTION search_objects_with_facets(
        scheme_id bigint,
        facet_filters jsonb DEFAULT NULL,
        limit_count integer DEFAULT 100,
        offset_count integer DEFAULT 0,
        distinct_mode boolean DEFAULT false,
        order_by jsonb DEFAULT NULL,
        parent_ids bigint[] DEFAULT NULL,
        max_depth integer DEFAULT 1
    ) RETURNS jsonb
    AS $$
    BEGIN
        RETURN _search_objects_with_facets_internal(
            scheme_id, facet_filters, limit_count, offset_count,
            distinct_mode, order_by, parent_ids, max_depth
        );
    END;
    $$ LANGUAGE plpgsql;
    ```

- [ ] **Тест**: Проверить в БД:
  - `SELECT search_objects_with_facets(1, null, 10, 0, false, null, 123, 1);` (одиночный - работает как раньше)
  - `SELECT search_objects_with_facets(1, null, 10, 0, false, null, ARRAY[123, 124, 125], 1);` (массив - children batch)
  - `SELECT search_objects_with_facets(1, null, 10, 0, false, null, ARRAY[123, 124, 125], 3);` (массив - descendants batch)

### 2. Создание специализированных QueryContext (БЕЗ ИЗМЕНЕНИЙ в существующем коде!)
- [ ] **НЕ ТРОГАЕМ** существующий `QueryContext<TProps>` - он остается без изменений!
- [ ] Создадим **отдельные контексты** для batch операций в `PostgresQueryProvider`:
  ```csharp
  // Для одиночных операций - используем существующий QueryContext
  // QueryContext(schemeId, userId, checkPermissions, parentId, maxDepth)
  
  // Для batch операций - создадим простой класс-маркер
  private class BatchQueryInfo
  {
      public long SchemeId { get; init; }
      public long[] ParentIds { get; init; }
      public int MaxDepth { get; init; }
      public long? UserId { get; init; }
      public bool CheckPermissions { get; init; }
  }
  ```
- [ ] **Преимущество**: Полная изоляция - никаких взаимоисключающих параметров, никаких валидаций

### 3. Создание специализированных методов выполнения (БЕЗ ИЗМЕНЕНИЙ в существующих!)
- [ ] **НЕ ТРОГАЕМ** существующие методы! `ExecuteToListAsync` и `ExecuteCountAsync` остаются как есть
- [ ] Создать **отдельные методы** для batch операций в `PostgresQueryProvider`:
  ```csharp
  // Новые методы специально для batch операций
  public async Task<List<TProps>> ExecuteBatchToListAsync<TProps>(
      BatchQueryInfo batchInfo,
      QueryParameters parameters,
      CancellationToken cancellationToken) where TProps : class, new()
  {
      var orderByJson = _orderingParser.ParseOrderBy<TProps>(parameters.OrderBy);
      var facetFilters = _filterParser.ParseFilters<TProps>(parameters.Filters);
      
      SearchJsonResult result;
      if (batchInfo.MaxDepth > 1)
      {
          // Вызов перегрузки для descendants batch (массив + max_depth)
          var sql = "SELECT search_objects_with_facets({0}, {1}::jsonb, {2}, {3}, {4}, {5}::jsonb, {6}, {7}) as result";
          result = await _context.Database.SqlQueryRaw<SearchJsonResult>(sql, 
              batchInfo.SchemeId, facetFilters, parameters.Limit ?? 100, parameters.Offset ?? 0, 
              batchInfo.CheckPermissions, orderByJson ?? "null", 
              batchInfo.ParentIds, batchInfo.MaxDepth).FirstOrDefaultAsync();
      }
      else
      {
          // Вызов перегрузки для children batch (только массив)
          var sql = "SELECT search_objects_with_facets({0}, {1}::jsonb, {2}, {3}, {4}, {5}::jsonb, {6}, {7}) as result";
          result = await _context.Database.SqlQueryRaw<SearchJsonResult>(sql, 
              batchInfo.SchemeId, facetFilters, parameters.Limit ?? 100, parameters.Offset ?? 0, 
              batchInfo.CheckPermissions, orderByJson ?? "null", 
              batchInfo.ParentIds, 1).FirstOrDefaultAsync();
      }
      
      // Остальная логика обработки результата...
  }
  
  public async Task<int> ExecuteBatchCountAsync<TProps>(
      BatchQueryInfo batchInfo,
      QueryParameters parameters) where TProps : class, new()
  {
      // Аналогичная логика для подсчета
  }
  ```

### 4. Создание специализированных Query классов
- [ ] Добавить в `PostgresQueryProvider` специальные классы для batch операций:
  ```csharp
  // Специальный Queryable для batch операций (не наследует от RedbQueryable)
  private class BatchRedbQueryable<TProps> : IRedbQueryable<TProps> where TProps : class, new()
  {
      private readonly PostgresQueryProvider _provider;
      private readonly BatchQueryInfo _batchInfo;
      // Используем существующие парсеры
      
      public async Task<List<TProps>> ToListAsync() =>
          await _provider.ExecuteBatchToListAsync<TProps>(_batchInfo, GetParameters(), CancellationToken.None);
          
      public async Task<int> CountAsync() =>
          await _provider.ExecuteBatchCountAsync<TProps>(_batchInfo, GetParameters());
      
      // Остальные LINQ методы Where, OrderBy, Skip, Take...
  }
  
  // Фабричные методы в PostgresQueryProvider:
  public IRedbQueryable<TProps> CreateChildrenBatchQuery<TProps>(long schemeId, long[] parentIds, long? userId = null, bool checkPermissions = false) 
      where TProps : class, new()
  {
      var batchInfo = new BatchQueryInfo 
      { 
          SchemeId = schemeId, 
          ParentIds = parentIds, 
          MaxDepth = 1, 
          UserId = userId, 
          CheckPermissions = checkPermissions 
      };
      return new BatchRedbQueryable<TProps>(_provider: this, _batchInfo: batchInfo, _filterParser, _orderingParser);
  }
      
  public IRedbQueryable<TProps> CreateDescendantsBatchQuery<TProps>(long schemeId, long[] parentIds, int maxDepth, long? userId = null, bool checkPermissions = false)
      where TProps : class, new()
  {
      var batchInfo = new BatchQueryInfo 
      { 
          SchemeId = schemeId, 
          ParentIds = parentIds, 
          MaxDepth = maxDepth, 
          UserId = userId, 
          CheckPermissions = checkPermissions 
      };
      return new BatchRedbQueryable<TProps>(_provider: this, _batchInfo: batchInfo, _filterParser, _orderingParser);
  }
  ```

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

### 🏗️ **SQL архитектура (PostgreSQL overloading):**
- **Приватная функция**: `_search_objects_with_facets_internal` с универсальной логикой (`parent_ids bigint[]`)
- **Две публичные перегрузки**:
  1. `search_objects_with_facets(..., parent_id bigint, max_depth)` - тонкая обертка для одиночных операций
  2. `search_objects_with_facets(..., parent_ids bigint[], max_depth)` - тонкая обертка для batch операций
- **PostgreSQL автовыбор**: компилятор сам выберет нужную перегрузку по типу параметра
- **Полная изоляция**: никаких условных IF в рантайме, каждая функция оптимизирована
- Сохранение всей логики фильтрации и форматов возврата

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

## Примечания
- **Критично**: Один SQL запрос с `parent_ids bigint[]` вместо N отдельных вызовов
- **Архитектурное превосходство**: PostgreSQL function overloading обеспечивает:
  - 🎯 **Чистый API**: каждая функция делает одну вещь идеально
  - ⚡ **Максимальная производительность**: никаких условных проверок в рантайме
  - 🚫 **Никаких взаимоисключающих параметров**: PostgreSQL сам выберет нужную перегрузку
  - 🧹 **Чистый C# код**: четкое разделение логики без сложных условий
- **Полная изоляция**: существующие структуры (`QueryContext`, `RedbQueryable`) остаются НЕТРОНУТЫМИ
- **Специализированные классы**: `BatchQueryInfo` и `BatchRedbQueryable` для batch операций
- Сохранить полную обратную совместимость
- Использовать тот же стиль кода и комментарии, что и в существующих методах
- Следовать паттерну максимальной консистентности
- **Рефакторинг как инвестиция**: один раз правильно, на годы вперед

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

### 🚀 **Преимущества архитектурного подхода:**
- **🎯 Идеальный API дизайн**: каждая функция имеет четкую единственную ответственность
- **⚡ Максимальная производительность**: 
  - PostgreSQL автоматически выбирает оптимальную перегрузку
  - Никаких лишних условных проверок в рантайме
  - 1 SQL запрос вместо N отдельных вызовов
- **🧹 Чистота кода**:
  - Полная изоляция: существующий код НЕТРОНУТ
  - Никаких взаимоисключающих параметров
  - Специализированные классы для специализированных задач
- **🔧 Техническое превосходство**:
  - Полная LINQ поддержка (фильтрация, сортировка, пагинация)
  - Интеграция с системой прав пользователей
  - Полиморфный поиск по схеме типа
- **📈 Долгосрочная ценность**:
  - Легкая расширяемость (добавить новые перегрузки)
  - Максимальная консистентность с существующими паттернами
  - Инвестиция в качество архитектуры
