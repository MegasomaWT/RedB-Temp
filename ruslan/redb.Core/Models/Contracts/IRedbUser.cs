using System;

namespace redb.Core.Models.Contracts
{
    /// <summary>
    /// Интерфейс пользователя REDB
    /// Представляет пользователя системы с его основными свойствами
    /// </summary>
    public interface IRedbUser
    {
        /// <summary>
        /// Уникальный идентификатор пользователя
        /// </summary>
        long Id { get; }
        
        /// <summary>
        /// Логин пользователя (уникальный)
        /// </summary>
        string Login { get; }
        
        /// <summary>
        /// Имя пользователя
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Пароль пользователя (хешированный)
        /// </summary>
        string Password { get; }
        
        /// <summary>
        /// Активен ли пользователь
        /// </summary>
        bool Enabled { get; }
        
        /// <summary>
        /// Дата регистрации пользователя
        /// </summary>
        DateTime DateRegister { get; }
        
        /// <summary>
        /// Дата увольнения (если null - пользователь активен)
        /// </summary>
        DateTime? DateDismiss { get; }
        
        /// <summary>
        /// Телефон пользователя (опционально)
        /// </summary>
        string? Phone { get; }
        
        /// <summary>
        /// Email пользователя (опционально)
        /// </summary>
        string? Email { get; }
    }
}
