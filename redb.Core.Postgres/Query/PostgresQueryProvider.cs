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
/// PostgreSQL провайдер для выполнения LINQ-запросов через search_objects_with_facets
/// </summary>
public class PostgresQueryProvider : IRedbQueryProvider
{
    private readonly RedbContext _context;
    private readonly IRedbObjectSerializer _serializer;
    private readonly PostgresFilterExpressionParser _filterParser;
    private readonly PostgresOrderingExpressionParser _orderingParser;
    private readonly PostgresFacetFilterBuilder _facetBuilder;
    private readonly ILogger? _logger;

    public PostgresQueryProvider(
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

    public IRedbQueryable<TProps> CreateQuery<TProps>(long schemeId, long? userId = null, bool checkPermissions = false) 
        where TProps : class, new()
    {
        var context = new QueryContext<TProps>(schemeId, userId, checkPermissions);
        return new RedbQueryable<TProps>(this, context, _filterParser, _orderingParser);
    }

    /// <summary>
    /// Создать новый запрос для дочерних объектов указанной схемы
    /// </summary>
    public IRedbQueryable<TProps> CreateChildrenQuery<TProps>(long schemeId, long parentId, long? userId = null, bool checkPermissions = false) 
        where TProps : class, new()
    {
        var context = new QueryContext<TProps>(schemeId, userId, checkPermissions, parentId);
        return new RedbQueryable<TProps>(this, context, _filterParser, _orderingParser);
    }

    /// <summary>
    /// Создать новый запрос для всех потомков указанной схемы (рекурсивный поиск)
    /// </summary>
    public IRedbQueryable<TProps> CreateDescendantsQuery<TProps>(long schemeId, long parentId, int maxDepth, long? userId = null, bool checkPermissions = false) 
        where TProps : class, new()
    {
        var context = new QueryContext<TProps>(schemeId, userId, checkPermissions, parentId, maxDepth);
        return new RedbQueryable<TProps>(this, context, _filterParser, _orderingParser);
    }

    public async Task<object> ExecuteAsync(Expression expression, Type elementType)
    {
        // Извлекаем QueryContext из выражения
        if (expression is ConstantExpression constantExpr && constantExpr.Value != null)
        {
            // Определяем тип операции по elementType
            if (elementType == typeof(int))
            {
                return await ExecuteCountAsyncGeneric(constantExpr.Value);
            }
            else if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return await ExecuteToListAsyncGeneric(constantExpr.Value);
            }
        }

        throw new NotSupportedException($"Expression type {expression.GetType().Name} with element type {elementType.Name} is not supported");
    }

