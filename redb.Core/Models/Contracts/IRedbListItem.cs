using System;

namespace redb.Core.Models.Contracts
{
    /// <summary>
    /// Интерфейс элемента списка REDB
    /// Представляет элемент справочного списка
    /// </summary>
    public interface IRedbListItem
    {
        /// <summary>
        /// Уникальный идентификатор элемента списка
        /// </summary>
        long Id { get; }
        
        /// <summary>
        /// Идентификатор списка, к которому принадлежит элемент
        /// </summary>
        long IdList { get; }
        
        /// <summary>
        /// Текстовое значение элемента списка
        /// </summary>
        string? Value { get; }
        
        /// <summary>
        /// Идентификатор объекта (если элемент списка ссылается на объект)
        /// </summary>
        long? IdObject { get; }
    }
}
