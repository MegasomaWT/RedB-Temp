using System.Linq.Expressions;

namespace redb.Core.Query.QueryExpressions;

/// <summary>
/// Интерфейс для обработки Expression Tree
/// </summary>
public interface IExpressionVisitor<out TResult>
{
    /// <summary>
    /// Обработать выражение и вернуть результат
    /// </summary>
    TResult Visit(Expression expression);
}

/// <summary>
/// Информация о поле в выражении
/// </summary>
public record PropertyInfo(string Name, Type Type);

/// <summary>
/// Операторы сравнения
/// </summary>
public enum ComparisonOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    StartsWith,
    EndsWith
}

/// <summary>
/// Логические операторы
/// </summary>
public enum LogicalOperator
{
    And,
    Or,
    Not
}

/// <summary>
/// Направление сортировки
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending
}
