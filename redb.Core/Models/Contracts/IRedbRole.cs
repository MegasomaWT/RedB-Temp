using System;

namespace redb.Core.Models.Contracts
{
    /// <summary>
    /// Интерфейс роли REDB
    /// Представляет роль пользователя в системе безопасности
    /// </summary>
    public interface IRedbRole
    {
        /// <summary>
        /// Уникальный идентификатор роли
        /// </summary>
        long Id { get; }
        
        /// <summary>
        /// Наименование роли
        /// </summary>
        string Name { get; }
    }
}