    private async Task<int> ExecuteCountAsyncGeneric(object contextObj)
    {
        // Используем рефлексию для вызова генерик метода
        var contextType = contextObj.GetType();
        if (contextType.IsGenericType && contextType.GetGenericTypeDefinition() == typeof(QueryContext<>))
        {
            var propsType = contextType.GetGenericArguments()[0];
            var method = typeof(PostgresQueryProvider).GetMethod(nameof(ExecuteCountAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var genericMethod = method!.MakeGenericMethod(propsType);
            var task = (Task<int>)genericMethod.Invoke(this, new[] { contextObj })!;
            return await task;
        }
        
        throw new NotSupportedException($"Unsupported context type: {contextType.Name}");
    }

    private async Task<object> ExecuteToListAsyncGeneric(object contextObj)
    {
        // Используем рефлексию для вызова генерик метода
        var contextType = contextObj.GetType();
        if (contextType.IsGenericType && contextType.GetGenericTypeDefinition() == typeof(QueryContext<>))
        {
            var propsType = contextType.GetGenericArguments()[0];
            var method = typeof(PostgresQueryProvider).GetMethod(nameof(ExecuteToListAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var genericMethod = method!.MakeGenericMethod(propsType);
            var task = (Task<object>)genericMethod.Invoke(this, new[] { contextObj, propsType })!;
            return await task;
        }
        
        throw new NotSupportedException($"Unsupported context type: {contextType.Name}");
    }

    private async Task<int> ExecuteCountAsync<TProps>(QueryContext<TProps> context) where TProps : class, new()
    {
        var facetFilters = _facetBuilder.BuildFacetFilters(context.Filter);
        var orderByJson = BuildOrderByJson(context);
        
        // Выполняем поиск с лимитом 0 для получения только count
        SearchJsonResult result;
        
        if (context.MaxDepth.HasValue && context.ParentId.HasValue)
        {
            // Для QueryDescendantsAsync - передаем 8 параметров включая max_depth
            var sql = "SELECT search_objects_with_facets({0}, {1}::jsonb, 0, 0, {2}, {3}::jsonb, {4}, {5}) as result";

            _logger?.LogDebug("LINQ Count Query (Descendants): SchemeId={SchemeId}, Filters={Filters}, OrderBy={OrderBy}, ParentId={ParentId}, MaxDepth={MaxDepth}", 
                context.SchemeId, facetFilters, orderByJson, context.ParentId, context.MaxDepth);

            result = await _context.Database.SqlQueryRaw<SearchJsonResult>(sql, 
                context.SchemeId, facetFilters, context.IsDistinct, orderByJson ?? "null", context.ParentId.Value, context.MaxDepth.Value)
                .FirstOrDefaultAsync();
        }
        else if (context.ParentId.HasValue)
        {
            // Для QueryChildrenAsync - передаем 7 параметров с parent_id
            var sql = "SELECT search_objects_with_facets({0}, {1}::jsonb, 0, 0, {2}, {3}::jsonb, {4}) as result";

            _logger?.LogDebug("LINQ Count Query (Children): SchemeId={SchemeId}, Filters={Filters}, OrderBy={OrderBy}, ParentId={ParentId}", 
                context.SchemeId, facetFilters, orderByJson, context.ParentId);

            result = await _context.Database.SqlQueryRaw<SearchJsonResult>(sql, 
                context.SchemeId, facetFilters, context.IsDistinct, orderByJson ?? "null", context.ParentId.Value)
                .FirstOrDefaultAsync();
        }
        else
        {
            // Для обычного QueryAsync - передаем 6 параметров (базовая функция)
            var sql = "SELECT search_objects_with_facets({0}, {1}::jsonb, 0, 0, {2}, {3}::jsonb) as result";

            _logger?.LogDebug("LINQ Count Query (Basic): SchemeId={SchemeId}, Filters={Filters}, OrderBy={OrderBy}", 
                context.SchemeId, facetFilters, orderByJson);

            result = await _context.Database.SqlQueryRaw<SearchJsonResult>(sql, 
                context.SchemeId, facetFilters, context.IsDistinct, orderByJson ?? "null")
                .FirstOrDefaultAsync();
        }

                if (result?.result != null)
        {
            var jsonDoc = System.Text.Json.JsonDocument.Parse(result.result);
            if (jsonDoc.RootElement.TryGetProperty("total_count", out var totalCountElement))
            {
                var count = totalCountElement.GetInt32();
                _logger?.LogDebug("LINQ Count Result: {Count} objects found", count);
                return count;
            }
        }
        
        _logger?.LogDebug("LINQ Count Result: No result returned, count = 0");
        return 0;
    }

    private async Task<object> ExecuteToListAsync<TProps>(QueryContext<TProps> context, Type propsType) where TProps : class, new()
    {
        var facetFilters = _facetBuilder.BuildFacetFilters(context.Filter);
        var parameters = _facetBuilder.BuildQueryParameters(context.Limit, context.Offset);
        var orderByJson = BuildOrderByJson(context);

        // Строим SQL запрос - функция возвращает jsonb
        SearchJsonResult result;
        
        if (context.MaxDepth.HasValue && context.ParentId.HasValue)
        {
            // Для QueryDescendantsAsync - передаем 8 параметров включая max_depth
            var sql = "SELECT search_objects_with_facets({0}, {1}::jsonb, {2}, {3}, {4}, {5}::jsonb, {6}, {7}) as result";

            _logger?.LogDebug("LINQ ToList Query (Descendants): SchemeId={SchemeId}, Filters={Filters}, Limit={Limit}, Offset={Offset}, OrderBy={OrderBy}, ParentId={ParentId}, MaxDepth={MaxDepth}", 
                context.SchemeId, facetFilters, parameters.Limit ?? 100, parameters.Offset ?? 0, orderByJson, context.ParentId, context.MaxDepth);

            result = await _context.Database.SqlQueryRaw<SearchJsonResult>(sql, 
                context.SchemeId, 
                facetFilters, 
                parameters.Limit ?? 100, 
                parameters.Offset ?? 0,
                context.IsDistinct,
                orderByJson ?? "null",
                context.ParentId.Value,
                context.MaxDepth.Value)
                .FirstOrDefaultAsync();
        }
        else if (context.ParentId.HasValue)
        {
            // Для QueryChildrenAsync - передаем 7 параметров с parent_id
            var sql = "SELECT search_objects_with_facets({0}, {1}::jsonb, {2}, {3}, {4}, {5}::jsonb, {6}) as result";

            _logger?.LogDebug("LINQ ToList Query (Children): SchemeId={SchemeId}, Filters={Filters}, Limit={Limit}, Offset={Offset}, OrderBy={OrderBy}, ParentId={ParentId}", 
                context.SchemeId, facetFilters, parameters.Limit ?? 100, parameters.Offset ?? 0, orderByJson, context.ParentId);

            result = await _context.Database.SqlQueryRaw<SearchJsonResult>(sql, 
                context.SchemeId, 
                facetFilters, 
                parameters.Limit ?? 100, 
                parameters.Offset ?? 0,
                context.IsDistinct,
                orderByJson ?? "null",
                context.ParentId.Value)
                .FirstOrDefaultAsync();
        }
        else
        {
            // Для обычного QueryAsync - передаем 6 параметров (базовая функция)
            var sql = "SELECT search_objects_with_facets({0}, {1}::jsonb, {2}, {3}, {4}, {5}::jsonb) as result";

            _logger?.LogDebug("LINQ ToList Query (Basic): SchemeId={SchemeId}, Filters={Filters}, Limit={Limit}, Offset={Offset}, OrderBy={OrderBy}", 
                context.SchemeId, facetFilters, parameters.Limit ?? 100, parameters.Offset ?? 0, orderByJson);

            result = await _context.Database.SqlQueryRaw<SearchJsonResult>(sql, 
                context.SchemeId, 
                facetFilters, 
                parameters.Limit ?? 100, 
                parameters.Offset ?? 0,
                context.IsDistinct,
                orderByJson ?? "null")
                .FirstOrDefaultAsync();
        }

        if (result?.result != null)
        {
            var jsonDoc = System.Text.Json.JsonDocument.Parse(result.result);
            if (jsonDoc.RootElement.TryGetProperty("objects", out var objectsElement))
            {
                var objectsJson = objectsElement.GetRawText();
                var objects = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(objectsJson);
                
                _logger?.LogDebug("LINQ ToList Result: {Count} objects returned from database", objects?.Length ?? 0);
                
                // Материализация результатов из JSON объектов
                return await MaterializeResultsFromJson<TProps>(objects, context);
            }
        }

        _logger?.LogDebug("LINQ ToList Result: No objects returned, returning empty list");
        return new List<RedbObject<TProps>>();
    }

    private async Task<List<RedbObject<TProps>>> MaterializeResultsFromJson<TProps>(System.Text.Json.JsonElement[] objects, QueryContext<TProps> context) 
        where TProps : class, new()
    {
        var materializedResults = new List<RedbObject<TProps>>();

        foreach (var objElement in objects)
        {
            try
            {
                // Объекты уже в JSON формате от get_object_json
                var objectJson = objElement.GetRawText();
                
                // Фильтрация по правам доступа если необходимо
                if (context.CheckPermissions && context.UserId.HasValue)
                {
                    // Извлекаем ID объекта для проверки прав
                    if (objElement.TryGetProperty("id", out var idElement))
                    {
                        var objectId = idElement.GetInt64();
                        var hasPermission = await CheckUserPermission(objectId, context.UserId.Value);
                        if (!hasPermission)
                        {
                            continue; // Пропускаем объект без прав доступа
                        }
                    }
                }
                
                // Десериализуем JSON данные объекта
                var redbObject = _serializer.Deserialize<TProps>(objectJson);
                materializedResults.Add(redbObject);
            }
            catch (Exception ex)
            {
                // Логируем ошибку десериализации, но продолжаем обработку
                Console.WriteLine($"Error deserializing object: {ex.Message}");
            }
        }

        return materializedResults;
    }

    private async Task<bool> CheckUserPermission(long objectId, long userId)
    {
        var sql = "SELECT EXISTS(SELECT 1 FROM get_user_permissions_for_object({0}, {1}) WHERE can_select = true) as has_permission";
        var result = await _context.Database.SqlQueryRaw<PermissionCheckResult>(sql, objectId, userId)
            .FirstOrDefaultAsync();
        
        return result?.HasPermission ?? false;
    }

    /// <summary>
    /// Формирует JSON для параметра order_by на основе сортировок из контекста
    /// </summary>
    private string? BuildOrderByJson<TProps>(QueryContext<TProps> context) where TProps : class, new()
    {
        if (!context.Orderings.Any())
            return null;

        var orderItems = context.Orderings.Select(ordering => new
        {
            field = ordering.Property.Name,
            direction = ordering.Direction == SortDirection.Ascending ? "ASC" : "DESC"
        });

        return JsonSerializer.Serialize(orderItems);
    }

    /// <summary>
    /// Результат функции search_objects_with_facets (возвращает jsonb)
    /// </summary>
    private class SearchJsonResult
    {
        public string result { get; set; } = string.Empty; // Lowercase для PostgreSQL
    }

    /// <summary>
    /// Результат проверки прав доступа
    /// </summary>
    private class PermissionCheckResult
    {
        public bool HasPermission { get; set; }
    }
}
