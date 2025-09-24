using System;
using System.Collections.Generic;

namespace redb.Core.Models.Permissions
{
    /// <summary>
    /// Набор разрешений пользователя для кеширования
    /// Содержит все разрешения пользователя для быстрого доступа
    /// </summary>
    public class UserPermissionSet
    {
        /// <summary>
        /// ID пользователя
        /// </summary>
        public long UserId { get; set; }
        
        /// <summary>
        /// Время создания набора разрешений
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Время истечения кеша
        /// </summary>
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(30);
        
        /// <summary>
        /// Разрешения на конкретные объекты
        /// Key: ObjectId, Value: PermissionFlags
        /// </summary>
        public Dictionary<long, PermissionFlags> ObjectPermissions { get; set; } = new();
        
        /// <summary>
        /// Глобальные разрешения (на все объекты)
        /// </summary>
        public PermissionFlags GlobalPermissions { get; set; } = PermissionFlags.None;
        
        /// <summary>
        /// Разрешения на схемы
        /// Key: SchemeId, Value: PermissionFlags
        /// </summary>
        public Dictionary<long, PermissionFlags> SchemePermissions { get; set; } = new();
        
        /// <summary>
        /// Версия кеша (для инвалидации)
        /// </summary>
        public long Version { get; set; } = 1;
        
        /// <summary>
        /// Проверить истек ли кеш
        /// </summary>
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        
        /// <summary>
        /// Получить разрешения для объекта с учетом иерархии
        /// </summary>
        public PermissionFlags GetPermissionsForObject(long objectId, long schemeId)
        {
            // 1. Проверяем конкретные разрешения на объект
            if (ObjectPermissions.TryGetValue(objectId, out var objectPerms) && objectPerms != PermissionFlags.None)
            {
                return objectPerms;
            }
            
            // 2. Проверяем разрешения на схему
            if (SchemePermissions.TryGetValue(schemeId, out var schemePerms) && schemePerms != PermissionFlags.None)
            {
                return schemePerms;
            }
            
            // 3. Возвращаем глобальные разрешения
            return GlobalPermissions;
        }
        
        /// <summary>
        /// Добавить разрешение на объект
        /// </summary>
        public void AddObjectPermission(long objectId, PermissionFlags permissions)
        {
            ObjectPermissions[objectId] = permissions;
        }
        
        /// <summary>
        /// Добавить разрешение на схему
        /// </summary>
        public void AddSchemePermission(long schemeId, PermissionFlags permissions)
        {
            SchemePermissions[schemeId] = permissions;
        }
        
        /// <summary>
        /// Установить глобальные разрешения
        /// </summary>
        public void SetGlobalPermissions(PermissionFlags permissions)
        {
            GlobalPermissions = permissions;
        }
        
        /// <summary>
        /// Проверить может ли пользователь выполнить операцию с объектом
        /// </summary>
        public bool CanPerformOperation(long objectId, long schemeId, PermissionFlags requiredPermission)
        {
            var userPermissions = GetPermissionsForObject(objectId, schemeId);
            return userPermissions.HasFlag(requiredPermission);
        }
        
        /// <summary>
        /// Инвалидировать кеш (установить истекшим)
        /// </summary>
        public void Invalidate()
        {
            ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        }
        
        /// <summary>
        /// Продлить время жизни кеша
        /// </summary>
        public void ExtendExpiration(TimeSpan extension)
        {
            ExpiresAt = DateTime.UtcNow.Add(extension);
        }
        
        /// <summary>
        /// Получить статистику кеша
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            return new CacheStatistics
            {
                UserId = UserId,
                ObjectPermissionsCount = ObjectPermissions.Count,
                SchemePermissionsCount = SchemePermissions.Count,
                HasGlobalPermissions = GlobalPermissions != PermissionFlags.None,
                CreatedAt = CreatedAt,
                ExpiresAt = ExpiresAt,
                IsExpired = IsExpired,
                Version = Version
            };
        }
    }
    
    /// <summary>
    /// Статистика кеша разрешений
    /// </summary>
    public class CacheStatistics
    {
        public long UserId { get; set; }
        public int ObjectPermissionsCount { get; set; }
        public int SchemePermissionsCount { get; set; }
        public bool HasGlobalPermissions { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired { get; set; }
        public long Version { get; set; }
        
        public override string ToString()
        {
            return $"User {UserId}: {ObjectPermissionsCount} objects, {SchemePermissionsCount} schemes, " +
                   $"Global: {HasGlobalPermissions}, Expired: {IsExpired}, Version: {Version}";
        }
    }
}
