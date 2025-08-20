namespace redb.Core.Models.Enums
{
    /// <summary>
    /// Статус пользователя
    /// </summary>
    public enum UserStatus
    {
        /// <summary>
        /// Активный пользователь
        /// </summary>
        Active,
        
        /// <summary>
        /// Деактивированный пользователь
        /// </summary>
        Disabled,
        
        /// <summary>
        /// Уволенный пользователь
        /// </summary>
        Dismissed,
        
        /// <summary>
        /// Заблокированный пользователь
        /// </summary>
        Blocked,
        
        /// <summary>
        /// Системный пользователь
        /// </summary>
        System
    }
}
