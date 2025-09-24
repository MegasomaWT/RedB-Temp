# 🛡️ ПЛАН РЕАЛИЗАЦИИ АРХИТЕКТУРЫ БЕЗОПАСНОСТИ REDB

## ⚠️ ВАЖНЫЕ ПРАВИЛА
**🔍 Если в процессе анализа или реализации найдены интересные паттерны, важные особенности или необходимость добавить что-то в БД - ОБЯЗАТЕЛЬНО спрашиваем пользователя перед продолжением!**
** Всегда ожидаем пока пользователь не подтвердит изменения в БД если они были необходимы**
** Этапы должны компилироватся и желательно тестироватся **
** для перехода на следующие этапы спрашиваем пользователя **
** не завершив успешно полностью этап не перепрыгиваем дальше, если есть необходимость уточняем у пользователя **

## 📋 ОБЩАЯ ИНФОРМАЦИЯ

**Цель:** Создание гибридной архитектуры безопасности с поддержкой:
- Контекста безопасности (текущий пользователь)
- Умного кеширования разрешений с инвалидацией
- Типизированной работы с объектами (дженерики вместо ID)
- Интеграции с существующими функциями PostgreSQL
- Красивого и понятного API

**Принципы:**
- ❌ **НЕТ обратной совместимости** - полный переход на классы!
- Изолированное тестирование каждого компонента
- Максимальное использование существующих возможностей БД
- Минимум кода для разработчика
- код для разработчика должен быть элегантным и красивым
- **ТОЛЬКО работа с объектами, никаких ID параметров!**

**🎯 ГЛАВНАЯ ИДЕЯ - РАБОТА С КЛАССАМИ:**
```csharp
// ❌ СТАРЫЙ ПОДХОД - работа с числами:
await redb.DeleteAsync(objectId: 1021, userId: 12345, checkPermissions: true);
await redb.CanUserEditObject(objectId: 1021, userId: 12345);

// ✅ НОВЫЙ ПОДХОД - работа с классами:
var project = await redb.LoadAsync<Project>(1021);
await redb.DeleteAsync(project);  // ID извлекается автоматически
await redb.CanUserEditAsync(project);  // Пользователь из контекста
```
---

## 🎯 ЭТАП 1: АНАЛИЗ И ПОДГОТОВКА

### 1.1 Анализ системы удаления и дженериков
- [x] **1.1.1** Проанализировать все методы в `IRedbService` принимающие `long id` параметры
- [x] **1.1.2** Изучить систему удаления с архивированием (`_deleted_objects`, триггер `ftr__objects__deleted_objects`)
- [x] **1.1.3** Определить места где можно заменить `long id` на дженерики `T where T : IRedbObject`
- [x] **1.1.4** Проанализировать методы `DeleteAsync`, `LoadAsync`, `SaveAsync` для типизации
- [x] **1.1.5** Создать список методов требующих рефакторинга

**🎯 ГЛАВНАЯ ЦЕЛЬ:** Перейти от работы с числами (ID) к работе с классами!
**❌ ПЛОХО:** `DeleteAsync(long objectId, long userId)`
**✅ ХОРОШО:** `DeleteAsync<T>(T obj)` где T - класс объекта

**Тестирование:** Создать `Stage20_CurrentSystemAnalysis.cs` для документирования текущего состояния

