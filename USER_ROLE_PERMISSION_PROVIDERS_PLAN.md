# 📋 ПЛАН РЕАЛИЗАЦИИ ПРОВАЙДЕРОВ ДЛЯ ПОЛЬЗОВАТЕЛЕЙ, РОЛЕЙ И РАЗРЕШЕНИЙ

## 🎯 **ЦЕЛЬ**
Создать полноценные провайдеры для управления пользователями, ролями и разрешениями с красивой архитектурой и всей необходимой бизнес-логикой.

---

## 📊 **АНАЛИЗ СУЩЕСТВУЮЩЕГО КОДА**

### **✅ ЧТО УЖЕ ЕСТЬ:**

#### **🗄️ Модели БД (EF Core):**
- **`_RUser`** - пользователи с полями: Id, Login, Password, Name, Phone, Email, DateRegister, DateDismiss, Enabled
- **`_RRole`** - роли с полями: Id, Name
- **`_RUsersRole`** - связка пользователей с ролями: Id, IdRole, IdUser
- **`_RPermission`** - разрешения: Id, IdRole, IdUser, IdRef, Select, Insert, Update, Delete

#### **🔌 Интерфейсы и модели:**
- **`IRedbUser`** - интерфейс пользователя (наследует IRedbObject)
- **`RedbUser`** - реализация пользователя с методом `FromEntity(_RUser)`
- **`IRedbSecurityContext`** - контекст безопасности
- **`RedbSecurityContext`** - реализация контекста с fallback логикой

#### **📊 EF Core доступ:**
- **`context.Users`** - DbSet<_RUser>
- **`context.Roles`** - DbSet<_RRole>
- **`context.UsersRoles`** - DbSet<_RUsersRole>
- **`context.Permissions`** - DbSet<_RPermission>

#### **🔐 Проверка прав:**
- **`IPermissionProvider`** - только проверка прав (CanUser*, GetReadableObjectIds)
- **SQL функции** - `get_user_permissions_for_object()`, VIEW `v_user_permissions`

### **❌ ЧТО ОТСУТСТВУЕТ:**
- **Провайдеры CRUD** для пользователей, ролей, разрешений
- **Бизнес-логика** (валидация, хеширование паролей, аудит)
- **Высокоуровневые методы** управления


!!!ВАЖНО НЕ ПРОПУСКАЕМ В ЭТАПАХ РЕАЛИЗАЦИИ ЕСЛИ НЕТ СПЕЦИАЛЬНОЙ МЕТКИ!!!
!!!ТЕСТЫ НАДО ДЕЛАТЬ ПОЛНОЦЕННЫЕ!!!
---

## 📝 **ЭТАПЫ РЕАЛИЗАЦИИ**

### **ЭТАП 1: ИНТЕРФЕЙСЫ ПРОВАЙДЕРОВ** ✅ **ЗАВЕРШЕН**
- [x] 1.1. **`IUserProvider`** - CRUD пользователей ✅
  - [x] `CreateUserAsync()` - создание пользователя
  - [x] `UpdateUserAsync()` - обновление пользователя
  - [x] `DeleteUserAsync()` - удаление/деактивация пользователя
  - [x] `GetUserByIdAsync()` - получение по ID
  - [x] `GetUserByLoginAsync()` - получение по логину
  - [x] `LoadUserAsync(string login)` - загрузка пользователя по логину
  - [x] `LoadUserAsync(long id)` - загрузка пользователя по ID
  - [x] `GetUsersAsync()` - список пользователей с фильтрацией
  - [x] `ValidateUserAsync()` - проверка логина/пароля
  - [x] `ChangePasswordAsync()` - смена пароля
  - [x] `EnableUserAsync()` / `DisableUserAsync()` - активация/деактивация

- [x] 1.2. **`IRoleProvider`** - CRUD ролей ✅
  - [x] `CreateRoleAsync()` - создание роли
  - [x] `UpdateRoleAsync()` - обновление роли
  - [x] `DeleteRoleAsync()` - удаление роли
  - [x] `GetRoleByIdAsync()` - получение по ID
  - [x] `GetRoleByNameAsync()` - получение по имени
  - [x] `GetRolesAsync()` - список ролей
  - [x] `AssignUserToRoleAsync()` - назначение роли пользователю
  - [x] `RemoveUserFromRoleAsync()` - удаление роли у пользователя
  - [x] `GetUserRolesAsync()` - роли пользователя
  - [x] `GetRoleUsersAsync()` - пользователи роли

