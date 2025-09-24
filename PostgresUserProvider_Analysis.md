# Глубокий анализ PostgresUserProvider - Реализация NotImplementedException

## 📋 Обзор

`PostgresUserProvider` является PostgreSQL-реализацией интерфейса `IUserProvider` и содержит 15 методов с `NotImplementedException`. Данный анализ описывает архитектуру, источники данных и стратегии реализации каждого метода.

## 🏗️ Архитектурный контекст

### Зависимости
- **RedbContext** - EF Core контекст для доступа к базе данных
- **IRedbSecurityContext** - контекст безопасности для получения текущего пользователя
- **Таблицы БД**: `_users`, `_roles`, `_permissions`, возможно `_user_sessions`

### Модели данных
- **_RUser** (DB модель) - таблица `_users`
- **RedbUser** (Entity) - бизнес-объект, реализует `IRedbUser`
- **CreateUserRequest**, **UpdateUserRequest** - DTO для операций
- **UserSearchCriteria** - критерии поиска
- **UserValidationResult** - результат валидации

## 🔍 Детальный анализ методов

### 1. CRUD ОПЕРАЦИИ

#### `CreateUserAsync(CreateUserRequest request, IRedbUser? currentUser = null)`
**Назначение**: Создание нового пользователя с валидацией и хешированием пароля

**Источники данных**:
- `request.Login`, `request.Password`, `request.Email`, `request.FullName`
- `_context.Users` - для проверки уникальности логина
- `currentUser?.Id` - для аудита (кто создал)

**Алгоритм реализации**:
```csharp
1. Валидация входных данных (логин, пароль, email)
2. Проверка уникальности логина через IsLoginAvailableAsync()
3. Хеширование пароля (используя SimplePasswordHasher или аналог)
4. Создание _RUser с полями:
   - Id = _context.GetNextKey()
   - Login = request.Login
   - PasswordHash = hashedPassword
   - Email = request.Email
   - FullName = request.FullName
   - Enabled = true (по умолчанию)
   - DateCreate = DateTime.Now
   - IdWhoCreate = currentUser?.Id ?? 0
5. Сохранение в БД
6. Возврат RedbUser.FromEntity(newUser)
```

**Безопасность**:
- Проверка прав на создание пользователей
- Хеширование пароля (никогда не храним в открытом виде)
- Аудит операции

#### `UpdateUserAsync(IRedbUser user, UpdateUserRequest request, IRedbUser? currentUser = null)`
**Назначение**: Обновление данных пользователя с защитой системных пользователей

**Источники данных**:
- `user.Id` - ID обновляемого пользователя
- `request` - новые данные
- `_context.Users.FindAsync(user.Id)` - текущие данные пользователя

**Алгоритм реализации**:
```csharp
1. Проверка существования пользователя
2. Защита системных пользователей (ID 0, 1) - запрет изменения критичных полей
3. Валидация новых данных
4. Проверка уникальности логина (если изменяется)
5. Обновление полей:
   - Login, Email, FullName (если разрешено)
   - DateModify = DateTime.Now
   - IdWhoChange = currentUser?.Id ?? user.Id
6. Сохранение изменений
7. Возврат обновленного RedbUser
```

**Особенности**:
- Системные пользователи (ID 0, 1) имеют ограничения на изменение
- Аудит изменений

#### `DeleteUserAsync(IRedbUser user, IRedbUser? currentUser = null)`
**Назначение**: Мягкое удаление пользователя (деактивация)

**Алгоритм реализации**:
```csharp
1. Проверка: системные пользователи (ID 0, 1) не могут быть удалены
2. Мягкое удаление - установка Enabled = false
3. Установка DateDelete = DateTime.Now
4. Аудит: IdWhoDelete = currentUser?.Id
5. Сохранение изменений
6. Возврат true при успехе
```

### 2. ПОИСК И ПОЛУЧЕНИЕ

#### `GetUsersAsync(UserSearchCriteria? criteria = null)`
**Назначение**: Поиск пользователей с фильтрацией и сортировкой

**Источники данных**:
- `_context.Users` - основная таблица
- `criteria?.Login`, `criteria?.Email`, `criteria?.Enabled` - фильтры
- `criteria?.SortBy`, `criteria?.SortDirection` - сортировка
- `criteria?.PageSize`, `criteria?.PageNumber` - пагинация

**Алгоритм реализации**:
```csharp
1. Базовый запрос: _context.Users.AsQueryable()
2. Применение фильтров:
   - По логину (Contains или Equals)
   - По email (Contains или Equals)  
   - По статусу (Enabled/Disabled)
   - По дате создания (диапазон)
3. Сортировка по указанному полю
4. Пагинация (Skip/Take)
5. Преобразование в List<IRedbUser> через RedbUser.FromEntity()
```

### 3. АУТЕНТИФИКАЦИЯ

#### `ValidateUserAsync(string login, string password)`
**Назначение**: Проверка логина и пароля с хешированием

