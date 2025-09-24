using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using redb.Core.Models.Entities;
using redb.Core.Models.Contracts;
using redb.Core.Query;
using redb.Core.Query.QueryExpressions;

namespace redb.Core.Postgres.Query;

/// <summary>
/// PostgreSQL реализация ITreeQueryable - древовидные LINQ-запросы к REDB
/// Расширяет RedbQueryable добавляя поддержку иерархических операторов
/// </summary>
public class PostgresTreeQueryable<TProps> : RedbQueryable<TProps>, ITreeQueryable<TProps>, IOrderedTreeQueryable<TProps>
    where TProps : class, new()
{
    protected readonly ITreeQueryProvider _treeProvider;
    protected readonly TreeQueryContext<TProps> _treeContext;

    public PostgresTreeQueryable(
        ITreeQueryProvider provider,
        TreeQueryContext<TProps> context,
        IFilterExpressionParser filterParser,
        IOrderingExpressionParser orderingParser)
        : base(provider, context, filterParser, orderingParser)
    {
        _treeProvider = provider;
        _treeContext = context;
    }

    // ===== ПЕРЕОПРЕДЕЛЕНИЕ БАЗОВЫХ МЕТОДОВ ДЛЯ TREE =====

    public override IRedbQueryable<TProps> Where(Expression<Func<TProps, bool>> predicate)
    {
        var newContext = _treeContext.Clone();
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

        // ✅ ИСПРАВЛЕНИЕ: Возвращаем PostgresTreeQueryable, а не базовый RedbQueryable
        return new PostgresTreeQueryable<TProps>(_treeProvider, newContext, _filterParser, _orderingParser);
    }

    // ===== ПЕРЕОПРЕДЕЛЕНИЕ СОРТИРОВКИ ДЛЯ СОХРАНЕНИЯ TREE КОНТЕКСТА =====

    /// <summary>
    /// ✅ ИСПРАВЛЕНИЕ ПРОБЛЕМЫ №4: Переопределяем OrderBy чтобы сохранить Tree контекст
    /// Базовый OrderBy возвращает RedbQueryable и теряет Tree контекст!
    /// ВОЗВРАЩАЕМ: IOrderedTreeQueryable (наследует от IOrderedRedbQueryable + сохраняет Tree методы)
    /// </summary>
    public override IOrderedRedbQueryable<TProps> OrderBy<TKey>(Expression<Func<TProps, TKey>> keySelector)
    {
        var newContext = _treeContext.Clone();
        newContext.Orderings.Clear(); // OrderBy заменяет предыдущую сортировку

        var ordering = _orderingParser.ParseOrdering(keySelector, SortDirection.Ascending);
        newContext.Orderings.Add(ordering);

        // ✅ ПРАВИЛЬНО: PostgresTreeQueryable реализует IOrderedTreeQueryable : IOrderedRedbQueryable
        // Возвращаем как IOrderedRedbQueryable, но фактически это PostgresTreeQueryable!
        return new PostgresTreeQueryable<TProps>(_treeProvider, newContext, _filterParser, _orderingParser);
    }

    /// <summary>
    /// ✅ ИСПРАВЛЕНИЕ ПРОБЛЕМЫ №4: Переопределяем OrderByDescending для Tree контекста
    /// </summary>
    public override IOrderedRedbQueryable<TProps> OrderByDescending<TKey>(Expression<Func<TProps, TKey>> keySelector)
    {
        var newContext = _treeContext.Clone();
        newContext.Orderings.Clear(); // OrderByDescending заменяет предыдущую сортировку

        var ordering = _orderingParser.ParseOrdering(keySelector, SortDirection.Descending);
        newContext.Orderings.Add(ordering);

        // ✅ ИСПРАВЛЕНИЕ: Возвращаем PostgresTreeQueryable, сохраняя Tree контекст!
        return new PostgresTreeQueryable<TProps>(_treeProvider, newContext, _filterParser, _orderingParser);
    }

    /// <summary>
    /// ✅ ИСПРАВЛЕНИЕ ПРОБЛЕМЫ №4: Переопределяем ThenBy для Tree контекста
    /// </summary>
    public override IOrderedRedbQueryable<TProps> ThenBy<TKey>(Expression<Func<TProps, TKey>> keySelector)
    {
        var newContext = _treeContext.Clone();

        var ordering = _orderingParser.ParseOrdering(keySelector, SortDirection.Ascending);
        newContext.Orderings.Add(ordering); // ThenBy добавляет к существующей сортировке

        // ✅ ИСПРАВЛЕНИЕ: Возвращаем PostgresTreeQueryable, сохраняя Tree контекст!
        return new PostgresTreeQueryable<TProps>(_treeProvider, newContext, _filterParser, _orderingParser);
    }

    /// <summary>
    /// ✅ ИСПРАВЛЕНИЕ ПРОБЛЕМЫ №4: Переопределяем ThenByDescending для Tree контекста
    /// </summary>
    public override IOrderedRedbQueryable<TProps> ThenByDescending<TKey>(Expression<Func<TProps, TKey>> keySelector)
    {
        var newContext = _treeContext.Clone();

        var ordering = _orderingParser.ParseOrdering(keySelector, SortDirection.Descending);
        newContext.Orderings.Add(ordering); // ThenByDescending добавляет к существующей сортировке

        // ✅ ИСПРАВЛЕНИЕ: Возвращаем PostgresTreeQueryable, сохраняя Tree контекст!
        return new PostgresTreeQueryable<TProps>(_treeProvider, newContext, _filterParser, _orderingParser);
    }

    // ===== ДРЕВОВИДНЫЕ ФИЛЬТРЫ =====

    public ITreeQueryable<TProps> WhereHasAncestor(Expression<Func<TProps, bool>> ancestorCondition)
    {
        if (ancestorCondition == null)
            throw new ArgumentNullException(nameof(ancestorCondition));

        var newContext = _treeContext.Clone();

        // Парсим условие для предков и добавляем древовидный фильтр
        var filterExpression = _filterParser.ParseFilter(ancestorCondition);
        var ancestorFilter = new TreeFilter(TreeFilterOperator.HasAncestor);
        ancestorFilter.FilterConditions = ConvertFilterToJson(filterExpression);

        newContext.TreeFilters.Add(ancestorFilter);

        return new PostgresTreeQueryable<TProps>(_treeProvider, newContext, _filterParser, _orderingParser);
    }

    public ITreeQueryable<TProps> WhereHasDescendant(Expression<Func<TProps, bool>> descendantCondition)
    {
        if (descendantCondition == null)
            throw new ArgumentNullException(nameof(descendantCondition));

        var newContext = _treeContext.Clone();

        // Парсим условие для потомков и добавляем древовидный фильтр
        var filterExpression = _filterParser.ParseFilter(descendantCondition);
        var descendantFilter = new TreeFilter(TreeFilterOperator.HasDescendant);
        descendantFilter.FilterConditions = ConvertFilterToJson(filterExpression);

        newContext.TreeFilters.Add(descendantFilter);

        return new PostgresTreeQueryable<TProps>(_treeProvider, newContext, _filterParser, _orderingParser);
    }

    public ITreeQueryable<TProps> WhereLevel(int level)
    {
        var newContext = _treeContext.Clone();
        var levelFilter = new TreeFilter(TreeFilterOperator.Level, level);
        newContext.TreeFilters.Add(levelFilter);

        return new PostgresTreeQueryable<TProps>(_treeProvider, newContext, _filterParser, _orderingParser);
    }

    public ITreeQueryable<TProps> WhereLevel(Expression<Func<int, bool>> levelCondition)
    {
        if (levelCondition == null)
            throw new ArgumentNullException(nameof(levelCondition));

        var newContext = _treeContext.Clone();
        var levelFilter = new TreeFilter(TreeFilterOperator.Level);

        // Анализируем условие уровня (например: level => level > 2)
        levelFilter.FilterConditions = ParseLevelCondition(levelCondition);
        newContext.TreeFilters.Add(levelFilter);

        return new PostgresTreeQueryable<TProps>(_treeProvider, newContext, _filterParser, _orderingParser);
    }

    public ITreeQueryable<TProps> WhereRoots()
    {
        var newContext = _treeContext.Clone();
        var rootFilter = new TreeFilter(TreeFilterOperator.IsRoot);
        newContext.TreeFilters.Add(rootFilter);

        return new PostgresTreeQueryable<TProps>(_treeProvider, newContext, _filterParser, _orderingParser);
    }

    public ITreeQueryable<TProps> WhereLeaves()
    {
        var newContext = _treeContext.Clone();
        var leafFilter = new TreeFilter(TreeFilterOperator.IsLeaf);
        newContext.TreeFilters.Add(leafFilter);

        return new PostgresTreeQueryable<TProps>(_treeProvider, newContext, _filterParser, _orderingParser);
    }

    public ITreeQueryable<TProps> WhereChildrenOf(long parentId)
    {
        var newContext = _treeContext.Clone();
        var childrenFilter = new TreeFilter(TreeFilterOperator.ChildrenOf, parentId);
        newContext.TreeFilters.Add(childrenFilter);

        return new PostgresTreeQueryable<TProps>(_treeProvider, newContext, _filterParser, _orderingParser);
    }

    public ITreeQueryable<TProps> WhereChildrenOf(IRedbObject parentObject)
    {
        if (parentObject == null)
            throw new ArgumentNullException(nameof(parentObject));

        return WhereChildrenOf(parentObject.Id);
    }

    public ITreeQueryable<TProps> WhereDescendantsOf(long ancestorId, int? maxDepth = null)
    {
        var newContext = _treeContext.Clone();
        var descendantsFilter = new TreeFilter(TreeFilterOperator.DescendantsOf, ancestorId)
        {
            MaxDepth = maxDepth
        };
        newContext.TreeFilters.Add(descendantsFilter);

        return new PostgresTreeQueryable<TProps>(_treeProvider, newContext, _filterParser, _orderingParser);
    }

    public ITreeQueryable<TProps> WhereDescendantsOf(IRedbObject ancestorObject, int? maxDepth = null)
    {
        if (ancestorObject == null)
            throw new ArgumentNullException(nameof(ancestorObject));

        return WhereDescendantsOf(ancestorObject.Id, maxDepth);
    }

    // ===== ПЕРЕОПРЕДЕЛЕННЫЕ МЕТОДЫ ДЛЯ ВОЗВРАТА TreeRedbObject =====

    // ===== БАЗОВЫЕ МЕТОДЫ (OVERRIDE ДЛЯ IRedbQueryable) =====

    public override async Task<List<RedbObject<TProps>>> ToListAsync()
    {
        // ✅ АРХИТЕКТУРНОЕ ИСПРАВЛЕНИЕ: TreeRedbObject<T> IS-A RedbObject<T> - конверсия не нужна!
        var treeObjects = await ((ITreeQueryable<TProps>)this).ToListAsync();
        return treeObjects.Cast<RedbObject<TProps>>().ToList();
    }

    public override async Task<RedbObject<TProps>?> FirstOrDefaultAsync()
    {
        // ✅ АРХИТЕКТУРНОЕ ИСПРАВЛЕНИЕ: TreeRedbObject<T> IS-A RedbObject<T> - конверсия не нужна!
        var treeObject = await ((ITreeQueryable<TProps>)this).FirstOrDefaultAsync();
        return treeObject; // Прямое приведение!
    }

    // ===== TREE-СПЕЦИФИЧНЫЕ МЕТОДЫ (РЕАЛИЗАЦИЯ ITreeQueryable) =====

    async Task<List<TreeRedbObject<TProps>>> ITreeQueryable<TProps>.ToListAsync()
    {
        var expression = BuildTreeExpression();
        var result = await _treeProvider.ExecuteAsync(expression, typeof(List<TreeRedbObject<TProps>>));
        return (List<TreeRedbObject<TProps>>)result;
    }

    async Task<TreeRedbObject<TProps>?> ITreeQueryable<TProps>.FirstOrDefaultAsync()
    {
        var limitedQuery = (ITreeQueryable<TProps>)Take(1);
        var results = await ((ITreeQueryable<TProps>)limitedQuery).ToListAsync();
        return results.FirstOrDefault();
    }

    // ✅ УДАЛЕНО: ConvertTreeToRedbObject больше не нужен!
    // TreeRedbObject<T> IS-A RedbObject<T> - прямое приведение работает!

    public override IRedbQueryable<TProps> Take(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Take count must be positive", nameof(count));

        var newContext = _treeContext.Clone();
        newContext.Limit = count;

        // ✅ ИСПРАВЛЕНИЕ: Возвращаем PostgresTreeQueryable, сохраняя Tree контекст!
        return new PostgresTreeQueryable<TProps>(_treeProvider, newContext, _filterParser, _orderingParser);
    }

    public override IRedbQueryable<TProps> Skip(int count)
    {
        if (count < 0)
            throw new ArgumentException("Skip count must be non-negative", nameof(count));

        var newContext = _treeContext.Clone();
        newContext.Offset = count;

        // ✅ ИСПРАВЛЕНИЕ: Возвращаем PostgresTreeQueryable, сохраняя Tree контекст!
        return new PostgresTreeQueryable<TProps>(_treeProvider, newContext, _filterParser, _orderingParser);
    }

    public override IRedbQueryable<TProps> WhereIn<TValue>(Expression<Func<TProps, TValue>> selector, IEnumerable<TValue> values)
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

    public override IRedbQueryable<TProps> Distinct()
    {
        var newContext = _treeContext.Clone();
        newContext.IsDistinct = true;

        // ✅ ИСПРАВЛЕНИЕ: Возвращаем PostgresTreeQueryable, сохраняя Tree контекст!
        return new PostgresTreeQueryable<TProps>(_treeProvider, newContext, _filterParser, _orderingParser);
    }

    public override IRedbQueryable<TProps> WithMaxRecursionDepth(int depth)
    {
        if (depth < 1)
            throw new ArgumentException("Max recursion depth must be positive", nameof(depth));

        var newContext = _treeContext.Clone();
        newContext.MaxRecursionDepth = depth;

        // ✅ ИСПРАВЛЕНИЕ: Возвращаем PostgresTreeQueryable, сохраняя Tree контекст!
        return new PostgresTreeQueryable<TProps>(_treeProvider, newContext, _filterParser, _orderingParser);
    }

    public override async Task<int> CountAsync()
    {
        var result = await _treeProvider.ExecuteAsync(BuildCountExpression(), typeof(int));
        return (int)result;
    }

    public override async Task<bool> AnyAsync()
    {
        var count = await CountAsync();
        return count > 0;
    }

    public override async Task<bool> AnyAsync(Expression<Func<TProps, bool>> predicate)
    {
        // Создаем новый запрос с дополнительным фильтром
        var filteredQuery = Where(predicate);
        return await filteredQuery.AnyAsync();
    }

    public override async Task<bool> AllAsync(Expression<Func<TProps, bool>> predicate)
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

    // ===== ДРЕВОВИДНО-СПЕЦИФИЧНЫЕ МЕТОДЫ =====

    public async Task<List<TreeRedbObject<TProps>>> ToTreeListAsync(int maxDepth = 10)
    {
        // Получаем плоский список объектов
        var flatObjects = await ToFlatListAsync();

        // Строим иерархическую структуру
        return BuildTreeStructure(flatObjects, maxDepth);
    }

    public async Task<List<TreeRedbObject<TProps>>> ToFlatListAsync()
    {
        // Используем Tree-специфичный ToListAsync для получения плоского списка
        return await ((ITreeQueryable<TProps>)this).ToListAsync();
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

    // ===== МЕТОДЫ LINQ НАСЛЕДУЮТСЯ ОТ БАЗОВОГО КЛАССА =====
    // 💀 УДАЛЕНЫ все дублированные методы Where/OrderBy/Take/Skip/Distinct!
    // ✅ Используется логика из RedbQueryable - БЕЗ ДУБЛИРОВАНИЯ!

    public ITreeQueryable<TProps> WithMaxDepth(int depth)
    {
        if (depth < 1)
            throw new ArgumentException("Max depth must be positive", nameof(depth));

        // ✅ ИСПРАВЛЕНИЕ: Создаем новый контекст с нужным MaxDepth (init-only свойство)
        var newContext = new TreeQueryContext<TProps>(_treeContext.SchemeId, _treeContext.UserId, _treeContext.CheckPermissions, _treeContext.RootObjectId, depth)
        {
            ParentIds = _treeContext.ParentIds,
            Filter = _treeContext.Filter,
            Orderings = new List<OrderingExpression>(_treeContext.Orderings),
            Limit = _treeContext.Limit,
            Offset = _treeContext.Offset,
            IsDistinct = _treeContext.IsDistinct,
            MaxRecursionDepth = _treeContext.MaxRecursionDepth,
            IsEmpty = _treeContext.IsEmpty
        };

        // Копируем древовидные фильтры
        newContext.TreeFilters = new List<TreeFilter>(_treeContext.TreeFilters);

        return new PostgresTreeQueryable<TProps>(_treeProvider, newContext, _filterParser, _orderingParser);
    }

    public IRedbProjectedQueryable<TResult> Select<TResult>(Expression<Func<TreeRedbObject<TProps>, TResult>> selector)
    {
        return new TreeProjectedQueryable<TProps, TResult>(this, selector);
    }

    // ===== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =====

    /// <summary>
    /// Построение выражения для древовидного запроса
    /// </summary>
    protected virtual Expression BuildTreeExpression()
    {
        return Expression.Constant(_treeContext);
    }

    /// <summary>
    /// Конвертация FilterExpression в словарь для JSON фильтра
    /// </summary>
    private Dictionary<string, object> ConvertFilterToJson(FilterExpression filter)
    {
        var result = new Dictionary<string, object>();

        // Упрощенная конвертация - для полной реализации нужен visitor
        if (filter is ComparisonExpression comparison)
        {
            var fieldName = comparison.Property.Name;

            switch (comparison.Operator)
            {
                case ComparisonOperator.Equal:
                    result[fieldName] = comparison.Value ?? "";
                    break;
                case ComparisonOperator.GreaterThan:
                    result[fieldName] = new Dictionary<string, object> { ["$gt"] = comparison.Value! };
                    break;
                case ComparisonOperator.LessThan:
                    result[fieldName] = new Dictionary<string, object> { ["$lt"] = comparison.Value! };
                    break;
                    // Добавить другие операторы по необходимости
            }
        }

        return result;
    }

    /// <summary>
    /// Парсинг условия уровня (например: level => level > 2)
    /// </summary>
    private Dictionary<string, object> ParseLevelCondition(Expression<Func<int, bool>> levelCondition)
    {
        // Упрощенный парсинг - анализируем тело лямбды
        if (levelCondition.Body is BinaryExpression binary)
        {
            switch (binary.NodeType)
            {
                case ExpressionType.GreaterThan:
                    if (binary.Right is ConstantExpression gtConstant)
                        return new Dictionary<string, object> { ["$gt"] = gtConstant.Value! };
                    break;
                case ExpressionType.LessThan:
                    if (binary.Right is ConstantExpression ltConstant)
                        return new Dictionary<string, object> { ["$lt"] = ltConstant.Value! };
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    if (binary.Right is ConstantExpression gteConstant)
                        return new Dictionary<string, object> { ["$gte"] = gteConstant.Value! };
                    break;
                case ExpressionType.LessThanOrEqual:
                    if (binary.Right is ConstantExpression lteConstant)
                        return new Dictionary<string, object> { ["$lte"] = lteConstant.Value! };
                    break;
                case ExpressionType.Equal:
                    if (binary.Right is ConstantExpression eqConstant)
                        return new Dictionary<string, object> { ["$eq"] = eqConstant.Value! };
                    break;
            }
        }

        // Fallback - возвращаем равенство 0 (корневой уровень)
        return new Dictionary<string, object> { ["$eq"] = 0 };
    }

    /// <summary>
    /// Построение иерархической структуры из плоского списка
    /// </summary>
    private List<TreeRedbObject<TProps>> BuildTreeStructure(List<TreeRedbObject<TProps>> flatObjects, int maxDepth)
    {
        if (!flatObjects.Any()) return flatObjects;

        // Создаем словарь для быстрого поиска
        var objectDict = flatObjects.ToDictionary(obj => obj.id, obj => obj);
        var roots = new List<TreeRedbObject<TProps>>();

        // Находим корневые элементы и строим связи
        foreach (var obj in flatObjects)
        {
            if (obj.parent_id == null)
            {
                // Корневой элемент
                roots.Add(obj);
            }
            else if (objectDict.TryGetValue(obj.parent_id.Value, out var parent))
            {
                // Устанавливаем связи Parent/Children
                obj.Parent = parent;
                parent.Children.Add(obj);
            }
        }

        return roots;
    }
}


