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
        
        // Contains методы обрабатываются специально

        // String методы
        if (declaringType == typeof(string))
        {
            return methodName switch
            {
                "Contains" => VisitStringMethodWithComparison(method, ComparisonOperator.Contains, ComparisonOperator.ContainsIgnoreCase),
                "StartsWith" => VisitStringMethodWithComparison(method, ComparisonOperator.StartsWith, ComparisonOperator.StartsWithIgnoreCase),
                "EndsWith" => VisitStringMethodWithComparison(method, ComparisonOperator.EndsWith, ComparisonOperator.EndsWithIgnoreCase),
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

        // Коллекции (массивы, List<T>, ICollection<T>, etc.)
        if (methodName == "Contains" && method.Object != null)
        {
            var objectType = method.Object.Type;
            
            // 🚀 МАССИВЫ: string[], int[], etc.
            if (objectType.IsArray)
            {
                return VisitCollectionContains(method);
            }
            
            // 📋 GENERIC КОЛЛЕКЦИИ: List<T>, ICollection<T>, etc.
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

    /// <summary>
    /// 🎯 ЗАКАЗЧИКОВА СЕМАНТИКА: Обработка строковых методов с поддержкой StringComparison
    /// Поддерживает: r.Article.Contains(filter, StringComparison.OrdinalIgnoreCase)
    /// </summary>
    private FilterExpression VisitStringMethodWithComparison(MethodCallExpression method, ComparisonOperator caseSensitiveOp, ComparisonOperator ignoreCaseOp)
    {
        if (method.Object == null)
            throw new ArgumentException("String method must have an object instance");

        var property = ExtractProperty(method.Object);
        var value = EvaluateExpression(method.Arguments[0]);

        // 🚀 ПРОВЕРЯЕМ КОЛИЧЕСТВО АРГУМЕНТОВ
        if (method.Arguments.Count == 1)
        {
            // Обычная версия: Contains(value) - учитывает регистр по умолчанию
            return new ComparisonExpression(property, caseSensitiveOp, value);
        }
        else if (method.Arguments.Count == 2)
        {
            // 🎯 ЗАКАЗЧИКОВА ВЕРСИЯ: Contains(value, StringComparison)
            var comparisonArg = method.Arguments[1];
            var stringComparison = EvaluateStringComparison(comparisonArg);
            
            // Определяем оператор на основе StringComparison
            var finalOperator = IsIgnoreCaseComparison(stringComparison) ? ignoreCaseOp : caseSensitiveOp;
            
            return new ComparisonExpression(property, finalOperator, value);
        }
        else
        {
            throw new NotSupportedException($"String method with {method.Arguments.Count} arguments is not supported");
        }
    }

    /// <summary>
    /// Извлечение StringComparison из выражения
    /// </summary>
    private StringComparison EvaluateStringComparison(Expression comparisonExpression)
    {
        var value = EvaluateExpression(comparisonExpression);
        
        if (value is StringComparison comparison)
        {
            return comparison;
        }
        
        // Если не удалось извлечь, используем по умолчанию
        return StringComparison.Ordinal;
    }

    /// <summary>
    /// Проверка нужно ли игнорировать регистр
    /// </summary>
    private bool IsIgnoreCaseComparison(StringComparison comparison)
    {
        return comparison switch
        {
            StringComparison.CurrentCultureIgnoreCase => true,
            StringComparison.InvariantCultureIgnoreCase => true,
            StringComparison.OrdinalIgnoreCase => true,
            _ => false
        };
    }

    private FilterExpression VisitEnumerableContains(MethodCallExpression method)
    {
        // Enumerable.Contains(source, value)
        if (method.Arguments.Count != 2)
            throw new ArgumentException("Contains method must have exactly 2 arguments");

        var sourceExpression = method.Arguments[0];
        var valueExpression = method.Arguments[1];

        // 🔍 СЛУЧАЙ 1: source.Contains(property) - property IN source
        if (IsPropertyAccess(valueExpression))
        {
            var property = ExtractProperty(valueExpression);
            var values = EvaluateExpression(sourceExpression);

            if (values is System.Collections.IEnumerable enumerable)
            {
                var valuesList = enumerable.Cast<object>().ToList();
                return new InExpression(property, valuesList);
            }
        }
        // 🚀 СЛУЧАЙ 2: source.Contains(value) где source=property (МАССИВЫ!)
        else if (IsPropertyAccess(sourceExpression))
        {
            // Enumerable.Contains(p.Tags1, "senior") → p.Tags1 ArrayContains "senior"
            var arrayProperty = ExtractProperty(sourceExpression);
            var value = EvaluateExpression(valueExpression);
            
            // ArrayContains обнаружен и обрабатывается
            
            // 🎯 ЭТО МАССИВ - используем ArrayContains
            return new ComparisonExpression(arrayProperty, ComparisonOperator.ArrayContains, value);
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

    /// <summary>
    /// 🎯 ЗАКАЗЧИКОВА СЕМАНТИКА: Извлечение свойства с поддержкой nullable полей (r.Auction.Costs)
    /// </summary>
    private redb.Core.Query.QueryExpressions.PropertyInfo ExtractProperty(Expression expression)
    {
        if (expression is MemberExpression member && member.Member is System.Reflection.PropertyInfo propInfo)
        {
            // 🚀 ПОДДЕРЖКА ВЛОЖЕННЫХ ПОЛЕЙ для nullable объектов
            var fullPath = BuildPropertyPath(member);
            return new redb.Core.Query.QueryExpressions.PropertyInfo(fullPath, propInfo.PropertyType);
        }

        throw new ArgumentException($"Expression must be a property access, got {expression.GetType().Name}");
    }

    /// <summary>
    /// 🎯 НОВЫЙ МЕТОД: Построение полного пути свойства для nullable полей (Auction.Costs)
    /// </summary>
    private string BuildPropertyPath(MemberExpression memberExpression)
    {
        var pathParts = new List<string>();
        var current = memberExpression;

        // Обходим цепочку свойств снизу вверх
        while (current != null && current.Member is System.Reflection.PropertyInfo)
        {
            pathParts.Add(current.Member.Name);
            
            if (current.Expression is MemberExpression parentMember)
            {
                current = parentMember;
            }
            else
            {
                // Дошли до корня (параметра r)
                break;
            }
        }

        // Переворачиваем порядок (был снизу вверх, нужен сверху вниз)
        pathParts.Reverse();
        
        // Для случая r.Auction.Costs получаем "Auction.Costs"
        return string.Join(".", pathParts);
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
        // VisitCollectionContains для обработки collection.Contains(value)
        
        // collection.Contains(value)
        if (method.Arguments.Count != 1)
            throw new ArgumentException("Collection Contains method must have exactly 1 argument");

        var collectionExpression = method.Object!;
        var valueExpression = method.Arguments[0];
        
        // Анализ Collection.Contains() выражения

        // 🔍 СЛУЧАЙ 1: collection.Contains(property) - property IN collection
        if (IsPropertyAccess(valueExpression))
        {
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
        // 🚀 СЛУЧАЙ 2: property.Contains(value) - value IN property (МАССИВЫ!)
        else if (IsPropertyAccess(collectionExpression))
        {
            var arrayProperty = ExtractProperty(collectionExpression);
            var value = EvaluateExpression(valueExpression);
            
            // 🎯 ЭТО МАССИВ - используем ArrayContains
            return new ComparisonExpression(arrayProperty, ComparisonOperator.ArrayContains, value);
        }
        else
        {
            throw new NotSupportedException("Unsupported Contains expression structure");
        }
    }
}
