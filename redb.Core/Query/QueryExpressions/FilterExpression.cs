using System.Collections.Generic;

namespace redb.Core.Query.QueryExpressions;

/// <summary>
/// Базовый класс для выражений фильтрации
/// </summary>
public abstract record FilterExpression;

/// <summary>
/// Выражение сравнения (property operator value)
/// </summary>
public record ComparisonExpression(
    PropertyInfo Property,
    ComparisonOperator Operator,
    object? Value
) : FilterExpression;

/// <summary>
/// Логическое выражение (AND, OR, NOT)
/// </summary>
public record LogicalExpression(
    LogicalOperator Operator,
    IReadOnlyList<FilterExpression> Operands
) : FilterExpression;

/// <summary>
/// Выражение для проверки на null
/// </summary>
public record NullCheckExpression(
    PropertyInfo Property,
    bool IsNull
) : FilterExpression;

/// <summary>
/// Выражение для проверки вхождения в список
/// </summary>
public record InExpression(
    PropertyInfo Property,
    IReadOnlyList<object> Values
) : FilterExpression;

/// <summary>
/// Информация о сортировке
/// </summary>
public record OrderingExpression(
    PropertyInfo Property,
    SortDirection Direction
);
