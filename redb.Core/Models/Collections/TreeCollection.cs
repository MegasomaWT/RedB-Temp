using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using redb.Core.Utils;
using redb.Core.Models.Contracts;
using redb.Core.Models.Entities;

namespace redb.Core.Models.Collections
{
    /// <summary>
    /// Специализированная коллекция для работы с древовидными структурами объектов
    /// Автоматически строит иерархию при добавлении узлов
    /// Поддерживает полиморфные деревья (объекты разных схем в одном дереве)
    /// </summary>
    public class TreeCollection : IEnumerable<ITreeRedbObject>
    {
        private readonly Dictionary<long, ITreeRedbObject> _nodesById = new();
        private readonly List<ITreeRedbObject> _roots = new();
        private readonly List<ITreeRedbObject> _orphans = new(); // Узлы, чьи родители еще не добавлены

        /// <summary>
        /// Получает количество узлов в коллекции
        /// </summary>
        public int Count => _nodesById.Count;

        /// <summary>
        /// Получает все корневые узлы (без родителей)
        /// </summary>
        public IEnumerable<ITreeRedbObject> Roots => _roots.AsReadOnly();

        /// <summary>
        /// Получает все листовые узлы (без детей)
        /// </summary>
        public IEnumerable<ITreeRedbObject> Leaves => _nodesById.Values.Where(n => n.IsLeaf);

        /// <summary>
        /// Получает узлы-сироты (родители которых не добавлены в коллекцию)
        /// </summary>
        public IEnumerable<ITreeRedbObject> Orphans => _orphans.AsReadOnly();

        /// <summary>
        /// Добавляет узел в коллекцию и автоматически строит иерархию
        /// </summary>
        /// <param name="node">Узел для добавления</param>
        public void Add(ITreeRedbObject node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            
            // Проверяем, не добавлен ли уже этот узел
            if (_nodesById.ContainsKey(node.Id))
            {
                throw new InvalidOperationException($"Узел с ID {node.Id} уже добавлен в коллекцию");
            }

            _nodesById[node.Id] = node;

            // Если это корневой узел
            if (node.IsRoot)
            {
                _roots.Add(node);
            }
            else
            {
                // Ищем родителя
                if (_nodesById.TryGetValue(node.ParentId!.Value, out var parent))
                {
                    // Родитель найден - устанавливаем связи
                    ConnectChild(parent, node);
                    
                    // Удаляем из списка сирот, если был там
                    _orphans.Remove(node);
                }
                else
                {
                    // Родитель не найден - добавляем в список сирот
                    _orphans.Add(node);
                }
            }

            // Проверяем, не стал ли этот узел родителем для каких-либо сирот
            CheckForOrphansToAdopt(node);
        }

        /// <summary>
        /// Добавляет диапазон узлов
        /// </summary>
        /// <param name="nodes">Узлы для добавления</param>
        public void AddRange(IEnumerable<ITreeRedbObject> nodes)
        {
            foreach (var node in nodes)
            {
                Add(node);
            }
        }

        /// <summary>
        /// Ищет узел по ID
        /// </summary>
        /// <param name="id">ID узла</param>
        /// <returns>Найденный узел или null</returns>
        public ITreeRedbObject? FindById(long id)
        {
            return _nodesById.GetValueOrDefault(id);
        }

        /// <summary>
        /// Ищет узлы по предикату
        /// </summary>
        /// <param name="predicate">Условие поиска</param>
        /// <returns>Коллекция найденных узлов</returns>
        public IEnumerable<ITreeRedbObject> FindNodes(Func<ITreeRedbObject, bool> predicate)
        {
            return _nodesById.Values.Where(predicate);
        }

        /// <summary>
        /// Удаляет узел из коллекции
        /// </summary>
        /// <param name="id">ID узла для удаления</param>
        /// <returns>true, если узел был удален</returns>
        public bool Remove(long id)
        {
            if (!_nodesById.TryGetValue(id, out var node))
                return false;

            // Удаляем узел из структуры
            if (node.IsRoot)
            {
                _roots.Remove(node);
            }
            else if (node.Parent != null)
            {
                node.Parent.Children.Remove(node);
            }

            // Делаем детей сиротами
            foreach (var child in node.Children.ToList())
            {
                child.Parent = null;
                _orphans.Add(child);
            }

            // Удаляем из всех коллекций
            _nodesById.Remove(id);
            _orphans.Remove(node);

            return true;
        }

