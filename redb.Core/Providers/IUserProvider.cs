using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using redb.Core.Models.Contracts;
using redb.Core.Models.Users;

namespace redb.Core.Providers
{
    /// <summary>
    /// Провайдер для управления пользователями
    /// Предоставляет CRUD операции и бизнес-логику для работы с пользователями
    /// </summary>
    public interface IUserProvider
    {
        // === CRUD ОПЕРАЦИИ ===
        
        /// <summary>
        /// Создать нового пользователя
        /// </summary>
        /// <param name="request">Данные для создания пользователя</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>Созданный пользователь</returns>
        Task<IRedbUser> CreateUserAsync(CreateUserRequest request, IRedbUser? currentUser = null);
        
        /// <summary>
        /// Обновить данные пользователя
        /// </summary>
        /// <param name="user">Пользователь для обновления</param>
        /// <param name="request">Новые данные пользователя</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>Обновленный пользователь</returns>
        Task<IRedbUser> UpdateUserAsync(IRedbUser user, UpdateUserRequest request, IRedbUser? currentUser = null);
        
        /// <summary>
        /// Удалить пользователя (мягкое удаление - деактивация)
        /// Системные пользователи (ID 0, 1) не могут быть удалены
        /// </summary>
        /// <param name="user">Пользователь для удаления</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>true если пользователь удален</returns>
        Task<bool> DeleteUserAsync(IRedbUser user, IRedbUser? currentUser = null);
        
        // === ПОИСК И ПОЛУЧЕНИЕ ===
        
        /// <summary>
        /// Получить пользователя по ID
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>Пользователь или null если не найден</returns>
        Task<IRedbUser?> GetUserByIdAsync(long userId);
        
        /// <summary>
        /// Получить пользователя по логину
        /// </summary>
        /// <param name="login">Логин пользователя</param>
        /// <returns>Пользователь или null если не найден</returns>
        Task<IRedbUser?> GetUserByLoginAsync(string login);
        
        /// <summary>
        /// Загрузить пользователя по логину (с исключением если не найден)
        /// </summary>
        /// <param name="login">Логин пользователя</param>
        /// <returns>Пользователь</returns>
        /// <exception cref="ArgumentException">Если пользователь не найден</exception>
        Task<IRedbUser> LoadUserAsync(string login);
        
        /// <summary>
        /// Загрузить пользователя по ID (с исключением если не найден)
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>Пользователь</returns>
        /// <exception cref="ArgumentException">Если пользователь не найден</exception>
        Task<IRedbUser> LoadUserAsync(long userId);
        
        /// <summary>
        /// Получить список пользователей с фильтрацией
        /// </summary>
        /// <param name="criteria">Критерии поиска (может быть null)</param>
        /// <returns>Список пользователей</returns>
        Task<List<IRedbUser>> GetUsersAsync(UserSearchCriteria? criteria = null);
        
        // === АУТЕНТИФИКАЦИЯ ===
        
        /// <summary>
        /// Проверить логин и пароль пользователя
        /// </summary>
        /// <param name="login">Логин</param>
        /// <param name="password">Пароль (в открытом виде)</param>
        /// <returns>Пользователь если данные верны, null если неверны</returns>
        Task<IRedbUser?> ValidateUserAsync(string login, string password);
        
        /// <summary>
        /// Изменить пароль пользователя
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <param name="currentPassword">Текущий пароль (для проверки)</param>
        /// <param name="newPassword">Новый пароль</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>true если пароль изменен</returns>
        Task<bool> ChangePasswordAsync(IRedbUser user, string currentPassword, string newPassword, IRedbUser? currentUser = null);
        
        /// <summary>
        /// Установить новый пароль пользователю (без проверки старого)
        /// Только для администраторов
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <param name="newPassword">Новый пароль</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>true если пароль установлен</returns>
        Task<bool> SetPasswordAsync(IRedbUser user, string newPassword, IRedbUser? currentUser = null);
        
        // === УПРАВЛЕНИЕ СТАТУСОМ ===
        
        /// <summary>
        /// Активировать пользователя
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>true если пользователь активирован</returns>
        Task<bool> EnableUserAsync(IRedbUser user, IRedbUser? currentUser = null);
        
        /// <summary>
        /// Деактивировать пользователя
        /// Системные пользователи (ID 0, 1) не могут быть деактивированы
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <param name="currentUser">Текущий пользователь (для аудита)</param>
        /// <returns>true если пользователь деактивирован</returns>
        Task<bool> DisableUserAsync(IRedbUser user, IRedbUser? currentUser = null);
        
        // === ВАЛИДАЦИЯ ===
        
        /// <summary>
        /// Проверить корректность данных пользователя
        /// </summary>
        /// <param name="request">Данные для проверки</param>
        /// <returns>Результат валидации</returns>
        Task<UserValidationResult> ValidateUserDataAsync(CreateUserRequest request);
        
        /// <summary>
        /// Проверить доступность логина
        /// </summary>
        /// <param name="login">Логин для проверки</param>
        /// <param name="excludeUserId">ID пользователя для исключения (при обновлении)</param>
        /// <returns>true если логин доступен</returns>
        Task<bool> IsLoginAvailableAsync(string login, long? excludeUserId = null);
        
        // === СТАТИСТИКА ===
        
        /// <summary>
        /// Получить количество пользователей
        /// </summary>
        /// <param name="includeDisabled">Включать деактивированных пользователей</param>
        /// <returns>Количество пользователей</returns>
        Task<int> GetUserCountAsync(bool includeDisabled = false);
        
        /// <summary>
        /// Получить количество активных пользователей за период
        /// </summary>
        /// <param name="fromDate">Начальная дата</param>
        /// <param name="toDate">Конечная дата</param>
        /// <returns>Количество активных пользователей</returns>
        Task<int> GetActiveUserCountAsync(DateTime fromDate, DateTime toDate);
    }
}
