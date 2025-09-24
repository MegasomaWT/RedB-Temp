# План работ по EAV Code‑First фреймворку (REDB)

Последнее обновление: 2025‑08‑12

Цель: типобезопасный фреймворк поверх REDB (EAV), позволяющий из C# типов (Code‑First) поднимать схемы/структуры в БД, работать с JSON `get_object_json`, выполнять сохранение в EAV и LINQ‑подобные запросы. Должна быть абстракция для разных СУБД.

---

## Фаза 1 — MVP (базовая функциональность)

- [x] Интеграция прав доступа
  - [x] Модель `VUserPermission` и маппинг VIEW `v_user_permissions`
  - [x] Методы сервиса: выборка доступных объектов, точечная проверка через функцию `get_user_permissions_for_object`

- [x] Code‑First: базовая синхронизация метаданных
  - [x] `EnsureSchemeFromTypeAsync<T>(schemeName, alias)` — upsert в `_schemes`
  - [x] `SyncStructuresFromTypeAsync<T>(schemeId, strictDelete=true)` — рефлексия public свойств, маппинг в `_structures` (тип, порядок, обязательность, массивы), удаление лишних полей по умолчанию
  - [x] Определение nullability без атрибутов через `NullabilityInfoContext`

 - [x] Дженерик‑обертка для JSON
  - [x] `RedbObject<TProps>` с полями корня `_objects` и `TProps` как `properties`
  - [x] Прямая десериализация из `get_object_json` через `System.Text.Json`

 - [x] Минимальные операции чтения
  - [x] Загрузка объекта: `LoadAsync<TProps>(objectId, depth)` → `RedbObject<TProps>`

Итог фазы: можно поднимать метаданные из типов и получать типизированные объекты из JSON.

---

## Фаза 2 — Полноценная работа с данными

- [x] Сохранение объектов в EAV
  - [x] INSERT/UPDATE `_objects`
  - [x] INSERT/UPDATE `_values` по `structures` (включая `_Array` для массивов)
  - [x] Рекурсивная запись вложенных `RedbObject<...>` (Object‑поля)
  - [x] Ключи/ID: получать из последовательности БД (`global_identity`) через абстракцию генератора ключей
  - [x] MD5 хеш по properties через `RedbHash.ComputeFor()` и методы в `RedbObject<TProps>`
  - [x] Семантика `_store_null` для корректного управления NULL значениями в `_values`
  - [x] Кеширование схем и структур в рамках транзакции

- [x] Удаление объектов (design → impl)
  - [x] Базовое удаление: удаляем из `_objects` (триггер `ftr__objects__deleted_objects` архивирует в `_deleted_objects` автоматически)
  - [x] Массовое удаление по поддереву (`_id_parent`): рекурсивно/каскадно (в БД уже есть ON DELETE CASCADE для `_objects`, `_values`)
  - [x] Особые случаи: `_list_items._id_object` — добавлено `ON DELETE SET NULL` в схему БД для автоматического обнуления
  - [x] Проверка прав перед удалением (`v_user_permissions`/`get_user_permissions_for_object`)
  - [x] Улучшение триггера архивации: добавлены `_Array`, метаданные структур (`_is_array`, `_store_null`), защита от NULL, оптимизация производительности
  - [x] Изменение типа `_deleted_objects._values` с `bytea` на `text` для лучшей читаемости JSON
  - [ ] (Опционально) Восстановление из `_deleted_objects`: дизайн `restore_deleted_object(id)`

- [x] Система безопасности и прав доступа
  - [x] Опциональная проверка прав во всех операциях CRUD:
    - `LoadAsync(checkPermissions: bool, userId: long?)` — проверка `CanSelect`
    - `SaveAsync(checkPermissions: bool)` — проверка `CanUpdate` для обновления, `CanInsert` для создания
    - `DeleteAsync(checkPermissions: bool)` — проверка `CanDelete` (по умолчанию включена)
    - `DeleteSubtreeAsync(checkPermissions: bool)` — проверка `CanDelete` (по умолчанию включена)
  - [x] Методы проверки прав: `CanUserSelectObject`, `CanUserEditObject`, `CanUserInsertScheme`, `CanUserDeleteObject`
  - [x] Гибкость: проверки можно отключить для системных операций или включить для пользовательских

- [x] Древовидные структуры объектов
  - [x] Базовая модель `TreeRedbObject<TProps>` с навигационными свойствами (`Parent`, `Children`)
  - [x] Интерфейс `ITreeNode<T>` и вычисляемые свойства (`IsRoot`, `IsLeaf`, `Level`)
  - [x] Методы сервиса: `LoadTreeAsync`, `GetChildrenAsync`, `GetPathToRootAsync`, `MoveObjectAsync`, `CreateChildAsync`, `GetDescendantsAsync`
  - [x] Расширения для обхода дерева (BFS/DFS) и построения хлебных крошек
  - [x] Специализированные коллекции `TreeCollection<TProps>` для работы с иерархиями
  - [x] Полное тестирование: создание иерархий, обход, перемещение узлов, статистика

