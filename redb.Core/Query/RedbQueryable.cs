using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using redb.Core.Models.Entities;
using redb.Core.Query.QueryExpressions;

namespace redb.Core.Query;

/// <summary>
/// Базовая реализация IRedbQueryable
/// </summary>
public class RedbQueryable<TProps> : IRedbQueryable<TProps>, IOrderedRedbQueryable<TProps>
    where TProps : class, new()
{
    protected readonly IRedbQueryProvider _provider;
    protected readonly QueryContext<TProps> _context;
    protected readonly IFilterExpressionParser _filterParser;
    protected readonly IOrderingExpressionParser _orderingParser;

    public RedbQueryable(
        IRedbQueryProvider provider,
        QueryContext<TProps> context,
        IFilterExpressionParser filterParser,
        IOrderingExpressionParser orderingParser)
    {
        _provider = provider;
        _context = context;
        _filterParser = filterParser;
        _orderingParser = orderingParser;
    }

    public virtual IRedbQueryable<TProps> Where(Expression<Func<TProps, bool>> predicate)
    {
        var newContext = _context.Clone();
        var filterExpression = _filterParser.ParseFilter(predicate);
        
        // ✅ ИСПРАВЛЕНИЕ: Проверяем на пустой фильтр (Where(x => false))
        if (IsEmptyFilter(filterExpression))
        {
            newContext.IsEmpty = true;
        }
        
        // Если уже есть фильтр, объединяем через AND
        if (newContext.Filter != null)
        {
            newContext.Filter = new LogicalExpression(
                LogicalOperator.And,
                new[] { newContext.Filter, filterExpression }
            );
        }
        else
        {
            newContext.Filter = filterExpression;
        }

        return new RedbQueryable<TProps>(_provider, newContext, _filterParser, _orderingParser);
    }

    public virtual IOrderedRedbQueryable<TProps> OrderBy<TKey>(Expression<Func<TProps, TKey>> keySelector)
    {
        var newContext = _context.Clone();
        newContext.Orderings.Clear(); // OrderBy заменяет предыдущую сортировку
        
        var ordering = _orderingParser.ParseOrdering(keySelector, SortDirection.Ascending);
        newContext.Orderings.Add(ordering);
        
        // ✅ ИСПРАВЛЕНИЕ: Сохраняем IsEmpty флаг после OrderBy
        // Даже если добавили ordering, query может оставаться пустой (Where(x => false))

        return new RedbQueryable<TProps>(_provider, newContext, _filterParser, _orderingParser);
    }

    public virtual IOrderedRedbQueryable<TProps> OrderByDescending<TKey>(Expression<Func<TProps, TKey>> keySelector)
    {
        var newContext = _context.Clone();
        newContext.Orderings.Clear(); // OrderByDescending заменяет предыдущую сортировку
        
        var ordering = _orderingParser.ParseOrdering(keySelector, SortDirection.Descending);
        newContext.Orderings.Add(ordering);
        
        // ✅ ИСПРАВЛЕНИЕ: Сохраняем IsEmpty флаг после OrderByDescending
        // IsEmpty уже скопирован в Clone(), дополнительных действий не требуется

        return new RedbQueryable<TProps>(_provider, newContext, _filterParser, _orderingParser);
    }

    public virtual IOrderedRedbQueryable<TProps> ThenBy<TKey>(Expression<Func<TProps, TKey>> keySelector)
    {
        var newContext = _context.Clone();
        
        var ordering = _orderingParser.ParseOrdering(keySelector, SortDirection.Ascending);
        newContext.Orderings.Add(ordering);
        
        // ✅ ИСПРАВЛЕНИЕ: Сохраняем IsEmpty флаг после ThenBy  
        // IsEmpty уже скопирован в Clone(), дополнительных действий не требуется

        return new RedbQueryable<TProps>(_provider, newContext, _filterParser, _orderingParser);
    }

    public virtual IOrderedRedbQueryable<TProps> ThenByDescending<TKey>(Expression<Func<TProps, TKey>> keySelector)
    {
        var newContext = _context.Clone();
        
        var ordering = _orderingParser.ParseOrdering(keySelector, SortDirection.Descending);
        newContext.Orderings.Add(ordering);
        
        // ✅ ИСПРАВЛЕНИЕ: Сохраняем IsEmpty флаг после ThenByDescending
        // IsEmpty уже скопирован в Clone(), дополнительных действий не требуется

        return new RedbQueryable<TProps>(_provider, newContext, _filterParser, _orderingParser);
    }

    public virtual IRedbQueryable<TProps> Take(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Take count must be positive", nameof(count));
            
        var newContext = _context.Clone();
        newContext.Limit = count;

        return new RedbQueryable<TProps>(_provider, newContext, _filterParser, _orderingParser);
    }

    public virtual IRedbQueryable<TProps> Skip(int count)
    {
        if (count < 0)
            throw new ArgumentException("Skip count must be non-negative", nameof(count));
            
        var newContext = _context.Clone();
        newContext.Offset = count;

        return new RedbQueryable<TProps>(_provider, newContext, _filterParser, _orderingParser);
    }

    public virtual async Task<List<RedbObject<TProps>>> ToListAsync()
    {
        var result = await _provider.ExecuteAsync(BuildExpression(), typeof(List<RedbObject<TProps>>));
        return (List<RedbObject<TProps>>)result;
    }

    public virtual async Task<int> CountAsync()
    {
        var result = await _provider.ExecuteAsync(BuildCountExpression(), typeof(int));
        return (int)result;
    }

    public virtual async Task<RedbObject<TProps>?> FirstOrDefaultAsync()
    {
        // Для FirstOrDefault ограничиваем до 1 записи
        var limitedContext = _context.Clone();
        limitedContext.Limit = 1;
        
        var tempQueryable = new RedbQueryable<TProps>(_provider, limitedContext, _filterParser, _orderingParser);
        var result = await tempQueryable.ToListAsync();
        
        return result.FirstOrDefault();
    }

    public virtual async Task<bool> AnyAsync()
    {
        var count = await CountAsync();
        return count > 0;
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<TProps, bool>> predicate)
    {
        // Создаем новый запрос с дополнительным фильтром
        var filteredQuery = Where(predicate);
        return await filteredQuery.AnyAsync();
    }

    public virtual IRedbQueryable<TProps> WhereIn<TValue>(Expression<Func<TProps, TValue>> selector, IEnumerable<TValue> values)
    {
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        var valuesList = values.ToList();
        if (!valuesList.Any())
        {
            // Если список пустой, возвращаем запрос который ничего не найдет
            return Where(_ => false);
        }

        // Создаем выражение: x => values.Contains(selector(x))
        var parameter = selector.Parameters[0];
        var selectorBody = selector.Body;
        
        // Создаем константу со списком значений
        var valuesConstant = Expression.Constant(valuesList);
        
        // Создаем вызов Contains
        var containsMethod = typeof(List<TValue>).GetMethod("Contains", new[] { typeof(TValue) });
        var containsCall = Expression.Call(valuesConstant, containsMethod!, selectorBody);
        
        // Создаем лямбда-выражение
        var lambda = Expression.Lambda<Func<TProps, bool>>(containsCall, parameter);
        
        return Where(lambda);
    }

    public virtual async Task<bool> AllAsync(Expression<Func<TProps, bool>> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        // All() == true, если все записи удовлетворяют условию
        var totalCount = await CountAsync();
        if (totalCount == 0)
            return true; // Все элементы пустого множества удовлетворяют любому условию

        var matchingCount = await Where(predicate).CountAsync();
        return totalCount == matchingCount;
    }

    public virtual IRedbProjectedQueryable<TResult> Select<TResult>(Expression<Func<RedbObject<TProps>, TResult>> selector)
    {
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));

        return new RedbProjectedQueryable<TProps, TResult>(this, selector);
    }

    public virtual IRedbQueryable<TProps> Distinct()
    {
        var newContext = _context.Clone();
        newContext.IsDistinct = true;
        
        return new RedbQueryable<TProps>(_provider, newContext, _filterParser, _orderingParser);
    }

    public virtual IRedbQueryable<TProps> WithMaxRecursionDepth(int depth)
    {
        if (depth < 1)
            throw new ArgumentException("Max recursion depth must be positive", nameof(depth));
            
        var newContext = _context.Clone();
        newContext.MaxRecursionDepth = depth;
        
        return new RedbQueryable<TProps>(_provider, newContext, _filterParser, _orderingParser);
    }

    /// <summary>
    /// Строит выражение для выполнения запроса
    /// </summary>
    protected virtual Expression BuildExpression()
    {
        // Здесь будет создаваться Expression, представляющее весь запрос
        // Пока возвращаем заглушку - конкретная реализация будет в провайдере
        return Expression.Constant(_context);
    }

    /// <summary>
    /// Строит выражение для подсчета записей
    /// </summary>
    protected virtual Expression BuildCountExpression()
    {
        // Аналогично для Count
        return Expression.Constant(_context);
    }
    
    /// <summary>
    /// ✅ НОВЫЙ МЕТОД: Определяет является ли фильтр пустым (Where(x => false))
    /// </summary>
    private bool IsEmptyFilter(FilterExpression filter)
    {
        // Проверяем на константный false фильтр (создается для Where(x => false))
        if (filter is ComparisonExpression comparison && 
            comparison.Property.Name == "__constant" &&
            comparison.Property.Type == typeof(bool) &&
            comparison.Operator == ComparisonOperator.Equal &&
            comparison.Value is bool boolValue && 
            boolValue == false)
        {
            return true;
        }
        
        return false;
    }
}
