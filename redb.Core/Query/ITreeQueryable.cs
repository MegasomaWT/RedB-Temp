using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using redb.Core.Models.Entities;
using redb.Core.Models.Contracts;

namespace redb.Core.Query;

/// <summary>
/// Интерфейс для типобезопасных древовидных LINQ-запросов к REDB
/// Расширяет IRedbQueryable добавляя поддержку иерархических операторов
/// </summary>
public interface ITreeQueryable<TProps> : IRedbQueryable<TProps> where TProps : class, new()
{
    // ===== ДРЕВОВИДНЫЕ ФИЛЬТРЫ =====
    
    /// <summary>
    /// Фильтр по предкам: найти объекты у которых есть предок удовлетворяющий условию
    /// Использует SQL оператор $hasAncestor
    /// </summary>
    /// <param name="ancestorCondition">Условие для поиска предков</param>
    /// <returns>Запрос с фильтром по предкам</returns>
    ITreeQueryable<TProps> WhereHasAncestor(Expression<Func<TProps, bool>> ancestorCondition);
    
    /// <summary>
    /// Фильтр по потомкам: найти объекты у которых есть потомок удовлетворяющий условию  
    /// Использует SQL оператор $hasDescendant
    /// </summary>
    /// <param name="descendantCondition">Условие для поиска потомков</param>
    /// <returns>Запрос с фильтром по потомкам</returns>
    ITreeQueryable<TProps> WhereHasDescendant(Expression<Func<TProps, bool>> descendantCondition);
    
    /// <summary>
    /// Фильтр по уровню в дереве
    /// Использует SQL оператор $level
    /// </summary>
    /// <param name="level">Уровень в дереве (0 = корень)</param>
    /// <returns>Запрос с фильтром по уровню</returns>
    ITreeQueryable<TProps> WhereLevel(int level);
    
    /// <summary>
    /// Фильтр по уровню в дереве с оператором сравнения
    /// Использует SQL операторы $level: {$gt: N}, {$lt: N}, etc.
    /// </summary>
    /// <param name="levelCondition">Условие для уровня (например: level => level > 2)</param>
    /// <returns>Запрос с фильтром по условию уровня</returns>
    ITreeQueryable<TProps> WhereLevel(Expression<Func<int, bool>> levelCondition);
    
    /// <summary>
    /// Только корневые элементы (parent_id IS NULL)
    /// Использует SQL оператор $isRoot
    /// </summary>
    /// <returns>Запрос только корневых объектов</returns>
    ITreeQueryable<TProps> WhereRoots();
    
    /// <summary>
    /// Только листья (объекты без детей)
    /// Использует SQL оператор $isLeaf  
    /// </summary>
    /// <returns>Запрос только листовых объектов</returns>
    ITreeQueryable<TProps> WhereLeaves();
    
    /// <summary>
    /// Прямые дети указанного объекта
    /// Использует SQL оператор $childrenOf
    /// </summary>
    /// <param name="parentId">ID родительского объекта</param>
    /// <returns>Запрос детей объекта</returns>
    ITreeQueryable<TProps> WhereChildrenOf(long parentId);
    
    /// <summary>
    /// Прямые дети указанного объекта
    /// Использует SQL оператор $childrenOf
    /// </summary>
    /// <param name="parentObject">Родительский объект</param>
    /// <returns>Запрос детей объекта</returns>
    ITreeQueryable<TProps> WhereChildrenOf(IRedbObject parentObject);
    
    /// <summary>
    /// Все потомки указанного объекта (рекурсивно)
    /// Использует SQL оператор $descendantsOf
    /// </summary>
    /// <param name="ancestorId">ID предка</param>
    /// <param name="maxDepth">Максимальная глубина поиска (null = без ограничений)</param>
    /// <returns>Запрос всех потомков объекта</returns>
    ITreeQueryable<TProps> WhereDescendantsOf(long ancestorId, int? maxDepth = null);
    
    /// <summary>
    /// Все потомки указанного объекта (рекурсивно)
    /// Использует SQL оператор $descendantsOf  
    /// </summary>
    /// <param name="ancestorObject">Объект-предок</param>
    /// <param name="maxDepth">Максимальная глубина поиска (null = без ограничений)</param>
    /// <returns>Запрос всех потомков объекта</returns>
    ITreeQueryable<TProps> WhereDescendantsOf(IRedbObject ancestorObject, int? maxDepth = null);

    // ===== ПЕРЕОПРЕДЕЛЕННЫЕ МЕТОДЫ ДЛЯ ВОЗВРАТА ДРЕВОВИДНЫХ ОБЪЕКТОВ =====
    
    /// <summary>
    /// Выполнить запрос и получить список древовидных объектов
    /// </summary>
    /// <returns>Список TreeRedbObject с навигационными свойствами</returns>
    new Task<List<TreeRedbObject<TProps>>> ToListAsync();
    
    /// <summary>
    /// Получить первый древовидный объект или null
    /// </summary>
    /// <returns>TreeRedbObject или null</returns>
    new Task<TreeRedbObject<TProps>?> FirstOrDefaultAsync();

    // ===== ДРЕВОВИДНО-СПЕЦИФИЧНЫЕ МЕТОДЫ =====
    
    /// <summary>
    /// Выполнить запрос и построить полное дерево с загруженными детьми
    /// Загружает объекты и их связи Parent/Children  
    /// </summary>
    /// <param name="maxDepth">Максимальная глубина загрузки детей</param>
    /// <returns>Список корневых TreeRedbObject с загруженными детьми</returns>
    Task<List<TreeRedbObject<TProps>>> ToTreeListAsync(int maxDepth = 10);
    
    /// <summary>
    /// Выполнить запрос и получить плоский список древовидных объектов  
    /// Без загрузки связей Parent/Children (для производительности)
    /// </summary>
    /// <returns>Плоский список TreeRedbObject</returns>
    Task<List<TreeRedbObject<TProps>>> ToFlatListAsync();

    // ===== TREE-СПЕЦИФИЧНЫЕ API =====
    // 💀 УДАЛЕНЫ дублированные 'new' объявления - используется базовый функционал IRedbQueryable!
    
    /// <summary>
    /// Настроить максимальную глубину поиска в дереве
    /// По умолчанию: 50 уровней (поиск потомков), 1 (поиск детей)
    /// </summary>
    ITreeQueryable<TProps> WithMaxDepth(int depth);
    
    /// <summary>
    /// Проекция для древовидных объектов (специфично для TreeRedbObject)
    /// </summary>
    IRedbProjectedQueryable<TResult> Select<TResult>(Expression<Func<TreeRedbObject<TProps>, TResult>> selector);
}

/// <summary>
/// Упорядоченный древовидный запрос (для поддержки ThenBy/ThenByDescending)
/// </summary>  
public interface IOrderedTreeQueryable<TProps> : ITreeQueryable<TProps>, IOrderedRedbQueryable<TProps> 
    where TProps : class, new()
{
    // 💀 УДАЛЕНЫ дублированные 'new' объявления ThenBy/ThenByDescending!
    // ✅ Используется базовый функционал IOrderedRedbQueryable
}
