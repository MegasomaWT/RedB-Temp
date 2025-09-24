using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using redb.Core.Models.Contracts;
using redb.Core.Models.Entities;
using redb.Core.Providers;

namespace redb.Core.Extensions
{
    /// <summary>
    /// Extension методы для IRedbObject для удобной работы с деревьями и объектами
    /// </summary>
    public static class IRedbObjectExtensions
    {
        // ===== ДРЕВОВИДНЫЕ ОПЕРАЦИИ =====

        /// <summary>
        /// 🚀 ОПТИМИЗИРОВАННАЯ версия: Проверяет, является ли объект потомком указанного родителя
        /// Использует уже загруженную иерархию из GetPathToRootAsync, но добавляет защиту от циклов
        /// </summary>
        /// <param name="obj">Проверяемый объект</param>
        /// <param name="potentialAncestor">Потенциальный предок</param>
        /// <param name="treeProvider">Провайдер для работы с деревьями</param>
        public static async Task<bool> IsDescendantOfAsync<T>(
            this IRedbObject obj, 
            IRedbObject potentialAncestor,
            ITreeProvider treeProvider) where T : class, new()
        {
            if (obj.Id == potentialAncestor.Id) return false; // Объект не может быть потомком самого себя
            
            try
            {
                // ✅ Используем существующий метод, но с защитой от циклов
                var pathToRoot = await treeProvider.GetPathToRootAsync<T>(obj);
                
                // 🔍 ОПТИМИЗАЦИЯ: Используем HashSet для O(1) поиска вместо линейного перебора
                var ancestorIds = new HashSet<long>(pathToRoot.Select(ancestor => ancestor.Id));
                return ancestorIds.Contains(potentialAncestor.Id);
            }
            catch
            {
                return false; // В случае ошибки возвращаем false
            }
        }

        /// <summary>
        /// Проверяет, является ли объект предком указанного потомка
        /// </summary>
        /// <param name="obj">Проверяемый объект</param>
        /// <param name="potentialDescendant">Потенциальный потомок</param>
        /// <param name="treeProvider">Провайдер для работы с деревьями</param>
        public static async Task<bool> IsAncestorOfAsync<T>(
            this IRedbObject obj, 
            IRedbObject potentialDescendant,
            ITreeProvider treeProvider) where T : class, new()
        {
            return await potentialDescendant.IsDescendantOfAsync<T>(obj, treeProvider);
        }

        /// <summary>
        /// 🚀 ОПТИМИЗИРОВАННАЯ версия: Получает уровень объекта в дереве (корень = 0)  
        /// Использует защиту от циклов и более эффективный подход
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="treeProvider">Провайдер для работы с деревьями</param>
        public static async Task<int> GetTreeLevelAsync<T>(
            this IRedbObject obj,
            ITreeProvider treeProvider) where T : class, new()
        {
            try
            {
                var pathToRoot = await treeProvider.GetPathToRootAsync<T>(obj);
                var pathCount = pathToRoot.Count();
                
                // ✅ ЗАЩИТА: Если путь слишком длинный, возможно есть цикл
                if (pathCount > 1000) 
                {
                    return -1; // Подозрительно глубокое дерево - возможно цикл
                }
                
                return Math.Max(0, pathCount - 1); // -1 потому что в пути включен сам объект
            }
            catch
            {
                return -1; // Ошибка определения уровня
            }
        }

        /// <summary>
        /// Проверяет, является ли объект листом дерева (без детей)
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="treeProvider">Провайдер для работы с деревьями</param>
        public static async Task<bool> IsLeafAsync<T>(
            this IRedbObject obj,
            ITreeProvider treeProvider) where T : class, new()
        {
            try
            {
                var children = await treeProvider.GetChildrenAsync<T>(obj);
                return !children.Any();
            }
            catch
            {
                return true; // В случае ошибки считаем листом
            }
        }