- [x] 1.3. **Расширение `IPermissionProvider`** - CRUD разрешений ✅
  - [x] `CreatePermissionAsync()` - создание разрешения
  - [x] `UpdatePermissionAsync()` - обновление разрешения
  - [x] `DeletePermissionAsync()` - удаление разрешения
  - [x] `GetPermissionsByUserAsync()` - разрешения пользователя
  - [x] `GetPermissionsByRoleAsync()` - разрешения роли
  - [x] `GetPermissionsByObjectAsync()` - разрешения на объект
  - [x] `GrantPermissionAsync()` - назначение разрешения
  - [x] `RevokePermissionAsync()` - отзыв разрешения
  - [x] `GetEffectivePermissionsAsync()` - эффективные права (с наследованием)

### **ЭТАП 2: БИЗНЕС-МОДЕЛИ И ЕНУМЫ** ✅ **ЗАВЕРШЕН**
- [x] 2.1. **Модели запросов и результатов** ✅
  - [x] `CreateUserRequest` - запрос создания пользователя
  - [x] `UpdateUserRequest` - запрос обновления пользователя
  - [x] `UserSearchCriteria` - критерии поиска пользователей
  - [x] `CreateRoleRequest` - запрос создания роли
  - [x] `PermissionRequest` - запрос на разрешение
  - [x] `EffectivePermissionResult` - результат эффективных прав

- [x] 2.2. **Енумы и константы** ✅
  - [x] `UserStatus` - статусы пользователя (Active, Disabled, Dismissed)
  - [x] `PermissionAction` - действия разрешений (Select, Insert, Update, Delete)
  - [x] `UserSortField` / `UserSortDirection` - сортировка пользователей

- [x] 2.3. **Модели валидации** ✅
  - [x] `UserValidationResult` - результат валидации пользователя
  - [x] `ValidationError` - ошибка валидации
  - [ ] `PasswordPolicy` - политика паролей (отложено это пока не надо !!! без моего разрешения)

### **ЭТАП 3: РЕАЛИЗАЦИЯ ПРОВАЙДЕРОВ (PostgreSQL)** ❌ **ТРЕБУЕТ ВОССТАНОВЛЕНИЯ**

#### **🚨 АНАЛИЗ КАСКАДНЫХ УДАЛЕНИЙ В БД:**
- ✅ **`_users_roles`**: `ON DELETE CASCADE` для `_id_role` и `_id_user` - при удалении пользователя/роли автоматически удаляются связи
- ✅ **`_permissions`**: `ON DELETE CASCADE` для `_id_role` и `_id_user` - при удалении пользователя/роли автоматически удаляются разрешения
- ✅ **Защита системных пользователей**: триггер `protect_system_users()` запрещает удаление пользователей ID 0,1

#### **📋 СТАТУС РЕАЛИЗАЦИИ:**
- ❌ 3.1. **`PostgresUserProvider`** - **ФАЙЛ УДАЛЕН, НУЖНО ВОССТАНОВИТЬ**
  - [x] ~~Создан класс с базовой структурой~~ - ПОТЕРЯНО
  - [x] ~~Реализованы простые методы~~ - ПОТЕРЯНО  
  - [x] ~~Добавлены заглушки для всех методов~~ - ПОТЕРЯНО
  - [x] ~~Интеграция с `IRedbSecurityContext`~~ - ПОТЕРЯНО
  - [x] ~~Хеширование паролей (SimplePasswordHasher)~~ - ПОТЕРЯНО
  - [x] ~~Валидация данных~~ - ПОТЕРЯНО
  - [x] ~~Полная реализация CRUD~~ - ПОТЕРЯНО

- ❌ 3.2. **`PostgresRoleProvider`** - **ФАЙЛ УДАЛЕН, НУЖНО ВОССТАНОВИТЬ**
  - [x] ~~Создан класс с базовой структурой~~ - ПОТЕРЯНО
  - [x] ~~Реализованы простые методы~~ - ПОТЕРЯНО
  - [x] ~~Добавлены заглушки для всех методов~~ - ПОТЕРЯНО
  - [x] ~~Интеграция с `IRedbSecurityContext`~~ - ПОТЕРЯНО
  - [x] ~~Управление связями пользователь-роль~~ - ПОТЕРЯНО
  - [x] ~~Каскадное удаление разрешений при удалении роли~~ - ПОТЕРЯНО
  - [x] ~~Валидация уникальности имен ролей~~ - ПОТЕРЯНО

