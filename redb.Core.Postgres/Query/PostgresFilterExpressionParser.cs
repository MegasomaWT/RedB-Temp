using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using redb.Core.Query.QueryExpressions;

namespace redb.Core.Postgres.Query;

/// <summary>
/// Парсер LINQ выражений для PostgreSQL реализации
/// Конвертирует Where условия в FilterExpression
/// </summary>
public class PostgresFilterExpressionParser : IFilterExpressionParser
{
    public FilterExpression ParseFilter<TProps>(Expression<Func<TProps, bool>> predicate) where TProps : class
    {
        return VisitExpression(predicate.Body);
    }

    private FilterExpression VisitExpression(Expression expression)
    {
        return expression switch
        {
            BinaryExpression binary => VisitBinaryExpression(binary),
            UnaryExpression unary => VisitUnaryExpression(unary),
            MethodCallExpression method => VisitMethodCallExpression(method),
            ConstantExpression constant when constant.Type == typeof(bool) => 
                VisitConstantBooleanExpression(constant),
            MemberExpression member when member.Type == typeof(bool) =>
                VisitBooleanMemberExpression(member),
            _ => throw new NotSupportedException($"Expression type {expression.NodeType} is not supported")
        };
    }

    private FilterExpression VisitBinaryExpression(BinaryExpression binary)
    {
        switch (binary.NodeType)
        {
            case ExpressionType.AndAlso:
                return new LogicalExpression(
                    LogicalOperator.And,
                    new[] { VisitExpression(binary.Left), VisitExpression(binary.Right) }
                );

            case ExpressionType.OrElse:
                return new LogicalExpression(
                    LogicalOperator.Or,
                    new[] { VisitExpression(binary.Left), VisitExpression(binary.Right) }
                );

            case ExpressionType.Equal:
                return VisitComparisonExpression(binary, ComparisonOperator.Equal);

            case ExpressionType.NotEqual:
                return VisitComparisonExpression(binary, ComparisonOperator.NotEqual);

            case ExpressionType.GreaterThan:
                return VisitComparisonExpression(binary, ComparisonOperator.GreaterThan);

            case ExpressionType.GreaterThanOrEqual:
                return VisitComparisonExpression(binary, ComparisonOperator.GreaterThanOrEqual);

            case ExpressionType.LessThan:
                return VisitComparisonExpression(binary, ComparisonOperator.LessThan);

            case ExpressionType.LessThanOrEqual:
                return VisitComparisonExpression(binary, ComparisonOperator.LessThanOrEqual);

            default:
                throw new NotSupportedException($"Binary operator {binary.NodeType} is not supported");
        }
    }

    private FilterExpression VisitComparisonExpression(BinaryExpression binary, ComparisonOperator op)
    {
        var (property, value) = ExtractPropertyAndValue(binary);

        // Проверка на null
        if (value == null)
        {
            return new NullCheckExpression(property, op == ComparisonOperator.Equal);
        }

        return new ComparisonExpression(property, op, value);
    }

    private FilterExpression VisitUnaryExpression(UnaryExpression unary)
    {
        switch (unary.NodeType)
        {
            case ExpressionType.Not:
                // Проверяем, это отрицание boolean свойства (!p.IsActive) или что-то другое
                if (unary.Operand is MemberExpression member && member.Type == typeof(bool))
                {
                    // !p.IsActive интерпретируется как p.IsActive == false
                    if (member.Member is System.Reflection.PropertyInfo propInfo && propInfo.PropertyType == typeof(bool))
                    {
                        var property = new redb.Core.Query.QueryExpressions.PropertyInfo(propInfo.Name, propInfo.PropertyType);
                        return new ComparisonExpression(property, ComparisonOperator.Equal, false);
                    }
                }
                
                // Общий случай отрицания
                var operand = VisitExpression(unary.Operand);
                return new LogicalExpression(LogicalOperator.Not, new[] { operand });

            default:
                throw new NotSupportedException($"Unary operator {unary.NodeType} is not supported");
        }
    }

    private FilterExpression VisitMethodCallExpression(MethodCallExpression method)
    {
        var methodName = method.Method.Name;
        var declaringType = method.Method.DeclaringType;

        // String методы
        if (declaringType == typeof(string))
        {
            return methodName switch
            {
                "Contains" => VisitStringMethod(method, ComparisonOperator.Contains),
                "StartsWith" => VisitStringMethod(method, ComparisonOperator.StartsWith),
                "EndsWith" => VisitStringMethod(method, ComparisonOperator.EndsWith),
                _ => throw new NotSupportedException($"String method {methodName} is not supported")
            };
        }

        // Enumerable методы
        if (declaringType == typeof(Enumerable))
        {
            return methodName switch
            {
                "Contains" => VisitEnumerableContains(method),
                _ => throw new NotSupportedException($"Enumerable method {methodName} is not supported")
            };
        }

        // Коллекции (List<T>, ICollection<T>, etc.)
        if (methodName == "Contains" && method.Object != null)
        {
            var objectType = method.Object.Type;
            if (objectType.IsGenericType)
            {
                var genericDef = objectType.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>) || 
                    genericDef == typeof(IList<>) ||
                    genericDef == typeof(ICollection<>) ||
                    genericDef == typeof(IEnumerable<>) ||
                    objectType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                {
                    return VisitCollectionContains(method);
                }
            }
        }

