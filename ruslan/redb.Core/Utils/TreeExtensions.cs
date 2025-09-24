using System;
using System.Collections.Generic;
using System.Linq;
using redb.Core.Models.Contracts;
using redb.Core.Models.Entities;

namespace redb.Core.Utils
{
    /// <summary>
    /// Расширения для работы с древовидными структурами
    /// </summary>
    public static class TreeExtensions
    {
        /// <summary>
        /// Обход дерева в ширину (Breadth-First Search) для полиморфных деревьев
        /// </summary>
        /// <param name="root">Корневой узел для обхода</param>
        /// <returns>Последовательность узлов в порядке BFS</returns>
        public static IEnumerable<ITreeRedbObject> BreadthFirstTraversal(this ITreeRedbObject root)
        {
            var queue = new Queue<ITreeRedbObject>();
            queue.Enqueue(root);
            
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                yield return current;
                
                foreach (var child in current.Children)
                    queue.Enqueue(child);
            }
        }

        /// <summary>
        /// Обход дерева в ширину (Breadth-First Search) для типизированных деревьев
        /// </summary>
        /// <typeparam name="TProps">Тип свойств объекта</typeparam>
        /// <param name="root">Корневой узел для обхода</param>
        /// <returns>Последовательность узлов в порядке BFS</returns>
        public static IEnumerable<ITreeRedbObject<TProps>> BreadthFirstTraversal<TProps>(this ITreeRedbObject<TProps> root)
            where TProps : class, new()
        {
            var queue = new Queue<ITreeRedbObject<TProps>>();
            queue.Enqueue(root);
            
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                yield return current;
                
                foreach (var child in current.Children)
                    queue.Enqueue(child);
            }
        }
        
        /// <summary>
        /// Обход дерева в глубину (Depth-First Search) - pre-order для полиморфных деревьев
        /// </summary>
        /// <param name="root">Корневой узел для обхода</param>
        /// <returns>Последовательность узлов в порядке DFS pre-order</returns>
        public static IEnumerable<ITreeRedbObject> DepthFirstTraversal(this ITreeRedbObject root)
        {
            yield return root;
            
            foreach (var child in root.Children)
            {
                foreach (var descendant in child.DepthFirstTraversal())
                    yield return descendant;
            }
        }

        /// <summary>
        /// Обход дерева в глубину (Depth-First Search) - pre-order для типизированных деревьев
        /// </summary>
        /// <typeparam name="TProps">Тип свойств объекта</typeparam>
        /// <param name="root">Корневой узел для обхода</param>
        /// <returns>Последовательность узлов в порядке DFS pre-order</returns>
        public static IEnumerable<ITreeRedbObject<TProps>> DepthFirstTraversal<TProps>(this ITreeRedbObject<TProps> root)
            where TProps : class, new()
        {
            yield return root;
            
            foreach (var child in root.Children)
            {
                foreach (var descendant in child.DepthFirstTraversal())
                    yield return descendant;
            }
        }
        
        /// <summary>
        /// Обход дерева в глубину (Depth-First Search) - post-order для полиморфных деревьев
        /// </summary>
        /// <param name="root">Корневой узел для обхода</param>
        /// <returns>Последовательность узлов в порядке DFS post-order</returns>
        public static IEnumerable<ITreeRedbObject> PostOrderTraversal(this ITreeRedbObject root)
        {
            foreach (var child in root.Children)
            {
                foreach (var descendant in child.PostOrderTraversal())
                    yield return descendant;
            }
            
            yield return root;
        }

        /// <summary>
        /// Обход дерева в глубину (Depth-First Search) - post-order для типизированных деревьев
        /// </summary>
        /// <typeparam name="TProps">Тип свойств объекта</typeparam>
        /// <param name="root">Корневой узел для обхода</param>
        /// <returns>Последовательность узлов в порядке DFS post-order</returns>
        public static IEnumerable<ITreeRedbObject<TProps>> PostOrderTraversal<TProps>(this ITreeRedbObject<TProps> root)
            where TProps : class, new()
        {
            foreach (var child in root.Children)
            {
                foreach (var descendant in child.PostOrderTraversal())
                    yield return descendant;
            }
            
            yield return root;
        }
        
        /// <summary>
        /// Поиск узла по ID в полиморфном дереве
        /// </summary>
        /// <param name="root">Корневой узел для поиска</param>
        /// <param name="id">ID искомого узла</param>
        /// <returns>Найденный узел или null</returns>
        public static ITreeRedbObject? FindById(this ITreeRedbObject root, long id)
        {
            return root.DepthFirstTraversal().FirstOrDefault(node => node.Id == id);
        }

        /// <summary>
        /// Поиск узла по ID в типизированном дереве
        /// </summary>
        /// <typeparam name="TProps">Тип свойств объекта</typeparam>
        /// <param name="root">Корневой узел для поиска</param>
        /// <param name="id">ID искомого узла</param>
        /// <returns>Найденный узел или null</returns>
        public static ITreeRedbObject<TProps>? FindById<TProps>(this ITreeRedbObject<TProps> root, long id)
            where TProps : class, new()
        {
            return root.DepthFirstTraversal().FirstOrDefault(node => node.Id == id);
        }
        
        /// <summary>
        /// Поиск узлов по предикату в полиморфном дереве
        /// </summary>
        /// <param name="root">Корневой узел для поиска</param>
        /// <param name="predicate">Условие поиска</param>
        /// <returns>Коллекция найденных узлов</returns>
        public static IEnumerable<ITreeRedbObject> FindNodes(this ITreeRedbObject root, 
            Func<ITreeRedbObject, bool> predicate)
        {
            return root.DepthFirstTraversal().Where(predicate);
        }

        /// <summary>
        /// Поиск узлов по предикату в типизированном дереве
        /// </summary>
        /// <typeparam name="TProps">Тип свойств объекта</typeparam>
        /// <param name="root">Корневой узел для поиска</param>
        /// <param name="predicate">Условие поиска</param>
        /// <returns>Коллекция найденных узлов</returns>
        public static IEnumerable<ITreeRedbObject<TProps>> FindNodes<TProps>(this ITreeRedbObject<TProps> root, 
            Func<ITreeRedbObject<TProps>, bool> predicate)
            where TProps : class, new()
        {
            return root.DepthFirstTraversal().Where(predicate);
        }
        
        /// <summary>
        /// Получает все листовые узлы полиморфного дерева
        /// </summary>
        /// <param name="root">Корневой узел</param>
        /// <returns>Коллекция листовых узлов</returns>
        public static IEnumerable<ITreeRedbObject> GetLeaves(this ITreeRedbObject root)
        {
            return root.DepthFirstTraversal().Where(node => node.IsLeaf);
        }

        /// <summary>
        /// Получает все листовые узлы типизированного дерева
        /// </summary>
        /// <typeparam name="TProps">Тип свойств объекта</typeparam>
        /// <param name="root">Корневой узел</param>
        /// <returns>Коллекция листовых узлов</returns>
        public static IEnumerable<ITreeRedbObject<TProps>> GetLeaves<TProps>(this ITreeRedbObject<TProps> root)
            where TProps : class, new()
        {
            return root.DepthFirstTraversal().Where(node => node.IsLeaf);
        }
        
        /// <summary>
        /// Получает все узлы определенного уровня в полиморфном дереве
        /// </summary>
        /// <param name="root">Корневой узел</param>
        /// <param name="level">Уровень (0 для корня)</param>
        /// <returns>Коллекция узлов указанного уровня</returns>
        public static IEnumerable<ITreeRedbObject> GetNodesAtLevel(this ITreeRedbObject root, int level)
        {
            return root.DepthFirstTraversal().Where(node => node.Level == level);
        }

        /// <summary>
        /// Получает все узлы определенного уровня в типизированном дереве
        /// </summary>
        /// <typeparam name="TProps">Тип свойств объекта</typeparam>
        /// <param name="root">Корневой узел</param>
        /// <param name="level">Уровень (0 для корня)</param>
        /// <returns>Коллекция узлов указанного уровня</returns>
        public static IEnumerable<ITreeRedbObject<TProps>> GetNodesAtLevel<TProps>(this ITreeRedbObject<TProps> root, int level)
            where TProps : class, new()
        {
            return root.DepthFirstTraversal().Where(node => node.Level == level);
        }
        
        /// <summary>
        /// Строит материализованный путь для полиморфного узла
        /// </summary>
        /// <param name="node">Узел</param>
        /// <param name="separator">Разделитель в пути</param>
        /// <returns>Материализованный путь вида "/1/5/23"</returns>
        public static string GetMaterializedPath(this ITreeRedbObject node, string separator = "/")
        {
            var pathIds = node.GetPathIds();
            return separator + string.Join(separator, pathIds);
        }

        /// <summary>
        /// Строит материализованный путь для типизированного узла
        /// </summary>
        /// <typeparam name="TProps">Тип свойств объекта</typeparam>
        /// <param name="node">Узел</param>
        /// <param name="separator">Разделитель в пути</param>
        /// <returns>Материализованный путь вида "/1/5/23"</returns>
        public static string GetMaterializedPath<TProps>(this ITreeRedbObject<TProps> node, string separator = "/")
            where TProps : class, new()
        {
            var pathIds = node.GetPathIds();
            return separator + string.Join(separator, pathIds);
        }
        
        /// <summary>
        /// Преобразует полиморфное дерево в плоский список с указанием уровней
        /// </summary>
        /// <param name="root">Корневой узел</param>
        /// <returns>Список пар (узел, уровень)</returns>
        public static IEnumerable<(ITreeRedbObject Node, int Level)> FlattenWithLevels(this ITreeRedbObject root)
        {
            return root.DepthFirstTraversal().Select(node => (node, node.Level));
        }

        /// <summary>
        /// Преобразует типизированное дерево в плоский список с указанием уровней
        /// </summary>
        /// <typeparam name="TProps">Тип свойств объекта</typeparam>
        /// <param name="root">Корневой узел</param>
        /// <returns>Список пар (узел, уровень)</returns>
        public static IEnumerable<(ITreeRedbObject<TProps> Node, int Level)> FlattenWithLevels<TProps>(this ITreeRedbObject<TProps> root)
            where TProps : class, new()
        {
            return root.DepthFirstTraversal().Select(node => (node, node.Level));
        }
        
        /// <summary>
        /// Проверяет, является ли полиморфное дерево сбалансированным (разница в глубине поддеревьев не превышает 1)
        /// </summary>
        /// <param name="root">Корневой узел</param>
        /// <returns>true, если дерево сбалансировано</returns>
        public static bool IsBalanced(this ITreeRedbObject root)
        {
            return CheckBalance(root) != -1;
        }

        /// <summary>
        /// Проверяет, является ли типизированное дерево сбалансированным (разница в глубине поддеревьев не превышает 1)
        /// </summary>
        /// <typeparam name="TProps">Тип свойств объекта</typeparam>
        /// <param name="root">Корневой узел</param>
        /// <returns>true, если дерево сбалансировано</returns>
        public static bool IsBalanced<TProps>(this ITreeRedbObject<TProps> root)
            where TProps : class, new()
        {
            return CheckBalance(root) != -1;
        }
        
        private static int CheckBalance(ITreeRedbObject node)
        {
            if (!node.Children.Any())
                return 0;
            
            var childHeights = new List<int>();
            foreach (var child in node.Children)
            {
                var height = CheckBalance(child);
                if (height == -1) return -1; // Не сбалансировано
                childHeights.Add(height);
            }
            
            var maxHeight = childHeights.Max();
            var minHeight = childHeights.Min();
            
            return Math.Abs(maxHeight - minHeight) <= 1 ? maxHeight + 1 : -1;
        }
        
        private static int CheckBalance<TProps>(ITreeRedbObject<TProps> node)
            where TProps : class, new()
        {
            if (!node.Children.Any())
                return 0;
            
            var childHeights = new List<int>();
            foreach (var child in node.Children)
            {
                var height = CheckBalance(child);
                if (height == -1) return -1; // Не сбалансировано
                childHeights.Add(height);
            }
            
            var maxHeight = childHeights.Max();
            var minHeight = childHeights.Min();
            
            return Math.Abs(maxHeight - minHeight) <= 1 ? maxHeight + 1 : -1;
        }
    }
}
