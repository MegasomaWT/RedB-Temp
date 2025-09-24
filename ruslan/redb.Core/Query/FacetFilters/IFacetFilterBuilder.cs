using System.Collections.Generic;
using redb.Core.Query.QueryExpressions;

namespace redb.Core.Query.FacetFilters;

/// <summary>
/// Построитель JSON фильтров для search_objects_with_facets
/// </summary>
public interface IFacetFilterBuilder
{
    /// <summary>
    /// Построить JSON для facet_filters из FilterExpression
    /// </summary>
    string BuildFacetFilters(FilterExpression? filter);
    
    /// <summary>
    /// Построить JSON для order из OrderingExpression
    /// </summary>
    string BuildOrderBy(IReadOnlyList<OrderingExpression> orderings);
    
    /// <summary>
    /// Построить параметры запроса (limit, offset)
    /// </summary>
    QueryParameters BuildQueryParameters(int? limit = null, int? offset = null);
}

/// <summary>
/// Параметры запроса
/// </summary>
public record QueryParameters(
    int? Limit = null,
    int? Offset = null
);
