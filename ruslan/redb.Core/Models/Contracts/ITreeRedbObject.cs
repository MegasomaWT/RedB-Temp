using System;
using System.Collections.Generic;

namespace redb.Core.Models.Contracts
{
    /// <summary>
    /// Интерфейс для древовидных объектов REDB с навигационными свойствами
    /// Расширяет IRedbObject добавляя возможности навигации по дереву в памяти
    /// Поддерживает полиморфные деревья (объекты разных схем в одном дереве)
    /// </summary>
    public interface ITreeRedbObject : IRedbObject
    {
        /// <summary>
        /// Ссылка на родительский объект (заполняется при загрузке дерева)
        /// null для корневых узлов
        /// </summary>
        ITreeRedbObject? Parent { get; set; }
        
        /// <summary>
        /// Коллекция дочерних объектов (заполняется при загрузке дерева)
        /// Пустая коллекция для листовых узлов
        /// </summary>
        ICollection<ITreeRedbObject> Children { get; set; }
        
        /// <summary>
        /// Проверяет, является ли узел листом (без детей)
        /// </summary>
        bool IsLeaf { get; }
        
        /// <summary>
        /// Получает уровень узла в дереве (0 для корня)
        /// Требует загруженную иерархию до корня через Parent ссылки
        /// </summary>
        int Level { get; }
        
        /// <summary>
        /// Получает путь от корня к текущему узлу в виде последовательности ID
        /// </summary>
        /// <returns>Последовательность ID от корня к текущему узлу</returns>
        IEnumerable<long> GetPathIds();
        
        /// <summary>
        /// Получает хлебные крошки (breadcrumbs) для навигации
        /// </summary>
        /// <param name="separator">Разделитель между элементами (по умолчанию " > ")</param>
        /// <param name="includeIds">Включать ли ID в скобках (по умолчанию false)</param>
        /// <returns>Строка вида "Root > Category > Subcategory" или "Root (1) > Category (5) > Subcategory (23)"</returns>
        string GetBreadcrumbs(string separator = " > ", bool includeIds = false);
        
        /// <summary>
        /// Проверяет, является ли текущий узел потомком указанного узла
        /// </summary>
        /// <param name="ancestor">Предполагаемый предок</param>
        /// <returns>true, если текущий узел является потомком ancestor</returns>
        bool IsDescendantOf(ITreeRedbObject ancestor);
        
        /// <summary>
        /// Проверяет, является ли текущий узел предком указанного узла
        /// </summary>
        /// <param name="descendant">Предполагаемый потомок</param>
        /// <returns>true, если текущий узел является предком descendant</returns>
        bool IsAncestorOf(ITreeRedbObject descendant);
        
        /// <summary>
        /// Получает все узлы поддерева (включая текущий) в порядке обхода в глубину
        /// </summary>
        /// <returns>Последовательность узлов поддерева</returns>
        IEnumerable<ITreeRedbObject> GetSubtree();
        
        /// <summary>
        /// Получает количество узлов в поддереве (включая текущий)
        /// </summary>
        int SubtreeSize { get; }
        
        /// <summary>
        /// Получает максимальную глубину поддерева от текущего узла
        /// </summary>
        int MaxDepth { get; }
        
        /// <summary>
        /// Получает всех предков узла (от родителя к корню)
        /// Требует загруженную иерархию до корня через Parent ссылки
        /// </summary>
        IEnumerable<ITreeRedbObject> Ancestors { get; }
        
        /// <summary>
        /// Получает всех потомков узла рекурсивно
        /// Требует загруженную иерархию вниз через Children коллекции
        /// </summary>
        IEnumerable<ITreeRedbObject> Descendants { get; }
    }

    /// <summary>
    /// Типизированный интерфейс для древовидных объектов с конкретным типом свойств
    /// Обеспечивает типобезопасность при работе с однородными деревьями
    /// Для обратной совместимости с существующим кодом
    /// </summary>
    /// <typeparam name="TProps">Тип свойств объекта</typeparam>
    public interface ITreeRedbObject<TProps> : ITreeRedbObject, IRedbObject<TProps>
        where TProps : class, new()
    {
        /// <summary>
        /// Типизированная ссылка на родительский объект
        /// </summary>
        new ITreeRedbObject<TProps>? Parent { get; set; }
        
        /// <summary>
        /// Типизированная коллекция дочерних объектов
        /// </summary>
        new ICollection<ITreeRedbObject<TProps>> Children { get; set; }
        
        /// <summary>
        /// Типизированная версия получения поддерева
        /// </summary>
        /// <returns>Типизированная последовательность узлов поддерева</returns>
        new IEnumerable<ITreeRedbObject<TProps>> GetSubtree();
        
        /// <summary>
        /// Типизированная версия получения предков
        /// </summary>
        new IEnumerable<ITreeRedbObject<TProps>> Ancestors { get; }
        
        /// <summary>
        /// Типизированная версия получения потомков
        /// </summary>
        new IEnumerable<ITreeRedbObject<TProps>> Descendants { get; }
    }
}