        /// <summary>
        /// Очищает коллекцию
        /// </summary>
        public void Clear()
        {
            _nodesById.Clear();
            _roots.Clear();
            _orphans.Clear();
        }

        /// <summary>
        /// Проверяет, содержится ли узел с указанным ID
        /// </summary>
        /// <param name="id">ID узла</param>
        /// <returns>true, если узел содержится в коллекции</returns>
        public bool Contains(long id)
        {
            return _nodesById.ContainsKey(id);
        }

        /// <summary>
        /// Получает статистику коллекции
        /// </summary>
        /// <returns>Объект со статистикой</returns>
        public TreeCollectionStats GetStats()
        {
            var allNodes = _nodesById.Values;
            var maxDepth = _roots.Any() ? _roots.Max(root => root.MaxDepth) : 0;
            var totalNodes = allNodes.Count();

            return new TreeCollectionStats
            {
                TotalNodes = totalNodes,
                RootNodes = _roots.Count,
                LeafNodes = Leaves.Count(),
                OrphanNodes = _orphans.Count,
                MaxDepth = maxDepth,
                AverageChildrenPerNode = totalNodes > 0 ? allNodes.Average(n => n.Children.Count) : 0
            };
        }

        /// <summary>
        /// Получает плоский список всех узлов с указанием уровней
        /// </summary>
        /// <returns>Список пар (узел, уровень)</returns>
        public IEnumerable<(ITreeRedbObject Node, int Level)> GetFlattenedWithLevels()
        {
            var result = new List<(ITreeRedbObject, int)>();

            foreach (var root in _roots)
            {
                foreach (var node in root.GetSubtree())
                {
                    var level = node.Level;
                    result.Add((node, level));
                }
            }

            // Добавляем сирот с уровнем -1 (неопределенный)
            foreach (var orphan in _orphans)
            {
                result.Add((orphan, -1));
            }

            return result;
        }

        /// <summary>
        /// Проверяет целостность дерева
        /// </summary>
        /// <returns>Список найденных проблем</returns>
        public IEnumerable<string> ValidateIntegrity()
        {
            var issues = new List<string>();

            foreach (var node in _nodesById.Values)
            {
                // Проверяем, что ParentId соответствует Parent
                if (node.ParentId.HasValue)
                {
                    if (node.Parent == null)
                    {
                        issues.Add($"Узел {node.Id} имеет ParentId={node.ParentId}, но Parent=null");
                    }
                    else if (node.Parent.Id != node.ParentId.Value)
                    {
                        issues.Add($"Узел {node.Id}: ParentId={node.ParentId} не соответствует Parent.Id={node.Parent.Id}");
                    }
                }

                // Проверяем, что все дети имеют правильную обратную ссылку
                foreach (var child in node.Children)
                {
                    if (child.Parent != node)
                    {
                        issues.Add($"Дочерний узел {child.Id} не ссылается на родителя {node.Id}");
                    }
                    if (child.ParentId != node.Id)
                    {
                        issues.Add($"Дочерний узел {child.Id}: ParentId={child.ParentId} не соответствует родителю {node.Id}");
                    }
                }
            }

            return issues;
        }

        #region Private Methods

        /// <summary>
        /// Устанавливает связь родитель-ребенок
        /// </summary>
        private void ConnectChild(ITreeRedbObject parent, ITreeRedbObject child)
        {
            child.Parent = parent;
            if (!parent.Children.Contains(child))
            {
                parent.Children.Add(child);
            }
        }

        /// <summary>
        /// Проверяет, не стал ли узел родителем для каких-либо сирот
        /// </summary>
        private void CheckForOrphansToAdopt(ITreeRedbObject potentialParent)
        {
            var childrenToAdopt = _orphans.Where(orphan => orphan.ParentId == potentialParent.Id).ToList();
            
            foreach (var child in childrenToAdopt)
            {
                ConnectChild(potentialParent, child);
                _orphans.Remove(child);
            }
        }

        #endregion

        #region IEnumerable Implementation

