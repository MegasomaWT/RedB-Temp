using Microsoft.EntityFrameworkCore;
using redb.Core.Models.Contracts;
using redb.Core.Models.Entities;
using redb.Core.Models.Enums;
using redb.Core.Models.Permissions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace redb.Core.Providers
{
    /// <summary>
    /// Провайдер для работы с правами доступа
    /// </summary>
    public interface IPermissionProvider
    {
        // ===== БАЗОВЫЕ МЕТОДЫ (используют _securityContext по умолчанию) =====
        
        /// <summary>
        /// Получить id объектов, доступных текущему пользователю на чтение
        /// </summary>
        IQueryable<long> GetReadableObjectIds();
        
        /// <summary>
        /// Проверить, может ли текущий пользователь редактировать объект
        /// </summary>
        Task<bool> CanUserEditObject(IRedbObject obj);
        
        /// <summary>
        /// Проверить, может ли текущий пользователь читать объект
        /// </summary>
        Task<bool> CanUserSelectObject(IRedbObject obj);
        
        /// <summary>
        /// Проверить, может ли текущий пользователь создавать объекты в схеме
        /// </summary>
        Task<bool> CanUserInsertScheme(IRedbScheme scheme);
        
        /// <summary>
        /// Проверить, может ли текущий пользователь удалить объект
        /// </summary>
        Task<bool> CanUserDeleteObject(IRedbObject obj);

        // ===== ПЕРЕГРУЗКИ С ЯВНЫМ ПОЛЬЗОВАТЕЛЕМ =====
        
        /// <summary>
        /// Получить id объектов, доступных пользователю на чтение
        /// </summary>
        IQueryable<long> GetReadableObjectIds(IRedbUser user);
        
        /// <summary>
        /// Проверить, может ли пользователь редактировать объект
        /// </summary>
        Task<bool> CanUserEditObject(IRedbObject obj, IRedbUser user);
        
        /// <summary>
        /// Проверить, может ли пользователь читать объект
        /// </summary>
        Task<bool> CanUserSelectObject(IRedbObject obj, IRedbUser user);
        
        /// <summary>
        /// Проверить, может ли пользователь создавать объекты в схеме
        /// </summary>
        Task<bool> CanUserInsertScheme(IRedbScheme scheme, IRedbUser user);
        
        /// <summary>
        /// Проверить, может ли пользователь удалить объект
        /// </summary>
        Task<bool> CanUserDeleteObject(IRedbObject obj, IRedbUser user);

        // ===== 🚀 КРАСИВЫЕ МЕТОДЫ С ОБЪЕКТАМИ =====
        
        /// <summary>
        /// Проверить, может ли текущий пользователь редактировать объект
        /// </summary>
        Task<bool> CanUserEditObject(RedbObject obj);
        
        /// <summary>
        /// Проверить, может ли текущий пользователь читать объект
        /// </summary>
        Task<bool> CanUserSelectObject(RedbObject obj);
        
        /// <summary>
        /// Проверить, может ли текущий пользователь удалить объект
        /// </summary>
        Task<bool> CanUserDeleteObject(RedbObject obj);
        
        /// <summary>
        /// Проверить, может ли пользователь редактировать объект
        /// </summary>
        Task<bool> CanUserEditObject(RedbObject obj, IRedbUser user);
        
        /// <summary>
        /// Проверить, может ли пользователь читать объект
        /// </summary>
        Task<bool> CanUserSelectObject(RedbObject obj, IRedbUser user);
        
        /// <summary>
        /// Проверить, может ли пользователь создавать объекты в схеме объекта
        /// </summary>
        Task<bool> CanUserInsertScheme(RedbObject obj, IRedbUser user);
        
        /// <summary>
        /// Проверить, может ли пользователь удалить объект
        /// </summary>
        Task<bool> CanUserDeleteObject(RedbObject obj, IRedbUser user);

        // ===== 🔧 НОВЫЕ CRUD МЕТОДЫ ДЛЯ РАЗРЕШЕНИЙ =====
        
        /// <summary>
        /// Создать новое разрешение
        /// </summary>
        /// <param name="request">Данные разрешения</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>Созданное разрешение</returns>
        Task<IRedbPermission> CreatePermissionAsync(PermissionRequest request, IRedbUser? currentUser = null);
        
        /// <summary>
        /// Обновить разрешение
        /// </summary>
        /// <param name="permission">Разрешение для обновления</param>
        /// <param name="request">Новые данные разрешения</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>Обновленное разрешение</returns>
        Task<IRedbPermission> UpdatePermissionAsync(IRedbPermission permission, PermissionRequest request, IRedbUser? currentUser = null);
        
        /// <summary>
        /// Удалить разрешение
        /// </summary>
        /// <param name="permission">Разрешение для удаления</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>true если разрешение удалено</returns>
        Task<bool> DeletePermissionAsync(IRedbPermission permission, IRedbUser? currentUser = null);
        
        // ===== 🔍 ПОИСК РАЗРЕШЕНИЙ =====
        
        /// <summary>
        /// Получить разрешения пользователя
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <returns>Список разрешений пользователя</returns>
        Task<List<IRedbPermission>> GetPermissionsByUserAsync(IRedbUser user);
        
        /// <summary>
        /// Получить разрешения роли
        /// </summary>
        /// <param name="role">Роль</param>
        /// <returns>Список разрешений роли</returns>
        Task<List<IRedbPermission>> GetPermissionsByRoleAsync(IRedbRole role);
        
        /// <summary>
        /// Получить разрешения на объект
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <returns>Список разрешений на объект</returns>
        Task<List<IRedbPermission>> GetPermissionsByObjectAsync(IRedbObject obj);
        
        /// <summary>
        /// Получить разрешение по ID
        /// </summary>
        /// <param name="permissionId">ID разрешения</param>
        /// <returns>Разрешение или null если не найдено</returns>
        Task<IRedbPermission?> GetPermissionByIdAsync(long permissionId);
        
        // ===== 🎯 УПРАВЛЕНИЕ РАЗРЕШЕНИЯМИ =====
        
        /// <summary>
        /// Назначить разрешение пользователю
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <param name="obj">Объект</param>
        /// <param name="actions">Действия разрешения</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>true если разрешение назначено</returns>
        Task<bool> GrantPermissionAsync(IRedbUser user, IRedbObject obj, PermissionAction actions, IRedbUser? currentUser = null);
        
        /// <summary>
        /// Назначить разрешение роли
        /// </summary>
        /// <param name="role">Роль</param>
        /// <param name="obj">Объект</param>
        /// <param name="actions">Действия разрешения</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>true если разрешение назначено</returns>
        Task<bool> GrantPermissionAsync(IRedbRole role, IRedbObject obj, PermissionAction actions, IRedbUser? currentUser = null);
        
        /// <summary>
        /// Отозвать разрешение у пользователя
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <param name="obj">Объект</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>true если разрешение отозвано</returns>
        Task<bool> RevokePermissionAsync(IRedbUser user, IRedbObject obj, IRedbUser? currentUser = null);
        
        /// <summary>
        /// Отозвать разрешение у роли
        /// </summary>
        /// <param name="role">Роль</param>
        /// <param name="obj">Объект</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>true если разрешение отозвано</returns>
        Task<bool> RevokePermissionAsync(IRedbRole role, IRedbObject obj, IRedbUser? currentUser = null);
        
        /// <summary>
        /// Отозвать все разрешения пользователя
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>Количество отозванных разрешений</returns>
        Task<int> RevokeAllUserPermissionsAsync(IRedbUser user, IRedbUser? currentUser = null);
        
        /// <summary>
        /// Отозвать все разрешения роли
        /// </summary>
        /// <param name="role">Роль</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>Количество отозванных разрешений</returns>
        Task<int> RevokeAllRolePermissionsAsync(IRedbRole role, IRedbUser? currentUser = null);
        
        // ===== 📊 ЭФФЕКТИВНЫЕ ПРАВА =====
        
        /// <summary>
        /// Получить эффективные права пользователя на объект (с учетом наследования и ролей)
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <param name="obj">Объект</param>
        /// <returns>Эффективные права пользователя</returns>
        Task<EffectivePermissionResult> GetEffectivePermissionsAsync(IRedbUser user, IRedbObject obj);
        
        /// <summary>
        /// Получить эффективные права пользователя на несколько объектов (пакетно)
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <param name="objects">Массив объектов</param>
        /// <returns>Словарь объект -> эффективные права</returns>
        Task<Dictionary<IRedbObject, EffectivePermissionResult>> GetEffectivePermissionsBatchAsync(IRedbUser user, IRedbObject[] objects);
        
        /// <summary>
        /// Получить все эффективные права пользователя
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <returns>Список всех эффективных прав пользователя</returns>
        Task<List<EffectivePermissionResult>> GetAllEffectivePermissionsAsync(IRedbUser user);
        
        // ===== 📈 СТАТИСТИКА =====
        
        /// <summary>
        /// Получить количество разрешений
        /// </summary>
        /// <returns>Общее количество разрешений</returns>
        Task<int> GetPermissionCountAsync();
        
        /// <summary>
        /// Получить количество разрешений пользователя
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <returns>Количество разрешений пользователя</returns>
        Task<int> GetUserPermissionCountAsync(IRedbUser user);
        
        /// <summary>
        /// Получить количество разрешений роли
        /// </summary>
        /// <param name="role">Роль</param>
        /// <returns>Количество разрешений роли</returns>
        Task<int> GetRolePermissionCountAsync(IRedbRole role);

        //=== для низко уровневого доступа
        Task<bool> CanUserEditObject(long objectId, long userId);

        Task<bool> CanUserSelectObject(long objectId, long userId);

        Task<bool> CanUserInsertScheme(long schemeId, long userId);

        Task<bool> CanUserDeleteObject(long objectId, long userId);
    }
}
