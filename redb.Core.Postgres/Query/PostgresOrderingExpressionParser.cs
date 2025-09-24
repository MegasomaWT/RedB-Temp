using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using redb.Core.Query.QueryExpressions;

namespace redb.Core.Postgres.Query;

/// <summary>
/// Парсер выражений сортировки для PostgreSQL реализации
/// ОБНОВЛЕНО: Поддержка тернарных операторов для nullable полей
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

    /// <summary>
    /// 🎯 ЗАКАЗЧИКОВА СЕМАНТИКА: Извлечение свойства с поддержкой тернарных операторов для nullable полей
    /// Поддерживает: r.Auction != null ? r.Auction.Baskets : 0
    /// </summary>
    private redb.Core.Query.QueryExpressions.PropertyInfo ExtractProperty(Expression expression)
    {
        return expression switch
        {
            // 🚀 ТЕРНАРНЫЙ ОПЕРАТОР (r.Auction != null ? r.Auction.Baskets : 0)
            ConditionalExpression conditional => ExtractFromConditional(conditional),
            
            // 🆕 БИНАРНОЕ ВЫРАЖЕНИЕ (r.Auction != null, r.Field == value)
            BinaryExpression binary => ExtractFromBinaryExpression(binary),
            
            // 📝 ОБЫЧНОЕ СВОЙСТВО (r.Name) или ВЛОЖЕННОЕ (r.Auction.Baskets)
            MemberExpression member when member.Member is System.Reflection.PropertyInfo propInfo => 
                ExtractFromMemberExpression(member, propInfo),
                
            _ => throw new ArgumentException($"Expression must be a property access, conditional, or binary comparison, got {expression.GetType().Name}")
        };
    }

    /// <summary>
    /// 🆕 ИЗВЛЕЧЕНИЕ свойства из бинарного выражения (r.Auction != null, r.Field == value)
    /// Поддерживает сортировку по nullable полям и полям с фиксированными значениями
    /// </summary>
    private redb.Core.Query.QueryExpressions.PropertyInfo ExtractFromBinaryExpression(BinaryExpression binary)
    {
        // Для сортировки в BinaryExpression нас интересует только свойство, не операция сравнения
        // Примеры:
        // r.Auction != null → сортируем по "Auction" 
        // r.Status == "Active" → сортируем по "Status"
        
        // Ищем MemberExpression в левой или правой части
        if (binary.Left is MemberExpression leftMember && leftMember.Member is System.Reflection.PropertyInfo leftProp)
        {
            return ExtractFromMemberExpression(leftMember, leftProp);
        }
        
        if (binary.Right is MemberExpression rightMember && rightMember.Member is System.Reflection.PropertyInfo rightProp)
        {
            return ExtractFromMemberExpression(rightMember, rightProp);
        }
        
        throw new ArgumentException($"Binary expression must have at least one property access side. Got: {binary.Left.GetType().Name} {binary.NodeType} {binary.Right.GetType().Name}");
    }

    /// <summary>
    /// Извлечение свойства из тернарного оператора (r.Auction != null ? r.Auction.Baskets : 0)
    /// </summary>
    private redb.Core.Query.QueryExpressions.PropertyInfo ExtractFromConditional(ConditionalExpression conditional)
    {
        // 🎯 ЗАКАЗЧИКОВА ЛОГИКА: В тернарном операторе главное поле находится в True-ветке
        // r.Auction != null ? r.Auction.Baskets : 0
        //                     ^^^^^^^^^^^^^^^^^ - это наше поле для сортировки
        
        if (conditional.IfTrue is MemberExpression trueMember && 
            trueMember.Member is System.Reflection.PropertyInfo truePropInfo)
        {
            return ExtractFromMemberExpression(trueMember, truePropInfo);
        }
        
        // Если True-ветка не является свойством, пробуем извлечь из условия
        // Это может быть случай: someCondition ? 0 : r.Field
        if (conditional.IfFalse is MemberExpression falseMember && 
            falseMember.Member is System.Reflection.PropertyInfo falsePropInfo)
        {
            return ExtractFromMemberExpression(falseMember, falsePropInfo);
        }
        
        throw new ArgumentException("Conditional expression must have at least one property access branch");
    }

    /// <summary>
    /// Извлечение свойства с поддержкой вложенных полей (r.Auction.Baskets → "Auction.Baskets")
    /// </summary>
    private redb.Core.Query.QueryExpressions.PropertyInfo ExtractFromMemberExpression(MemberExpression member, System.Reflection.PropertyInfo propInfo)
    {
        // 🚀 ПОДДЕРЖКА ВЛОЖЕННЫХ ПОЛЕЙ для nullable объектов (аналогично FilterExpressionParser)
        var fullPath = BuildPropertyPath(member);
        return new redb.Core.Query.QueryExpressions.PropertyInfo(fullPath, propInfo.PropertyType);
    }

    /// <summary>
    /// Построение полного пути свойства для nullable полей (Auction.Baskets)
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
        
        // Для случая r.Auction.Baskets получаем "Auction.Baskets"
        return string.Join(".", pathParts);
    }
}