**Источники данных**:
- `_context.Users.FirstOrDefaultAsync(u => u.Login == login)`
- `password` - пароль для проверки
- `user.PasswordHash` - хеш из БД

**Алгоритм реализации**:
```csharp
1. Поиск пользователя по логину
2. Проверка что пользователь активен (Enabled = true)
3. Проверка пароля через SimplePasswordHasher.VerifyPassword()
4. Обновление LastLoginDate = DateTime.Now (если есть такое поле)
5. Возврат IRedbUser при успехе, null при неудаче
```

#### `ChangePasswordAsync(IRedbUser user, string currentPassword, string newPassword, IRedbUser? currentUser = null)`
**Назначение**: Смена пароля с проверкой текущего пароля

**Алгоритм реализации**:
```csharp
1. Загрузка пользователя из БД
2. Проверка текущего пароля через SimplePasswordHasher.VerifyPassword()
3. Валидация нового пароля (длина, сложность)
4. Хеширование нового пароля
5. Обновление PasswordHash в БД
6. Аудит: DatePasswordChange = DateTime.Now
7. Возврат true при успехе
```

#### `SetPasswordAsync(IRedbUser user, string newPassword, IRedbUser? currentUser = null)`
**Назначение**: Установка нового пароля (только для администраторов)

**Особенности**:
- Проверка прав администратора у `currentUser`
- Без проверки текущего пароля
- Принудительная смена пароля

### 4. УПРАВЛЕНИЕ СТАТУСОМ

#### `EnableUserAsync(IRedbUser user, IRedbUser? currentUser = null)`
**Алгоритм реализации**:
```csharp
1. Загрузка пользователя из БД
2. Установка Enabled = true
3. Очистка DateDisabled = null
4. Аудит операции
5. Сохранение изменений
```

#### `DisableUserAsync(IRedbUser user, IRedbUser? currentUser = null)`
**Алгоритм реализации**:
```csharp
1. Проверка: системные пользователи (ID 0, 1) не могут быть деактивированы
2. Установка Enabled = false
3. Установка DateDisabled = DateTime.Now
4. Аудит операции
5. Сохранение изменений
```

### 5. ВАЛИДАЦИЯ

#### `ValidateUserDataAsync(CreateUserRequest request)`
**Назначение**: Комплексная валидация данных пользователя

**Проверки**:
```csharp
1. Логин:
   - Не пустой
   - Длина 3-50 символов
   - Только латиница, цифры, подчеркивание
   - Уникальность через IsLoginAvailableAsync()

2. Пароль:
   - Минимальная длина 8 символов
   - Наличие заглавных/строчных букв
   - Наличие цифр
   - Наличие спецсимволов

3. Email (если указан):
   - Валидный формат email
   - Уникальность (если требуется)

4. FullName:
   - Не пустое
   - Максимальная длина 200 символов
```

**Возврат**: `UserValidationResult` с списком ошибок

### 6. СТАТИСТИКА

#### `GetActiveUserCountAsync(DateTime fromDate, DateTime toDate)`
**Назначение**: Подсчет активных пользователей за период

**Требует**:
- Таблица логирования активности пользователей (`_user_sessions` или `_user_activity`)
- Поля: UserId, LastActivityDate, SessionStart, SessionEnd

**Алгоритм реализации**:
```csharp
1. Запрос к таблице активности:
   WHERE LastActivityDate BETWEEN fromDate AND toDate
2. Группировка по UserId
3. Подсчет уникальных пользователей
4. Возврат количества
```

## 🔐 Безопасность и аудит

### Системные пользователи
- **ID 0**: Системный пользователь (SYSTEM)
- **ID 1**: Администратор по умолчанию (ADMIN)
- Защищены от удаления и критичных изменений

### Аудит операций
Все операции должны записывать:
- Кто выполнил операцию (`currentUser?.Id`)
- Когда выполнена операция (`DateTime.Now`)
- Что изменилось (для Update операций)

### Хеширование паролей
Использовать `SimplePasswordHasher` или аналогичный с поддержкой внешних хешей:
```csharp
// При создании/изменении пароля (внутренне)
var hashedPassword = SimplePasswordHasher.HashPassword(password);

// При создании с готовым хешем (извне)
var user = new _RUser { PasswordHash = externalHash };

// При проверке пароля
var isValid = SimplePasswordHasher.VerifyPassword(password, user.PasswordHash);

// Проверка что строка уже является хешем
var isAlreadyHashed = SimplePasswordHasher.IsHashedPassword(passwordOrHash);
```

**Стратегия обработки паролей**:
1. **Открытый пароль** - хешируем через `SimplePasswordHasher.HashPassword()`
2. **Готовый хеш** - сохраняем как есть (для миграции/импорта)
3. **Автоопределение** - проверяем формат строки для определения типа

## 🗃️ Структура базы данных

