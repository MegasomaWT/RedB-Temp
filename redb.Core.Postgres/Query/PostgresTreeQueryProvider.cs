using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using redb.Core.Models.Entities;
using redb.Core.Query;
using redb.Core.Query.QueryExpressions;
using redb.Core.Serialization;

namespace redb.Core.Postgres.Query;

/// <summary>
/// PostgreSQL провайдер для выполнения древовидных LINQ-запросов через search_tree_objects_with_facets
/// Расширяет функциональность PostgresQueryProvider добавляя поддержку иерархических операторов
/// </summary>
public class PostgresTreeQueryProvider : ITreeQueryProvider
{
    private readonly RedbContext _context;
    private readonly IRedbObjectSerializer _serializer;
    private readonly PostgresFilterExpressionParser _filterParser;
    private readonly PostgresOrderingExpressionParser _orderingParser;
    private readonly PostgresFacetFilterBuilder _facetBuilder;
    private readonly ILogger? _logger;

    public PostgresTreeQueryProvider(
        RedbContext context,
        IRedbObjectSerializer serializer,
        ILogger? logger = null)
    {
        _context = context;
        _serializer = serializer;
        _logger = logger;
        _filterParser = new PostgresFilterExpressionParser();
        _orderingParser = new PostgresOrderingExpressionParser();
        _facetBuilder = new PostgresFacetFilterBuilder(logger);
    }

    // ===== РЕАЛИЗАЦИЯ IRedbQueryProvider (БАЗОВАЯ ФУНКЦИОНАЛЬНОСТЬ) =====
    
    public IRedbQueryable<TProps> CreateQuery<TProps>(long schemeId, long? userId = null, bool checkPermissions = false) 
        where TProps : class, new()
    {
        // Для обычных запросов создаем стандартный RedbQueryable
        // Он будет использовать search_objects_with_facets() через базовый PostgresQueryProvider
        var baseProvider = new PostgresQueryProvider(_context, _serializer, _logger);
        return baseProvider.CreateQuery<TProps>(schemeId, userId, checkPermissions);
    }

    // ===== РЕАЛИЗАЦИЯ ITreeQueryProvider (ДРЕВОВИДНАЯ ФУНКЦИОНАЛЬНОСТЬ) =====
    
    public ITreeQueryable<TProps> CreateTreeQuery<TProps>(
        long schemeId, 
        long? userId = null, 
        bool checkPermissions = false,
        long? rootObjectId = null,
        int? maxDepth = null
    ) where TProps : class, new()
    {
        var context = new TreeQueryContext<TProps>(schemeId, userId, checkPermissions, rootObjectId, maxDepth);
        return new PostgresTreeQueryable<TProps>(this, context, _filterParser, _orderingParser);
    }

    // ===== ВЫПОЛНЕНИЕ ЗАПРОСОВ =====
    
    public async Task<object> ExecuteAsync(Expression expression, Type elementType)
    {
        // Извлекаем контекст из выражения
        if (expression is ConstantExpression constantExpr && constantExpr.Value != null)
        {
            // Определяем тип операции по elementType
            if (elementType == typeof(int))
            {
                return await ExecuteCountAsyncGeneric(constantExpr.Value);
            }
            else if (elementType.IsGenericType)
            {
                var genericType = elementType.GetGenericTypeDefinition();
                if (genericType == typeof(List<>))
                {
                    // Определяем тип результата (RedbObject или TreeRedbObject)
                    var itemType = elementType.GetGenericArguments()[0];
                    if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(TreeRedbObject<>))
                    {
                        return await ExecuteTreeToListAsyncGeneric(constantExpr.Value);
                    }
                    else
                    {
                        return await ExecuteToListAsyncGeneric(constantExpr.Value);
                    }
                }
            }
        }

