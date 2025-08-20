using System;
using System.Linq.Expressions;

namespace redb.Core.Query.QueryExpressions;

/// <summary>
/// Парсер для конвертации Where-выражений в FilterExpression
/// </summary>
public interface IFilterExpressionParser
{
    /// <summary>
    /// Распарсить лямбда-выражение фильтрации
    /// </summary>
    FilterExpression ParseFilter<TProps>(Expression<Func<TProps, bool>> predicate) where TProps : class;
}
