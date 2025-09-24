using System;

namespace redb.Core.Models.Enums
{
    /// <summary>
    /// Действия разрешений (флаги для комбинирования)
    /// </summary>
    [Flags]
    public enum PermissionAction
    {
        /// <summary>
        /// Нет прав
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Право на чтение
        /// </summary>
        Select = 1,
        
        /// <summary>
        /// Право на создание дочерних объектов
        /// </summary>
        Insert = 2,
        
        /// <summary>
        /// Право на редактирование
        /// </summary>
        Update = 4,
        
        /// <summary>
        /// Право на удаление
        /// </summary>
        Delete = 8,
        
        /// <summary>
        /// Все права
        /// </summary>
        All = Select | Insert | Update | Delete,
        
        /// <summary>
        /// Права на чтение и редактирование
        /// </summary>
        ReadWrite = Select | Update,
        
        /// <summary>
        /// Права на чтение и создание
        /// </summary>
        ReadCreate = Select | Insert
    }
}
