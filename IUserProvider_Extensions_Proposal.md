# Предложения по расширению IUserProvider

Анализ текущего интерфейса `IUserProvider.cs` показал, что он уже очень хорошо спроектирован и покрывает основные потребности управления пользователями. Однако предлагаются следующие дополнительные методы для более комплексного функционала:

## 1. Управление ролями пользователя (упрощенный API)

```csharp
// === УПРАВЛЕНИЕ РОЛЯМИ (УДОБНЫЕ МЕТОДЫ) ===

/// <summary>
/// Получить роли пользователя (удобный метод без обращения к IRoleProvider)
/// </summary>
/// <param name="user">Пользователь</param>
/// <returns>Список ролей пользователя</returns>
Task<List<IRedbRole>> GetUserRolesAsync(IRedbUser user);

/// <summary>
/// Назначить роли пользователю (удобный метод)
/// </summary>
/// <param name="user">Пользователь</param>
/// <param name="roleIds">Массив ID ролей</param>
/// <param name="currentUser">Текущий пользователь (для аудита)</param>
/// <returns>true если роли назначены</returns>
Task<bool> AssignRolesToUserAsync(IRedbUser user, long[] roleIds, IRedbUser? currentUser = null);

/// <summary>
/// Проверить есть ли у пользователя конкретная роль по ID
/// </summary>
/// <param name="user">Пользователь</param>
/// <param name="roleId">ID роли</param>
/// <returns>true если у пользователя есть роль</returns>
Task<bool> UserHasRoleAsync(IRedbUser user, long roleId);
```

## 2. Аудит и отслеживание активности

```csharp
// === АУДИТ И АКТИВНОСТЬ ===

/// <summary>
/// Обновить время последнего входа пользователя
/// </summary>
/// <param name="user">Пользователь</param>
Task UpdateLastLoginAsync(IRedbUser user);

/// <summary>
/// Получить историю входов пользователя
/// </summary>
/// <param name="user">Пользователь</param>
/// <param name="limit">Максимальное количество записей</param>
/// <returns>История входов</returns>
Task<List<UserLoginHistory>> GetUserLoginHistoryAsync(IRedbUser user, int limit = 10);

/// <summary>
/// Записать событие безопасности (неудачный вход, смена пароля и т.д.)
/// </summary>
/// <param name="user">Пользователь</param>
/// <param name="eventType">Тип события</param>
/// <param name="description">Описание события</param>
/// <param name="currentUser">Текущий пользователь (для аудита)</param>
Task LogSecurityEventAsync(IRedbUser user, string eventType, string description, IRedbUser? currentUser = null);
```

## 3. Расширенная безопасность паролей

```csharp
// === БЕЗОПАСНОСТЬ ПАРОЛЕЙ ===

/// <summary>
/// Проверить соответствие пароля политике безопасности
/// </summary>
/// <param name="password">Пароль для проверки</param>
/// <returns>Результат валидации пароля</returns>
Task<PasswordValidationResult> ValidatePasswordStrengthAsync(string password);

/// <summary>
/// Проверить не использовался ли пароль ранее
/// </summary>
/// <param name="user">Пользователь</param>
/// <param name="password">Пароль для проверки</param>
/// <param name="historyDepth">Глубина истории паролей</param>
/// <returns>true если пароль использовался ранее</returns>
Task<bool> IsPasswordUsedBeforeAsync(IRedbUser user, string password, int historyDepth = 5);

/// <summary>
/// Установить обязательную смену пароля при следующем входе
/// </summary>
/// <param name="user">Пользователь</param>
/// <param name="currentUser">Текущий пользователь (для аудита)</param>
/// <returns>true если флаг установлен</returns>
Task<bool> ForcePasswordChangeAsync(IRedbUser user, IRedbUser? currentUser = null);

/// <summary>
/// Заблокировать пользователя временно (после неудачных попыток входа)
/// </summary>
/// <param name="user">Пользователь</param>
/// <param name="lockDuration">Длительность блокировки</param>
/// <param name="reason">Причина блокировки</param>
/// <param name="currentUser">Текущий пользователь (для аудита)</param>
/// <returns>true если пользователь заблокирован</returns>
Task<bool> LockUserTemporarilyAsync(IRedbUser user, TimeSpan lockDuration, string reason, IRedbUser? currentUser = null);
```

## 4. Массовые операции

```csharp
// === МАССОВЫЕ ОПЕРАЦИИ ===

/// <summary>
/// Импортировать пользователей из CSV или другого источника
/// </summary>
/// <param name="users">Список пользователей для импорта</param>
/// <param name="currentUser">Текущий пользователь (для аудита)</param>
/// <returns>Результат импорта</returns>
Task<UserImportResult> ImportUsersAsync(List<CreateUserRequest> users, IRedbUser? currentUser = null);

/// <summary>
/// Экспортировать пользователей для резервного копирования
/// </summary>
/// <param name="criteria">Критерии отбора пользователей</param>
/// <returns>Данные для экспорта</returns>
Task<List<UserExportData>> ExportUsersAsync(UserSearchCriteria? criteria = null);

/// <summary>
/// Массовое обновление статуса пользователей
/// </summary>
/// <param name="userIds">Массив ID пользователей</param>
/// <param name="enabled">Новый статус</param>
/// <param name="currentUser">Текущий пользователь (для аудита)</param>
/// <returns>Количество обновленных пользователей</returns>
Task<int> BulkUpdateStatusAsync(long[] userIds, bool enabled, IRedbUser? currentUser = null);
```