### Таблица _users
```sql
- Id (bigint, PK)
- Login (varchar(50), unique)
- PasswordHash (varchar(255))
- Email (varchar(255), nullable)
- FullName (varchar(200))
- Enabled (boolean, default true)
- DateCreate (timestamp)
- DateModify (timestamp, nullable)
- DateDelete (timestamp, nullable)
- IdWhoCreate (bigint, FK to _users)
- IdWhoChange (bigint, FK to _users, nullable)
- IdWhoDelete (bigint, FK to _users, nullable)
```

### Дополнительные таблицы (при необходимости)
```sql
-- Для статистики активности
_user_sessions:
- Id (bigint, PK)
- UserId (bigint, FK)
- SessionStart (timestamp)
- SessionEnd (timestamp, nullable)
- LastActivityDate (timestamp)
- IpAddress (varchar(45))
```

## 🚀 Приоритеты реализации

### Высокий приоритет
1. `ValidateUserAsync` - критично для аутентификации
2. `CreateUserAsync` - базовая функциональность
3. `GetUserByIdAsync`, `GetUserByLoginAsync` - уже реализованы
4. `IsLoginAvailableAsync` - уже реализован

### Средний приоритет
1. `UpdateUserAsync` - управление пользователями
2. `ChangePasswordAsync` - безопасность
3. `EnableUserAsync`, `DisableUserAsync` - управление статусом
4. `ValidateUserDataAsync` - валидация данных

### Низкий приоритет
1. `GetUsersAsync` - административные функции
2. `SetPasswordAsync` - административные функции
3. `DeleteUserAsync` - редко используется
4. `GetActiveUserCountAsync` - статистика

## 📝 Примечания по реализации

1. **Транзакции**: Используйте транзакции для операций изменения данных
2. **Логирование**: Добавьте логирование всех операций через ILogger
3. **Кеширование**: Рассмотрите кеширование часто запрашиваемых пользователей
4. **Валидация**: Используйте FluentValidation для сложной валидации
5. **Тестирование**: Каждый метод должен иметь unit-тесты

## 🔄 Интеграция с существующим кодом

Методы должны интегрироваться с:
- **RedbServiceConfiguration** - настройки безопасности
- **IRedbSecurityContext** - текущий пользователь
- **SimplePasswordHasher** - хеширование паролей
- **SequenceKeyGenerator** - генерация ID
- **IMetadataCache** - кеширование метаданных пользователей

---

## ✅ ПЛАН РЕАЛИЗАЦИИ - ЧЕКПОИНТЫ

### 🔥 ВЫСОКИЙ ПРИОРИТЕТ (критично для работы системы)

#### Аутентификация
- [ ] **ValidateUserAsync** - проверка логина/пароля для входа в систему
- [ ] **ChangePasswordAsync** - смена пароля пользователем

#### Базовые CRUD операции  
- [ ] **CreateUserAsync** - создание новых пользователей
- [ ] **UpdateUserAsync** - редактирование данных пользователей

#### Валидация
- [ ] **ValidateUserDataAsync** - проверка корректности данных пользователя

### 🟡 СРЕДНИЙ ПРИОРИТЕТ (управление и администрирование)

#### Управление статусом
- [ ] **EnableUserAsync** - активация пользователей
- [ ] **DisableUserAsync** - деактивация пользователей

#### Административные функции
- [ ] **SetPasswordAsync** - принудительная смена пароля администратором
- [ ] **GetUsersAsync** - поиск и фильтрация пользователей

### 🟢 НИЗКИЙ ПРИОРИТЕТ (дополнительный функционал)

#### Редко используемые операции
- [ ] **DeleteUserAsync** - мягкое удаление пользователей
- [ ] **GetActiveUserCountAsync** - статистика активности

### 🛠️ ТЕХНИЧЕСКАЯ ПОДГОТОВКА

#### Инфраструктура
- [ ] **Проверить SimplePasswordHasher** - убедиться что поддерживает внешние хеши
- [ ] **Добавить метод IsHashedPassword** - для автоопределения типа пароля
- [ ] **Настроить логирование** - ILogger для всех операций
- [ ] **Подготовить транзакции** - для операций изменения данных

#### Модели данных
- [ ] **Проверить _RUser модель** - все ли поля присутствуют
- [ ] **Создать UserValidationResult** - если отсутствует
- [ ] **Проверить CreateUserRequest/UpdateUserRequest** - DTO модели

---

## 🚀 НАЧИНАЕМ РЕАЛИЗАЦИЮ

**Предлагаемый порядок реализации:**

1. **ValidateUserAsync** - самый критичный для аутентификации
2. **CreateUserAsync** - базовая функциональность создания пользователей  
3. **ChangePasswordAsync** - безопасность паролей
4. **UpdateUserAsync** - управление пользователями
5. **ValidateUserDataAsync** - валидация данных

**Готовы приступать к реализации первого метода?** 🎯
