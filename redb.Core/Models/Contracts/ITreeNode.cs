using System;
using System.Collections.Generic;
using System.Linq;

namespace redb.Core.Models.Contracts
{
    /// <summary>
    /// Интерфейс для древовидных узлов с навигационными свойствами и операциями обхода
    /// </summary>
    /// <typeparam name="T">Тип узла дерева</typeparam>
    public interface ITreeNode<T> where T : class, ITreeNode<T>
    {
        /// <summary>
        /// Уникальный идентификатор узла
        /// </summary>
        long Id { get; set; }
        
        /// <summary>
        /// Идентификатор родительского узла (null для корневых узлов)
        /// </summary>
        long? ParentId { get; set; }
        
        /// <summary>
        /// Ссылка на родительский узел (заполняется при загрузке дерева)
        /// </summary>
        T? Parent { get; set; }
        
        /// <summary>
        /// Коллекция дочерних узлов (заполняется при загрузке дерева)
        /// </summary>
        ICollection<T> Children { get; set; }
        
        /// <summary>
        /// Проверяет, является ли узел корневым (без родителя)
        /// </summary>
        bool IsRoot => ParentId == null;
        
        /// <summary>
        /// Проверяет, является ли узел листом (без детей)
        /// </summary>
        bool IsLeaf => !Children.Any();
        
        /// <summary>
        /// Получает уровень узла в дереве (0 для корня)
        /// Требует загруженную иерархию до корня
        /// </summary>
        int Level
        {
            get
            {
                int level = 0;
                var current = Parent;
                while (current != null)
                {
                    level++;
                    current = current.Parent;
                }
                return level;
            }
        }
        
        /// <summary>
        /// Получает всех предков узла (от родителя к корню)
        /// Требует загруженную иерархию до корня
        /// </summary>
        IEnumerable<T> Ancestors
        {
            get
            {
                var current = Parent;
                while (current != null)
                {
                    yield return current;
                    current = current.Parent;
                }
            }
        }
        
        /// <summary>
        /// Получает всех потомков узла рекурсивно
        /// Требует загруженную иерархию вниз
        /// </summary>
        IEnumerable<T> Descendants
        {
            get
            {
                foreach (var child in Children)
                {
                    yield return child;
                    foreach (var descendant in child.Descendants)
                        yield return descendant;
                }
            }
        }
    }
}
