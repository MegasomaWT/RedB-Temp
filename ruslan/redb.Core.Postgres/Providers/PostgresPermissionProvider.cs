using redb.Core.Providers;
using redb.Core.DBModels;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using redb.Core.Models.Enums;
using redb.Core.Models.Permissions;
using redb.Core.Models.Contracts;
using redb.Core.Models.Entities;
using redb.Core.Models.Security;
using System.Collections.Concurrent;

namespace redb.Core.Postgres.Providers
{
    /// <summary>
    /// PostgreSQL реализация провайдера прав доступа
    /// </summary>
    public class PostgresPermissionProvider : IPermissionProvider
    {
        private readonly RedbContext _context;
        private readonly IRedbSecurityContext _securityContext;
        
        // ===== 🚀 SQL-БАЗИРОВАННЫЕ МЕТОДЫ ПРОВЕРКИ ПРАВ =====
        
        /// <summary>
        /// Кеш результатов SQL запросов прав (userId_objectId -> (result, cachedAt))
        /// </summary>
        private static readonly ConcurrentDictionary<string, (UserPermissionResult result, DateTime cachedAt)> _permissionCache = new();
        
        /// <summary>
        /// Время жизни кеша прав
        /// </summary>
        private static readonly TimeSpan _cacheLifetime = TimeSpan.FromMinutes(5);
        
        /// <summary>
        /// Статистика кеша прав
        /// </summary>
        private static long _cacheRequests = 0;
        private static long _cacheHits = 0;

        public PostgresPermissionProvider(RedbContext context, IRedbSecurityContext securityContext)
        {
            _context = context;
            _securityContext = securityContext;
        }

        // ===== 🔧 ПРИВАТНЫЕ SQL МЕТОДЫ =====
        
        /// <summary>
        /// Получить эффективные права через SQL функцию get_user_permissions_for_object
        /// Использует мощную рекурсивную логику БД с кешированием
        /// </summary>
        /// <param name="objectId">ID объекта</param>
        /// <param name="userId">ID пользователя</param>
        /// <returns>Результат SQL функции или null если права не найдены</returns>
        private async Task<UserPermissionResult?> GetEffectivePermissionViaSqlAsync(long objectId, long userId)
        {
            // Проверяем кеш
            var cacheKey = $"{userId}_{objectId}";
            Interlocked.Increment(ref _cacheRequests);
            
            if (_permissionCache.TryGetValue(cacheKey, out var cached))
            {
                var isExpired = DateTime.UtcNow - cached.cachedAt > _cacheLifetime;
                if (!isExpired)
                {
                    Interlocked.Increment(ref _cacheHits);
                    return cached.result;
                }
            }

            var result = await _context.Database
                .SqlQueryRaw<UserPermissionResult>(
                    "SELECT * FROM get_user_permissions_for_object({0}, {1})", 
                    objectId, userId)
                .FirstOrDefaultAsync();

            // Кешируем результат
            if (result != null)
            {
                _permissionCache[cacheKey] = (result, DateTime.UtcNow);
            }

            return result;
        }

        /// <summary>
        /// Очистить кеш прав (при изменении permissions)
        /// </summary>
        /// <param name="userId">ID пользователя (null = все пользователи)</param>
        /// <param name="objectId">ID объекта (null = все объекты)</param>
        private static void InvalidatePermissionCache(long? userId = null, long? objectId = null)
        {
            if (userId.HasValue && objectId.HasValue)
            {
                // Очистить конкретную запись
                _permissionCache.TryRemove($"{userId}_{objectId}", out _);
            }
            else if (userId.HasValue)
            {
                // Очистить все записи пользователя
                var keysToRemove = _permissionCache.Keys
                    .Where(k => k.StartsWith($"{userId}_"))
                    .ToList();
                foreach (var key in keysToRemove)
                    _permissionCache.TryRemove(key, out _);
            }
            else if (objectId.HasValue)
            {
                // Очистить все записи объекта
                var keysToRemove = _permissionCache.Keys
                    .Where(k => k.EndsWith($"_{objectId}"))
                    .ToList();
                foreach (var key in keysToRemove)
                    _permissionCache.TryRemove(key, out _);
            }
            else
            {
                // Очистить весь кеш
                _permissionCache.Clear();
            }
        }