- ❌ 3.3. **Расширение `PostgresPermissionProvider`** - **ЗАГЛУШКИ ОТСУТСТВУЮТ**
  - [ ] Добавлены заглушки для всех новых CRUD методов - НЕТ ЗАГЛУШЕК
  - [x] Интеграция с существующими методами проверки прав - ОК
  - [ ] Реализация CRUD методов - TODO
  - [ ] Кеширование разрешений - TODO
  - [ ] Инвалидация кеша при изменениях - TODO

### **ЭТАП 4: ИНТЕГРАЦИЯ С REDBSERVICE** ✅ **ЗАВЕРШЕН**
- [x] 4.1. **Обновление `IRedbService`** ✅
  - [x] Добавление свойств провайдеров
  - [x] `IUserProvider UserProvider { get; }`
  - [x] `IRoleProvider RoleProvider { get; }`
  - [x] Обновленный `IPermissionProvider PermissionProvider { get; }`

- [x] 4.2. **Обновление `RedbService`** ✅
  - [x] Инициализация новых провайдеров в конструкторе
  - [x] Передача зависимостей (context, securityContext, logger)
  - [x] Делегирование методов к провайдерам
  - [x] **Тесты:** Stage26 (интерфейсы) и Stage27 (интеграция) - все проходят
  - ❌ **ПРОБЛЕМА:** Stage26 и Stage27 не запускаются из-за ошибок компиляции

### **ЭТАП 5: БЕЗОПАСНОСТЬ И ВАЛИДАЦИЯ**
- [ ] 5.1. **Хеширование паролей**
  - [ ] Интерфейс `IPasswordHasher`
  - [ ] Реализация с BCrypt
  - [ ] Методы `HashPassword()`, `VerifyPassword()`

- [ ] 5.2. **Валидация данных**
  - [ ] Валидация логинов (уникальность, формат)
  - [ ] Валидация паролей (сложность, длина)
  - [ ] Валидация email и телефонов
  - [ ] Валидация имен ролей

- [ ] 5.3. **Аудит и логирование**
  - [ ] Логирование всех операций CRUD
  - [ ] Аудит изменений разрешений
  - [ ] Отслеживание попыток входа

### **ЭТАП 6: КЕШИРОВАНИЕ И ПРОИЗВОДИТЕЛЬНОСТЬ**
- [ ] 6.1. **Кеширование пользователей**
  - [ ] Кеш пользователей по ID и логину
  - [ ] Инвалидация при изменениях
  - [ ] TTL для кеша

- [ ] 6.2. **Кеширование разрешений**
  - [ ] Кеш эффективных прав пользователя
  - [ ] Многоуровневое кеширование (пользователь → объект → права)
  - [ ] Инвалидация при изменении ролей/разрешений

- [ ] 6.3. **Оптимизация запросов**
  - [ ] Использование включений (Include) для связанных данных
  - [ ] Пакетные операции для массовых изменений
  - [ ] Индексы для часто используемых запросов

### **ЭТАП 7: ТЕСТИРОВАНИЕ**
- [ ] 7.1. **Unit тесты провайдеров**
  - [ ] Тесты всех CRUD операций
  - [ ] Тесты валидации
  - [ ] Тесты безопасности (хеширование паролей)

- [ ] 7.2. **Integration тесты**
  - [ ] Тесты с реальной БД
  - [ ] Тесты каскадных операций
  - [ ] Тесты производительности

- [ ] 7.3. **Тесты безопасности**
  - [ ] Тесты проверки прав доступа
  - [ ] Тесты инъекций
  - [ ] Тесты аутентификации

---

## 🏗️ **АРХИТЕКТУРА РЕШЕНИЯ**

### **Структура файлов:**
```
redb.Core/
├── Providers/
│   ├── IUserProvider.cs
│   ├── IRoleProvider.cs
│   ├── IPermissionProvider.cs (обновить)
│   └── Security/
│       ├── IPasswordHasher.cs
│       └── PasswordPolicy.cs
├── Models/
│   ├── Users/
│   │   ├── CreateUserRequest.cs
│   │   ├── UpdateUserRequest.cs
│   │   ├── UserSearchCriteria.cs
│   │   └── UserValidationResult.cs
│   ├── Roles/
│   │   ├── CreateRoleRequest.cs
│   │   └── RoleAssignmentRequest.cs
│   ├── Permissions/
│   │   ├── PermissionRequest.cs
│   │   ├── EffectivePermissionResult.cs
│   │   └── PermissionScope.cs
│   └── Enums/
│       ├── UserStatus.cs
│       └── PermissionAction.cs

redb.Core.Postgres/
├── Providers/
│   ├── PostgresUserProvider.cs
│   ├── PostgresRoleProvider.cs
│   ├── PostgresPermissionProvider.cs (обновить)
│   └── Security/
│       └── BCryptPasswordHasher.cs
└── Cache/
    ├── UserCache.cs
    └── PermissionCache.cs
```

