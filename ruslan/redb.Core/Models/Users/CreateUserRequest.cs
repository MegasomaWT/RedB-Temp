using System;

namespace redb.Core.Models.Users
{
    /// <summary>
    /// Запрос на создание нового пользователя
    /// </summary>
    public class CreateUserRequest
    {
        /// <summary>
        /// Логин пользователя (уникальный)
        /// </summary>
        public string Login { get; set; } = "";
        
        /// <summary>
        /// Пароль пользователя (в открытом виде, будет захеширован)
        /// </summary>
        public string Password { get; set; } = "";
        
        /// <summary>
        /// Имя пользователя
        /// </summary>
        public string Name { get; set; } = "";
        
        /// <summary>
        /// Телефон пользователя (опционально)
        /// </summary>
        public string? Phone { get; set; }
        
        /// <summary>
        /// Email пользователя (опционально)
        /// </summary>
        public string? Email { get; set; }
        
        /// <summary>
        /// Активен ли пользователь при создании
        /// </summary>
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// Роли для назначения пользователю при создании
        /// </summary>
        public long[]? RoleIds { get; set; }
        
        /// <summary>
        /// Дата регистрации (если не указана, используется текущая)
        /// </summary>
        public DateTime? DateRegister { get; set; }
    }
}
