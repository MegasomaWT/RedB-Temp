using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using redb.Core.Models.Entities;

namespace redb.Core.Query;

/// <summary>
/// Основной интерфейс для типобезопасных LINQ-запросов к REDB
/// </summary>
public interface IRedbQueryable<TProps> where TProps : class, new()
{
    /// <summary>
    /// Фильтрация по условию
    /// </summary>
    IRedbQueryable<TProps> Where(Expression<Func<TProps, bool>> predicate);
    
    /// <summary>
    /// Сортировка по возрастанию
    /// </summary>
    IOrderedRedbQueryable<TProps> OrderBy<TKey>(Expression<Func<TProps, TKey>> keySelector);
    
    /// <summary>
    /// Сортировка по убыванию
    /// </summary>
    IOrderedRedbQueryable<TProps> OrderByDescending<TKey>(Expression<Func<TProps, TKey>> keySelector);
    
    /// <summary>
    /// Ограничение количества записей
    /// </summary>
    IRedbQueryable<TProps> Take(int count);
    
    /// <summary>
    /// Пропуск записей
    /// </summary>
    IRedbQueryable<TProps> Skip(int count);
    
    /// <summary>
    /// Выполнить запрос и получить список объектов
    /// </summary>
    Task<List<RedbObject<TProps>>> ToListAsync();
    
    /// <summary>
    /// Подсчет количества записей без загрузки данных
    /// </summary>
    Task<int> CountAsync();
    
    /// <summary>
    /// Получить первый объект или null
    /// </summary>
    Task<RedbObject<TProps>?> FirstOrDefaultAsync();
    
    /// <summary>
    /// Проверить наличие записей
    /// </summary>
    Task<bool> AnyAsync();
    
    /// <summary>
    /// Проверить наличие записей, удовлетворяющих условию
    /// </summary>
    Task<bool> AnyAsync(Expression<Func<TProps, bool>> predicate);
    
    /// <summary>
    /// Фильтрация по вхождению значения в список (WHERE field IN (...))
    /// </summary>
    IRedbQueryable<TProps> WhereIn<TValue>(Expression<Func<TProps, TValue>> selector, IEnumerable<TValue> values);
    
    /// <summary>
    /// Проверить что ВСЕ записи удовлетворяют условию
    /// </summary>
    Task<bool> AllAsync(Expression<Func<TProps, bool>> predicate);
    
    /// <summary>
    /// Проекция полей - возврат только выбранных свойств
    /// </summary>
    IRedbProjectedQueryable<TResult> Select<TResult>(Expression<Func<RedbObject<TProps>, TResult>> selector);
    
    /// <summary>
    /// Получить уникальные значения (по всем полям объекта)
    /// </summary>
    IRedbQueryable<TProps> Distinct();
    
    /// <summary>
    /// Настроить максимальную глубину рекурсии для сложных запросов ($and/$or/$not)
    /// По умолчанию: 10 уровней
    /// </summary>
    IRedbQueryable<TProps> WithMaxRecursionDepth(int depth);
}

/// <summary>
/// Интерфейс для упорядоченных запросов (после OrderBy)
/// </summary>
public interface IOrderedRedbQueryable<TProps> : IRedbQueryable<TProps> where TProps : class, new()
{
    /// <summary>
    /// Дополнительная сортировка по возрастанию
    /// </summary>
    IOrderedRedbQueryable<TProps> ThenBy<TKey>(Expression<Func<TProps, TKey>> keySelector);
    
    /// <summary>
    /// Дополнительная сортировка по убыванию
    /// </summary>
    IOrderedRedbQueryable<TProps> ThenByDescending<TKey>(Expression<Func<TProps, TKey>> keySelector);
}