### **Ключевые интерфейсы:**

#### **1. IUserProvider:**
```csharp
public interface IUserProvider
{
    // === CRUD ОПЕРАЦИИ ===
    Task<IRedbUser> CreateUserAsync(CreateUserRequest request, IRedbUser? currentUser = null);
    Task<IRedbUser> UpdateUserAsync(long userId, UpdateUserRequest request, IRedbUser? currentUser = null);
    Task<bool> DeleteUserAsync(long userId, IRedbUser? currentUser = null);
    
    // === ПОИСК И ПОЛУЧЕНИЕ ===
    Task<IRedbUser?> GetUserByIdAsync(long userId);
    Task<IRedbUser?> GetUserByLoginAsync(string login);
    Task<List<IRedbUser>> GetUsersAsync(UserSearchCriteria? criteria = null);
    
    // === АУТЕНТИФИКАЦИЯ ===
    Task<IRedbUser?> ValidateUserAsync(string login, string password);
    Task<bool> ChangePasswordAsync(long userId, string currentPassword, string newPassword, IRedbUser? currentUser = null);
    
    // === УПРАВЛЕНИЕ СТАТУСОМ ===
    Task<bool> EnableUserAsync(long userId, IRedbUser? currentUser = null);
    Task<bool> DisableUserAsync(long userId, IRedbUser? currentUser = null);
    
    // === ВАЛИДАЦИЯ ===
    Task<UserValidationResult> ValidateUserDataAsync(CreateUserRequest request);
    Task<bool> IsLoginAvailableAsync(string login, long? excludeUserId = null);
}
```

#### **2. IRoleProvider:**
```csharp
public interface IRoleProvider
{
    // === CRUD РОЛЕЙ ===
    Task<_RRole> CreateRoleAsync(CreateRoleRequest request, IRedbUser? currentUser = null);
    Task<_RRole> UpdateRoleAsync(long roleId, string newName, IRedbUser? currentUser = null);
    Task<bool> DeleteRoleAsync(long roleId, IRedbUser? currentUser = null);
    
    // === ПОИСК РОЛЕЙ ===
    Task<_RRole?> GetRoleByIdAsync(long roleId);
    Task<_RRole?> GetRoleByNameAsync(string roleName);
    Task<List<_RRole>> GetRolesAsync();
    
    // === УПРАВЛЕНИЕ ПОЛЬЗОВАТЕЛЬ-РОЛЬ ===
    Task<bool> AssignUserToRoleAsync(long userId, long roleId, IRedbUser? currentUser = null);
    Task<bool> RemoveUserFromRoleAsync(long userId, long roleId, IRedbUser? currentUser = null);
    Task<List<_RRole>> GetUserRolesAsync(long userId);
    Task<List<IRedbUser>> GetRoleUsersAsync(long roleId);
    
    // === ВАЛИДАЦИЯ ===
    Task<bool> IsRoleNameAvailableAsync(string roleName, long? excludeRoleId = null);
}
```

#### **3. Расширенный IPermissionProvider:**
```csharp
public interface IPermissionProvider
{
    // === СУЩЕСТВУЮЩИЕ МЕТОДЫ ПРОВЕРКИ ===
    IQueryable<long> GetReadableObjectIds(long userId);
    Task<bool> CanUserEditObject(long objectId, long userId);
    // ... остальные методы проверки
    
    // === НОВЫЕ CRUD МЕТОДЫ ===
    Task<_RPermission> CreatePermissionAsync(PermissionRequest request, IRedbUser? currentUser = null);
    Task<_RPermission> UpdatePermissionAsync(long permissionId, PermissionRequest request, IRedbUser? currentUser = null);
    Task<bool> DeletePermissionAsync(long permissionId, IRedbUser? currentUser = null);
    
    // === ПОИСК РАЗРЕШЕНИЙ ===
    Task<List<_RPermission>> GetPermissionsByUserAsync(long userId);
    Task<List<_RPermission>> GetPermissionsByRoleAsync(long roleId);
    Task<List<_RPermission>> GetPermissionsByObjectAsync(long objectId);
    
    // === УПРАВЛЕНИЕ РАЗРЕШЕНИЯМИ ===
    Task<bool> GrantPermissionAsync(long? userId, long? roleId, long objectId, PermissionAction actions, IRedbUser? currentUser = null);
    Task<bool> RevokePermissionAsync(long? userId, long? roleId, long objectId, IRedbUser? currentUser = null);
    
    // === ЭФФЕКТИВНЫЕ ПРАВА ===
    Task<EffectivePermissionResult> GetEffectivePermissionsAsync(long userId, long objectId);
    Task<Dictionary<long, EffectivePermissionResult>> GetEffectivePermissionsBatchAsync(long userId, long[] objectIds);
}
```

