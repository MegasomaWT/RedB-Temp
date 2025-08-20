using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using redb.Core.Models.Contracts;
using redb.Core.Models.Entities;
using redb.Core.Models.Users;
using redb.Core.Providers;
using redb.Core.DBModels;
using redb.Core.Postgres.Security;

namespace redb.Core.Postgres.Providers
{
    /// <summary>
    /// PostgreSQL реализация провайдера пользователей
    /// </summary>
    public class PostgresUserProvider : IUserProvider
    {
        private readonly RedbContext _context;
        private readonly IRedbSecurityContext _securityContext;

        public PostgresUserProvider(RedbContext context, IRedbSecurityContext securityContext)
        {
            _context = context;
            _securityContext = securityContext;
        }

        // === CRUD ОПЕРАЦИИ ===

        public async Task<IRedbUser> CreateUserAsync(CreateUserRequest request, IRedbUser? currentUser = null)
        {
            // 1. Валидация входных данных
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Login))
                throw new ArgumentException("Логин не может быть пустым", nameof(request));

            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Пароль не может быть пустым", nameof(request));

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Имя не может быть пустым", nameof(request));

            // 2. Проверка уникальности логина
            if (!await IsLoginAvailableAsync(request.Login))
                throw new InvalidOperationException($"Логин '{request.Login}' уже занят");

            // 3. Хеширование пароля
            var hashedPassword = SimplePasswordHasher.HashPassword(request.Password);

            // 4. ГЕНЕРАЦИЯ ID ЧЕРЕЗ АСИНХРОННЫЙ МЕТОД
            var newUserId = await _context.GetNextKeyAsync();

            // 5. Создание нового пользователя
            var newUser = new _RUser
            {
                Id = newUserId,
                Login = request.Login,
                Password = hashedPassword,
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                Enabled = true,
                DateRegister = DateTime.Now,
                DateDismiss = null
            };