- [x] Поиск/фильтрация (см. [LINQ_QUERY_PLAN.md](./LINQ_QUERY_PLAN.md))
  - [x] Компиляция простых LINQ‑выражений в `search_objects_with_facets`
  - [x] Построение `facet_filters` для строк, чисел, дат, булевых, массивов
  - [x] Типобезопасные запросы: `redb.Query<TProps>().Where(...).OrderBy(...).ToListAsync()`
  - [x] Полная поддержка операторов: ==, !=, >, <, >=, <=, Contains, StartsWith, EndsWith
  - [x] Логические операторы: AND (&&), OR (||), NOT (!)
  - [x] Все типы данных: String, int/long, DateTime, bool, Guid, double
  - [x] Агрегации и методы: Count, FirstOrDefault, Take, Skip, OrderBy/Desc, ThenBy/Desc
  - [x] PostgreSQL провайдер с рефлексией и полным тестированием

- [x] Валидация/ограничения
  - [x] Проверка соответствия типов C# ↔ `_types` (33 поддерживаемых типа)
  - [x] Проверка обязательности (AllowNotNull) и массивов (IsArray) через NullabilityInfoContext
  - [x] Отчеты об изменениях схемы при `strictDelete=true` с анализом критических изменений
  - [x] Расширенная типизация: Email, URL, JSON, XML, DateOnly, TimeOnly, Enum, географические типы
  - [x] Интеграция валидации в `IRedbService` через `IValidationProvider`

Итог фазы: двусторонняя работа (чтение/запись), древовидные структуры и базовый LINQ‑подобный поиск.

---

## Фаза 3 — Абстракция провайдеров и расширения

- [x] Провайдерная абстракция
  - [x] Интерфейс уровня абстракции (`ISchemeSyncProvider`, `IObjectStorageProvider`, `ITreeProvider`, `IPermissionProvider`, `IQueryableProvider`) — зафиксирован
  - [x] Выделить провайдер‑специфичные реализации вне `RedbService` (PostgreSQL провайдеры)
  - [x] Композитный сервис `RedbService` как фасад над провайдерами
  - [x] Dependency Injection для управления зависимостями провайдеров
  - [x] Упрощение API: автоматическое определение схемы по имени класса (`string? schemeName = null`)

- [ ] Расширенный LINQ
  - [x] Больше операторов (OrderBy/ThenBy, Take/Skip, Any/All, Contains)
  - [ ] Частичная материализация в JSON + пост‑фильтрация при сложных запросах

- [ ] Производительность/инфраструктура
  - [ ] Кеширование метаданных схем/структур/типов
  - [ ] Материализованные представления/индексы для горячих сценариев
  - [ ] Диагностика: логирование сгенерированных SQL/функций

- [ ] Диагностические и утилитарные методы во фреймворке
  - [ ] Добавить в `IRedbService` методы диагностики объектов:
    - `Task<ObjectDiagnosticInfo> GetObjectDiagnosticsAsync(long objectId)` - анализ объекта в _objects и _values
    - `Task<SchemaDiagnosticInfo> GetSchemaDiagnosticsAsync(long schemeId)` - статистика по схеме
    - `Task<DatabaseHealthInfo> GetDatabaseHealthAsync()` - общее состояние БД
  - [ ] Расширить `IValidationProvider` аналитическими методами:
    - `Task<PerformanceReport> AnalyzeSchemePerformanceAsync(long schemeId)` - анализ производительности схемы
    - `Task<StorageReport> GetStorageUsageAsync()` - использование дискового пространства
    - `Task<ArchiveReport> GetArchiveStatisticsAsync()` - статистика по _deleted_objects
  - [ ] Добавить утилитарные расширения в `redb.Core.Utils`:
    - Методы анализа структуры данных в EAV
    - Помощники для отладки и диагностики
    - Экспорт/импорт метаданных схем

Итог фазы: переносимость между СУБД, зрелый запросный слой, производительность.

---

## Технические решения и договоренности

- Имена свойств C# должны совпадать с именами в JSON/БД (snake_case vs PascalCase не используем — имена 1:1)
- Без атрибутов — чистая рефлексия public свойств
- JSON: `System.Text.Json` (без Newtonsoft). При необходимости — своя `JsonNamingPolicy` (позже)
- Обязательность полей: по nullability аннотациям C#
- Массивы: `_structures._is_array = true`, значения в `_values._Array` (JSON)
- Вложенные объекты: `RedbObject<TNested>` → тип `Object` (_types = −9223372036854775703), хранение как ID в `_Long`
- Правила удаления структур: по умолчанию strict (удаляем лишние поля схемы)

