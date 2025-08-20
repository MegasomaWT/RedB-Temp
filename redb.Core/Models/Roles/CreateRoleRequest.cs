namespace redb.Core.Models.Roles
{
    /// <summary>
    /// Запрос на создание новой роли
    /// </summary>
    public class CreateRoleRequest
    {
        /// <summary>
        /// Имя роли (уникальное)
        /// </summary>
        public string Name { get; set; } = "";
        
        /// <summary>
        /// Описание роли (опционально)
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Пользователи для назначения в роль при создании
        /// </summary>
        public long[]? UserIds { get; set; }
    }
}
