using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using redb.Core.Query.QueryExpressions;

namespace redb.Core.Postgres.Query;

/// <summary>
/// Парсер выражений сортировки для PostgreSQL реализации
/// </summary>
public class PostgresOrderingExpressionParser : IOrderingExpressionParser
{
    public OrderingExpression ParseOrdering<TProps, TKey>(Expression<Func<TProps, TKey>> keySelector, SortDirection direction) where TProps : class
    {
        var property = ExtractProperty(keySelector.Body);
        return new OrderingExpression(property, direction);
    }

    public IReadOnlyList<OrderingExpression> ParseMultipleOrderings<TProps>(IEnumerable<(LambdaExpression KeySelector, SortDirection Direction)> orderings) where TProps : class
    {
        var result = new List<OrderingExpression>();

        foreach (var (keySelector, direction) in orderings)
        {
            var property = ExtractProperty(keySelector.Body);
            result.Add(new OrderingExpression(property, direction));
        }

        return result;
    }

    private redb.Core.Query.QueryExpressions.PropertyInfo ExtractProperty(Expression expression)
    {
        if (expression is MemberExpression member && member.Member is System.Reflection.PropertyInfo propInfo)
        {
            return new redb.Core.Query.QueryExpressions.PropertyInfo(propInfo.Name, propInfo.PropertyType);
        }

        throw new ArgumentException($"Expression must be a property access, got {expression.GetType().Name}");
    }
}
