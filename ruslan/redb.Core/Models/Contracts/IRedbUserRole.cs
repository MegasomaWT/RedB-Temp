using System;

namespace redb.Core.Models.Contracts
{
    /// <summary>
    /// Интерфейс связи пользователь-роль REDB
    /// Представляет назначение роли пользователю
    /// </summary>
    public interface IRedbUserRole
    {
        /// <summary>
        /// Уникальный идентификатор связи
        /// </summary>
        long Id { get; }
        
        /// <summary>
        /// Идентификатор роли
        /// </summary>
        long IdRole { get; }
        
        /// <summary>
        /// Идентификатор пользователя
        /// </summary>
        long IdUser { get; }
    }
}
