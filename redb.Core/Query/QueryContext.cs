using System.Collections.Generic;
using redb.Core.Query.QueryExpressions;

namespace redb.Core.Query;

/// <summary>
/// Контекст запроса - содержит всю информацию о LINQ-запросе
/// </summary>
public class QueryContext<TProps> where TProps : class, new()
{
    public long SchemeId { get; init; }
    public long? UserId { get; init; }
    public bool CheckPermissions { get; init; }
    public long? ParentId { get; init; }
    public long[]? ParentIds { get; set; } // Для batch операций
    public int? MaxDepth { get; init; }
    
    public FilterExpression? Filter { get; set; }
    public List<OrderingExpression> Orderings { get; set; } = new();
    public int? Limit { get; set; }
    public int? Offset { get; set; }
    public bool IsDistinct { get; set; }
    
    public QueryContext(long schemeId, long? userId = null, bool checkPermissions = false, long? parentId = null, int? maxDepth = null)
    {
        SchemeId = schemeId;
        UserId = userId;
        CheckPermissions = checkPermissions;
        ParentId = parentId;
        MaxDepth = maxDepth;
    }
    
    /// <summary>
    /// Создать копию контекста
    /// </summary>
    public QueryContext<TProps> Clone()
    {
        return new QueryContext<TProps>(SchemeId, UserId, CheckPermissions, ParentId, MaxDepth)
        {
            ParentIds = ParentIds, // Копируем batch массив
            Filter = Filter,
            Orderings = new List<OrderingExpression>(Orderings),
            Limit = Limit,
            Offset = Offset,
            IsDistinct = IsDistinct
        };
    }
}
