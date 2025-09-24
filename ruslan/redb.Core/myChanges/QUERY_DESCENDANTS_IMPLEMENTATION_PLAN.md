# План реализации QueryDescendantsAsync

## Цель
Добавить методы `QueryDescendantsAsync<TProps>(IRedbObject parentObj, int? maxDepth = null)` аналогично существующему `QueryChildrenAsync<TProps>()`, но для поиска среди **всех потомков** (не только прямых детей) с поддержкой LINQ-фильтрации.

## Проблема производительности
Текущий `GetDescendantsAsync` использует **N+1 подход**: множественные вызовы `GetChildrenWithUserAsync` → много запросов к БД.
`QueryDescendantsAsync` должен использовать **один рекурсивный SQL** → критично быстрее.

## Архитектурное решение
**Модифицировать существующую функцию** `search_objects_with_facets` вместо создания новой:
- ✅ Избегаем дублирования сложной логики фильтрации (~300 строк)
- ✅ max_depth = 1 (default) → текущее поведение QueryChildrenAsync  
- ✅ max_depth > 1 → новое поведение QueryDescendantsAsync
- ✅ Единая точка поддержки фильтрации

## Задачи

### Рекомендуемый порядок выполнения (снизу вверх):

### 1. Модификация SQL функции
- [x] В `redb.Core.Postgres/sql/redbPostgre.sql` модифицировать функцию `search_objects_with_facets`:
  - Добавить параметр: `max_depth integer DEFAULT 1`
  - **Условная логика для оптимальной производительности:**
    - `IF max_depth = 1 THEN` → использовать **текущую простую логику** (QueryChildrenAsync остается быстрой)
    - `ELSE` → использовать **WITH RECURSIVE CTE** для обхода дерева потомков (QueryDescendantsAsync)
  - Сохранить всю существующую логику LINQ-фильтрации в обеих ветках
  - Обновить сигнатуру комментария к функции
- [ ] **Тест**: Проверить в БД: 
  - `SELECT search_objects_with_facets(1, null, 10, 0, false, null, 123, 1);` (простая логика)
  - `SELECT search_objects_with_facets(1, null, 10, 0, false, null, 123, 3);` (рекурсивная логика)

### 2. Расширение QueryContext  
- [x] В `redb.Core/Query/QueryContext.cs` добавить поле `MaxDepth` в класс `QueryContext<TProps>`:
  ```csharp
  public int? MaxDepth { get; init; }
  ```
- [x] Обновить конструктор `QueryContext<TProps>` для принятия maxDepth:
  ```csharp
  public QueryContext(long schemeId, long? userId = null, bool checkPermissions = false, long? parentId = null, int? maxDepth = null)
  ```
- [x] Обновить метод `Clone()` для копирования MaxDepth

### 3. Обновление логики выполнения запросов
- [x] **НЕ ТРОГАЕМ существующие SQL вызовы!** QueryAsync и QueryChildrenAsync остаются как есть
- [x] Добавить новую логику в `PostgresQueryProvider.ExecuteToListAsync<TProps>()`:
  ```csharp
  // Если есть MaxDepth - передаем 8 параметров (для QueryDescendantsAsync)
  if (context.MaxDepth.HasValue) {
      var sql = "SELECT search_objects_with_facets({0}, {1}::jsonb, {2}, {3}, {4}, {5}::jsonb, {6}, {7}) as result";
      // передаем context.MaxDepth
  } else {
      // Существующая логика - 7 параметров (для QueryAsync и QueryChildrenAsync)
      var sql = "SELECT search_objects_with_facets({0}, {1}::jsonb, {2}, {3}, {4}, {5}::jsonb, {6}) as result";
  }
  ```
- [x] Аналогичную логику в `PostgresQueryProvider.ExecuteCountAsync<TProps>()`
- [x] Добавить метод `CreateDescendantsQuery<TProps>()` в PostgresQueryProvider

### 4. Реализация в PostgresQueryableProvider
- [x] В `redb.Core.Postgres/Providers/PostgresQueryableProvider.cs` добавить методы:
  ```csharp
  public async Task<IRedbQueryable<TProps>> QueryDescendantsAsync<TProps>(IRedbObject parentObj, int? maxDepth = null) where TProps : class, new()
  public async Task<IRedbQueryable<TProps>> QueryDescendantsAsync<TProps>(IRedbObject parentObj, IRedbUser user, int? maxDepth = null) where TProps : class, new()
  public IRedbQueryable<TProps> QueryDescendants<TProps>(IRedbObject parentObj, int? maxDepth = null) where TProps : class, new()
  ```
- [x] Добавить приватный метод `QueryDescendantsPrivate<TProps>()` по аналогии с `QueryChildrenPrivate<TProps>()`:
  ```csharp
  private IRedbQueryable<TProps> QueryDescendantsPrivate<TProps>(long schemeId, long parentId, int? maxDepth = null, long? userId = null, bool checkPermissions = false) where TProps : class, new()
  {
      var actualMaxDepth = maxDepth ?? _configuration.DefaultLoadDepth;
      var queryProvider = new PostgresQueryProvider(_context, _serializer, _logger);
      return queryProvider.CreateDescendantsQuery<TProps>(schemeId, parentId, actualMaxDepth, userId, checkPermissions);
  }
  ```