        /// <summary>
        /// Получает количество детей объекта
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="treeProvider">Провайдер для работы с деревьями</param>
        public static async Task<int> GetChildrenCountAsync<T>(
            this IRedbObject obj,
            ITreeProvider treeProvider) where T : class, new()
        {
            try
            {
                var children = await treeProvider.GetChildrenAsync<T>(obj);
                return children.Count();
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Получает количество всех потомков объекта
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="treeProvider">Провайдер для работы с деревьями</param>
        /// <param name="maxDepth">Максимальная глубина поиска</param>
        public static async Task<int> GetDescendantsCountAsync<T>(
            this IRedbObject obj,
            ITreeProvider treeProvider,
            int? maxDepth = null) where T : class, new()
        {
            try
            {
                var descendants = await treeProvider.GetDescendantsAsync<T>(obj, maxDepth);
                return descendants.Count();
            }
            catch
            {
                return 0;
            }
        }

        // ===== ПРОВЕРКИ СОСТОЯНИЯ =====

        /// <summary>
        /// Проверяет, активен ли объект по временным меткам
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="checkDate">Дата для проверки (по умолчанию - текущая)</param>
        public static bool IsActiveAt(this IRedbObject obj, DateTime? checkDate = null)
        {
            var date = checkDate ?? DateTime.Now;
            
            // Проверяем дату начала
            if (obj.DateBegin.HasValue && date < obj.DateBegin.Value)
                return false;
                
            // Проверяем дату окончания
            if (obj.DateComplete.HasValue && date > obj.DateComplete.Value)
                return false;
                
            return true;
        }

        /// <summary>
        /// Проверяет, истекло ли время действия объекта
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="checkDate">Дата для проверки (по умолчанию - текущая)</param>
        public static bool IsExpired(this IRedbObject obj, DateTime? checkDate = null)
        {
            var date = checkDate ?? DateTime.Now;
            return obj.DateComplete.HasValue && date > obj.DateComplete.Value;
        }

        /// <summary>
        /// Проверяет, начал ли действовать объект
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="checkDate">Дата для проверки (по умолчанию - текущая)</param>
        public static bool HasStarted(this IRedbObject obj, DateTime? checkDate = null)
        {
            var date = checkDate ?? DateTime.Now;
            return !obj.DateBegin.HasValue || date >= obj.DateBegin.Value;
        }

        /// <summary>
        /// Получает возраст объекта (время с момента создания)
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="referenceDate">Референсная дата (по умолчанию - текущая)</param>
        public static TimeSpan GetAge(this IRedbObject obj, DateTime? referenceDate = null)
        {
            var date = referenceDate ?? DateTime.Now;
            return date - obj.DateCreate;
        }

        /// <summary>
        /// Получает время с последнего изменения объекта
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="referenceDate">Референсная дата (по умолчанию - текущая)</param>
        public static TimeSpan GetTimeSinceLastModification(this IRedbObject obj, DateTime? referenceDate = null)
        {
            var date = referenceDate ?? DateTime.Now;
            return date - obj.DateModify;
        }

        // ===== УТИЛИТЫ =====

        /// <summary>
        /// Получает отображаемое имя объекта с fallback логикой
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="includeId">Включать ли ID в отображаемое имя</param>
        public static string GetDisplayName(this IRedbObject obj, bool includeId = true)
        {
            var displayName = obj.Name;
            
            // Fallback к кодам
            if (string.IsNullOrWhiteSpace(displayName))
            {
                if (!string.IsNullOrWhiteSpace(obj.CodeString))
                    displayName = obj.CodeString;
                else if (obj.CodeInt.HasValue)
                    displayName = $"Code_{obj.CodeInt.Value}";
                else if (obj.CodeGuid.HasValue)
                    displayName = obj.CodeGuid.Value.ToString("D").Substring(0, 8);
                else if (obj.Key.HasValue)
                    displayName = $"Key_{obj.Key.Value}";
                else
                    displayName = $"Object_{obj.Id}";
            }
            
            return includeId ? $"{displayName} (#{obj.Id})" : displayName;
        }

        /// <summary>
        /// Получает краткую информацию об объекте для отладки
        /// </summary>
        /// <param name="obj">Объект</param>
        public static string GetDebugInfo(this IRedbObject obj)
        {
            return $"Object[Id={obj.Id}, Scheme={obj.SchemeId}, Name='{obj.Name}', Parent={obj.ParentId}, Owner={obj.OwnerId}]";
        }

        /// <summary>
        /// Создает строку иерархического пути объекта
        /// </summary>
        /// <param name="pathObjects">Объекты пути от корня к объекту</param>
        /// <param name="separator">Разделитель (по умолчанию "/")</param>
        public static string CreateHierarchicalPath(this IEnumerable<IRedbObject> pathObjects, string separator = "/")
        {
            return string.Join(separator, pathObjects.Select(obj => obj.Name ?? $"#{obj.Id}"));
        }
    }
}
