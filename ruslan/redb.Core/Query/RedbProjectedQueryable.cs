using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using redb.Core.Models.Entities;

namespace redb.Core.Query;

/// <summary>
/// Реализация проекций LINQ-запросов в REDB с поддержкой фильтрации и сортировки
/// Выполняет операции в памяти после получения данных из БД
/// </summary>
public class RedbProjectedQueryable<TProps, TResult> : IRedbProjectedQueryable<TResult>
    where TProps : class, new()
{
    private readonly IRedbQueryable<TProps> _sourceQuery;
    private readonly Expression<Func<RedbObject<TProps>, TResult>> _projection;
    
    // Цепочка операций для выполнения после проекции
    private readonly List<Expression<Func<TResult, bool>>> _wherePredicates = new();
    private readonly List<(Expression KeySelector, bool IsDescending)> _orderByExpressions = new();

    public RedbProjectedQueryable(
        IRedbQueryable<TProps> sourceQuery,
        Expression<Func<RedbObject<TProps>, TResult>> projection)
    {
        _sourceQuery = sourceQuery;
        _projection = projection;
    }
    
    // Приватный конструктор для создания копий с дополнительными операциями
    private RedbProjectedQueryable(
        IRedbQueryable<TProps> sourceQuery,
        Expression<Func<RedbObject<TProps>, TResult>> projection,
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
        
        return new RedbProjectedQueryable<TProps, TResult>(
            _sourceQuery, 
            _projection, 
            newWherePredicates, 
            _orderByExpressions);
    }

    public IRedbProjectedQueryable<TResult> OrderBy<TKey>(Expression<Func<TResult, TKey>> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        
        // Заменяем существующие сортировки (как в LINQ)
        var newOrderByExpressions = new List<(Expression, bool)> { (keySelector, false) };
        
        return new RedbProjectedQueryable<TProps, TResult>(
            _sourceQuery, 
            _projection, 
            _wherePredicates, 
            newOrderByExpressions);
    }

    public IRedbProjectedQueryable<TResult> OrderByDescending<TKey>(Expression<Func<TResult, TKey>> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        
        // Заменяем существующие сортировки с descending
        var newOrderByExpressions = new List<(Expression, bool)> { (keySelector, true) };
        
        return new RedbProjectedQueryable<TProps, TResult>(
            _sourceQuery, 
            _projection, 
            _wherePredicates, 
            newOrderByExpressions);
    }

    public IRedbProjectedQueryable<TResult> Take(int count)
    {
        // Take можем делегировать к исходному запросу
        return new RedbProjectedQueryable<TProps, TResult>(
            _sourceQuery.Take(count), 
            _projection, 
            _wherePredicates, 
            _orderByExpressions);
    }

    public IRedbProjectedQueryable<TResult> Skip(int count)
    {
        // Skip можем делегировать к исходному запросу
        return new RedbProjectedQueryable<TProps, TResult>(
            _sourceQuery.Skip(count), 
            _projection, 
            _wherePredicates, 
            _orderByExpressions);
    }

    public IRedbProjectedQueryable<TResult> Distinct()
    {
        // Distinct можем делегировать к исходному запросу
        return new RedbProjectedQueryable<TProps, TResult>(
            _sourceQuery.Distinct(), 
            _projection, 
            _wherePredicates, 
            _orderByExpressions);
    }

    public async Task<List<TResult>> ToListAsync()
    {
        // Получаем полные объекты и применяем проекцию в памяти
        var fullObjects = await _sourceQuery.ToListAsync();
        var compiledProjection = _projection.Compile();
        
        // Применяем проекцию к каждому объекту
        var projectedResults = fullObjects.Select(redbObj => compiledProjection(redbObj));
        
        // Применяем Where фильтры после проекции
        foreach (var wherePredicate in _wherePredicates)
        {
            var compiledPredicate = wherePredicate.Compile();
            projectedResults = projectedResults.Where(compiledPredicate);
        }
        
        // Применяем OrderBy сортировки после проекции
        if (_orderByExpressions.Count > 0)
        {
            IOrderedEnumerable<TResult>? orderedResults = null;
            
            for (int i = 0; i < _orderByExpressions.Count; i++)
            {
                var (keySelector, isDescending) = _orderByExpressions[i];
                
                // Компилируем выражение динамически
                var compiledDelegate = ((LambdaExpression)keySelector).Compile();
                
                if (i == 0)
                {
                    // Первая сортировка - используем Func<TResult, object> для универсальности
                    Func<TResult, object> universalKeySelector = item => compiledDelegate.DynamicInvoke(item) ?? new object();
                    
                    orderedResults = isDescending 
                        ? projectedResults.OrderByDescending(universalKeySelector)
                        : projectedResults.OrderBy(universalKeySelector);
                }
                else
                {
                    // Последующие сортировки (ThenBy)
                    Func<TResult, object> universalKeySelector = item => compiledDelegate.DynamicInvoke(item) ?? new object();
                    
                    orderedResults = isDescending
                        ? orderedResults!.ThenByDescending(universalKeySelector)
                        : orderedResults!.ThenBy(universalKeySelector);
                }
            }
            
            if (orderedResults != null)
                projectedResults = orderedResults;
        }
        
        return projectedResults.ToList();
    }

    public async Task<int> CountAsync()
    {
        // Если есть Where операции после проекции, нужно их учесть
        if (_wherePredicates.Count > 0)
        {
            var results = await ToListAsync();
            return results.Count;
        }
        
        // Иначе количество не зависит от проекции
        return await _sourceQuery.CountAsync();
    }

    public async Task<TResult?> FirstOrDefaultAsync()
    {
        // Для FirstOrDefault применяем все операции и берем первый элемент
        var results = await ToListAsync();
        return results.FirstOrDefault();
    }
}