        throw new NotSupportedException($"Method {declaringType?.Name}.{methodName} is not supported");
    }

    private FilterExpression VisitStringMethod(MethodCallExpression method, ComparisonOperator op)
    {
        if (method.Object == null)
            throw new ArgumentException("String method must have an object instance");

        var property = ExtractProperty(method.Object);
        var value = EvaluateExpression(method.Arguments[0]);

        return new ComparisonExpression(property, op, value);
    }

    private FilterExpression VisitEnumerableContains(MethodCallExpression method)
    {
        // Enumerable.Contains(source, value)
        if (method.Arguments.Count != 2)
            throw new ArgumentException("Contains method must have exactly 2 arguments");

        var sourceExpression = method.Arguments[0];
        var valueExpression = method.Arguments[1];

        // Пытаемся понять структуру: source.Contains(property) или source.Contains(value)
        if (IsPropertyAccess(valueExpression))
        {
            // source.Contains(property) - property IN source
            var property = ExtractProperty(valueExpression);
            var values = EvaluateExpression(sourceExpression);

            if (values is System.Collections.IEnumerable enumerable)
            {
                var valuesList = enumerable.Cast<object>().ToList();
                return new InExpression(property, valuesList);
            }
        }

        throw new NotSupportedException("Unsupported Contains expression structure");
    }

    private FilterExpression VisitConstantBooleanExpression(ConstantExpression constant)
    {
        // Константное boolean выражение (true/false)
        var value = (bool)constant.Value!;
        
        // Создаем фиктивное условие, которое всегда истинно или ложно
        // Это может быть полезно для условий типа Where(x => true) или Where(x => false)
        var dummyProperty = new redb.Core.Query.QueryExpressions.PropertyInfo("__constant", typeof(bool));
        return new ComparisonExpression(dummyProperty, ComparisonOperator.Equal, value);
    }

    private FilterExpression VisitBooleanMemberExpression(MemberExpression member)
    {
        // Прямое обращение к boolean свойству: p.IsActive
        // Интерпретируется как p.IsActive == true
        if (member.Member is System.Reflection.PropertyInfo propInfo && propInfo.PropertyType == typeof(bool))
        {
            var property = new redb.Core.Query.QueryExpressions.PropertyInfo(propInfo.Name, propInfo.PropertyType);
            return new ComparisonExpression(property, ComparisonOperator.Equal, true);
        }

        throw new ArgumentException($"Boolean member expression must be a boolean property, got {member.Member?.GetType().Name}");
    }

    private (redb.Core.Query.QueryExpressions.PropertyInfo Property, object? Value) ExtractPropertyAndValue(BinaryExpression binary)
    {
        // Определяем какая сторона - свойство, а какая - значение
        if (IsPropertyAccess(binary.Left))
        {
            var property = ExtractProperty(binary.Left);
            var value = EvaluateExpression(binary.Right);
            return (property, value);
        }
        else if (IsPropertyAccess(binary.Right))
        {
            var property = ExtractProperty(binary.Right);
            var value = EvaluateExpression(binary.Left);
            return (property, value);
        }
        else
        {
            throw new NotSupportedException("At least one side of comparison must be a property access");
        }
    }

    private bool IsPropertyAccess(Expression expression)
    {
        return expression is MemberExpression member && member.Member is System.Reflection.PropertyInfo;
    }

    private redb.Core.Query.QueryExpressions.PropertyInfo ExtractProperty(Expression expression)
    {
        if (expression is MemberExpression member && member.Member is System.Reflection.PropertyInfo propInfo)
        {
            return new redb.Core.Query.QueryExpressions.PropertyInfo(propInfo.Name, propInfo.PropertyType);
        }

        throw new ArgumentException($"Expression must be a property access, got {expression.GetType().Name}");
    }

    private object? EvaluateExpression(Expression expression)
    {
        // Компилируем и выполняем выражение для получения значения
        if (expression is ConstantExpression constant)
        {
            return constant.Value;
        }

        // Для более сложных выражений компилируем lambda
        var lambda = Expression.Lambda(expression);
        var compiled = lambda.Compile();
        return compiled.DynamicInvoke();
    }

    private FilterExpression VisitCollectionContains(MethodCallExpression method)
    {
        // collection.Contains(value)
        if (method.Arguments.Count != 1)
            throw new ArgumentException("Collection Contains method must have exactly 1 argument");

        var collectionExpression = method.Object!;
        var valueExpression = method.Arguments[0];

        // Проверяем, что аргумент - это свойство
        if (IsPropertyAccess(valueExpression))
        {
            // collection.Contains(property) - property IN collection
            var property = ExtractProperty(valueExpression);
            var values = EvaluateExpression(collectionExpression);

            if (values is System.Collections.IEnumerable enumerable)
            {
                var valuesList = enumerable.Cast<object>().ToList();
                return new InExpression(property, valuesList);
            }
            else
            {
                throw new ArgumentException("Collection expression must evaluate to IEnumerable");
            }
        }
        else
        {
            throw new NotSupportedException("Collection Contains argument must be a property access");
        }
    }
}
