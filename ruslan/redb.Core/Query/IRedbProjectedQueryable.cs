using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace redb.Core.Query;

/// <summary>
/// Интерфейс для проекций LINQ-запросов в REDB
/// </summary>
public interface IRedbProjectedQueryable<TResult>
{
    /// <summary>
    /// Дополнительная фильтрация проецированных результатов
    /// </summary>
    IRedbProjectedQueryable<TResult> Where(Expression<Func<TResult, bool>> predicate);
    
    /// <summary>
    /// Сортировка проецированных результатов
    /// </summary>
    IRedbProjectedQueryable<TResult> OrderBy<TKey>(Expression<Func<TResult, TKey>> keySelector);
    
    /// <summary>
    /// Сортировка проецированных результатов по убыванию
    /// </summary>
    IRedbProjectedQueryable<TResult> OrderByDescending<TKey>(Expression<Func<TResult, TKey>> keySelector);
    
    /// <summary>
    /// Ограничение количества результатов
    /// </summary>
    IRedbProjectedQueryable<TResult> Take(int count);
    
    /// <summary>
    /// Пропуск результатов
    /// </summary>
    IRedbProjectedQueryable<TResult> Skip(int count);
    
    /// <summary>
    /// Уникальные значения
    /// </summary>
    IRedbProjectedQueryable<TResult> Distinct();
    
    /// <summary>
    /// Выполнить запрос и получить список результатов
    /// </summary>
    Task<List<TResult>> ToListAsync();
    
    /// <summary>
    /// Подсчитать количество результатов
    /// </summary>
    Task<int> CountAsync();
    
    /// <summary>
    /// Получить первый результат или значение по умолчанию
    /// </summary>
    Task<TResult?> FirstOrDefaultAsync();
}