        /// <summary>
        /// Получить статистику кеша прав
        /// </summary>
        /// <returns>Информация о производительности кеша</returns>
        public static string GetCacheStatistics()
        {
            var hitRate = _cacheRequests > 0 ? (double)_cacheHits / _cacheRequests * 100 : 0;
            return $"Кеш прав: Запросов={_cacheRequests}, " +
                   $"Попаданий={_cacheHits}, " +
                   $"Hit Rate={hitRate:F1}%, " +
                   $"Записей в кеше={_permissionCache.Count}";
        }

        // ===== БАЗОВЫЕ МЕТОДЫ (используют _securityContext) =====
        
        public IQueryable<long> GetReadableObjectIds()
        {
            var effectiveUser = ((RedbSecurityContext)_securityContext).GetEffectiveUser();
            return GetReadableObjectIds(effectiveUser.Id);
        }

        public async Task<bool> CanUserEditObject(IRedbObject obj)
        {
            var effectiveUser = ((RedbSecurityContext)_securityContext).GetEffectiveUser();
            return await CanUserEditObject(obj.Id, effectiveUser.Id);
        }

        public async Task<bool> CanUserSelectObject(IRedbObject obj)
        {
            var effectiveUser = ((RedbSecurityContext)_securityContext).GetEffectiveUser();
            return await CanUserSelectObject(obj.Id, effectiveUser.Id);
        }

        public async Task<bool> CanUserInsertScheme(IRedbScheme scheme)
        {
            var effectiveUser = ((RedbSecurityContext)_securityContext).GetEffectiveUser();
            return await CanUserInsertScheme(scheme.Id, effectiveUser.Id);
        }

        // ===== ОСНОВНЫЕ МЕТОДЫ С ЯВНЫМ УКАЗАНИЕМ ПОЛЬЗОВАТЕЛЯ =====

        public IQueryable<long> GetReadableObjectIds(IRedbUser user)
        {
            return GetReadableObjectIds(user.Id);
        }
        
        public IQueryable<long> GetReadableObjectIds(long userId)
        {
            // ✅ Используем мощный SQL VIEW v_user_permissions для эффективного поиска
            return _context.Database
                .SqlQuery<long>($"""
                    SELECT DISTINCT object_id 
                    FROM v_user_permissions 
                    WHERE user_id = {userId} 
                      AND can_select = true
                    """);
        }

        public async Task<bool> CanUserEditObject(long objectId, long userId)
        {
            var permission = await GetEffectivePermissionViaSqlAsync(objectId, userId);
            if (permission == null)
            {
                throw new InvalidOperationException($"Не удалось получить права для объекта {objectId} и пользователя {userId}. SQL функция get_user_permissions_for_object недоступна или вернула NULL.");
            }
            return permission.CanUpdate;
        }

        public async Task<bool> CanUserSelectObject(long objectId, long userId)
        {
            var permission = await GetEffectivePermissionViaSqlAsync(objectId, userId);
            if (permission == null)
            {
                throw new InvalidOperationException($"Не удалось получить права для объекта {objectId} и пользователя {userId}. SQL функция get_user_permissions_for_object недоступна или вернула NULL.");
            }
            return permission.CanSelect;
        }

        public async Task<bool> CanUserInsertScheme(long schemeId, long userId)
        {
            var permission = await GetEffectivePermissionViaSqlAsync(schemeId, userId);
            if (permission == null)
            {
                throw new InvalidOperationException($"Не удалось получить права для схемы {schemeId} и пользователя {userId}. SQL функция get_user_permissions_for_object недоступна или вернула NULL.");
            }
            return permission.CanInsert;
        }

        public async Task<bool> CanUserDeleteObject(IRedbObject obj)
        {
            var effectiveUser = ((RedbSecurityContext)_securityContext).GetEffectiveUser();
            return await CanUserDeleteObject(obj, effectiveUser);
        }

        public async Task<bool> CanUserDeleteObject(RedbObject obj, IRedbUser user)
        {
            return await CanUserDeleteObject(obj.Id, user.Id);
        }

