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
        var sql = "SELECT search_objects_with_facets({0}, {1}::jsonb, 0, 0, {2}::jsonb, {3}) as result";

        _logger?.LogDebug("LINQ Count Query: SchemeId={SchemeId}, Filters={Filters}, OrderBy={OrderBy}", 
            context.SchemeId, facetFilters, orderByJson);

        var result = await _context.Database.SqlQueryRaw<SearchJsonResult>(sql, 
            context.SchemeId, facetFilters, orderByJson ?? "null", 
            context.MaxRecursionDepth ?? 10) // 🆕 max_recursion_depth (default 10)
            .FirstOrDefaultAsync();

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
        var sql = "SELECT search_objects_with_facets({0}, {1}::jsonb, {2}, {3}, {4}::jsonb, {5}) as result";

        _logger?.LogDebug("LINQ ToList Query: SchemeId={SchemeId}, Filters={Filters}, Limit={Limit}, Offset={Offset}, OrderBy={OrderBy}", 
            context.SchemeId, facetFilters, parameters.Limit?.ToString() ?? "NULL (все записи)", parameters.Offset ?? 0, orderByJson);

        // Логирование SQL запроса для отладки
        _logger?.LogDebug("LINQ SQL Query: {SQL}", sql);
        _logger?.LogDebug("LINQ SQL Params: SchemeId={SchemeId}, Filters={Filters}, Limit={Limit}, Offset={Offset}", 
            context.SchemeId, facetFilters, parameters.Limit?.ToString() ?? "NULL (все записи)", parameters.Offset ?? 0);

                var result = await _context.Database.SqlQueryRaw<SearchJsonResult>(sql, 
            context.SchemeId, 
            facetFilters, 
            parameters.Limit ?? int.MaxValue,  // ✅ NULL → int.MaxValue (фактически без лимита)
            parameters.Offset ?? 0,
            orderByJson ?? "null",
            context.MaxRecursionDepth ?? 10) // 🆕 max_recursion_depth (default 10)
            .FirstOrDefaultAsync();

        if (result?.result != null)
        {
            _logger?.LogDebug("🔍 SQL ОТВЕТ: Получен JSON длиной {Length} символов", result.result.Length);
            _logger?.LogDebug("🔍 SQL JSON: {JsonContent}", result.result);
            
            var jsonDoc = System.Text.Json.JsonDocument.Parse(result.result);
            if (jsonDoc.RootElement.TryGetProperty("objects", out var objectsElement))
            {
                var objectsJson = objectsElement.GetRawText();
                _logger?.LogDebug("🔍 OBJECTS JSON: {ObjectsJson}", objectsJson);
                
                var objects = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(objectsJson);
                
                _logger?.LogDebug("📊 SQL РЕЗУЛЬТАТ: {Count} объектов получено из базы данных", objects?.Length ?? 0);
                
                // Материализация результатов из JSON объектов
                return await MaterializeResultsFromJson<TProps>(objects, context);
            }
        }
        else
        {
            _logger?.LogWarning("⚠️ SQL РЕЗУЛЬТАТ ПУСТОЙ: result?.result == null");
        }

        _logger?.LogDebug("LINQ ToList Result: No objects returned, returning empty list");
        return new List<RedbObject<TProps>>();
    }

    private async Task<List<RedbObject<TProps>>> MaterializeResultsFromJson<TProps>(System.Text.Json.JsonElement[] objects, QueryContext<TProps> context) 
        where TProps : class, new()
    {
        _logger?.LogDebug("🔍 МАТЕРИАЛИЗАЦИЯ: Получено {Count} JSON объектов для десериализации", objects?.Length ?? 0);
        
        var materializedResults = new List<RedbObject<TProps>>();
        var successCount = 0;
        var errorCount = 0;

        if (objects == null || objects.Length == 0)
        {
            _logger?.LogDebug("⚠️ МАТЕРИАЛИЗАЦИЯ: JSON массив пустой или null");
            return materializedResults;
        }

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
                successCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                // ✅ КРИТИЧНОЕ ИСПРАВЛЕНИЕ: ЛОГИРУЕМ ошибки десериализации!
                _logger?.LogError(ex, "❌ ОШИБКА ДЕСЕРИАЛИЗАЦИИ объекта #{Index}: {ObjectJson}", errorCount, objElement.GetRawText());
                // Продолжаем обработку других объектов
            }
        }

        _logger?.LogDebug("📊 МАТЕРИАЛИЗАЦИЯ ЗАВЕРШЕНА: Успешно={Success}, Ошибок={Errors}, Итого объектов={Total}", 
            successCount, errorCount, materializedResults.Count);

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
