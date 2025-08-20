using System;

namespace redb.Core.Models.Contracts
{
    /// <summary>
    /// Интерфейс разрешения REDB
    /// Представляет права доступа пользователя или роли к объекту/схеме
    /// </summary>
    public interface IRedbPermission
    {
        /// <summary>
        /// Уникальный идентификатор разрешения
        /// </summary>
        long Id { get; }
        
        /// <summary>
        /// Идентификатор роли (если разрешение назначено роли)
        /// </summary>
        long? IdRole { get; }
        
        /// <summary>
        /// Идентификатор пользователя (если разрешение назначено пользователю)
        /// </summary>
        long? IdUser { get; }
        
        /// <summary>
        /// Идентификатор объекта/схемы, к которому применяется разрешение
        /// </summary>
        long IdRef { get; }
        
        /// <summary>
        /// Право на чтение (SELECT)
        /// </summary>
        bool? Select { get; }
        
        /// <summary>
        /// Право на создание (INSERT)
        /// </summary>
        bool? Insert { get; }
        
        /// <summary>
        /// Право на изменение (UPDATE)
        /// </summary>
        bool? Update { get; }
        
        /// <summary>
        /// Право на удаление (DELETE)
        /// </summary>
        bool? Delete { get; }
    }
}