## 5. Расширенная статистика и мониторинг

```csharp
// === РАСШИРЕННАЯ СТАТИСТИКА ===

/// <summary>
/// Получить пользователей с истекающими паролями
/// </summary>
/// <param name="daysBeforeExpiry">Количество дней до истечения</param>
/// <returns>Список пользователей</returns>
Task<List<IRedbUser>> GetUsersWithExpiringPasswordsAsync(int daysBeforeExpiry = 30);

/// <summary>
/// Получить заблокированных пользователей
/// </summary>
/// <returns>Информация о заблокированных пользователях</returns>
Task<List<UserLockInfo>> GetLockedUsersAsync();

/// <summary>
/// Получить неактивных пользователей (не входили в систему долго)
/// </summary>
/// <param name="inactiveDays">Количество дней неактивности</param>
/// <returns>Список неактивных пользователей</returns>
Task<List<IRedbUser>> GetInactiveUsersAsync(int inactiveDays = 90);

/// <summary>
/// Получить статистику по пользователям
/// </summary>
/// <returns>Общая статистика пользователей</returns>
Task<UserStatistics> GetUserStatisticsAsync();
```

## 6. Работа с профилями

```csharp
// === ПРОФИЛИ ПОЛЬЗОВАТЕЛЕЙ ===

/// <summary>
/// Обновить аватар пользователя
/// </summary>
/// <param name="user">Пользователь</param>
/// <param name="avatarData">Данные аватара</param>
/// <param name="currentUser">Текущий пользователь (для аудита)</param>
/// <returns>true если аватар обновлен</returns>
Task<bool> UpdateUserAvatarAsync(IRedbUser user, byte[] avatarData, IRedbUser? currentUser = null);

/// <summary>
/// Получить расширенную информацию профиля
/// </summary>
/// <param name="user">Пользователь</param>
/// <returns>Профиль пользователя</returns>
Task<UserProfile> GetUserProfileAsync(IRedbUser user);

/// <summary>
/// Обновить настройки пользователя (язык, тема и т.д.)
/// </summary>
/// <param name="user">Пользователь</param>
/// <param name="settings">Словарь настроек</param>
/// <param name="currentUser">Текущий пользователь (для аудита)</param>
/// <returns>true если настройки обновлены</returns>
Task<bool> UpdateUserSettingsAsync(IRedbUser user, Dictionary<string, object> settings, IRedbUser? currentUser = null);
```

## Необходимые дополнительные модели

Для реализации предложенных методов потребуются следующие модели:

### UserLoginHistory
```csharp
public class UserLoginHistory
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public DateTime LoginTime { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
}
```

### PasswordValidationResult
```csharp
public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public int StrengthScore { get; set; } // 0-100
}
```

### UserImportResult
```csharp
public class UserImportResult
{
    public int TotalUsers { get; set; }
    public int SuccessfulImports { get; set; }
    public int FailedImports { get; set; }
    public List<string> Errors { get; set; } = new();
}
```

### UserExportData
```csharp
public class UserExportData
{
    public long Id { get; set; }
    public string Login { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool Enabled { get; set; }
    public DateTime DateRegister { get; set; }
    public DateTime? DateDismiss { get; set; }
    public List<string> Roles { get; set; } = new();
}
```

### UserLockInfo
```csharp
public class UserLockInfo
{
    public IRedbUser User { get; set; }
    public DateTime LockTime { get; set; }
    public DateTime? UnlockTime { get; set; }
    public string Reason { get; set; } = "";
    public bool IsTemporary { get; set; }
}
```

### UserStatistics
```csharp
public class UserStatistics
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int DisabledUsers { get; set; }
    public int UsersRegisteredToday { get; set; }
    public int UsersRegisteredThisWeek { get; set; }
    public int UsersRegisteredThisMonth { get; set; }
    public int LockedUsers { get; set; }
    public int UsersWithExpiredPasswords { get; set; }
    public Dictionary<string, int> UsersByRole { get; set; } = new();
}
```

### UserProfile
```csharp
public class UserProfile
{
    public IRedbUser User { get; set; }
    public byte[]? Avatar { get; set; }
    public DateTime? LastLogin { get; set; }
    public string? LastLoginIp { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
    public List<IRedbRole> Roles { get; set; } = new();
    public int LoginCount { get; set; }
    public bool IsLocked { get; set; }
    public bool ForcePasswordChange { get; set; }
}
```

## Ключевые преимущества предложенных дополнений

1. **Безопасность** - расширенная работа с паролями, аудит действий
2. **Удобство администрирования** - массовые операции, статистика
3. **Мониторинг** - отслеживание активности, заблокированных пользователей
4. **Интеграция** - упрощенная работа с ролями прямо через UserProvider

Все предложенные методы дополняют существующий интерфейс, не нарушая его архитектуру, и покрывают типичные потребности корпоративных систем управления пользователями.
