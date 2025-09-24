using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using redb.Core.Models.Entities;
using redb.Core.Query;

namespace redb.Core.Postgres.Query;

/// <summary>
/// Реализация проекций для древовидных LINQ-запросов в REDB
/// Специализированная версия для TreeRedbObject<TProps>
/// </summary>
public class TreeProjectedQueryable<TProps, TResult> : IRedbProjectedQueryable<TResult>
    where TProps : class, new()
{
    private readonly ITreeQueryable<TProps> _sourceQuery;
    private readonly Expression<Func<TreeRedbObject<TProps>, TResult>> _projection;
    
    // Цепочка операций для выполнения после проекции
    private readonly List<Expression<Func<TResult, bool>>> _wherePredicates = new();
    private readonly List<(Expression KeySelector, bool IsDescending)> _orderByExpressions = new();

    public TreeProjectedQueryable(
        ITreeQueryable<TProps> sourceQuery,
        Expression<Func<TreeRedbObject<TProps>, TResult>> projection)
    {
        _sourceQuery = sourceQuery;
        _projection = projection;
    }
    
    // Приватный конструктор для создания копий с дополнительными операциями
    private TreeProjectedQueryable(
        ITreeQueryable<TProps> sourceQuery,
        Expression<Func<TreeRedbObject<TProps>, TResult>> projection,
        List<Expression<Func<TResult, bool>>> wherePredicates,
        List<(Expression KeySelector, bool IsDescending)> orderByExpressions)
    {
        _sourceQuery = sourceQuery;
        _projection = projection;
        _wherePredicates = new List<Expression<Func<TResult, bool>>>(wherePredicates);
        _orderByExpressions = new List<(Expression, bool)>(orderByExpressions);
    }

    public IRedbProjectedQueryable<TResult> Where(Expression<Func<TResult, bool>> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        
        // Добавляем фильтр к цепочке операций
        var newWherePredicates = new List<Expression<Func<TResult, bool>>>(_wherePredicates) { predicate };
        
        return new TreeProjectedQueryable<TProps, TResult>(
            _sourceQuery,
            _projection, 
            newWherePredicates, 
            _orderByExpressions);
    }

    public IRedbProjectedQueryable<TResult> OrderBy<TKey>(Expression<Func<TResult, TKey>> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        
        // Заменяем существующую сортировку
        var newOrderByExpressions = new List<(Expression, bool)> { (keySelector, false) };
        
        return new TreeProjectedQueryable<TProps, TResult>(
            _sourceQuery,
            _projection,
            _wherePredicates,
            newOrderByExpressions);
    }

    public IRedbProjectedQueryable<TResult> OrderByDescending<TKey>(Expression<Func<TResult, TKey>> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        
        // Заменяем существующую сортировку
        var newOrderByExpressions = new List<(Expression, bool)> { (keySelector, true) };
        
        return new TreeProjectedQueryable<TProps, TResult>(
            _sourceQuery,
            _projection,
            _wherePredicates,
            newOrderByExpressions);
    }

    public IRedbProjectedQueryable<TResult> Take(int count)
    {
        // Применяем Take к исходному запросу
        var limitedSource = (ITreeQueryable<TProps>)_sourceQuery.Take(count);
        return new TreeProjectedQueryable<TProps, TResult>(limitedSource, _projection, _wherePredicates, _orderByExpressions);
    }

    public IRedbProjectedQueryable<TResult> Skip(int count)
    {
        // Применяем Skip к исходному запросу
        var skippedSource = (ITreeQueryable<TProps>)_sourceQuery.Skip(count);
        return new TreeProjectedQueryable<TProps, TResult>(skippedSource, _projection, _wherePredicates, _orderByExpressions);
    }

    public IRedbProjectedQueryable<TResult> Distinct()
    {
        // Применяем Distinct к исходному запросу
        var distinctSource = (ITreeQueryable<TProps>)_sourceQuery.Distinct();
        return new TreeProjectedQueryable<TProps, TResult>(distinctSource, _projection, _wherePredicates, _orderByExpressions);
    }

    public async Task<List<TResult>> ToListAsync()
    {
        // 🚨 КРИТИЧНАЯ ПРОБЛЕМА ПРОИЗВОДИТЕЛЬНОСТИ - ВСЕ В ПАМЯТИ! 
        // TODO: Переработать на SQL-based проекции для высокой производительности
        
        // ⚡ ОПТИМИЗАЦИЯ: Применяем лимиты К ИСХОДНОМУ ЗАПРОСУ перед загрузкой
        var optimizedSourceQuery = _sourceQuery;
        
        // Если есть только проекция без дополнительных фильтров - можем использовать лимиты
        if (!_wherePredicates.Any() && !_orderByExpressions.Any())
        {
            // Проекция без дополнительной логики - используем исходные лимиты
            var fullObjects = await optimizedSourceQuery.ToListAsync();
            var simpleProjection = _projection.Compile();
            return fullObjects.Select(redbObj => simpleProjection(redbObj)).ToList();
        }
        
        // 🐌 FALLBACK: Старая логика в памяти (для сложных случаев)
        // ВНИМАНИЕ: Неэффективно на больших данных!
        var allObjects = await optimizedSourceQuery.ToListAsync();
        var complexProjection = _projection.Compile();
        
        // Применяем проекцию к каждому объекту
        var projectedResults = allObjects.Select(redbObj => complexProjection(redbObj));
        
        // Применяем Where фильтры после проекции
        foreach (var wherePredicate in _wherePredicates)
        {
            var compiledWhere = wherePredicate.Compile();
            projectedResults = projectedResults.Where(compiledWhere);
        }
        
        // Применяем сортировку после проекции
        IOrderedEnumerable<TResult>? orderedResults = null;
        foreach (var (keySelector, isDescending) in _orderByExpressions)
        {
            // Компилируем expression в delegate
            var compiledKeySelector = ((LambdaExpression)keySelector).Compile();
            
            if (orderedResults == null)
            {
                // Первая сортировка
                orderedResults = isDescending 
                    ? projectedResults.OrderByDescending(item => compiledKeySelector.DynamicInvoke(item))
                    : projectedResults.OrderBy(item => compiledKeySelector.DynamicInvoke(item));
            }
            else
            {
                // Дополнительная сортировка
                orderedResults = isDescending 
                    ? orderedResults.ThenByDescending(item => compiledKeySelector.DynamicInvoke(item))
                    : orderedResults.ThenBy(item => compiledKeySelector.DynamicInvoke(item));
            }
        }
        
        var finalResults = orderedResults?.AsEnumerable() ?? projectedResults;
        return finalResults.ToList();
    }

    public async Task<int> CountAsync()
    {
        var results = await ToListAsync();
        return results.Count;
    }

    public async Task<TResult?> FirstOrDefaultAsync()
    {
        var results = await ToListAsync();
        return results.FirstOrDefault();
    }
}