        public async Task<bool> CanUserInsertScheme(RedbObject obj, IRedbUser user)
        {
            return await CanUserInsertScheme(obj.SchemeId, user.Id);
        }

        public async Task<bool> CanUserEditObject(IRedbObject obj, IRedbUser user)
        {
            return await CanUserEditObject(obj.Id, user.Id);
        }

        public async Task<bool> CanUserSelectObject(IRedbObject obj, IRedbUser user)
        {
            return await CanUserSelectObject(obj.Id, user.Id);
        }

        public async Task<bool> CanUserInsertScheme(IRedbScheme scheme, IRedbUser user)
        {
            return await CanUserInsertScheme(scheme.Id, user.Id);
        }

        public async Task<bool> CanUserDeleteObject(IRedbObject obj, IRedbUser user)
        {
            return await CanUserDeleteObject(obj.Id, user.Id);
        }
        
        public async Task<bool> CanUserEditObject(RedbObject obj)
        {
            var effectiveUser = ((RedbSecurityContext)_securityContext).GetEffectiveUser();
            return await CanUserEditObject(obj.Id, effectiveUser.Id);
        }
        
        public async Task<bool> CanUserSelectObject(RedbObject obj)
        {
            var effectiveUser = ((RedbSecurityContext)_securityContext).GetEffectiveUser();
            return await CanUserSelectObject(obj.Id, effectiveUser.Id);
        }
        
        public async Task<bool> CanUserDeleteObject(RedbObject obj)
        {
            var effectiveUser = ((RedbSecurityContext)_securityContext).GetEffectiveUser();
            return await CanUserDeleteObject(obj.Id, effectiveUser.Id);
        }
        
        public async Task<bool> CanUserEditObject(RedbObject obj, IRedbUser user)
        {
            return await CanUserEditObject(obj.Id, user.Id);
        }
        
        public async Task<bool> CanUserSelectObject(RedbObject obj, IRedbUser user)
        {
            return await CanUserSelectObject(obj.Id, user.Id);
        }

        public async Task<bool> CanUserDeleteObject(long objectId, long userId)
        {
            var permission = await GetEffectivePermissionViaSqlAsync(objectId, userId);
            if (permission == null)
            {
                throw new InvalidOperationException($"Не удалось получить права для объекта {objectId} и пользователя {userId}. SQL функция get_user_permissions_for_object недоступна или вернула NULL.");
            }
            return permission.CanDelete;
        }

        // ===== 🔧 НОВЫЕ CRUD МЕТОДЫ ДЛЯ РАЗРЕШЕНИЙ =====

        public async Task<IRedbPermission> CreatePermissionAsync(PermissionRequest request, IRedbUser? currentUser = null)
        {
            var newPermission = new _RPermission
            {
                Id = _context.GetNextKey(),
                IdUser = request.UserId,
                IdRole = request.RoleId,
                IdRef = request.ObjectId,
                Select = request.CanSelect,
                Insert = request.CanInsert,
                Update = request.CanUpdate,
                Delete = request.CanDelete
            };

            _context.Set<_RPermission>().Add(newPermission);
            await _context.SaveChangesAsync();

            // ⭐ Инвалидируем кеш после создания разрешения
            InvalidatePermissionCache(request.UserId, request.ObjectId);
            if (request.ObjectId != 0) // Если не глобальные права
            {
                InvalidatePermissionCache(null, request.ObjectId); // Инвалидируем для всех пользователей этого объекта
            }

            return RedbPermission.FromEntity(newPermission);
        }

        public async Task<IRedbPermission> UpdatePermissionAsync(IRedbPermission permission, PermissionRequest request, IRedbUser? currentUser = null)
        {
            var dbPermission = await _context.Set<_RPermission>()
                .FirstOrDefaultAsync(p => p.Id == permission.Id);

            if (dbPermission == null)
                throw new ArgumentException($"Разрешение с ID {permission.Id} не найдено");

            // Обновляем права
            dbPermission.Select = request.CanSelect;
            dbPermission.Insert = request.CanInsert;
            dbPermission.Update = request.CanUpdate;
            dbPermission.Delete = request.CanDelete;

            await _context.SaveChangesAsync();

            return RedbPermission.FromEntity(dbPermission);
        }