### Ключи/ID
- Источник: последовательность БД `global_identity` (server‑side, monotonic)
- Абстракция: использовать общий контракт (например, `RedbContext.GetNextKey()` / сервис генератора ключей)
- Провайдер PostgreSQL: `nextval('global_identity')` (см. реализацию `SequenceKeyGenerator` / `NextGlobalIdAsync()`)
- Применение: каждый INSERT в `_objects`, `_values`, `_structures`, `_schemes`, и др. получает ID из генератора
- Транзакции: вызовы `nextval` внутри одной транзакции перед фактическими INSERT
- (Опционально) Предзагрузка пачки ID для батчевых операций

#### Методы (контракты)
- Уже есть:
  - В `redb.Core.RedbContext.cs`:
    - `abstract long GetNextKey();` — синхронно, один ключ
    - `abstract List<long> GetKeysBatch(int count);` — синхронно, пачка ключей
  - В `redb.Core.Postgres.SequenceKeyGenerator.cs`:
    - `override long GetNextKey()` — с кешированием; источник `nextval('global_identity')`
    - `override List<long> GetKeysBatch(int count)` — напрямую из БД; `SELECT nextval('global_identity') FROM generate_series(1, count)`
    - Вспомогательные: `RefillCache()`, `GenerateSingleKey()`, `GenerateKeys(int count)`

- В `redb.Core.RedbContext` (добавить абстрактные методы):
  - `abstract Task<long> GetNextKeyAsync();` — асинхронно, один ключ
  - `abstract Task<IReadOnlyList<long>> GetKeysBatchAsync(int count);` — асинхронно, пачка ключей
  
- Паттерн использования в сервисах сохранения:
  - Для новой записи: `var id = await _context.GetNextKeyAsync();`
  - Для батча: `var ids = await _context.GetKeysBatchAsync(n);`

---

## Известные вопросы/задачи на уточнение

- [ ] Тип `Hash` в `_objects`: в БД `uuid`, в модели C# может быть `Guid?` — финализировать типизацию по всем слоям
- [ ] Добавлять ли `namespace` из `_schemes._name_space` в JSON корня (`get_object_json`)? (сейчас не требуется)
- [ ] Правила миграции при изменении типов полей (например, String→Long)

---

## Быстрый чек‑лист (что уже есть в REDB)

- [x] `get_object_json(object_id, max_depth)` — сборка полного JSON
- [x] `get_facets(scheme_id)` — фасеты для UI
- [x] `search_objects_with_facets(scheme_id, filters, limit, offset)` — поиск
- [x] `get_user_permissions_for_object(object_id, user_id)` — права
- [x] VIEW `v_user_permissions` — быстрый JOIN по правам
- [x] Триггеры валидации имен `_structures`/`_schemes`
- [x] Триггер аудита `_deleted_objects`
- [x] Триггер `auto_create_node_permissions()` — ускорение поиска прав

---

## Статус выполненного (по репозиторию)

- [x] Модель `VUserPermission`, маппинг VIEW, методы в `RedbService`
- [x] Методы Code‑First в `RedbService`: `EnsureSchemeFromTypeAsync`, `SyncStructuresFromTypeAsync`
- [x] Интерфейс `ISchemeSyncService` (для будущих провайдеров)
- [x] `RedbObject<TProps>` и загрузка из JSON
- [x] Сохранение объектов в EAV: метод `SaveAsync<TProps>` с полной поддержкой INSERT/UPDATE
- [x] Удаление объектов в EAV: методы `DeleteAsync`, `DeleteSubtreeAsync` с проверкой прав и очисткой `_list_items`
- [x] Генератор ключей: абстрактные методы в `RedbContext` + реализация PostgreSQL
- [x] Утилита хеширования `RedbHash` и методы в `RedbObject<TProps>`
- [x] Древовидные структуры: `TreeRedbObject<TProps>`, `ITreeNode<T>`, методы работы с деревом
- [x] LINQ‑подобный запросный слой (см. [LINQ_QUERY_PLAN.md](./LINQ_QUERY_PLAN.md))

---

## Следующие шаги (рекомендуемые)

1) ✅ Реализовать сохранение: `_objects` + `_values` (+ рекурсия/массивы), с использованием генератора ключей
2) ✅ Реализовать удаление: одиночное/по поддереву + проверка прав + стратегия для `_list_items`
3) ✅ Реализовать древовидные структуры (`TreeRedbObject<TProps>`, методы работы с деревом)
4) ✅ **ЗАВЕРШЕНО:** LINQ‑запросный слой: Where/OrderBy → `search_objects_with_facets` (см. [LINQ_QUERY_PLAN.md](./LINQ_QUERY_PLAN.md))
5) ✅ **ЗАВЕРШЕНО:** Вынести Code‑First в провайдерный сервис (PostgreSQL) и оставить интерфейс в `redb.Core`
6) ✅ **ЗАВЕРШЕНО:** Валидация/ограничения (проверка типов, обязательности, отчеты об изменениях схемы)
7) ✅ **ЗАВЕРШЕНО:** Подготовить авто‑тесты: синхронизация структур, загрузка/сохранение/удаление (round‑trip)
8) Добавить диагностические методы во фреймворк: анализ объектов, схем, производительности и архива

