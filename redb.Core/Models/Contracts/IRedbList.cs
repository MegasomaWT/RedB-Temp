using System;

namespace redb.Core.Models.Contracts
{
    /// <summary>
    /// Интерфейс списка REDB
    /// Представляет справочный список для полей типа список
    /// </summary>
    public interface IRedbList
    {
        /// <summary>
        /// Уникальный идентификатор списка
        /// </summary>
        long Id { get; }
        
        /// <summary>
        /// Наименование списка
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Псевдоним списка (краткое имя)
        /// </summary>
        string? Alias { get; }
    }
}
