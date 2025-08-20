using System;

namespace redb.Core.Models.Users
{
    /// <summary>
    /// Запрос на обновление данных пользователя
    /// </summary>
    public class UpdateUserRequest
    {
        /// <summary>
        /// Новый логин пользователя (если null - не изменяется)
        /// Системные пользователи (ID 0, 1) не могут изменить логин
        /// </summary>
        public string? Login { get; set; }
        
        /// <summary>
        /// Новое имя пользователя (если null - не изменяется)
        /// Системные пользователи (ID 0, 1) не могут изменить имя
        /// </summary>
        public string? Name { get; set; }
        
        /// <summary>
        /// Новый телефон пользователя (если null - не изменяется)
        /// </summary>
        public string? Phone { get; set; }
        
        /// <summary>
        /// Новый email пользователя (если null - не изменяется)
        /// </summary>
        public string? Email { get; set; }
        
        /// <summary>
        /// Новый статус активности (если null - не изменяется)
        /// </summary>
        public bool? Enabled { get; set; }
        
        /// <summary>
        /// Дата увольнения (если null - не изменяется)
        /// </summary>
        public DateTime? DateDismiss { get; set; }
        
        /// <summary>
        /// Новые роли пользователя (если null - не изменяются)
        /// Если указан пустой массив - все роли убираются
        /// </summary>
        public long[]? RoleIds { get; set; }
    }
}
