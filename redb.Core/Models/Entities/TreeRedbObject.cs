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
    /// Типизированная версия древовидного объекта REDB для обратной совместимости
    /// Расширяет базовый TreeRedbObject добавляя типизированные свойства и навигацию
    /// </summary>
    /// <typeparam name="TProps">Тип свойств объекта</typeparam>
    public class TreeRedbObject<TProps> : TreeRedbObject, ITreeRedbObject<TProps>
        where TProps : class, new()
    {
        /// <summary>
        /// Типизированные свойства объекта
        /// </summary>
        public TProps properties { get; set; } = new TProps();

        /// <summary>
        /// Типизированная ссылка на родительский объект
        /// </summary>
        public new ITreeRedbObject<TProps>? Parent { get; set; }
        
        /// <summary>
        /// Типизированная коллекция дочерних объектов
        /// </summary>
        public new ICollection<ITreeRedbObject<TProps>> Children { get; set; } = new List<ITreeRedbObject<TProps>>();

        /// <summary>
        /// Типизированная версия получения поддерева
        /// </summary>
        public new IEnumerable<ITreeRedbObject<TProps>> GetSubtree()
        {
            yield return this;
            
            foreach (var child in Children)
            {
                foreach (var node in child.GetSubtree())
                    yield return node;
            }
        }
        
        /// <summary>
        /// Типизированная версия получения предков
        /// </summary>
        public new IEnumerable<ITreeRedbObject<TProps>> Ancestors
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
        /// Типизированная версия получения потомков
        /// </summary>
        public new IEnumerable<ITreeRedbObject<TProps>> Descendants
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
        
        // ===== ТИПИЗИРОВАННЫЕ МЕТОДЫ КЕША И МЕТАДАННЫХ =====
        
        /// <summary>
        /// Получить схему для типа TProps (с использованием кеша атрибутов)
        /// </summary>
        public async Task<IRedbScheme> GetSchemeForTypeAsync()
        {
            if (GetSchemeSyncProvider() == null)
                throw new InvalidOperationException("SchemeSyncProvider не установлен. Используйте RedbObject.SetSchemeSyncProvider().");
                
            return await GetSchemeSyncProvider()!.EnsureSchemeFromTypeAsync<TProps>();
        }
        
        /// <summary>
        /// Получить структуры схемы для типа TProps (с использованием кеша)
        /// </summary>
        public async Task<IReadOnlyCollection<IRedbStructure>> GetStructuresForTypeAsync()
        {
            var scheme = await GetSchemeForTypeAsync();
            return scheme.Structures;
        }
        
        /// <summary>
        /// Пересчитать хеш на основе текущих свойств типа TProps
        /// </summary>
        public void RecomputeHashForType()
        {
            RecomputeHash(); // Используем существующую реализацию
        }
        
        /// <summary>
        /// Получить новый хеш на основе текущих свойств без изменения объекта
        /// </summary>
        public Guid ComputeHashForType()
        {
            return ComputeHash(); // Используем существующую реализацию
        }
        
        /// <summary>
        /// Проверить, соответствует ли текущий хеш свойствам типа TProps
        /// </summary>
        public bool IsHashValidForType()
        {
            if (!hash.HasValue)
                return false;
                
            var computedHash = ComputeHashForType();
            return hash.Value == computedHash;
        }
        
        /// <summary>
        /// Создать копию объекта с теми же метаданными но новыми свойствами
        /// </summary>
        public IRedbObject<TProps> CloneWithProperties(TProps newProperties)
        {
            return new TreeRedbObject<TProps>
            {
                properties = newProperties,
                // Копируем все метаданные кроме ID (чтобы создать новый объект)
                parent_id = this.parent_id,
                scheme_id = this.scheme_id,
                owner_id = this.owner_id,
                who_change_id = this.who_change_id,
                date_create = DateTime.Now, // Новое время создания
                date_modify = DateTime.Now, // Новое время изменения
                date_begin = this.date_begin,
                date_complete = this.date_complete,
                key = this.key,
                code_int = this.code_int,
                code_string = this.code_string,
                code_guid = this.code_guid,
                name = this.name,
                note = this.note,
                @bool = this.@bool,
                // hash будет пересчитан автоматически при сохранении
            };
        }
        
        /// <summary>
        /// Инвалидировать кеш схемы для типа TProps
        /// </summary>
        public void InvalidateCacheForType()
        {
            // Для TreeRedbObject кеш инвалидируется через базовый механизм
            // Конкретная реализация зависит от того, как организован кеш в системе
        }
        
        /// <summary>
        /// Прогреть кеш схемы для типа TProps асинхронно
        /// </summary>
        public async Task WarmupCacheForTypeAsync()
        {
            // Для TreeRedbObject кеш прогревается через базовый механизм
            // Получение схемы автоматически прогреет кеш
            await GetSchemeForTypeAsync();
        }

        // Синхронизация с базовым классом для полиморфной работы
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
    }
}