### 5. Расширение интерфейса IQueryableProvider
- [x] Добавить в `redb.Core/Providers/IQueryProvider.cs`:
  ```csharp
  /// <summary>
  /// Создать типобезопасный запрос для всех потомков объекта (автоматически определит схему по типу)
  /// </summary>
  Task<IRedbQueryable<TProps>> QueryDescendantsAsync<TProps>(IRedbObject parentObj, int? maxDepth = null) where TProps : class, new();
  
  /// <summary>
  /// Создать типобезопасный запрос для всех потомков объекта с указанным пользователем
  /// </summary>
  Task<IRedbQueryable<TProps>> QueryDescendantsAsync<TProps>(IRedbObject parentObj, IRedbUser user, int? maxDepth = null) where TProps : class, new();
  
  /// <summary>
  /// Синхронная версия запроса потомков
  /// </summary>
  IRedbQueryable<TProps> QueryDescendants<TProps>(IRedbObject parentObj, int? maxDepth = null) where TProps : class, new();
  ```

### 6. Реализация в RedbService
- [x] Добавить в `redb.Core.Postgres/RedbService.cs` методы для делегирования в `_queryProvider`

### 7. Тестирование и проверка
- [x] Компиляция проекта без ошибок
- [ ] Тест базового вызова `QueryDescendantsAsync<TProps>(parentObj)`
- [ ] Тест с maxDepth: `QueryDescendantsAsync<Employee>(company, 3)`
- [ ] Тест с LINQ фильтрами: `QueryDescendantsAsync<Employee>(company).Where(e => e.IsActive)`
- [ ] Тест с сортировкой: `QueryDescendantsAsync<Employee>(company).OrderBy(e => e.Name)`
- [ ] Тест пагинации: `QueryDescendantsAsync<Employee>(company).Skip(10).Take(5)`
- [ ] Сравнение производительности с `GetDescendantsAsync`

## Ключевые принципы консистентности

### 🔄 **Аналогия с QueryChildrenAsync:**
- Та же структура методов (3 варианта: async, async с user, sync)
- Тот же паттерн приватных методов
- Аналогичная обработка схем и пользователей
- Консистентные исключения и проверки

### 🏗️ **SQL архитектура:**
- Модификация существующей функции `search_objects_with_facets`
- Добавление параметра `max_depth integer DEFAULT 1`
- **Условная оптимизация производительности:**
  - `max_depth = 1` → текущая быстрая логика (QueryChildrenAsync **не замедляется**)
  - `max_depth > 1` → рекурсивная CTE (QueryDescendantsAsync)
- Сохранение всей логики фильтрации и форматов возврата

### ⚙️ **Обработка maxDepth:**
- Использовать `_configuration.DefaultLoadDepth` (не DefaultMaxTreeDepth)
- Паттерн: `var actualMaxDepth = maxDepth ?? _configuration.DefaultLoadDepth`  
- NULL означает использование дефолтного значения конфигурации
- max_depth защищает от бесконечных циклов (циклические ссылки не обрабатываются отдельно)

## Примечания
- **Критично**: Один рекурсивный SQL вместо N+1 запросов
- **Элегантная архитектура**: QueryAsync и QueryChildrenAsync остаются НЕТРОНУТЫМИ
  - Передают 7 параметров → PostgreSQL использует `max_depth DEFAULT 1` → быстрая логика
  - Только QueryDescendantsAsync передает 8-й параметр → рекурсивная логика
- Сохранить полную обратную совместимость (никого не трогаем!)
- Использовать тот же стиль кода и комментарии, что и в QueryChildrenAsync  
- Следовать паттерну максимальной консистентности с существующими методами
- Все изменения должны быть минимальными и не затрагивать работающий функционал

## Результат
После выполнения можно будет использовать:
```csharp
var company = await service.LoadAsync<Company>(companyId);

// Все активные сотрудники во всей компании (любой уровень вложенности)
var allActiveEmployees = await service.QueryDescendantsAsync<Employee>(company)
    .Where(e => e.IsActive == true)
    .Where(e => e.Salary > 50000)
    .OrderBy(e => e.Department)
    .ThenBy(e => e.Name)
    .ToListAsync();

// С ограничением глубины
var managementLevel = await service.QueryDescendantsAsync<Employee>(company, maxDepth: 2)
    .Where(e => e.Position.Contains("Manager"))
    .ToListAsync();

// Пагинация среди всех потомков
var page = await service.QueryDescendantsAsync<Product>(category)
    .Where(p => p.Price > 100)
    .OrderBy(p => p.Name)
    .Skip(pageSize * pageNumber)
    .Take(pageSize)
    .ToListAsync();
```

## ✅ Принятые архитектурные решения

1. **SQL функция:** Модификация существующей `search_objects_with_facets` (добавление `max_depth`)
2. **Поведение maxDepth:** NULL → использовать `_configuration.DefaultLoadDepth`  
3. **Циклические ссылки:** Полагаемся только на `max_depth` (как в GetDescendantsAsync)
4. **Полиморфность:** НЕ поддерживается (нельзя использовать LINQ без знания типа)
