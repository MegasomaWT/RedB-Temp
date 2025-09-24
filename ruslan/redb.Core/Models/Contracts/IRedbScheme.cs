using System;
using System.Collections.Generic;

namespace redb.Core.Models.Contracts
{
    /// <summary>
    /// Интерфейс схемы REDB
    /// Представляет схему (тип) объектов в системе с инкапсуляцией структур
    /// </summary>
    public interface IRedbScheme
    {
        /// <summary>
        /// Уникальный идентификатор схемы
        /// </summary>
        long Id { get; }
        
        /// <summary>
        /// Идентификатор родительской схемы (для иерархии схем)
        /// </summary>
        long? IdParent { get; }
        
        /// <summary>
        /// Наименование схемы
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Псевдоним схемы (краткое имя)
        /// </summary>
        string? Alias { get; }
        
        /// <summary>
        /// Пространство имен схемы (для C# классов)
        /// </summary>
        string? NameSpace { get; }
        
        /// <summary>
        /// Коллекция структур (полей) данной схемы
        /// Схема инкапсулирует свои структуры для целостности данных
        /// </summary>
        IReadOnlyCollection<IRedbStructure> Structures { get; }
        
        /// <summary>
        /// Быстрый доступ к структуре по имени
        /// Избегает необходимости поиска в коллекции
        /// </summary>
        IRedbStructure? GetStructureByName(string name);
    }
}
