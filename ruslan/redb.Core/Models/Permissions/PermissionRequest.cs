namespace redb.Core.Models.Permissions
{
    /// <summary>
    /// Запрос на создание или обновление разрешения
    /// </summary>
    public class PermissionRequest
    {
        /// <summary>
        /// ID пользователя (null если разрешение для роли)
        /// </summary>
        public long? UserId { get; set; }
        
        /// <summary>
        /// ID роли (null если разрешение для пользователя)
        /// </summary>
        public long? RoleId { get; set; }
        
        /// <summary>
        /// ID объекта (0 для глобальных прав)
        /// </summary>
        public long ObjectId { get; set; }
        
        /// <summary>
        /// Право на чтение
        /// </summary>
        public bool? CanSelect { get; set; }
        
        /// <summary>
        /// Право на создание дочерних объектов
        /// </summary>
        public bool? CanInsert { get; set; }
        
        /// <summary>
        /// Право на редактирование
        /// </summary>
        public bool? CanUpdate { get; set; }
        
        /// <summary>
        /// Право на удаление
        /// </summary>
        public bool? CanDelete { get; set; }
        
        /// <summary>
        /// Валидация запроса
        /// </summary>
        public bool IsValid()
        {
            // Должен быть указан либо пользователь, либо роль, но не оба
            if (UserId.HasValue && RoleId.HasValue)
                return false;
                
            if (!UserId.HasValue && !RoleId.HasValue)
                return false;
                
            // Должно быть указано хотя бы одно право
            if (!CanSelect.HasValue && !CanInsert.HasValue && !CanUpdate.HasValue && !CanDelete.HasValue)
                return false;
                
            return true;
        }
    }
}