        public async Task<bool> DeletePermissionAsync(IRedbPermission permission, IRedbUser? currentUser = null)
        {
            var dbPermission = await _context.Set<_RPermission>()
                .FirstOrDefaultAsync(p => p.Id == permission.Id);

            if (dbPermission == null)
                return false;

            _context.Set<_RPermission>().Remove(dbPermission);
            var result = await _context.SaveChangesAsync();

            // ⭐ Инвалидируем кеш после удаления разрешения
            InvalidatePermissionCache(permission.IdUser, permission.IdRef);
            if (permission.IdRef != 0) // Если не глобальные права
            {
                InvalidatePermissionCache(null, permission.IdRef); // Инвалидируем для всех пользователей этого объекта
            }

            return result > 0;
        }

        // ===== 🔍 ПОИСК РАЗРЕШЕНИЙ =====

        public async Task<List<IRedbPermission>> GetPermissionsByUserAsync(IRedbUser user)
        {
            var permissions = await _context.Set<_RPermission>()
                .Where(p => p.IdUser == user.Id)
                .Include(p => p.UserNavigation)
                .ToListAsync();
            
            return permissions.Select(p => (IRedbPermission)RedbPermission.FromEntity(p)).ToList();
        }

        public async Task<List<IRedbPermission>> GetPermissionsByRoleAsync(IRedbRole role)
        {
            var permissions = await _context.Set<_RPermission>()
                .Where(p => p.IdRole == role.Id)
                .Include(p => p.RoleNavigation)
                .ToListAsync();
            
            return permissions.Select(p => (IRedbPermission)RedbPermission.FromEntity(p)).ToList();
        }

        public async Task<List<IRedbPermission>> GetPermissionsByObjectAsync(IRedbObject obj)
        {
            var permissions = await _context.Set<_RPermission>()
                .Where(p => p.IdRef == obj.Id || p.IdRef == 0) // Включаем глобальные права
                .Include(p => p.UserNavigation)
                .Include(p => p.RoleNavigation)
                .ToListAsync();
            
            return permissions.Select(p => (IRedbPermission)RedbPermission.FromEntity(p)).ToList();
        }

        public async Task<IRedbPermission?> GetPermissionByIdAsync(long permissionId)
        {
            var permission = await _context.Set<_RPermission>()
                .Where(p => p.Id == permissionId)
                .Include(p => p.UserNavigation)
                .Include(p => p.RoleNavigation)
                .FirstOrDefaultAsync();
            
            return permission != null ? RedbPermission.FromEntity(permission) : null;
        }

        // ===== 🎯 УПРАВЛЕНИЕ РАЗРЕШЕНИЯМИ =====

        public async Task<bool> GrantPermissionAsync(IRedbUser user, IRedbObject obj, PermissionAction actions, IRedbUser? currentUser = null)
        {
            return await GrantPermissionAsync(user.Id, null, obj.Id, actions, currentUser);
        }

        public async Task<bool> GrantPermissionAsync(IRedbRole role, IRedbObject obj, PermissionAction actions, IRedbUser? currentUser = null)
        {
            return await GrantPermissionAsync(null, role.Id, obj.Id, actions, currentUser);
        }

