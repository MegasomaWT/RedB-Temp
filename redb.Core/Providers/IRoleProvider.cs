using System.Collections.Generic;
using System.Threading.Tasks;
using redb.Core.Models.Contracts;
using redb.Core.Models.Roles;

namespace redb.Core.Providers
{
    /// <summary>
    /// Провайдер для управления ролями пользователей
    /// Предоставляет CRUD операции для ролей и управление связями пользователь-роль
    /// </summary>
    public interface IRoleProvider
    {
        // === CRUD РОЛЕЙ ===
        
        /// <summary>
        /// Создать новую роль
        /// </summary>
        /// <param name="request">Данные для создания роли</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>Созданная роль</returns>
        Task<IRedbRole> CreateRoleAsync(CreateRoleRequest request, IRedbUser? currentUser = null);
        
        /// <summary>
        /// Обновить роль
        /// </summary>
        /// <param name="role">Роль для обновления</param>
        /// <param name="newName">Новое имя роли</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>Обновленная роль</returns>
        Task<IRedbRole> UpdateRoleAsync(IRedbRole role, string newName, IRedbUser? currentUser = null);
        
        /// <summary>
        /// Удалить роль
        /// При удалении роли все связанные разрешения также удаляются (каскадно)
        /// </summary>
        /// <param name="role">Роль для удаления</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>true если роль удалена</returns>
        Task<bool> DeleteRoleAsync(IRedbRole role, IRedbUser? currentUser = null);
        
        // === ПОИСК РОЛЕЙ ===
        
        /// <summary>
        /// Получить роль по ID
        /// </summary>
        /// <param name="roleId">ID роли</param>
        /// <returns>Роль или null если не найдена</returns>
        Task<IRedbRole?> GetRoleByIdAsync(long roleId);
        
        /// <summary>
        /// Получить роль по имени
        /// </summary>
        /// <param name="roleName">Имя роли</param>
        /// <returns>Роль или null если не найдена</returns>
        Task<IRedbRole?> GetRoleByNameAsync(string roleName);
        
        /// <summary>
        /// Загрузить роль по ID (с исключением если не найдена)
        /// </summary>
        /// <param name="roleId">ID роли</param>
        /// <returns>Роль</returns>
        /// <exception cref="ArgumentException">Если роль не найдена</exception>
        Task<IRedbRole> LoadRoleAsync(long roleId);
        
        /// <summary>
        /// Загрузить роль по имени (с исключением если не найдена)
        /// </summary>
        /// <param name="roleName">Имя роли</param>
        /// <returns>Роль</returns>
        /// <exception cref="ArgumentException">Если роль не найдена</exception>
        Task<IRedbRole> LoadRoleAsync(string roleName);
        
        /// <summary>
        /// Получить все роли
        /// </summary>
        /// <returns>Список всех ролей</returns>
        Task<List<IRedbRole>> GetRolesAsync();
        
        // === УПРАВЛЕНИЕ ПОЛЬЗОВАТЕЛЬ-РОЛЬ ===
        
        /// <summary>
        /// Назначить роль пользователю
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <param name="role">Роль</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>true если роль назначена</returns>
        Task<bool> AssignUserToRoleAsync(IRedbUser user, IRedbRole role, IRedbUser? currentUser = null);
        
        /// <summary>
        /// Убрать роль у пользователя
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <param name="role">Роль</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>true если роль убрана</returns>
        Task<bool> RemoveUserFromRoleAsync(IRedbUser user, IRedbRole role, IRedbUser? currentUser = null);
        
        /// <summary>
        /// Установить роли пользователя (заменить все существующие)
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <param name="roles">Массив ролей</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>true если роли установлены</returns>
        Task<bool> SetUserRolesAsync(IRedbUser user, IRedbRole[] roles, IRedbUser? currentUser = null);
        
        /// <summary>
        /// Получить роли пользователя
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <returns>Список ролей пользователя</returns>
        Task<List<IRedbRole>> GetUserRolesAsync(IRedbUser user);
        
        /// <summary>
        /// Получить пользователей роли
        /// </summary>
        /// <param name="role">Роль</param>
        /// <returns>Список пользователей с данной ролью</returns>
        Task<List<IRedbUser>> GetRoleUsersAsync(IRedbRole role);
        
        /// <summary>
        /// Проверить, есть ли у пользователя роль
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <param name="role">Роль</param>
        /// <returns>true если у пользователя есть роль</returns>
        Task<bool> UserHasRoleAsync(IRedbUser user, IRedbRole role);
        
        // === ВАЛИДАЦИЯ ===
        
        /// <summary>
        /// Проверить доступность имени роли
        /// </summary>
        /// <param name="roleName">Имя роли для проверки</param>
        /// <param name="excludeRole">Роль для исключения (при обновлении)</param>
        /// <returns>true если имя роли доступно</returns>
        Task<bool> IsRoleNameAvailableAsync(string roleName, IRedbRole? excludeRole = null);
        
        // === СТАТИСТИКА ===
        
        /// <summary>
        /// Получить количество ролей
        /// </summary>
        /// <returns>Количество ролей</returns>
        Task<int> GetRoleCountAsync();
        
        /// <summary>
        /// Получить количество пользователей в роли
        /// </summary>
        /// <param name="role">Роль</param>
        /// <returns>Количество пользователей в роли</returns>
        Task<int> GetRoleUserCountAsync(IRedbRole role);
        
        /// <summary>
        /// Получить статистику по ролям (роль -> количество пользователей)
        /// </summary>
        /// <returns>Словарь роль -> количество пользователей</returns>
        Task<Dictionary<IRedbRole, int>> GetRoleStatisticsAsync();
    }
}
