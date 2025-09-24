# План реализации QueryChildrenAsync

## Цель
Добавить методы `QueryChildrenAsync<TProps>(IRedbObject parentObj)` аналогично существующему `QueryAsync<TProps>()`, но с дополнительной фильтрацией по parent_id.

## Задачи

### Рекомендуемый порядок выполнения (снизу вверх):

### 1. Модификация SQL функции
- [x] В `redb.Core.Postgres/sql/redbPostgre.sql` расширить функцию `search_objects_with_facets`:
  - Добавить параметр `parent_id bigint DEFAULT NULL`
  - Добавить PL/pgSQL проверку `IF parent_id IS NOT NULL THEN...` (консистентно с остальными параметрами)
- [ ] **Тест**: Проверить в БД: `SELECT search_objects_with_facets(1, null, 10, 0, false, null, 123);`

### 2. Расширение QueryContext  
- [x] В `redb.Core/Query/QueryContext.cs` добавить поле `ParentId` в класс `QueryContext<TProps>`:
  ```csharp
  public long? ParentId { get; init; }
  ```
- [x] Обновить конструктор `QueryContext<TProps>` для принятия parentId:
  ```csharp
  public QueryContext(long schemeId, long? userId = null, bool checkPermissions = false, long? parentId = null)
  ```
- [x] Обновить метод `Clone()` для копирования ParentId

### 3. Обновление логики выполнения запросов
- [x] В `PostgresQueryProvider.ExecuteToListAsync<TProps>()` изменить SQL вызов:
  ```csharp
  var sql = "SELECT search_objects_with_facets({0}, {1}::jsonb, {2}, {3}, {4}, {5}::jsonb, {6}) as result";
  var result = await _context.Database.SqlQueryRaw<SearchJsonResult>(sql, 
      context.SchemeId, facetFilters, parameters.Limit ?? 100, parameters.Offset ?? 0, 
      context.IsDistinct, orderByJson ?? "null", context.ParentId)
  ```
- [x] В `PostgresQueryProvider.ExecuteCountAsync<TProps>()` изменить SQL вызов аналогично

### 4. Расширение PostgresQueryProvider
- [x] В `redb.Core.Postgres/Query/PostgresQueryProvider.cs` добавить новый метод `CreateChildrenQuery`:
  ```csharp
  public IRedbQueryable<TProps> CreateChildrenQuery<TProps>(long schemeId, long parentId, long? userId = null, bool checkPermissions = false) 
      where TProps : class, new()
  {
      var context = new QueryContext<TProps>(schemeId, userId, checkPermissions, parentId);
      return new RedbQueryable<TProps>(this, context, _filterParser, _orderingParser);
  }
  ```

### 5. Реализация в PostgresQueryableProvider
- [x] В `redb.Core.Postgres/Providers/PostgresQueryableProvider.cs` добавить методы:
  ```csharp
  public async Task<IRedbQueryable<TProps>> QueryChildrenAsync<TProps>(IRedbObject parentObj) where TProps : class, new()
  public async Task<IRedbQueryable<TProps>> QueryChildrenAsync<TProps>(IRedbObject parentObj, IRedbUser user) where TProps : class, new()
  public IRedbQueryable<TProps> QueryChildren<TProps>(IRedbObject parentObj) where TProps : class, new()
  ```
- [x] Добавить приватный метод `QueryChildrenPrivate<TProps>()` по аналогии с `QueryPrivate<TProps>()`:
  ```csharp
  private IRedbQueryable<TProps> QueryChildrenPrivate<TProps>(long schemeId, long parentId, long? userId = null, bool checkPermissions = false) where TProps : class, new()
  {
      var queryProvider = new PostgresQueryProvider(_context, _serializer, _logger);
      return queryProvider.CreateChildrenQuery<TProps>(schemeId, parentId, userId, checkPermissions);
  }
  ```

### 6. Расширение интерфейса IQueryableProvider
- [x] Добавить в `redb.Core/Providers/IQueryProvider.cs`:
  ```csharp
  /// <summary>
  /// Создать типобезопасный запрос для дочерних объектов (автоматически определит схему по типу)
  /// </summary>
  Task<IRedbQueryable<TProps>> QueryChildrenAsync<TProps>(IRedbObject parentObj) where TProps : class, new();
  
  /// <summary>
  /// Создать типобезопасный запрос для дочерних объектов с указанным пользователем
  /// </summary>
  Task<IRedbQueryable<TProps>> QueryChildrenAsync<TProps>(IRedbObject parentObj, IRedbUser user) where TProps : class, new();
  
  /// <summary>
  /// Синхронная версия запроса дочерних объектов
  /// </summary>
  IRedbQueryable<TProps> QueryChildren<TProps>(IRedbObject parentObj) where TProps : class, new();
  ```

### 7. Реализация в RedbService
- [x] Добавить в `redb.Core.Postgres/RedbService.cs` методы для делегирования в `_queryProvider`

### 8. Тестирование и проверка
- [ ] Компиляция проекта без ошибок
- [ ] Тест базового вызова `QueryChildrenAsync<TProps>(parentObj)`
- [ ] Тест с LINQ фильтрами: `QueryChildrenAsync<Employee>(department).Where(e => e.IsActive)`
- [ ] Тест с сортировкой: `QueryChildrenAsync<Employee>(department).OrderBy(e => e.Name)`
- [ ] Тест пагинации: `QueryChildrenAsync<Employee>(department).Skip(10).Take(5)`

## Примечания
- Сохранить полную обратную совместимость
- Использовать тот же стиль кода и комментарии, что и в существующих методах
- Следовать паттерну существующих методов `QueryAsync`
- Все изменения должны быть минимальными и не затрагивать работающий функционал

## Результат
После выполнения можно будет использовать:
```csharp
var department = await service.LoadAsync<Department>(departmentId);
var activeEmployees = await service.QueryChildrenAsync<Employee>(department)
    .Where(e => e.IsActive == true)
    .OrderBy(e => e.Name)
    .ToListAsync();
```
