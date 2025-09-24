using redb.Core.Models.Contracts;
using redb.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace redb.Core.Models.Entities
{
    /// <summary>
    /// Базовый класс для древовидных объектов REDB с навигацией по иерархии
    /// Поддерживает полиморфные деревья - объекты разных схем в одном дереве
    /// Расширяет RedbObject добавляя навигационные свойства и методы обхода
    /// </summary>
    public class TreeRedbObject : RedbObject, ITreeRedbObject
    {
        /// <summary>
        /// Ссылка на родительский объект (заполняется при загрузке дерева)
        /// </summary>
        public ITreeRedbObject? Parent { get; set; }
        
        /// <summary>
        /// Коллекция дочерних объектов (заполняется при загрузке дерева)
        /// </summary>
        public ICollection<ITreeRedbObject> Children { get; set; } = new List<ITreeRedbObject>();

        /// <summary>
        /// Проверяет, является ли узел листом (без детей)
        /// </summary>
        public bool IsLeaf => !Children.Any();
        
        /// <summary>
        /// Получает уровень узла в дереве (0 для корня)
        /// </summary>
        public int Level
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
        /// </summary>
        public IEnumerable<ITreeRedbObject> Ancestors
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
        /// </summary>
        public IEnumerable<ITreeRedbObject> Descendants
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

        /// <summary>
        /// Получает путь от корня к текущему узлу в виде ID
        /// </summary>
        public IEnumerable<long> GetPathIds()
        {
            var path = new List<long>();
            var current = this;
            
            while (current != null)
            {
                path.Insert(0, current.Id);
                current = current.Parent as TreeRedbObject;
            }
            
            return path;
        }
        
        /// <summary>
        /// Получает хлебные крошки (breadcrumbs) для навигации
        /// </summary>
        /// <param name="separator">Разделитель между элементами</param>
        /// <param name="includeIds">Включать ли ID в скобках</param>
        /// <returns>Строка вида "Root > Category > Subcategory"</returns>
        public string GetBreadcrumbs(string separator = " > ", bool includeIds = false)
        {
            var ancestors = Ancestors.Reverse().ToList();
            var path = ancestors.Append(this);
            
            var names = path.Select(node => 
            {
                var displayName = node.Name ?? $"Object {node.Id}";
                return includeIds ? $"{displayName} ({node.Id})" : displayName;
            });
            
            return string.Join(separator, names);
        }
        
        /// <summary>
        /// Проверяет, является ли текущий узел потомком указанного узла
        /// </summary>
        /// <param name="ancestor">Предполагаемый предок</param>
        /// <returns>true, если current является потомком ancestor</returns>
        public bool IsDescendantOf(ITreeRedbObject ancestor)
        {
            var current = Parent;
            while (current != null)
            {
                if (current.Id == ancestor.Id)
                    return true;
                current = current.Parent;
            }
            return false;
        }
        
        /// <summary>
        /// Проверяет, является ли текущий узел предком указанного узла
        /// </summary>
        /// <param name="descendant">Предполагаемый потомок</param>
        /// <returns>true, если current является предком descendant</returns>
        public bool IsAncestorOf(ITreeRedbObject descendant)
        {
            return descendant.IsDescendantOf(this);
        }
        
        /// <summary>
        /// Получает все узлы поддерева (включая текущий) в порядке обхода в глубину
        /// </summary>
        /// <returns>Последовательность узлов поддерева</returns>
        public IEnumerable<ITreeRedbObject> GetSubtree()
        {
            yield return this;
            
            foreach (var child in Children)
            {
                foreach (var node in child.GetSubtree())
                    yield return node;
            }
        }
        
        /// <summary>
        /// Получает количество узлов в поддереве (включая текущий)
        /// </summary>
        public int SubtreeSize => GetSubtree().Count();
        
        /// <summary>
        /// Получает максимальную глубину поддерева от текущего узла
        /// </summary>
        public int MaxDepth
        {
            get
            {
                if (!Children.Any())
                    return 0;
                
                return 1 + Children.Max(child => child.MaxDepth);
            }
        }
        
        /// <summary>
        /// Пересчитать MD5-хеш по значениям объекта и записать в поле hash
        /// </summary>
        public override void RecomputeHash()
        {
            hash = RedbHash.ComputeFor((IRedbObject)this);
        }

        /// <summary>
        /// Получить MD5-хеш по значениям объекта, не меняя поле hash
        /// </summary>
        public override Guid ComputeHash() => RedbHash.ComputeFor((IRedbObject)this) ?? Guid.Empty;
    }

    /// <summary>
    /// ✅ АРХИТЕКТУРНОЕ ИСПРАВЛЕНИЕ: Типизированная версия древовидного объекта REDB
    /// НОВОЕ НАСЛЕДОВАНИЕ: RedbObject<TProps> вместо TreeRedbObject
    /// ПРЕИМУЩЕСТВА: Прямое приведение типов, устранение дублирования properties, нет конверсии
    /// </summary>
    /// <typeparam name="TProps">Тип свойств объекта</typeparam>
    public class TreeRedbObject<TProps> : RedbObject<TProps>, ITreeRedbObject<TProps>
        where TProps : class, new()
    {
        // ✅ ИСПРАВЛЕНИЕ: properties наследуется от RedbObject<TProps> - дублирование устранено!

        /// <summary>
        /// ✅ TREE-СПЕЦИФИЧНЫЕ СВОЙСТВА (теперь не наследуются от TreeRedbObject)
        /// </summary>
        
        /// <summary>
        /// Типизированная ссылка на родительский объект
        /// </summary>
        public ITreeRedbObject<TProps>? Parent { get; set; }
        
        /// <summary>
        /// Типизированная коллекция дочерних объектов
        /// </summary>
        public ICollection<ITreeRedbObject<TProps>> Children { get; set; } = new List<ITreeRedbObject<TProps>>();

        /// <summary>
        /// ✅ TREE НАВИГАЦИОННЫЕ СВОЙСТВА (перенесены из базового TreeRedbObject)
        /// </summary>
        
        /// <summary>
        /// Проверяет, является ли узел листом (без детей)
        /// </summary>
        public bool IsLeaf => !Children.Any();
        
        /// <summary>
        /// Получает уровень узла в дереве (0 для корня)
        /// </summary>
        public int Level
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
        /// Типизированная версия получения поддерева
        /// </summary>
        public IEnumerable<ITreeRedbObject<TProps>> GetSubtree()
        {
            yield return this;
            
            foreach (var child in Children)
            {
                foreach (var node in child.GetSubtree())
                    yield return node;
            }
        }
        
        /// <summary>
        /// Получает всех предков узла (от родителя к корню)
        /// </summary>
        public IEnumerable<ITreeRedbObject<TProps>> Ancestors
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
        /// </summary>
        public IEnumerable<ITreeRedbObject<TProps>> Descendants
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

        /// <summary>
        /// ✅ МЕТОДЫ НАВИГАЦИИ (перенесены из базового TreeRedbObject)
        /// </summary>
        
        /// <summary>
        /// Получает путь от корня к текущему узлу в виде ID
        /// </summary>
        public IEnumerable<long> GetPathIds()
        {
            var path = new List<long>();
            var current = (ITreeRedbObject<TProps>?)this;
            
            while (current != null)
            {
                path.Insert(0, current.Id);
                current = current.Parent;
            }
            
            return path;
        }
        
        /// <summary>
        /// Получает хлебные крошки (breadcrumbs) для навигации
        /// </summary>
        public string GetBreadcrumbs(string separator = " > ", bool includeIds = false)
        {
            var ancestors = Ancestors.Reverse().ToList();
            var path = ancestors.Append(this);
            
            var names = path.Select(node => 
            {
                var displayName = node.Name ?? $"Object {node.Id}";
                return includeIds ? $"{displayName} ({node.Id})" : displayName;
            });
            
            return string.Join(separator, names);
        }
        
        /// <summary>
        /// Проверяет, является ли текущий узел потомком указанного узла
        /// </summary>
        public bool IsDescendantOf(ITreeRedbObject<TProps> ancestor)
        {
            var current = Parent;
            while (current != null)
            {
                if (current.Id == ancestor.Id)
                    return true;
                current = current.Parent;
            }
            return false;
        }
        
        /// <summary>
        /// Проверяет, является ли текущий узел предком указанного узла
        /// </summary>
        public bool IsAncestorOf(ITreeRedbObject<TProps> descendant)
        {
            return descendant.IsDescendantOf(this);
        }
        
        /// <summary>
        /// Получает количество узлов в поддереве (включая текущий)
        /// </summary>
        public int SubtreeSize => GetSubtree().Count();
        
        /// <summary>
        /// Получает максимальную глубину поддерева от текущего узла
        /// </summary>
        public int MaxDepth
        {
            get
            {
                if (!Children.Any())
                    return 0;
                
                return 1 + Children.Max(child => child.MaxDepth);
            }
        }
        
        // ✅ УБРАНО: Методы кеша и метаданных наследуются от RedbObject<TProps>
        // Дублирование устранено - используем базовую реализацию!

        // ✅ ИНТЕРФЕЙСНАЯ СОВМЕСТИМОСТЬ: Реализация ITreeRedbObject для полиморфной работы
        ITreeRedbObject? ITreeRedbObject.Parent 
        { 
            get => Parent; 
            set => Parent = value as ITreeRedbObject<TProps>; 
        }
        
        ICollection<ITreeRedbObject> ITreeRedbObject.Children 
        { 
            get => Children.Cast<ITreeRedbObject>().ToList(); 
            set => Children = value.Cast<ITreeRedbObject<TProps>>().ToList(); 
        }
        
        // ✅ TREE НАВИГАЦИОННЫЕ МЕТОДЫ ДЛЯ БАЗОВОГО ИНТЕРФЕЙСА
        IEnumerable<ITreeRedbObject> ITreeRedbObject.GetSubtree()
        {
            return GetSubtree().Cast<ITreeRedbObject>();
        }
        
        IEnumerable<ITreeRedbObject> ITreeRedbObject.Ancestors
        {
            get => Ancestors.Cast<ITreeRedbObject>();
        }
        
        IEnumerable<ITreeRedbObject> ITreeRedbObject.Descendants
        {
            get => Descendants.Cast<ITreeRedbObject>();
        }
        
        bool ITreeRedbObject.IsDescendantOf(ITreeRedbObject ancestor)
        {
            var current = Parent;
            while (current != null)
            {
                if (current.Id == ancestor.Id)
                    return true;
                current = current.Parent;
            }
            return false;
        }
        
        bool ITreeRedbObject.IsAncestorOf(ITreeRedbObject descendant)
        {
            return descendant.IsDescendantOf(this);
        }
        
        IEnumerable<long> ITreeRedbObject.GetPathIds()
        {
            return GetPathIds();
        }
        
        string ITreeRedbObject.GetBreadcrumbs(string separator, bool includeIds)
        {
            return GetBreadcrumbs(separator, includeIds);
        }
    }
}