        private async Task<bool> GrantPermissionAsync(long? userId, long? roleId, long objectId, PermissionAction actions, IRedbUser? currentUser = null)
        {
            // Ищем существующее разрешение
            var existingPermission = await _context.Set<_RPermission>()
                .FirstOrDefaultAsync(p => p.IdUser == userId && p.IdRole == roleId && p.IdRef == objectId);

            if (existingPermission != null)
            {
                // Обновляем существующее разрешение (добавляем права)
                existingPermission.Select = existingPermission.Select == true || actions.HasFlag(PermissionAction.Select);
                existingPermission.Insert = existingPermission.Insert == true || actions.HasFlag(PermissionAction.Insert);
                existingPermission.Update = existingPermission.Update == true || actions.HasFlag(PermissionAction.Update);
                existingPermission.Delete = existingPermission.Delete == true || actions.HasFlag(PermissionAction.Delete);
            }
            else
            {
                // Создаем новое разрешение
                var newPermission = new _RPermission
                {
                    Id = _context.GetNextKey(),
                    IdUser = userId,
                    IdRole = roleId,
                    IdRef = objectId,
                    Select = actions.HasFlag(PermissionAction.Select),
                    Insert = actions.HasFlag(PermissionAction.Insert),
                    Update = actions.HasFlag(PermissionAction.Update),
                    Delete = actions.HasFlag(PermissionAction.Delete)
                };

                _context.Set<_RPermission>().Add(newPermission);
            }

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> RevokePermissionAsync(IRedbUser user, IRedbObject obj, IRedbUser? currentUser = null)
        {
            return await RevokePermissionAsync(user.Id, null, obj.Id, currentUser);
        }

        public async Task<bool> RevokePermissionAsync(IRedbRole role, IRedbObject obj, IRedbUser? currentUser = null)
        {
            return await RevokePermissionAsync(null, role.Id, obj.Id, currentUser);
        }

        private async Task<bool> RevokePermissionAsync(long? userId, long? roleId, long objectId, IRedbUser? currentUser = null)
        {
            var permission = await _context.Set<_RPermission>()
                .FirstOrDefaultAsync(p => p.IdUser == userId && p.IdRole == roleId && p.IdRef == objectId);

            if (permission == null)
                return false;

            _context.Set<_RPermission>().Remove(permission);
            var result = await _context.SaveChangesAsync();

            return result > 0;
        }

        public async Task<int> RevokeAllUserPermissionsAsync(IRedbUser user, IRedbUser? currentUser = null)
        {
            var permissions = await _context.Set<_RPermission>()
                .Where(p => p.IdUser == user.Id)
                .ToListAsync();

            _context.Set<_RPermission>().RemoveRange(permissions);
            var result = await _context.SaveChangesAsync();

            return result;
        }

        public async Task<int> RevokeAllRolePermissionsAsync(IRedbRole role, IRedbUser? currentUser = null)
        {
            var permissions = await _context.Set<_RPermission>()
                .Where(p => p.IdRole == role.Id)
                .ToListAsync();

            _context.Set<_RPermission>().RemoveRange(permissions);
            var result = await _context.SaveChangesAsync();

            return result;
        }

        public async Task<EffectivePermissionResult> GetEffectivePermissionsAsync(IRedbUser user, IRedbObject obj)
        {
            return await GetEffectivePermissionsAsync(user.Id, obj.Id);
        }

        public async Task<Dictionary<IRedbObject, EffectivePermissionResult>> GetEffectivePermissionsBatchAsync(IRedbUser user, IRedbObject[] objects)
        {
            var objectIds = objects.Select(o => o.Id).ToArray();
            var results = await GetEffectivePermissionsBatchAsync(user.Id, objectIds);
            
            // Преобразуем результат в нужный формат
            var finalResults = new Dictionary<IRedbObject, EffectivePermissionResult>();
            foreach (var obj in objects)
            {
                if (results.TryGetValue(obj.Id, out var result))
                {
                    finalResults[obj] = result;
                }
            }
            
            return finalResults;
        }

        public async Task<List<EffectivePermissionResult>> GetAllEffectivePermissionsAsync(IRedbUser user)
        {
            return await GetAllEffectivePermissionsAsync(user.Id);
        }

        // ===== ⚡ ЭФФЕКТИВНЫЕ ПРАВА =====

        public async Task<EffectivePermissionResult> GetEffectivePermissionsAsync(long userId, long objectId)
        {
            // Получаем прямые права пользователя (включая глобальные права _id_ref=0)
            var userPermissions = await _context.Set<_RPermission>()
                .Where(p => p.IdUser == userId && (p.IdRef == objectId || p.IdRef == 0))
                .ToListAsync();

            // Получаем роли пользователя
            var userRoles = await _context.Set<_RUsersRole>()
                .Where(ur => ur.IdUser == userId)
                .Select(ur => ur.IdRole)
                .ToListAsync();

            // Получаем права ролей (включая глобальные права _id_ref=0)
            var rolePermissions = await _context.Set<_RPermission>()
                .Where(p => userRoles.Contains(p.IdRole ?? 0) && (p.IdRef == objectId || p.IdRef == 0))
                .ToListAsync();

            // Объединяем права (OR логика)
            var result = new EffectivePermissionResult
            {
                UserId = userId,
                ObjectId = objectId,
                PermissionSourceId = objectId,
                PermissionType = "Combined",
                CanSelect = userPermissions.Any(p => p.Select == true) || rolePermissions.Any(p => p.Select == true),
                CanInsert = userPermissions.Any(p => p.Insert == true) || rolePermissions.Any(p => p.Insert == true),
                CanUpdate = userPermissions.Any(p => p.Update == true) || rolePermissions.Any(p => p.Update == true),
                CanDelete = userPermissions.Any(p => p.Delete == true) || rolePermissions.Any(p => p.Delete == true)
            };

            // Определяем источник разрешения
            if (userPermissions.Any() && rolePermissions.Any())
            {
                result.PermissionType = "UserAndRole";
            }
            else if (userPermissions.Any())
            {
                result.PermissionType = "User";
                result.PermissionUserId = userId;
            }
            else if (rolePermissions.Any())
            {
                result.PermissionType = "Role";
                result.RoleId = rolePermissions.First().IdRole;
            }

            return result;
        }

        public async Task<Dictionary<long, EffectivePermissionResult>> GetEffectivePermissionsBatchAsync(long userId, long[] objectIds)
        {
            if (objectIds == null || objectIds.Length == 0)
                return new Dictionary<long, EffectivePermissionResult>();

            // Получаем прямые права пользователя на все объекты
            var userPermissions = await _context.Set<_RPermission>()
                .Where(p => p.IdUser == userId && objectIds.Contains(p.IdRef))
                .ToListAsync();

            // Получаем роли пользователя
            var userRoles = await _context.Set<_RUsersRole>()
                .Where(ur => ur.IdUser == userId)
                .Select(ur => ur.IdRole)
                .ToListAsync();

            // Получаем права ролей на все объекты
            var rolePermissions = await _context.Set<_RPermission>()
                .Where(p => userRoles.Contains(p.IdRole ?? 0) && objectIds.Contains(p.IdRef))
                .ToListAsync();

            // Группируем права по объектам
            var userPermissionsByObject = userPermissions.GroupBy(p => p.IdRef).ToDictionary(g => g.Key, g => g.ToList());
            var rolePermissionsByObject = rolePermissions.GroupBy(p => p.IdRef).ToDictionary(g => g.Key, g => g.ToList());

            var result = new Dictionary<long, EffectivePermissionResult>();

            // Создаем результат для каждого объекта
            foreach (var objectId in objectIds)
            {
                var userPerms = userPermissionsByObject.GetValueOrDefault(objectId, new List<_RPermission>());
                var rolePerms = rolePermissionsByObject.GetValueOrDefault(objectId, new List<_RPermission>());

                var effectiveResult = new EffectivePermissionResult
                {
                    UserId = userId,
                    ObjectId = objectId,
                    PermissionSourceId = objectId,
                    PermissionType = "Combined",
                    CanSelect = userPerms.Any(p => p.Select == true) || rolePerms.Any(p => p.Select == true),
                    CanInsert = userPerms.Any(p => p.Insert == true) || rolePerms.Any(p => p.Insert == true),
                    CanUpdate = userPerms.Any(p => p.Update == true) || rolePerms.Any(p => p.Update == true),
                    CanDelete = userPerms.Any(p => p.Delete == true) || rolePerms.Any(p => p.Delete == true)
                };

                // Определяем источник разрешения
                if (userPerms.Any() && rolePerms.Any())
                {
                    effectiveResult.PermissionType = "UserAndRole";
                }
                else if (userPerms.Any())
                {
                    effectiveResult.PermissionType = "User";
                    effectiveResult.PermissionUserId = userId;
                }
                else if (rolePerms.Any())
                {
                    effectiveResult.PermissionType = "Role";
                    effectiveResult.RoleId = rolePerms.First().IdRole;
                }

                result[objectId] = effectiveResult;
            }

            return result;
        }

        public async Task<List<EffectivePermissionResult>> GetAllEffectivePermissionsAsync(long userId)
        {
            // Получаем все прямые права пользователя
            var userPermissions = await _context.Set<_RPermission>()
                .Where(p => p.IdUser == userId)
                .ToListAsync();

            // Получаем роли пользователя
            var userRoles = await _context.Set<_RUsersRole>()
                .Where(ur => ur.IdUser == userId)
                .Select(ur => ur.IdRole)
                .ToListAsync();

            // Получаем все права ролей
            var rolePermissions = await _context.Set<_RPermission>()
                .Where(p => userRoles.Contains(p.IdRole ?? 0))
                .ToListAsync();

            // Собираем все уникальные объекты
            var allObjectIds = userPermissions.Select(p => p.IdRef)
                .Concat(rolePermissions.Select(p => p.IdRef))
                .Distinct()
                .ToList();

            var result = new List<EffectivePermissionResult>();

            // Группируем права по объектам
            var userPermissionsByObject = userPermissions.GroupBy(p => p.IdRef).ToDictionary(g => g.Key, g => g.ToList());
            var rolePermissionsByObject = rolePermissions.GroupBy(p => p.IdRef).ToDictionary(g => g.Key, g => g.ToList());

            // Создаем результат для каждого объекта
            foreach (var objectId in allObjectIds)
            {
                var userPerms = userPermissionsByObject.GetValueOrDefault(objectId, new List<_RPermission>());
                var rolePerms = rolePermissionsByObject.GetValueOrDefault(objectId, new List<_RPermission>());

                var effectiveResult = new EffectivePermissionResult
                {
                    UserId = userId,
                    ObjectId = objectId,
                    PermissionSourceId = objectId,
                    PermissionType = "Combined",
                    CanSelect = userPerms.Any(p => p.Select == true) || rolePerms.Any(p => p.Select == true),
                    CanInsert = userPerms.Any(p => p.Insert == true) || rolePerms.Any(p => p.Insert == true),
                    CanUpdate = userPerms.Any(p => p.Update == true) || rolePerms.Any(p => p.Update == true),
                    CanDelete = userPerms.Any(p => p.Delete == true) || rolePerms.Any(p => p.Delete == true)
                };

                // Определяем источник разрешения
                if (userPerms.Any() && rolePerms.Any())
                {
                    effectiveResult.PermissionType = "UserAndRole";
                }
                else if (userPerms.Any())
                {
                    effectiveResult.PermissionType = "User";
                    effectiveResult.PermissionUserId = userId;
                }
                else if (rolePerms.Any())
                {
                    effectiveResult.PermissionType = "Role";
                    effectiveResult.RoleId = rolePerms.First().IdRole;
                }

                // Добавляем только если есть хоть какие-то права
                if (effectiveResult.HasAnyPermission)
                {
                    result.Add(effectiveResult);
                }
            }

            return result.OrderBy(r => r.ObjectId).ToList();
        }

        // ===== 📊 СТАТИСТИКА =====

        public async Task<int> GetPermissionCountAsync()
        {
            return await _context.Set<_RPermission>().CountAsync();
        }

        public async Task<int> GetUserPermissionCountAsync(IRedbUser user)
        {
            return await GetUserPermissionCountAsync(user.Id);
        }

        public async Task<int> GetUserPermissionCountAsync(long userId)
        {
            // Подсчитываем прямые права пользователя
            var directPermissions = await _context.Set<_RPermission>()
                .Where(p => p.IdUser == userId)
                .CountAsync();

            // Подсчитываем права через роли
            var userRoles = await _context.Set<_RUsersRole>()
                .Where(ur => ur.IdUser == userId)
                .Select(ur => ur.IdRole)
                .ToListAsync();

            var rolePermissions = await _context.Set<_RPermission>()
                .Where(p => userRoles.Contains(p.IdRole ?? 0))
                .CountAsync();

            return directPermissions + rolePermissions;
        }

        public async Task<int> GetRolePermissionCountAsync(IRedbRole role)
        {
            return await GetRolePermissionCountAsync(role.Id);
        }

        public async Task<int> GetRolePermissionCountAsync(long roleId)
        {
            return await _context.Set<_RPermission>()
                .Where(p => p.IdRole == roleId)
                .CountAsync();
        }
    }
}