        public IEnumerator<ITreeRedbObject> GetEnumerator()
        {
            return _nodesById.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    /// <summary>
    /// Типизированная версия коллекции для обратной совместимости
    /// Обертка над полиморфной TreeCollection с типобезопасностью
    /// </summary>
    /// <typeparam name="TProps">Тип свойств объектов</typeparam>
    public class TreeCollection<TProps> : IEnumerable<ITreeRedbObject<TProps>> where TProps : class, new()
    {
        private readonly TreeCollection _baseCollection = new();

        /// <summary>
        /// Получает количество узлов в коллекции
        /// </summary>
        public int Count => _baseCollection.Count;

        /// <summary>
        /// Получает все корневые узлы (без родителей)
        /// </summary>
        public IEnumerable<ITreeRedbObject<TProps>> Roots => _baseCollection.Roots.Cast<ITreeRedbObject<TProps>>();

        /// <summary>
        /// Получает все листовые узлы (без детей)
        /// </summary>
        public IEnumerable<ITreeRedbObject<TProps>> Leaves => _baseCollection.Leaves.Cast<ITreeRedbObject<TProps>>();

        /// <summary>
        /// Получает узлы-сироты (родители которых не добавлены в коллекцию)
        /// </summary>
        public IEnumerable<ITreeRedbObject<TProps>> Orphans => _baseCollection.Orphans.Cast<ITreeRedbObject<TProps>>();

        /// <summary>
        /// Добавляет типизированный узел в коллекцию
        /// </summary>
        public void Add(ITreeRedbObject<TProps> node)
        {
            _baseCollection.Add(node);
        }

        /// <summary>
        /// Добавляет диапазон типизированных узлов
        /// </summary>
        public void AddRange(IEnumerable<ITreeRedbObject<TProps>> nodes)
        {
            _baseCollection.AddRange(nodes.Cast<ITreeRedbObject>());
        }

        /// <summary>
        /// Ищет типизированный узел по ID
        /// </summary>
        public ITreeRedbObject<TProps>? FindById(long id)
        {
            return _baseCollection.FindById(id) as ITreeRedbObject<TProps>;
        }

        /// <summary>
        /// Ищет типизированные узлы по предикату
        /// </summary>
        public IEnumerable<ITreeRedbObject<TProps>> FindNodes(Func<ITreeRedbObject<TProps>, bool> predicate)
        {
            return _baseCollection.FindNodes(node => node is ITreeRedbObject<TProps> typed && predicate(typed))
                .Cast<ITreeRedbObject<TProps>>();
        }

        /// <summary>
        /// Удаляет узел из коллекции
        /// </summary>
        public bool Remove(long id) => _baseCollection.Remove(id);

        /// <summary>
        /// Очищает коллекцию
        /// </summary>
        public void Clear() => _baseCollection.Clear();

        /// <summary>
        /// Проверяет, содержится ли узел с указанным ID
        /// </summary>
        public bool Contains(long id) => _baseCollection.Contains(id);

        /// <summary>
        /// Получает статистику коллекции
        /// </summary>
        public TreeCollectionStats GetStats() => _baseCollection.GetStats();

        /// <summary>
        /// Получает плоский список всех типизированных узлов с указанием уровней
        /// </summary>
        public IEnumerable<(ITreeRedbObject<TProps> Node, int Level)> GetFlattenedWithLevels()
        {
            return _baseCollection.GetFlattenedWithLevels()
                .Where(item => item.Node is ITreeRedbObject<TProps>)
                .Select(item => ((ITreeRedbObject<TProps>)item.Node, item.Level));
        }

        /// <summary>
        /// Проверяет целостность дерева
        /// </summary>
        public IEnumerable<string> ValidateIntegrity() => _baseCollection.ValidateIntegrity();

        public IEnumerator<ITreeRedbObject<TProps>> GetEnumerator()
        {
            return _baseCollection.Cast<ITreeRedbObject<TProps>>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Статистика коллекции дерева
    /// </summary>
    public class TreeCollectionStats
    {
        public int TotalNodes { get; set; }
        public int RootNodes { get; set; }
        public int LeafNodes { get; set; }
        public int OrphanNodes { get; set; }
        public int MaxDepth { get; set; }
        public double AverageChildrenPerNode { get; set; }

        public override string ToString()
        {
            return $"Nodes: {TotalNodes}, Roots: {RootNodes}, Leaves: {LeafNodes}, Orphans: {OrphanNodes}, MaxDepth: {MaxDepth}, AvgChildren: {AverageChildrenPerNode:F1}";
        }
    }
}