### **Модели запросов:**

#### **CreateUserRequest:**
```csharp
public class CreateUserRequest
{
    public string Login { get; set; } = "";
    public string Password { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool Enabled { get; set; } = true;
    public long[]? RoleIds { get; set; } // Роли для назначения
}
```

#### **PermissionRequest:**
```csharp
public class PermissionRequest
{
    public long? UserId { get; set; }
    public long? RoleId { get; set; }
    public long ObjectId { get; set; }
    public bool? CanSelect { get; set; }
    public bool? CanInsert { get; set; }
    public bool? CanUpdate { get; set; }
    public bool? CanDelete { get; set; }
}
```

---

## ⏱️ **ВРЕМЕННЫЕ РАМКИ**

### **ДЕТАЛЬНАЯ ОЦЕНКА:**

- **Этап 1: Интерфейсы провайдеров** - **2 дня**
  - 1.1. IUserProvider - 0.5 дня
  - 1.2. IRoleProvider - 0.5 дня
  - 1.3. Расширение IPermissionProvider - 1 день

- **Этап 2: Бизнес-модели** - **1.5 дня**
  - 2.1. Модели запросов - 1 день
  - 2.2. Енумы - 0.25 дня
  - 2.3. Модели валидации - 0.25 дня

- **Этап 3: Реализация провайдеров** - **5-6 дней**
  - 3.1. PostgresUserProvider - 2.5 дня
  - 3.2. PostgresRoleProvider - 1.5 дня
  - 3.3. Расширение PostgresPermissionProvider - 2 дня

- **Этап 4: Интеграция с RedbService** - **1 день**
  - 4.1-4.2. Обновление интерфейсов и реализации - 1 день

- **Этап 5: Безопасность и валидация** - **2 дня**
  - 5.1. Хеширование паролей - 0.5 дня
  - 5.2. Валидация данных - 1 день
  - 5.3. Аудит и логирование - 0.5 дня

- **Этап 6: Кеширование** - **2 дня**
  - 6.1. Кеширование пользователей - 0.5 дня
  - 6.2. Кеширование разрешений - 1 день
  - 6.3. Оптимизация запросов - 0.5 дня

- **Этап 7: Тестирование** - **3 дня**
  - 7.1. Unit тесты - 1.5 дня
  - 7.2. Integration тесты - 1 день
  - 7.3. Тесты безопасности - 0.5 дня

**Общее время:** **16-17 дней**

### **КРИТИЧЕСКИЙ ПУТЬ:**
1. **Этап 1** → **Этап 2** → **Этап 3** → **Этап 4** → **Этап 5**
2. **Этап 6** и **Этап 7** могут выполняться частично параллельно

### **ПРИОРИТЕТЫ:**
- **🔥 ВЫСОКИЙ:** Этапы 1-4 (основная функциональность)
- **🟡 СРЕДНИЙ:** Этап 5 (безопасность и валидация)
- **🟢 НИЗКИЙ:** Этапы 6-7 (оптимизация и тестирование)

---

## 🎯 **КРИТЕРИИ ГОТОВНОСТИ**

### **Функциональные:**
- ✅ Все CRUD операции для пользователей, ролей, разрешений
- ✅ Валидация данных и безопасность паролей
- ✅ Интеграция с существующей системой прав
- ✅ Кеширование для производительности
- ✅ Аудит и логирование операций

### **Нефункциональные:**
- ✅ Производительность: операции выполняются за разумное время
- ✅ Безопасность: пароли хешируются, права проверяются
- ✅ Надежность: транзакции, откаты при ошибках
- ✅ Совместимость: работает с существующим кодом

### **Тестирование:**
- ✅ Unit тесты покрывают 90%+ кода
- ✅ Integration тесты для всех сценариев
- ✅ Тесты безопасности и производительности

**План готов к реализации! 🚀**