            // 6. Сохранение в БД
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // 7. Возврат созданного пользователя
            return RedbUser.FromEntity(newUser);
        }

        public async Task<IRedbUser> UpdateUserAsync(IRedbUser user, UpdateUserRequest request, IRedbUser? currentUser = null)
        {
            // Защита системных пользователей
            if (user.Id == 0 || user.Id == 1)
            {
                throw new InvalidOperationException($"Системный пользователь с ID {user.Id} не может быть изменен");
            }

            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            if (dbUser == null)
            {
                throw new ArgumentException($"Пользователь с ID {user.Id} не найден");
            }

            // Обновляем только те поля, которые указаны в запросе
            if (request.Login != null)
            {
                // Проверяем уникальность логина
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Login == request.Login && u.Id != user.Id);
                if (existingUser != null)
                {
                    throw new InvalidOperationException($"Пользователь с логином '{request.Login}' уже существует");
                }
                dbUser.Login = request.Login;
            }

            if (request.Name != null)
                dbUser.Name = request.Name;

            if (request.Phone != null)
                dbUser.Phone = request.Phone;

            if (request.Email != null)
                dbUser.Email = request.Email;

            if (request.Enabled.HasValue)
                dbUser.Enabled = request.Enabled.Value;

            if (request.DateDismiss.HasValue)
                dbUser.DateDismiss = request.DateDismiss.Value;

            // Обновляем роли если указаны
            if (request.RoleIds != null)
            {
                // Удаляем старые связи
                var existingRoles = await _context.Set<_RUsersRole>()
                    .Where(ur => ur.IdUser == user.Id)
                    .ToListAsync();
                _context.Set<_RUsersRole>().RemoveRange(existingRoles);

                // Добавляем новые связи
                foreach (var roleId in request.RoleIds)
                {
                    _context.Set<_RUsersRole>().Add(new _RUsersRole
                    {
                        IdUser = user.Id,
                        IdRole = roleId
                    });
                }
            }

            await _context.SaveChangesAsync();

            return RedbUser.FromEntity(dbUser);
        }

        public async Task<bool> DeleteUserAsync(IRedbUser user, IRedbUser? currentUser = null)
        {
            // Защита системных пользователей
            if (user.Id == 0 || user.Id == 1)
            {
                throw new InvalidOperationException($"Системный пользователь с ID {user.Id} не может быть удален");
            }

            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            if (dbUser == null)
            {
                return false; // Пользователь уже не существует
            }

            // Мягкое удаление - просто деактивация пользователя
            dbUser.Enabled = false;
            dbUser.DateDismiss = DateTime.UtcNow;
            
            // НЕ изменяем логин, чтобы избежать потенциальных конфликтов
            // Логин остается прежним, но пользователь деактивирован

            try 
            {
                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                // Логирование подробной информации об ошибке
                throw new InvalidOperationException(
                    $"Ошибка при удалении пользователя ID={user.Id}, Login='{user.Login}'. " +
                    $"Попытка установить Login='{dbUser.Login}' (длина: {dbUser.Login.Length}). " +
                    $"Детали: {ex.Message}", ex);
            }
        }

        // === ПОИСК И ПОЛУЧЕНИЕ ===

        public async Task<IRedbUser?> GetUserByIdAsync(long userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            return user != null ? RedbUser.FromEntity(user) : null;
        }

        public async Task<IRedbUser?> GetUserByLoginAsync(string login)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
            return user != null ? RedbUser.FromEntity(user) : null;
        }

        public async Task<IRedbUser> LoadUserAsync(string login)
        {
            var user = await GetUserByLoginAsync(login);
            if (user == null)
                throw new ArgumentException($"Пользователь с логином '{login}' не найден");
            return user;
        }

        public async Task<IRedbUser> LoadUserAsync(long userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
                throw new ArgumentException($"Пользователь с ID {userId} не найден");
            return user;
        }

        public async Task<List<IRedbUser>> GetUsersAsync(UserSearchCriteria? criteria = null)
        {
            var query = _context.Users.AsQueryable();

            // Применяем фильтры
            if (criteria != null)
            {
                // Исключение системных пользователей
                if (criteria.ExcludeSystemUsers)
                {
                    query = query.Where(u => u.Id != 0 && u.Id != 1);
                }

                // Фильтр по логину
                if (!string.IsNullOrEmpty(criteria.LoginPattern))
                {
                    query = query.Where(u => u.Login.Contains(criteria.LoginPattern));
                }

                // Фильтр по имени
                if (!string.IsNullOrEmpty(criteria.NamePattern))
                {
                    query = query.Where(u => u.Name != null && u.Name.Contains(criteria.NamePattern));
                }

                // Фильтр по email
                if (!string.IsNullOrEmpty(criteria.EmailPattern))
                {
                    query = query.Where(u => u.Email != null && u.Email.Contains(criteria.EmailPattern));
                }

                // Фильтр по статусу активности
                if (criteria.Enabled.HasValue)
                {
                    query = query.Where(u => u.Enabled == criteria.Enabled.Value);
                }

                // Фильтр по роли
                if (criteria.RoleId.HasValue)
                {
                    var usersInRole = _context.Set<_RUsersRole>()
                        .Where(ur => ur.IdRole == criteria.RoleId.Value)
                        .Select(ur => ur.IdUser);
                    query = query.Where(u => usersInRole.Contains(u.Id));
                }

                // Фильтр по дате регистрации
                if (criteria.RegisteredFrom.HasValue)
                {
                    query = query.Where(u => u.DateRegister >= criteria.RegisteredFrom.Value);
                }

                if (criteria.RegisteredTo.HasValue)
                {
                    query = query.Where(u => u.DateRegister <= criteria.RegisteredTo.Value);
                }

                // Применяем сортировку
                query = criteria.SortBy switch
                {
                    UserSortField.Id => criteria.SortDirection == UserSortDirection.Ascending
                        ? query.OrderBy(u => u.Id)
                        : query.OrderByDescending(u => u.Id),
                    UserSortField.Login => criteria.SortDirection == UserSortDirection.Ascending
                        ? query.OrderBy(u => u.Login)
                        : query.OrderByDescending(u => u.Login),
                    UserSortField.Name => criteria.SortDirection == UserSortDirection.Ascending
                        ? query.OrderBy(u => u.Name)
                        : query.OrderByDescending(u => u.Name),
                    UserSortField.Email => criteria.SortDirection == UserSortDirection.Ascending
                        ? query.OrderBy(u => u.Email)
                        : query.OrderByDescending(u => u.Email),
                    UserSortField.DateRegister => criteria.SortDirection == UserSortDirection.Ascending
                        ? query.OrderBy(u => u.DateRegister)
                        : query.OrderByDescending(u => u.DateRegister),
                    UserSortField.DateDismiss => criteria.SortDirection == UserSortDirection.Ascending
                        ? query.OrderBy(u => u.DateDismiss)
                        : query.OrderByDescending(u => u.DateDismiss),
                    UserSortField.Enabled => criteria.SortDirection == UserSortDirection.Ascending
                        ? query.OrderBy(u => u.Enabled)
                        : query.OrderByDescending(u => u.Enabled),
                    _ => query.OrderBy(u => u.Name)
                };

                // Применяем пагинацию
                if (criteria.Offset > 0)
                {
                    query = query.Skip(criteria.Offset);
                }

                if (criteria.Limit > 0)
                {
                    query = query.Take(criteria.Limit);
                }
            }
            else
            {
                // По умолчанию исключаем системных пользователей и сортируем по имени
                query = query
                    .Where(u => u.Id != 0 && u.Id != 1)
                    .OrderBy(u => u.Name)
                    .Take(100);
            }

            var users = await query.ToListAsync();
            return users.Select(u => (IRedbUser)RedbUser.FromEntity(u)).ToList();
        }

        // === АУТЕНТИФИКАЦИЯ ===

        public async Task<IRedbUser?> ValidateUserAsync(string login, string password)
        {
            // Валидация входных параметров
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                return null;

            try
            {
                // 1. Поиск пользователя по логину
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
                if (user == null) 
                    return null;

                // 2. Проверка что пользователь активен
                if (!user.Enabled) 
                    return null;

                // 3. Проверка пароля через SimplePasswordHasher
                if (!Security.SimplePasswordHasher.VerifyPassword(password, user.Password))
                    return null;

                // 4. При успешной аутентификации возвращаем пользователя
                return RedbUser.FromEntity(user);
            }
            catch (Exception ex)
            {
                // В случае любой ошибки возвращаем null (не раскрываем детали)
                // TODO: Добавить логирование ошибок
                return null;
            }
        }

        public async Task<bool> ChangePasswordAsync(IRedbUser user, string currentPassword, string newPassword, IRedbUser? currentUser = null)
        {
            // 1. Валидация входных параметров
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (string.IsNullOrWhiteSpace(currentPassword))
                throw new ArgumentException("Текущий пароль не может быть пустым", nameof(currentPassword));

            if (string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException("Новый пароль не может быть пустым", nameof(newPassword));

            // 2. Защита системных пользователей
            if (user.Id == 0 || user.Id == 1)
                throw new InvalidOperationException("Нельзя изменять пароль системных пользователей через обычный API");

            // 3. Загрузка пользователя из БД
            var dbUser = await _context.Users.FindAsync(user.Id);
            if (dbUser == null)
                throw new InvalidOperationException($"Пользователь с ID {user.Id} не найден");

            // 4. Проверка что пользователь активен
            if (!dbUser.Enabled)
                throw new InvalidOperationException("Нельзя изменять пароль отключенному пользователю");

            // 5. Проверка текущего пароля
            if (!SimplePasswordHasher.VerifyPassword(currentPassword, dbUser.Password))
                throw new UnauthorizedAccessException("Неверный текущий пароль");

            // 6. Проверка что новый пароль отличается от текущего
            if (SimplePasswordHasher.VerifyPassword(newPassword, dbUser.Password))
                throw new ArgumentException("Новый пароль должен отличаться от текущего");

            // 7. Хеширование нового пароля
            dbUser.Password = SimplePasswordHasher.HashPassword(newPassword);

            // 8. Сохранение изменений
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SetPasswordAsync(IRedbUser user, string newPassword, IRedbUser? currentUser = null)
        {
            // Защита системных пользователей (только для ID 1 - admin, ID 0 - system остается без изменений)
            if (user.Id == 0)
            {
                throw new InvalidOperationException("Пароль системного пользователя (ID 0) не может быть изменен");
            }

            // Проверяем права текущего пользователя (должен быть администратором)
            if (currentUser != null && currentUser.Id != 1 && currentUser.Id != user.Id)
            {
                throw new UnauthorizedAccessException("Только администратор может устанавливать пароли других пользователей");
            }

            // Валидация пароля
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            {
                throw new ArgumentException("Пароль должен содержать минимум 4 символа");
            }

            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            if (dbUser == null)
            {
                throw new ArgumentException($"Пользователь с ID {user.Id} не найден");
            }

            // Хешируем новый пароль
            dbUser.Password = SimplePasswordHasher.HashPassword(newPassword);
            
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        // === УПРАВЛЕНИЕ СТАТУСОМ ===

        public async Task<bool> EnableUserAsync(IRedbUser user, IRedbUser? currentUser = null)
        {
            // Защита системных пользователей - они всегда должны быть активными
            if (user.Id == 0 || user.Id == 1)
            {
                return true; // Системные пользователи всегда активны
            }

            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            if (dbUser == null)
            {
                throw new ArgumentException($"Пользователь с ID {user.Id} не найден");
            }

            // Если пользователь уже активен
            if (dbUser.Enabled)
            {
                return true; // Уже активен
            }

            // Активируем пользователя
            dbUser.Enabled = true;
            dbUser.DateDismiss = null; // Очищаем дату увольнения

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> DisableUserAsync(IRedbUser user, IRedbUser? currentUser = null)
        {
            // Защита системных пользователей
            if (user.Id == 0 || user.Id == 1)
            {
                throw new InvalidOperationException($"Системный пользователь с ID {user.Id} не может быть деактивирован");
            }

            // Защита от самоблокировки
            if (currentUser != null && currentUser.Id == user.Id)
            {
                throw new InvalidOperationException("Пользователь не может деактивировать самого себя");
            }

            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            if (dbUser == null)
            {
                throw new ArgumentException($"Пользователь с ID {user.Id} не найден");
            }

            // Если пользователь уже неактивен
            if (!dbUser.Enabled)
            {
                return true; // Уже деактивирован
            }

            // Деактивируем пользователя
            dbUser.Enabled = false;
            dbUser.DateDismiss = DateTime.UtcNow;

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        // === ВАЛИДАЦИЯ ===

        public async Task<UserValidationResult> ValidateUserDataAsync(CreateUserRequest request)
        {
            var result = new UserValidationResult();

            // Проверка логина
            if (string.IsNullOrWhiteSpace(request.Login))
            {
                result.AddError("Login", "Логин обязателен для заполнения");
            }
            else
            {
                if (request.Login.Length < 3)
                {
                    result.AddError("Login", "Логин должен содержать минимум 3 символа");
                }

                if (!await IsLoginAvailableAsync(request.Login))
                {
                    result.AddError("Login", $"Пользователь с логином '{request.Login}' уже существует");
                }

                // Проверка на запрещенные символы
                if (request.Login.Contains(" ") || request.Login.Contains("@"))
                {
                    result.AddError("Login", "Логин не может содержать пробелы или символ @");
                }
            }

            // Проверка имени
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                result.AddError("Name", "Имя обязательно для заполнения");
            }

            // Проверка пароля
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                result.AddError("Password", "Пароль обязателен для заполнения");
            }
            else if (request.Password.Length < 4)
            {
                result.AddError("Password", "Пароль должен содержать минимум 4 символа");
            }

            // Проверка email (если указан)
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                if (!request.Email.Contains("@") || !request.Email.Contains("."))
                {
                    result.AddError("Email", "Некорректный формат email");
                }

                // Проверка уникальности email
                var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
                if (emailExists)
                {
                    result.AddError("Email", $"Пользователь с email '{request.Email}' уже существует");
                }
            }

            // Проверка телефона (если указан)
            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                if (request.Phone.Length < 7)
                {
                    result.AddError("Phone", "Номер телефона слишком короткий");
                }
            }

            return result;
        }

        public async Task<bool> IsLoginAvailableAsync(string login, long? excludeUserId = null)
        {
            var query = _context.Users.Where(u => u.Login == login);
            if (excludeUserId.HasValue)
                query = query.Where(u => u.Id != excludeUserId.Value);
            
            return !await query.AnyAsync();
        }

        // === СТАТИСТИКА ===

        public async Task<int> GetUserCountAsync(bool includeDisabled = false)
        {
            var query = _context.Users.AsQueryable();
            if (!includeDisabled)
                query = query.Where(u => u.Enabled);
            
            return await query.CountAsync();
        }

        public async Task<int> GetActiveUserCountAsync(DateTime fromDate, DateTime toDate)
        {
            // Подсчет активных пользователей за период
            // Требует логирования активности пользователей
            throw new NotImplementedException("GetActiveUserCountAsync будет реализован в следующих итерациях");
        }

    }
}
