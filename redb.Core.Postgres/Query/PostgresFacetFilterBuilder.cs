using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using redb.Core.Query.FacetFilters;
using redb.Core.Query.QueryExpressions;

namespace redb.Core.Postgres.Query;

/// <summary>
/// Построитель JSON фильтров для функции search_objects_with_facets
/// </summary>
public class PostgresFacetFilterBuilder : IFacetFilterBuilder
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger? _logger;

    public PostgresFacetFilterBuilder(ILogger? logger = null)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    public string BuildFacetFilters(FilterExpression? filter)
    {
        if (filter == null)
        {
            _logger?.LogDebug("LINQ Filter: No filter provided, returning empty filter: {{}}");
            return "{}";
        }

        var filterObject = BuildFilterObject(filter);
        var filterJson = JsonSerializer.Serialize(filterObject, _jsonOptions);
        
        _logger?.LogDebug("LINQ Filter Generated: {FilterJson}", filterJson);
        return filterJson;
    }

    public string BuildOrderBy(IReadOnlyList<OrderingExpression> orderings)
    {
        if (!orderings.Any())
        {
            _logger?.LogDebug("LINQ OrderBy: No ordering provided, returning empty array: []");
            return "[]";
        }

        var orderArray = orderings.Select(o => new Dictionary<string, string>
        {
            [o.Property.Name] = o.Direction == SortDirection.Ascending ? "asc" : "desc"
        }).ToArray();

        var orderJson = JsonSerializer.Serialize(orderArray, _jsonOptions);
        _logger?.LogDebug("LINQ OrderBy Generated: {OrderJson}", orderJson);
        return orderJson;
    }

    public QueryParameters BuildQueryParameters(int? limit = null, int? offset = null)
    {
        return new QueryParameters(limit, offset);
    }

    private object BuildFilterObject(FilterExpression filter)
    {
        return filter switch
        {
            ComparisonExpression comparison => BuildComparisonFilter(comparison),
            LogicalExpression logical => BuildLogicalFilter(logical),
            NullCheckExpression nullCheck => BuildNullCheckFilter(nullCheck),
            InExpression inExpr => BuildInFilter(inExpr),
            _ => throw new NotSupportedException($"Filter expression type {filter.GetType().Name} is not supported")
        };
    }

    private object BuildComparisonFilter(ComparisonExpression comparison)
    {
        var fieldName = comparison.Property.Name;
        var value = comparison.Value;

        return comparison.Operator switch
        {
            ComparisonOperator.Equal => new Dictionary<string, object> { [fieldName] = value! },
            ComparisonOperator.NotEqual => new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$ne"] = value } 
            },
            ComparisonOperator.GreaterThan => new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$gt"] = value } 
            },
            ComparisonOperator.GreaterThanOrEqual => new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$gte"] = value } 
            },
            ComparisonOperator.LessThan => new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$lt"] = value } 
            },
            ComparisonOperator.LessThanOrEqual => new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$lte"] = value } 
            },
            ComparisonOperator.Contains => new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$contains"] = value } 
            },
            ComparisonOperator.StartsWith => new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$startsWith"] = value } 
            },
            ComparisonOperator.EndsWith => new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$endsWith"] = value } 
            },
            _ => throw new NotSupportedException($"Comparison operator {comparison.Operator} is not supported")
        };
    }

    private object BuildLogicalFilter(LogicalExpression logical)
    {
        return logical.Operator switch
        {
            LogicalOperator.And => BuildAndFilter(logical.Operands),
            LogicalOperator.Or => new Dictionary<string, object> 
            { 
                ["$or"] = logical.Operands.Select(BuildFilterObject).ToArray() 
            },
            LogicalOperator.Not => new Dictionary<string, object> 
            { 
                ["$not"] = BuildFilterObject(logical.Operands.First()) 
            },
            _ => throw new NotSupportedException($"Logical operator {logical.Operator} is not supported")
        };
    }

    private object BuildAndFilter(IReadOnlyList<FilterExpression> operands)
    {
        // Для AND мы можем объединить условия в один объект, если они не конфликтуют
        // В противном случае используем $and
        var result = new Dictionary<string, object>();

        foreach (var operand in operands)
        {
            var filterObj = BuildFilterObject(operand);
            
            if (filterObj is Dictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    if (result.ContainsKey(kvp.Key))
                    {
                        // Конфликт ключей - используем $and
                        return new Dictionary<string, object>
                        {
                            ["$and"] = operands.Select(BuildFilterObject).ToArray()
                        };
                    }
                    result[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                // Сложный объект - используем $and
                return new Dictionary<string, object>
                {
                    ["$and"] = operands.Select(BuildFilterObject).ToArray()
                };
            }
        }

        return result;
    }

    private object BuildNullCheckFilter(NullCheckExpression nullCheck)
    {
        var fieldName = nullCheck.Property.Name;
        
        if (nullCheck.IsNull)
        {
            return new Dictionary<string, object?> { [fieldName] = null };
        }
        else
        {
            return new Dictionary<string, object> 
            { 
                [fieldName] = new Dictionary<string, object?> { ["$ne"] = null } 
            };
        }
    }

    private object BuildInFilter(InExpression inExpr)
    {
        var fieldName = inExpr.Property.Name;
        
        return new Dictionary<string, object> 
        { 
            [fieldName] = new Dictionary<string, object> { ["$in"] = inExpr.Values.ToArray() } 
        };
    }
}
