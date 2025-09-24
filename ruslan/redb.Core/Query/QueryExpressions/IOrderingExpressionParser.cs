using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace redb.Core.Query.QueryExpressions;

/// <summary>
/// Парсер для конвертации OrderBy-выражений в OrderingExpression
/// </summary>
public interface IOrderingExpressionParser
{
    /// <summary>
    /// Распарсить выражение сортировки
    /// </summary>
    OrderingExpression ParseOrdering<TProps, TKey>(Expression<Func<TProps, TKey>> keySelector, SortDirection direction) where TProps : class;
    
    /// <summary>
    /// Распарсить множественную сортировку
    /// </summary>
    IReadOnlyList<OrderingExpression> ParseMultipleOrderings<TProps>(IEnumerable<(LambdaExpression KeySelector, SortDirection Direction)> orderings) where TProps : class;
}
