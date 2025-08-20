namespace redb.Core.Models.Permissions
{
    /// <summary>
    /// Результат получения эффективных прав пользователя на объект
    /// </summary>
    public class EffectivePermissionResult
    {
        /// <summary>
        /// ID объекта
        /// </summary>
        public long ObjectId { get; set; }
        
        /// <summary>
        /// ID пользователя
        /// </summary>
        public long UserId { get; set; }
        
        /// <summary>
        /// ID источника разрешения (объект, от которого наследуется право)
        /// </summary>
        public long PermissionSourceId { get; set; }
        
        /// <summary>
        /// Тип разрешения (пользовательское или ролевое)
        /// </summary>
        public string PermissionType { get; set; } = "";
        
        /// <summary>
        /// ID роли (если разрешение ролевое)
        /// </summary>
        public long? RoleId { get; set; }
        
        /// <summary>
        /// ID пользователя в разрешении (если разрешение пользовательское)
        /// </summary>
        public long? PermissionUserId { get; set; }
        
        /// <summary>
        /// Право на чтение
        /// </summary>
        public bool CanSelect { get; set; }
        
        /// <summary>
        /// Право на создание дочерних объектов
        /// </summary>
        public bool CanInsert { get; set; }
        
        /// <summary>
        /// Право на редактирование
        /// </summary>
        public bool CanUpdate { get; set; }
        
        /// <summary>
        /// Право на удаление
        /// </summary>
        public bool CanDelete { get; set; }
        
        /// <summary>
        /// Разрешение унаследовано от родительского объекта
        /// </summary>
        public bool IsInherited => PermissionSourceId != ObjectId;
        
        /// <summary>
        /// Есть ли какие-либо права
        /// </summary>
        public bool HasAnyPermission => CanSelect || CanInsert || CanUpdate || CanDelete;
        
        /// <summary>
        /// Есть ли полные права (все действия разрешены)
        /// </summary>
        public bool HasFullPermission => CanSelect && CanInsert && CanUpdate && CanDelete;
    }
}