        throw new NotSupportedException($"Tree query expression type {expression.GetType().Name} with element type {elementType.Name} is not supported");
    }

    // ===== ВЫПОЛНЕНИЕ COUNT ЗАПРОСОВ =====
    
    private async Task<int> ExecuteCountAsyncGeneric(object contextObj)
    {
        // Используем рефлексию для вызова типизированного метода
        var contextType = contextObj.GetType();
        
        if (contextType.IsGenericType && contextType.GetGenericTypeDefinition() == typeof(TreeQueryContext<>))
        {
            // Древовидный запрос
            var propsType = contextType.GetGenericArguments()[0];
            var method = typeof(PostgresTreeQueryProvider).GetMethod(nameof(ExecuteTreeCountAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var genericMethod = method!.MakeGenericMethod(propsType);
            var task = (Task<int>)genericMethod.Invoke(this, new[] { contextObj })!;
            return await task;
        }
        else if (contextType.IsGenericType && contextType.GetGenericTypeDefinition() == typeof(QueryContext<>))
        {
            // Обычный запрос - делегируем базовому провайдеру
            var baseProvider = new PostgresQueryProvider(_context, _serializer, _logger);
            return await (Task<int>)typeof(PostgresQueryProvider)
                .GetMethod("ExecuteCountAsyncGeneric", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(baseProvider, new[] { contextObj })!;
        }
        
        throw new NotSupportedException($"Unsupported context type: {contextType.Name}");
    }

    // ===== ВЫПОЛНЕНИЕ TOLIST ЗАПРОСОВ =====
    
    private async Task<object> ExecuteToListAsyncGeneric(object contextObj)
    {
        var contextType = contextObj.GetType();
        
        if (contextType.IsGenericType && contextType.GetGenericTypeDefinition() == typeof(QueryContext<>))
        {
            // Обычный запрос - делегируем базовому провайдеру
            var baseProvider = new PostgresQueryProvider(_context, _serializer, _logger);
            return await (Task<object>)typeof(PostgresQueryProvider)
                .GetMethod("ExecuteToListAsyncGeneric", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(baseProvider, new[] { contextObj })!;
        }
        else if (contextType.IsGenericType && contextType.GetGenericTypeDefinition() == typeof(TreeQueryContext<>))
        {
            // Древовидный запрос - используем наш метод
            var propsType = contextType.GetGenericArguments()[0];
            var method = typeof(PostgresTreeQueryProvider).GetMethod(nameof(ExecuteTreeToListAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var genericMethod = method!.MakeGenericMethod(propsType);
            var task = (Task<object>)genericMethod.Invoke(this, new[] { contextObj })!;
            return await task;
        }
        
        throw new NotSupportedException($"Unsupported context type for ToList: {contextType.Name}");
    }
    
    private async Task<object> ExecuteTreeToListAsyncGeneric(object contextObj)
    {
        var contextType = contextObj.GetType();
        
        if (contextType.IsGenericType && contextType.GetGenericTypeDefinition() == typeof(TreeQueryContext<>))
        {
            // Древовидный запрос
            var propsType = contextType.GetGenericArguments()[0];
            var method = typeof(PostgresTreeQueryProvider).GetMethod(nameof(ExecuteTreeToListAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var genericMethod = method!.MakeGenericMethod(propsType);
            var task = (Task<object>)genericMethod.Invoke(this, new[] { contextObj })!;
            return await task;
        }
        
        throw new NotSupportedException($"Unsupported context type for TreeToList: {contextType.Name}");
    }

    // ===== ДРЕВОВИДНЫЕ МЕТОДЫ ВЫПОЛНЕНИЯ =====

    /// <summary>
    /// Выполнение COUNT для древовидного запроса через search_tree_objects_with_facets
    /// </summary>
    private async Task<int> ExecuteTreeCountAsync<TProps>(TreeQueryContext<TProps> context) where TProps : class, new()
    {
        try
        {
            // Строим JSON фильтр с древовидными операторами
            var facetFilter = BuildTreeFacetFilter(context);
            var filterJson = facetFilter?.RootElement.ToString() ?? "{}";

            _logger?.LogDebug("Выполнение древовидного COUNT запроса: SchemeId={SchemeId}, Filter={Filter}", 
                context.SchemeId, filterJson);

            // Вызываем search_tree_objects_with_facets с правильными параметрами для COUNT
            // Параметры: scheme_id, parent_id, facet_filters, limit, offset, order_by, max_depth, max_recursion_depth
            var sql = @"
                SELECT (result->>'total_count')::int as ""Value""
                FROM search_tree_objects_with_facets({0}, {1}, {2}::jsonb, 1, 0, NULL::jsonb, {3}, {4}) as result";

            int totalCount;
            
            // 🚀 ИСПРАВЛЕНИЕ: Если rootObjectId=null, используем обычный поиск во всей схеме
            if (!context.RootObjectId.HasValue)
            {
                // Поиск во ВСЕЙ схеме - используем search_objects_with_facets
                var allSchemeSql = @"
                    SELECT (result->>'total_count')::int as ""Value""
                    FROM search_objects_with_facets({0}, {1}::jsonb, 1, 0, NULL::jsonb, {2}) as result";
                
                totalCount = await _context.Database.SqlQueryRaw<int>(
                    allSchemeSql, 
                    context.SchemeId,
                    filterJson,
                    context.MaxRecursionDepth ?? 10  // max_recursion_depth
                ).FirstOrDefaultAsync();
            }
            else
            {
                // Поиск в поддереве - используем search_tree_objects_with_facets
                totalCount = await _context.Database.SqlQueryRaw<int>(
                sql, 
                context.SchemeId, 
                    context.RootObjectId.Value,  // parent_id - конкретный объект
                filterJson, 
                    context.MaxDepth ?? 50,
                    context.MaxRecursionDepth ?? 10  // max_recursion_depth
            ).FirstOrDefaultAsync();
            }

            _logger?.LogDebug("Древовидный COUNT результат: {Count}", totalCount);
            return totalCount;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка выполнения древовидного COUNT запроса");
            throw;
        }
    }

    /// <summary>
    /// Выполнение ToList для древовидного запроса через search_tree_objects_with_facets
    /// </summary>
    private async Task<object> ExecuteTreeToListAsync<TProps>(TreeQueryContext<TProps> context) where TProps : class, new()
    {
        try
        {
            // Строим JSON фильтры
            var facetFilter = BuildTreeFacetFilter(context);
            var orderBy = BuildOrderByFilter(context);

            var filterJson = facetFilter?.RootElement.ToString() ?? "{}";
            var orderByJson = orderBy?.RootElement.ToString() ?? "null";

            _logger?.LogDebug("Выполнение древовидного ToList запроса: SchemeId={SchemeId}, Limit={Limit}, Offset={Offset}", 
                context.SchemeId, context.Limit, context.Offset);

            _logger?.LogDebug("Tree ToList Query: SchemeId={SchemeId}, RootObjectId={RootObjectId}, Filters={Filters}", 
                context.SchemeId, context.RootObjectId, filterJson);
                
            // 🔍 ДЕТАЛЬНОЕ ЛОГИРОВАНИЕ ДЛЯ ОТЛАДКИ JSON ПРОБЛЕМЫ
            _logger?.LogInformation($"🔍 SQL PARAMETERS DEBUG:");
            _logger?.LogInformation($"   📊 SchemeId: {context.SchemeId} ({context.SchemeId.GetType().Name})");
            _logger?.LogInformation($"   📊 RootObjectId: {context.RootObjectId} ({context.RootObjectId?.GetType().Name})");
            _logger?.LogInformation($"   📊 FilterJson: {filterJson}");
            _logger?.LogInformation($"   📊 OrderByJson: {orderByJson}");
            _logger?.LogInformation($"   📊 Limit: {context.Limit?.ToString() ?? "NULL (все записи)"}");
            _logger?.LogInformation($"   📊 Offset: {context.Offset ?? 0}");
            _logger?.LogInformation($"   📊 MaxDepth: {context.MaxDepth ?? 50}");
            _logger?.LogInformation($"   📊 MaxRecursionDepth: {context.MaxRecursionDepth ?? 10}");

            // Tree DateTime фильтрация работает корректно после исправления конфликта $descendantsOf

            // Вызываем search_tree_objects_with_facets с правильными параметрами  
            // Параметры: scheme_id, parent_id, facet_filters, limit, offset, order_by, max_depth, max_recursion_depth
            var sql = @"
                SELECT result->>'objects' as ""Value""
                FROM search_tree_objects_with_facets({0}, {1}, {2}::jsonb, {3}, {4}, {5}::jsonb, {6}, {7}) as result";

            string objectsJson;
            
            // ✅ НОВАЯ ЛОГИКА: Обработка множественных родителей через ParentIds[]
            if (context.ParentIds != null && context.ParentIds.Length > 0)
            {
                // Множественные корни - выполняем отдельные запросы и объединяем результаты
                var allResults = new List<List<TreeRedbObject<TProps>>>();
                
                foreach (var parentId in context.ParentIds)
                {
                    var singleParentSql = @"
                        SELECT result->>'objects' as ""Value""
                        FROM search_tree_objects_with_facets({0}, {1}, {2}::jsonb, {3}, {4}, {5}::jsonb, {6}, {7}) as result";
                    
                    var singleObjectsJson = await _context.Database.SqlQueryRaw<string>(
                        singleParentSql,
                        context.SchemeId,
                        parentId,                    // parent_id - текущий родитель
                        filterJson,
                        context.Limit ?? int.MaxValue,  // ✅ NULL → int.MaxValue (фактически без лимита)  
                        0,                          // offset_count = 0 для каждого запроса
                        orderByJson,                // order_by
                        context.MaxDepth ?? 50,     // max_depth
                        context.MaxRecursionDepth ?? 10  // max_recursion_depth
                    ).FirstOrDefaultAsync();
                    
                    if (!string.IsNullOrEmpty(singleObjectsJson))
                    {
                        var singleResults = DeserializeTreeObjects<TProps>(singleObjectsJson);
                        allResults.Add(singleResults);
                    }
                }
                
                // Объединяем результаты всех запросов
                var combinedResults = allResults.SelectMany(r => r).ToList();
                
                // Применяем общие лимиты и сортировку
                if (context.Offset.HasValue && context.Offset.Value > 0)
                {
                    combinedResults = combinedResults.Skip(context.Offset.Value).ToList();
                }
                
                if (context.Limit.HasValue)
                {
                    combinedResults = combinedResults.Take(context.Limit.Value).ToList();
                }
                
                _logger?.LogDebug("Множественные родители: найдено {Count} объектов из {ParentCount} родителей", 
                    combinedResults.Count, context.ParentIds.Length);
                
                return (object)combinedResults;
            }
            // 🚀 ИСПРАВЛЕНИЕ: Если rootObjectId=null, используем обычный поиск во всей схеме
            else if (!context.RootObjectId.HasValue)
            {
                // Поиск во ВСЕЙ схеме - используем search_objects_with_facets (без древовидных ограничений)
                var allSchemeSql = @"
                    SELECT result->>'objects' as ""Value""
                    FROM search_objects_with_facets({0}, {1}::jsonb, {2}, {3}, {4}::jsonb, {5}) as result";
                
                objectsJson = await _context.Database.SqlQueryRaw<string>(
                    allSchemeSql,
                    context.SchemeId,
                    filterJson,
                    context.Limit ?? 100,       // limit_count
                    context.Offset ?? 0,        // offset_count
                    orderByJson,                // order_by
                    context.MaxRecursionDepth ?? 10  // max_recursion_depth
                ).FirstOrDefaultAsync();
            }
            else
            {
                // Поиск в поддереве - используем search_tree_objects_with_facets
                objectsJson = await _context.Database.SqlQueryRaw<string>(
                sql,
                context.SchemeId,
                    context.RootObjectId.Value,  // parent_id - конкретный объект
                filterJson,
                    context.Limit ?? 100,       // limit_count
                    context.Offset ?? 0,        // offset_count
                    orderByJson,                // order_by
                    context.MaxDepth ?? 50,     // max_depth
                    context.MaxRecursionDepth ?? 10  // max_recursion_depth
            ).FirstOrDefaultAsync();
            }

            // Десериализация результата в древовидные объекты
            var result = DeserializeTreeObjects<TProps>(objectsJson);
            
            _logger?.LogDebug("Древовидный ToList результат: {Count} объектов", result.Count);
            return (object)result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка выполнения древовидного ToList запроса");
            throw;
        }
    }

    // ===== ПОСТРОЕНИЕ ФИЛЬТРОВ =====

    /// <summary>
    /// Построение JSON фасет-фильтра с поддержкой древовидных операторов
    /// </summary>
    private JsonDocument? BuildTreeFacetFilter<TProps>(TreeQueryContext<TProps> context) where TProps : class, new()
    {
        var filters = new Dictionary<string, object>();

        // 1. 🚀 МОЩНАЯ СИСТЕМА ФИЛЬТРОВ (используем PostgresFacetFilterBuilder)
        if (context.Filter != null)
        {
            // Используем ту же мощную систему что и в обычном LINQ - ВСЕ 25+ операторов!
            var facetFiltersJson = _facetBuilder.BuildFacetFilters(context.Filter);
            
            // Парсим JSON обратно в Dictionary для объединения с tree фильтрами
            if (facetFiltersJson != "{}")
            {
                var facetFiltersDict = JsonSerializer.Deserialize<Dictionary<string, object>>(facetFiltersJson);
                if (facetFiltersDict != null)
                {
                    foreach (var kvp in facetFiltersDict)
            {
                filters[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        // 2. 🌳 Древовидные операторы (новая функциональность)
        if (context.TreeFilters != null && context.TreeFilters.Any())
        {
            foreach (var treeFilter in context.TreeFilters)
            {
                AddTreeFilterToJson(filters, treeFilter);
            }
        }

        // 3. 🌳 УБРАНО: НЕ добавляем $descendantsOf если уже есть rootObjectId
        // Причина: search_tree_objects_with_facets УЖЕ ограничивает по parent_id
        // Дополнительный $descendantsOf создает КОНФЛИКТ и возвращает 0 результатов!
        // if (context.RootObjectId.HasValue) - УДАЛЕНО!

        // Возвращаем JSON документ если есть фильтры
        if (!filters.Any()) return null;
        
        var jsonString = JsonSerializer.Serialize(filters, new JsonSerializerOptions { WriteIndented = false });
        return JsonDocument.Parse(jsonString);
    }

    /// <summary>
    /// Добавление древовидного фильтра в JSON словарь
    /// </summary>
    private void AddTreeFilterToJson(Dictionary<string, object> filters, TreeFilter treeFilter)
    {
        switch (treeFilter.Operator)
        {
            case TreeFilterOperator.HasAncestor:
                // ✅ ИСПРАВЛЕНИЕ: Передаем FilterConditions как вложенный JSON объект 
                filters["$hasAncestor"] = treeFilter.FilterConditions ?? new Dictionary<string, object>();
                break;
            
            case TreeFilterOperator.HasDescendant:
                // ✅ ИСПРАВЛЕНИЕ: Передаем FilterConditions как вложенный JSON объект для потомков
                filters["$hasDescendant"] = treeFilter.FilterConditions ?? new Dictionary<string, object>();
                break;
            
            case TreeFilterOperator.Level:
                if (treeFilter.Value is int level)
                {
                    filters["$level"] = level;
                }
                else if (treeFilter.FilterConditions != null)
                {
                    filters["$level"] = treeFilter.FilterConditions;
                }
                break;
            
            case TreeFilterOperator.IsRoot:
                filters["$isRoot"] = true;
                break;
            
            case TreeFilterOperator.IsLeaf:
                filters["$isLeaf"] = true;
                break;

            case TreeFilterOperator.ChildrenOf:
                filters["$childrenOf"] = treeFilter.Value;
                break;

            case TreeFilterOperator.DescendantsOf:
                filters["$descendantsOf"] = new { 
                    ancestor_id = treeFilter.Value,
                    max_depth = treeFilter.MaxDepth 
                };
                break;

            default:
                throw new NotSupportedException($"Древовидный оператор {treeFilter.Operator} не поддерживается");
        }
    }



    /// <summary>
    /// Построение JSON для сортировки (аналогично базовому провайдеру)
    /// </summary>
    private JsonDocument? BuildOrderByFilter<TProps>(TreeQueryContext<TProps> context) where TProps : class, new()
    {
        if (context.Orderings == null || !context.Orderings.Any())
            return null;

        var orderByArray = context.Orderings.Select(ordering => new
        {
            field = ordering.Property.Name,
            direction = ordering.Direction == SortDirection.Descending ? "DESC" : "ASC"
        }).ToArray();

        var jsonString = JsonSerializer.Serialize(orderByArray);
        return JsonDocument.Parse(jsonString);
    }

    // 💀 ConvertFilterToFacets() УДАЛЕН! ЗАМЕНЕН НА PostgresFacetFilterBuilder!
    // Теперь Tree использует ТУ ЖЕ МОЩНУЮ СИСТЕМУ что и обычный LINQ - ВСЕ 25+ операторов!

    /// <summary>
    /// Десериализация JSON результата в список TreeRedbObject
    /// </summary>
    private List<TreeRedbObject<TProps>> DeserializeTreeObjects<TProps>(string? objectsJson) where TProps : class, new()
    {
        if (string.IsNullOrEmpty(objectsJson) || objectsJson == "null")
            return new List<TreeRedbObject<TProps>>();

        try
        {
            // Парсим JSON массив объектов
            var jsonArray = JsonSerializer.Deserialize<JsonElement[]>(objectsJson);
            var result = new List<TreeRedbObject<TProps>>();

            if (jsonArray == null) return result;

            foreach (var jsonElement in jsonArray)
            {
                try
                {
                    // Десериализуем как обычный RedbObject
                    var redbObj = _serializer.Deserialize<TProps>(jsonElement.GetRawText());
                    if (redbObj == null) continue;

                    // Конвертируем в TreeRedbObject
                    var treeObj = ConvertToTreeObject(redbObj);
                    result.Add(treeObj);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Ошибка десериализации древовидного объекта из JSON");
                    // Продолжаем обработку других объектов
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка десериализации JSON массива древовидных объектов");
            return new List<TreeRedbObject<TProps>>();
        }
    }

    /// <summary>
    /// Конвертация RedbObject в TreeRedbObject
    /// </summary>
    private TreeRedbObject<TProps> ConvertToTreeObject<TProps>(RedbObject<TProps> source) where TProps : class, new()
    {
        return new TreeRedbObject<TProps>
        {
            id = source.id,
            parent_id = source.parent_id,
            scheme_id = source.scheme_id,
            owner_id = source.owner_id,
            who_change_id = source.who_change_id,
            date_create = source.date_create,
            date_modify = source.date_modify,
            date_begin = source.date_begin,
            date_complete = source.date_complete,
            key = source.key,
            code_int = source.code_int,
            code_string = source.code_string,
            code_guid = source.code_guid,
            name = source.name,
            note = source.note,
            @bool = source.@bool,
            hash = source.hash,
            properties = source.properties
        };
    }
}

// ===== ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ =====

/// <summary>
/// Контекст древовидного запроса - расширение QueryContext с поддержкой древовидных параметров
/// </summary>
public class TreeQueryContext<TProps> : QueryContext<TProps> where TProps : class, new()
{
    public long? RootObjectId { get; set; }               // Ограничение поиска поддеревом
    public List<TreeFilter> TreeFilters { get; set; }     // Древовидные фильтры
    
    // ✅ ИСПРАВЛЕНИЕ: MaxDepth теперь наследуется от базового QueryContext

    public TreeQueryContext(long schemeId, long? userId, bool checkPermissions, long? rootObjectId, int? maxDepth) 
        : base(schemeId, userId, checkPermissions, null, maxDepth)  // ✅ Передаем maxDepth в базовый конструктор
    {
        RootObjectId = rootObjectId;
        TreeFilters = new List<TreeFilter>();
    }

    /// <summary>
    /// Создать копию древовидного контекста
    /// </summary>
    public new TreeQueryContext<TProps> Clone()
    {
        // ✅ ИСПРАВЛЕНИЕ: MaxDepth теперь передается через базовый конструктор
        var clone = new TreeQueryContext<TProps>(SchemeId, UserId, CheckPermissions, RootObjectId, MaxDepth)
        {
            ParentIds = ParentIds,      // ✅ СИНХРОНИЗАЦИЯ: копируем batch массив
            Filter = Filter,
            Orderings = new List<OrderingExpression>(Orderings),
            Limit = Limit,
            Offset = Offset,
            IsDistinct = IsDistinct,
            MaxRecursionDepth = MaxRecursionDepth,
            IsEmpty = IsEmpty           // ✅ ИСПРАВЛЕНИЕ: копируем IsEmpty флаг для TreeQueryContext
        };
        
        // Копируем древовидные фильтры
        clone.TreeFilters = new List<TreeFilter>(TreeFilters);
        return clone;
    }
}

/// <summary>
/// Древовидный фильтр - представление иерархического оператора
/// </summary>
public class TreeFilter
{
    public TreeFilterOperator Operator { get; set; }
    public object? Value { get; set; }
    public int? MaxDepth { get; set; }
    public Dictionary<string, object>? FilterConditions { get; set; }  // Для сложных фильтров типа $hasAncestor

    public TreeFilter(TreeFilterOperator op, object? value = null)
    {
        Operator = op;
        Value = value;
        FilterConditions = new Dictionary<string, object>();
    }
}

/// <summary>
/// Типы древовидных операторов (соответствуют SQL операторам из search_tree_objects_with_facets)
/// </summary>
public enum TreeFilterOperator
{
    HasAncestor,      // $hasAncestor - найти объекты с предком удовлетворяющим условию
    HasDescendant,    // $hasDescendant - найти объекты с потомком удовлетворяющим условию  
    Level,            // $level - фильтр по уровню в дереве
    IsRoot,           // $isRoot - только корневые объекты
    IsLeaf,           // $isLeaf - только листья
    ChildrenOf,       // $childrenOf - прямые дети объекта
    DescendantsOf     // $descendantsOf - все потомки объекта
}