### 1.2 Анализ существующих функций БД и логики по умолчанию
- [x] **1.2.1** Изучить функцию `get_user_permissions_for_object(object_id, user_id)`
  - Если `user_id = NULL` → возвращает первый найденный permission (для триггеров)
  - Если нет разрешений → возвращает `NULL` (в C# становится `false`)
- [x] **1.2.2** Изучить VIEW `v_user_permissions` и его структуру
- [x] **1.2.3** Проанализировать триггер `auto_create_node_permissions()` 
- [x] **1.2.4** Изучить глобальные права (`_id_ref = 0`) - права на ВСЕ объекты
  - Admin имеет глобальные права: `_id_ref = 0, _select=true, _insert=true, _update=true, _delete=true`
  - Глобальные права имеют низкий приоритет (level = 999)
- [x] **1.2.5** Понять логику иерархического наследования прав:
  - Поиск прав идет вверх по дереву через `_id_parent`
  - Первое найденное разрешение применяется
  - Приоритет: Пользователь > Роль, Специфичные > Глобальные

**Тестирование:** Создать `Stage21_DatabaseFunctionsAnalysis.cs` для тестирования функций БД

---

## 🏗️ ЭТАП 2: БАЗОВЫЕ ИНТЕРФЕЙСЫ И МОДЕЛИ

### 2.1 Создание базовых интерфейсов безопасности ✅
- [x] **2.1.1** Создать `IRedbObject` базовый интерфейс для всех объектов - ПРИОРИТЕТ №1!
  ```csharp
  public interface IRedbObject
  {
      long Id { get; }           // Для извлечения ID из объекта
      long SchemeId { get; }     // Для проверки прав на схему
      string Name { get; }       // Для логирования и отладки
  }
  ```
- [x] **2.1.2** Создать `IRedbUser` интерфейс
  ```csharp
  public interface IRedbUser : IRedbObject
  {
      string Login { get; }
      bool Enabled { get; }
      DateTime DateRegister { get; }
      DateTime? DateDismiss { get; }
  }
  ```
- [x] **2.1.3** Создать `IRedbRole` интерфейс
- [x] **2.1.4** Создать `IRedbPermission` интерфейс с enum `PermissionType`
- [x] **2.1.5** Создать `IRedbSecurityContext` интерфейс

**🎯 ГЛАВНОЕ:** `IRedbObject` позволит работать с любыми объектами как с классами!

**Тестирование:** ✅ Создан `Stage22_BasicInterfacesTest.cs` - все интерфейсы работают корректно!

### 2.2 Реализация базовых моделей ✅
- [x] **2.2.1** Создать `RedbUser` класс реализующий `IRedbUser`
- [x] **2.2.2** Создать `RedbSecurityContext` класс реализующий `IRedbSecurityContext`
- [x] **2.2.3** Добавить поддержку временного системного контекста
- [x] **2.2.4** Создать `PermissionFlags` enum (Select, Insert, Update, Delete)
- [x] **2.2.5** Создать `UserPermissionSet` для кеширования

**Тестирование:** ✅ Создан `Stage23_PermissionModelsTest.cs` - все модели работают корректно!

---

## 🔐 ЭТАП 3: КОНТЕКСТ БЕЗОПАСНОСТИ

### 3.1 Реализация SecurityContext с многоуровневой системой fallback ✅
- [x] **3.1.1** Создать `RedbSecurityContext` класс с fallback логикой
  ```csharp
  public class RedbSecurityContext : IRedbSecurityContext
  {
      public IRedbUser? CurrentUser { get; }
      public bool IsSystemContext { get; }
      public bool IsAuthenticated { get; }
      public long GetEffectiveUserId(); // Fallback к admin (-9223372036854775800)
  }
  ```
- [x] **3.1.2** Добавить поддержку системного контекста (без пользователя)
- [x] **3.1.3** Реализовать многоуровневую систему приоритетов:
  - **Уровень 1:** Явно указанный пользователь (высший приоритет)
  - **Уровень 2:** Контекст безопасности (автоматически)
  - **Уровень 3:** Системный контекст (для системных операций)
  - **Уровень 4:** Дефолтный admin (если ничего не указано)
- [x] **3.1.4** Добавить поддержку Ambient Context pattern
- [x] **3.1.5** Создать `SecurityContextScope` для временного изменения контекста

**Тестирование:** ✅ Создан `Stage24_AdvancedSecurityContext.cs` - многоуровневая система работает!

### 3.2 Интеграция с RedbService и красивый API ✅
- [x] **3.2.1** Добавить `IRedbSecurityContext` в конструктор `RedbService`
- [x] **3.2.2** Создать методы управления контекстом:
  ```csharp
  void SetCurrentUser(IRedbUser user);
  IDisposable CreateSystemContext();
  long GetEffectiveUserId(); // Fallback логика
  ```
- [x] **3.2.3** Добавить автоматическую установку `owner_id` и `who_change_id` из контекста
- [ ] **3.2.4** ИСПРАВИТЬ: Проблема с `SaveAsync` - вызов несуществующего метода
  ```csharp
  // ❌ ПРОБЛЕМА: Этот метод НЕ существует в IObjectStorageProvider
  return await SaveAsync(obj, effectiveUser.UserId, effectiveUser.ShouldCheckPermissions && checkPermissions);
  
  // ✅ ПРАВИЛЬНО: Использовать существующий метод
  return await _objectStorage.SaveAsync(obj, effectiveUser.ShouldCheckPermissions && checkPermissions);
  ```
- [x] **3.2.5** Добавить красивые перегрузки методов:
  ```csharp
  // Новые красивые методы
  Task<long> SaveAsync<T>(RedbObject<T> obj, bool checkPermissions = true);
  Task<bool> DeleteAsync<T>(T obj, bool checkPermissions = true);
  
  // Старые методы для совместимости (помечены [Obsolete])
  Task<long> SaveAsync<T>(RedbObject<T> obj, long userId, bool checkPermissions);
  ```

**Тестирование:** Создать `Stage25_RedbServiceIntegration.cs` для проверки интеграции

### 3.3 Логика приоритетов пользователей и проверки прав
- [ ] **3.3.1** **ПРИОРИТЕТЫ ПОЛЬЗОВАТЕЛЕЙ (от высшего к низшему):**
  ```csharp
  // 1. ЯВНЫЙ пользователь (высший приоритет) - передан в параметре
  Task<long> SaveAsync<T>(RedbObject<T> obj, IRedbUser explicitUser, bool checkPermissions = false);
  
  // 2. Пользователь из КОНТЕКСТА (средний приоритет) - SecurityContext.CurrentUser  
  Task<long> SaveAsync<T>(RedbObject<T> obj, bool checkPermissions = true);
  
  // 3. SYS пользователь (ID=0) - если нигде не указан пользователь
  ```
- [ ] **3.3.2** **ЛОГИКА checkPermissions ПО УМОЛЧАНИЮ:**
  - **Явный пользователь** → `checkPermissions = false` (доверяем разработчику)
  - **Контекстный пользователь** → `checkPermissions = true` (проверяем права)
  - **Системный контекст** → `checkPermissions = false` (полные права)
- [ ] **3.3.3** **ЗАМЕНА ADMIN НА SYS:**
  - Изменить `SystemAdmin` на `SystemUser` (ID=0, login="sys")
  - Обновить все ссылки с "admin" на "sys" в коде
  - Использовать `sys` (ID=0) как fallback пользователя вместо ID=-9223372036854775800
- [ ] **3.3.4** **РЕАЛИЗАЦИЯ ПРИОРИТЕТНОЙ ЛОГИКИ:**
  ```csharp
  public long GetEffectiveUserId(IRedbUser? explicitUser = null)
  {
      // 1. Явный пользователь (высший приоритет)
      if (explicitUser != null) return explicitUser.Id;
      
      // 2. Пользователь из контекста
      if (CurrentUser != null) return CurrentUser.Id;
      
      // 3. Системный контекст
      if (IsSystemContext) return 0; // sys
      
      // 4. Fallback к sys
      return 0; // sys пользователь
  }
  ```

**Тестирование:** Создать `Stage25_UserPriorityLogic.cs` для проверки приоритетов

---

## 👥 ЭТАП 4: УПРАВЛЕНИЕ ПОЛЬЗОВАТЕЛЯМИ

### 4.1 Создание UserManagementService
- [ ] **4.1.1** Создать `IUserManagementService` интерфейс
  ```csharp
  public interface IUserManagementService
  {
      Task<IRedbUser> CreateUserAsync(CreateUserRequest request);
      Task<IRedbUser> GetUserAsync(long userId);
      Task<IRedbUser> UpdateUserAsync(long userId, UpdateUserRequest request);
      Task<bool> DeleteUserAsync(long userId);
      Task<bool> EnableUserAsync(long userId, bool enabled);
  }
  ```
- [ ] **4.1.2** Создать `CreateUserRequest` и `UpdateUserRequest` DTO
- [ ] **4.1.3** Реализовать `PostgresUserManagementService`
- [ ] **4.1.4** Добавить валидацию данных пользователя
- [ ] **4.1.5** Интегрировать с существующей таблицей `_users`

**Тестирование:** Создать `Stage26_UserManagement.cs` для CRUD операций с пользователями

### 4.2 Управление ролями пользователей
- [ ] **4.2.1** Добавить методы управления ролями в `IUserManagementService`
  ```csharp
  Task AssignRoleAsync(IRedbUser user, IRedbRole role);
  Task RemoveRoleAsync(IRedbUser user, IRedbRole role);
  Task<IEnumerable<IRedbRole>> GetUserRolesAsync(IRedbUser user);
  ```
- [ ] **4.2.2** Реализовать работу с таблицей `_users_roles`
- [ ] **4.2.3** Добавить проверки на дублирование ролей
- [ ] **4.2.4** Создать события изменения ролей для инвалидации кеша
- [ ] **4.2.5** Добавить массовые операции с ролями

**Тестирование:** Создать `Stage27_UserRoleManagement.cs` для управления ролями

---

## 🛡️ ЭТАП 5: РАСШИРЕНИЕ СИСТЕМЫ РАЗРЕШЕНИЙ

### 5.1 Расширение PermissionProvider
- [ ] **5.1.1** Расширить `IPermissionProvider` новыми методами
  ```csharp
  Task<UserPermissionSet> GetUserPermissionsAsync(IRedbUser user, long objectId);
  Task<bool> CanUserAccessAsync(IRedbUser user, IRedbObject obj, PermissionType type);
  Task<Dictionary<long, UserPermissionSet>> GetBulkPermissionsAsync(IRedbUser user, long[] objectIds);
  ```
- [ ] **5.1.2** Интегрировать с функцией `get_user_permissions_for_object`
- [ ] **5.1.3** Добавить поддержку глобальных прав (`_id_ref = 0`)
- [ ] **5.1.4** Реализовать массовую проверку прав для производительности
- [ ] **5.1.5** Добавить поддержку иерархического наследования

**Тестирование:** Создать `Stage28_ExtendedPermissions.cs` для расширенных проверок прав

### 5.2 Управление разрешениями
- [ ] **5.2.1** Создать `IPermissionManagementService`
  ```csharp
  Task GrantPermissionAsync(IRedbUser user, IRedbObject obj, PermissionFlags flags);
  Task GrantRolePermissionAsync(IRedbRole role, IRedbObject obj, PermissionFlags flags);
  Task RevokePermissionAsync(IRedbUser user, IRedbObject obj);
  Task<PermissionAuditReport> GetPermissionAuditAsync(IRedbObject obj);
  ```
- [ ] **5.2.2** Реализовать работу с таблицей `_permissions`
- [ ] **5.2.3** Добавить поддержку массовых операций с правами
- [ ] **5.2.4** Создать отчеты по правам доступа
- [ ] **5.2.5** Интегрировать с триггером `auto_create_node_permissions`

**Тестирование:** Создать `Stage29_PermissionManagement.cs` для управления правами

---

## ⚡ ЭТАП 6: УМНОЕ КЕШИРОВАНИЕ

### 6.1 Базовая система кеширования
- [ ] **6.1.1** Создать `IPermissionCacheManager` интерфейс
  ```csharp
  public interface IPermissionCacheManager
  {
      Task<UserPermissionSet> GetCachedPermissionsAsync(long userId, long objectId);
      Task InvalidateUserAsync(long userId);
      Task InvalidateObjectAsync(long objectId);
      Task InvalidateHierarchyAsync(long parentObjectId);
  }
  ```
- [ ] **6.1.2** Реализовать `MemoryPermissionCache` с `IMemoryCache`
- [ ] **6.1.3** Добавить TTL для кеша (30 минут по умолчанию)
- [ ] **6.1.4** Реализовать кеш с зависимостями (пользователь, объект, иерархия)
- [ ] **6.1.5** Добавить метрики кеша (hit rate, miss rate)

**Тестирование:** Создать `Stage30_BasicCaching.cs` для проверки базового кеширования

### 6.2 Система инвалидации кеша
- [ ] **6.2.1** Создать `ICacheInvalidationService`
- [ ] **6.2.2** Реализовать инвалидацию по зависимостям
  ```csharp
  // При изменении пользователя - инвалидировать все его права
  // При изменении роли - инвалидировать всех пользователей с этой ролью
  // При изменении разрешения - инвалидировать объект и иерархию
  ```
- [ ] **6.2.3** Добавить каскадную инвалидацию для иерархических прав
- [ ] **6.2.4** Интегрировать с триггером `auto_create_node_permissions`
- [ ] **6.2.5** Создать фоновую очистку устаревшего кеша

**Тестирование:** Создать `Stage31_CacheInvalidation.cs` для проверки инвалидации

---

## 🔄 ЭТАП 7: СОБЫТИЙНАЯ СИСТЕМА

### 7.1 Базовая событийная архитектура
- [ ] **7.1.1** Создать базовые события
  ```csharp
  public class UserUpdatedEvent : IRedbEvent
  public class RoleUpdatedEvent : IRedbEvent  
  public class PermissionChangedEvent : IRedbEvent
  public class ObjectCreatedEvent : IRedbEvent
  ```
- [ ] **7.1.2** Создать `IRedbEventBus` для публикации событий
- [ ] **7.1.3** Реализовать `InMemoryEventBus` 
- [ ] **7.1.4** Добавить поддержку асинхронных обработчиков
- [ ] **7.1.5** Создать базовый `IEventHandler<T>` интерфейс

**Тестирование:** Создать `Stage32_EventSystem.cs` для проверки событий

### 7.2 Обработчики событий для кеша
- [ ] **7.2.1** Создать `PermissionCacheEventHandler`
- [ ] **7.2.2** Обработка `UserUpdatedEvent` - инвалидация кеша пользователя
- [ ] **7.2.3** Обработка `RoleUpdatedEvent` - инвалидация всех пользователей роли
- [ ] **7.2.4** Обработка `PermissionChangedEvent` - инвалидация объекта и иерархии
- [ ] **7.2.5** Обработка `ObjectCreatedEvent` - обновление иерархических прав

**Тестирование:** Создать `Stage33_EventHandlers.cs` для проверки обработчиков

---

## 🎯 ЭТАП 8: ОБНОВЛЕНИЕ REDBSERVICE - РАБОТА С КЛАССАМИ ВМЕСТО ID

### 8.1 Замена ID параметров на дженерики - ГЛАВНЫЙ ПРИОРИТЕТ!
- [ ] **8.1.1** Обновить сигнатуры методов для работы с КЛАССАМИ:
  ```csharp
  // ❌ СТАРЫЙ ПОДХОД - работа с числами:
  Task<bool> DeleteAsync(long objectId, long userId)
  Task MoveObjectAsync(long objectId, long? newParentId, long userId)
  Task<bool> CanUserEditObject(long objectId, long userId)
  
  // ✅ НОВЫЙ ПОДХОД - работа с классами:
  Task<bool> DeleteAsync<T>(T obj) where T : IRedbObject
  Task MoveObjectAsync<T>(T obj, T? newParent) where T : IRedbObject  
  Task<bool> CanUserEditAsync<T>(T obj) where T : IRedbObject
  ```
- [ ] **8.1.2** ❌ **УДАЛИТЬ старые методы с ID** - никакой обратной совместимости!
- [ ] **8.1.3** Добавить автоматическое извлечение ID из объектов через `IRedbObject.Id`
- [ ] **8.1.4** Реализовать автоматическую проверку прав на основе объектов
- [ ] **8.1.5** Добавить параметр `checkPermissions` для отключения проверок

**🎯 ЦЕЛЬ:** Разработчик работает ТОЛЬКО с объектами! Никаких ID параметров!

**Тестирование:** Создать `Stage34_GenericMethods.cs` для проверки дженериков

### 8.2 Автоматическая установка метаданных
- [ ] **8.2.1** Автоматическая установка `owner_id` при создании объекта
- [ ] **8.2.2** Автоматическая установка `who_change_id` при изменении
- [ ] **8.2.3** Обновление `date_modify` при сохранении
- [ ] **8.2.4** Поддержка системного контекста (owner_id = system user)
- [ ] **8.2.5** Валидация прав перед установкой метаданных

**Тестирование:** Создать `Stage35_AutoMetadata.cs` для проверки метаданных

---

## 🔄 ЭТАП 8.5: ПЕРЕРАБОТКА ВСЕХ ТЕСТОВ НА ОБЪЕКТЫ

### 8.5.1 Анализ и переработка существующих тестовых этапов
- [ ] **8.5.1.1** Проанализировать все этапы 1-19 на использование ID вместо объектов
- [ ] **8.5.1.2** Переработать Stage04_PermissionChecks - использовать объекты вместо ID
- [ ] **8.5.1.3** Переработать Stage05_CreateObject - использовать новые методы с контекстом
- [ ] **8.5.1.4** Переработать Stage06_VerifyCreatedObject - работа с объектами
- [ ] **8.5.1.5** Переработать Stage07_UpdateObject - автоматические метаданные

### 8.5.2 Обновление CRUD операций в тестах
- [ ] **8.5.2.1** Переработать Stage08_FinalVerification - новые методы проверки
- [ ] **8.5.2.2** Переработать Stage09_DatabaseAnalysis - анализ через объекты
- [ ] **8.5.2.3** Переработать Stage10_ComparativeAnalysis - сравнение объектов
- [ ] **8.5.2.4** Переработать Stage11_ObjectDeletion - DeleteAsync<T>(T obj)
- [ ] **8.5.2.5** Переработать Stage12_TreeFunctionality - древовидные операции с объектами

### 8.5.3 Обновление LINQ и запросов
- [ ] **8.5.3.1** Переработать Stage13_LinqQueries - использовать контекст безопасности
- [ ] **8.5.3.2** Переработать Stage16_AdvancedLinq - новые методы запросов
- [ ] **8.5.3.3** Переработать Stage17_AdvancedLinqOperators - работа с объектами
- [ ] **8.5.3.4** Переработать Stage18_SortingAndPagination - контекст пользователя
- [ ] **8.5.3.5** Переработать Stage19_DateTimeSorting - автоматические метаданные

### 8.5.4 Создание примеров красивого кода
- [ ] **8.5.4.1** Создать Stage36_BeautifulCodeExamples - демонстрация нового API
  ```csharp
  // ❌ СТАРЫЙ КОД:
  await redb.DeleteAsync(objectId: 1021, userId: 12345, checkPermissions: true);
  
  // ✅ НОВЫЙ КОД:
  var project = await redb.LoadAsync<Project>(1021);
  await redb.DeleteAsync(project);  // Красиво и просто!
  ```
- [ ] **8.5.4.2** Показать работу с контекстом безопасности в тестах
- [ ] **8.5.4.3** Продемонстрировать автоматическую установку метаданных
- [ ] **8.5.4.4** Примеры системного контекста для административных операций

**🎯 ЦЕЛЬ:** Все тесты должны использовать ТОЛЬКО объекты, никаких ID параметров!
**📋 РЕЗУЛЬТАТ:** Тесты станут примерами красивого использования нового API

**Тестирование:** Запустить ВСЕ этапы 1-36 и убедиться что они работают с новой архитектурой

---

## 🧪 ЭТАП 9: КОМПЛЕКСНОЕ ТЕСТИРОВАНИЕ

### 9.1 Тесты безопасности
- [ ] **9.1.1** Тесты проверки прав доступа
- [ ] **9.1.2** Тесты иерархического наследования прав
- [ ] **9.1.3** Тесты глобальных прав (`_id_ref = 0`)
- [ ] **9.1.4** Тесты приоритета прав (пользователь > роль)
- [ ] **9.1.5** Тесты безопасности при отсутствии прав

**Тестирование:** Создать `Stage36_SecurityTests.cs` для комплексных тестов безопасности

### 9.2 Тесты производительности кеширования
- [ ] **9.2.1** Тесты hit rate кеша при повторных запросах
- [ ] **9.2.2** Тесты инвалидации кеша при изменениях
- [ ] **9.2.3** Тесты каскадной инвалидации
- [ ] **9.2.4** Нагрузочные тесты с большим количеством пользователей
- [ ] **9.2.5** Тесты утечек памяти в кеше

**Тестирование:** Создать `Stage37_CachePerformance.cs` для тестов производительности

### 9.3 Интеграционные тесты
- [ ] **9.3.1** Тесты интеграции с PostgreSQL функциями
- [ ] **9.3.2** Тесты работы триггеров БД с новой архитектурой
- [ ] **9.3.3** Тесты событийной системы end-to-end
- [ ] **9.3.4** Тесты обратной совместимости со старым API
- [ ] **9.3.5** Тесты миграции данных

**Тестирование:** Создать `Stage38_IntegrationTests.cs` для интеграционных тестов

---

## 📚 ЭТАП 10: ДОКУМЕНТАЦИЯ И ПРИМЕРЫ

### 10.1 Обновление документации
- [ ] **10.1.1** Обновить README с примерами новой архитектуры
- [ ] **10.1.2** Создать руководство по миграции
- [ ] **10.1.3** Документировать все новые интерфейсы и классы
- [ ] **10.1.4** Создать диаграммы архитектуры
- [ ] **10.1.5** Добавить troubleshooting guide

### 10.2 Примеры использования
- [ ] **10.2.1** Примеры базового использования с безопасностью
  ```csharp
  // Простое создание пользователя
  var user = await userService.CreateUserAsync(new CreateUserRequest 
  { 
      Login = "john", 
      Name = "John Doe" 
  });
  
  // Работа с объектами
  var project = await redb.LoadAsync<Project>(projectId);
  project.Name = "Updated";
  await redb.SaveAsync(project); // Автоматически проверяет права
  ```
- [ ] **10.2.2** Примеры управления правами
- [ ] **10.2.3** Примеры работы с системным контекстом
- [ ] **10.2.4** Примеры кастомных проверок безопасности
- [ ] **10.2.5** Примеры оптимизации производительности

**Тестирование:** Создать `Stage39_UsageExamples.cs` с рабочими примерами

---

## ✅ КРИТЕРИИ ГОТОВНОСТИ

### Функциональные требования
- [ ] Все методы RedbService поддерживают дженерики
- [ ] Автоматическая проверка прав во всех операциях
- [ ] Кеширование работает с hit rate > 80%
- [ ] Инвалидация кеша работает корректно
- [ ] События публикуются и обрабатываются
- [ ] Обратная совместимость сохранена

### Нефункциональные требования  
- [ ] Производительность не хуже текущей
- [ ] Покрытие тестами > 90%
- [ ] Документация полная и актуальная
- [ ] Код соответствует стандартам проекта
- [ ] Нет утечек памяти в кеше
- [ ] Логирование всех операций безопасности

---

## 🚀 ПОРЯДОК ВЫПОЛНЕНИЯ

1. **Этапы 1-2:** Анализ и базовые интерфейсы (фундамент)
2. **Этап 3:** Контекст безопасности (ядро системы)
3. **Этапы 4-5:** Управление пользователями и правами (функциональность)
4. **Этапы 6-7:** Кеширование и события (производительность)
5. **Этап 8:** Обновление RedbService (интеграция)
6. **Этапы 9-10:** Тестирование и документация (качество)

**Каждый этап должен быть полностью завершен и протестирован перед переходом к следующему!**
