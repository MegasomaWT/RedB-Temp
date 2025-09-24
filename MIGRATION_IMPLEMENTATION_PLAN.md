# 📋 ПЛАН РЕАЛИЗАЦИИ СИСТЕМЫ МИГРАЦИЙ

## 🎯 **ЦЕЛЬ**
Расширить `SyncStructuresFromTypeAsync` с поддержкой лямбда-конверторов и версионирования миграций.

---

## 📝 **ЭТАПЫ РЕАЛИЗАЦИИ**

### **ЭТАП 1: БАЗОВАЯ ИНФРАСТРУКТУРА БД**
- [ ] 1.1. Расширить таблицу `_schemes` версионированием
  - [ ] Добавить поля: `_version varchar(50)`, `_created_at timestamp`, `_updated_at timestamp`
  - [ ] Добавить индексы для быстрого поиска по версиям
  - [ ] Обновить существующие записи с версией "1.0"

- [ ] 1.2. Создать таблицу `_migrations` - журнал миграций
  - [ ] `_id bigint PRIMARY KEY` - ID миграции
  - [ ] `_scheme_id bigint` - ссылка на схему
  - [ ] `_from_version varchar(50)` - исходная версия
  - [ ] `_to_version varchar(50)` - целевая версия
  - [ ] `_migration_type varchar(100)` - тип миграции (ChangeType, Rename, Split, Merge)
  - [ ] `_field_name varchar(250)` - имя поля
  - [ ] `_migration_data jsonb` - детали миграции (старые/новые имена, конверторы)
  - [ ] `_applied_at timestamp` - время применения
  - [ ] `_applied_by bigint` - кто применил (ссылка на _users)
  - [ ] `_status varchar(50)` - статус (Pending, InProgress, Completed, Failed, Rollback)
  - [ ] `_records_processed bigint` - обработано записей
  - [ ] `_records_failed bigint` - ошибок при обработке
  - [ ] `_execution_time_ms bigint` - время выполнения в мс
  - [ ] `_error_message text` - сообщение об ошибке
  - [ ] Индексы и ограничения

- [ ] 1.3. Создать таблицу `_migration_errors` - детальные ошибки
  - [ ] `_id bigint PRIMARY KEY` - ID ошибки
  - [ ] `_migration_id bigint` - ссылка на миграцию
  - [ ] `_object_id bigint` - объект с ошибкой
  - [ ] `_structure_id bigint` - структура с ошибкой
  - [ ] `_old_value text` - старое значение
  - [ ] `_error_message text` - описание ошибки
  - [ ] `_occurred_at timestamp` - время ошибки

- [ ] 1.4. Создать C# модели для новых таблиц
  - [ ] `_RScheme` - обновить с версионированием
  - [ ] `_RMigration` - модель миграции
  - [ ] `_RMigrationError` - модель ошибки миграции
  - [ ] `MigrationResult` - результат миграции
  - [ ] `MigrationOptions` - настройки миграции
  - [ ] `ConversionError` - ошибки конверсии
  - [ ] `MigrationStrategy` - стратегии обработки ошибок

- [ ] 1.5. Создать базовые интерфейсы
  - [ ] `IDataConverter` - базовый интерфейс конвертора
  - [ ] `LambdaConverter<TOld, TNew>` - лямбда конвертор

### **ЭТАП 2: КОНФИГУРАЦИЯ МИГРАЦИЙ**
- [ ] 2.1. Создать `MigrationConfig<TProps>`
  - [ ] Fluent API методы
  - [ ] `RenameField(oldName, newName)`
  - [ ] `ChangeFieldType<TOld, TNew>(fieldName, converter)`
  - [ ] `SplitField<TSource>(sourceField, targetConverters)`
  - [ ] `MergeFields<TTarget>(sourceFields, targetField, merger)`

- [ ] 2.2. Создать `FieldMigration`
  - [ ] Типы миграций: Rename, ChangeType, Split, Merge
  - [ ] Хранение конверторов и параметров

- [ ] 2.3. Система версионирования
  - [ ] `SchemeVersion` - атрибут для классов
  - [ ] Автоматическое определение версий
  - [ ] Проверка необходимости миграции

### **ЭТАП 3: РАСШИРЕНИЕ ISchemeSyncProvider**
- [ ] 3.1. Добавить новые методы в интерфейс
  - [ ] `SyncStructuresWithMigrationAsync<TProps>`
  - [ ] `SyncSchemeWithMigrationAsync<TProps>`
  - [ ] `GetSchemeVersionAsync(schemeId)`
  - [ ] `NeedsMigrationAsync<TProps>()`

- [ ] 3.2. Обновить `PostgresSchemeSyncProvider`
  - [ ] Реализовать новые методы
  - [ ] Интеграция с существующим `SyncStructuresFromTypeAsync`

### **ЭТАП 4: ОБРАБОТКА МИГРАЦИЙ**
- [ ] 4.1. Методы применения миграций
  - [ ] `ApplyFieldMigrationAsync` - общий метод
  - [ ] `ApplyRenameMigrationAsync` - переименование
  - [ ] `ApplyChangeTypeMigrationAsync` - смена типа
  - [ ] `ApplySplitMigrationAsync` - декомпозиция
  - [ ] `ApplyMergeMigrationAsync` - композиция

- [ ] 4.2. Пакетная обработка данных
  - [ ] `GetFieldValuesBatchAsync` - получение пакета данных
  - [ ] `ProcessConversionBatchAsync` - обработка пакета
  - [ ] `SaveConvertedValueAsync` - сохранение результата

- [ ] 4.3. Управление структурами БД
  - [ ] `CreateTempStructureAsync` - временные структуры
  - [ ] `RenameStructureAsync` - переименование структур
  - [ ] `DeleteStructureAsync` - удаление структур

### **ЭТАП 5: ВЕРСИОНИРОВАНИЕ И ЖУРНАЛИРОВАНИЕ**
- [ ] 5.1. Работа с версиями схем
  - [ ] `GetCurrentSchemeVersionAsync` - текущая версия
  - [ ] `UpdateSchemeVersionAsync` - обновление версии
  - [ ] `GetMigrationHistoryAsync` - история миграций

- [ ] 5.2. Журналирование миграций
  - [ ] `LogMigrationAsync` - запись в журнал
  - [ ] Детальная информация о процессе
  - [ ] Статистика и ошибки

- [ ] 5.3. Валидация миграций
  - [ ] `ValidateMigrationAsync` - предварительная проверка
  - [ ] Проверка совместимости версий
  - [ ] Оценка времени выполнения

### **ЭТАП 6: ИНТЕГРАЦИЯ С REDBSERVICE**
- [ ] 6.1. Обновить `IRedbService`
  - [ ] Добавить методы миграции
  - [ ] Делегирование к `ISchemeSyncProvider`

- [ ] 6.2. Обновить `RedbService`
  - [ ] Реализация новых методов
  - [ ] Интеграция с существующими методами

- [ ] 6.3. Обратная совместимость
  - [ ] Существующие методы работают без изменений
  - [ ] Автоматическое определение необходимости миграции

### **ЭТАП 7: ТЕСТИРОВАНИЕ**
- [ ] 7.1. Создать тестовые модели
  - [ ] Модели для разных типов миграций
  - [ ] Сложные сценарии конверсии

- [ ] 7.2. Unit тесты
  - [ ] Тесты конверторов
  - [ ] Тесты пакетной обработки
  - [ ] Тесты версионирования

- [ ] 7.3. Integration тесты
  - [ ] Полные сценарии миграций
  - [ ] Тесты с реальной БД
  - [ ] Тесты производительности

---

## 🏗️ **АРХИТЕКТУРА РЕШЕНИЯ**

### **ДЕТАЛЬНАЯ СХЕМА БД:**

#### **1. Расширение таблицы `_schemes`:**
```sql
-- Добавляем поля версионирования к существующей таблице
ALTER TABLE _schemes 
ADD COLUMN _version varchar(50) DEFAULT '1.0' NOT NULL,
ADD COLUMN _created_at timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
ADD COLUMN _updated_at timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL;

-- Индексы для быстрого поиска
CREATE INDEX IX__schemes__version ON _schemes(_version);
CREATE INDEX IX__schemes__updated_at ON _schemes(_updated_at);

-- Триггер для автоматического обновления _updated_at
CREATE OR REPLACE FUNCTION update_schemes_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW._updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr__schemes__updated_at
    BEFORE UPDATE ON _schemes
    FOR EACH ROW
    EXECUTE FUNCTION update_schemes_updated_at();
```

#### **2. Таблица `_migrations` - журнал миграций:**
```sql
CREATE TABLE _migrations(
    _id bigint NOT NULL DEFAULT nextval('global_identity'),
    _scheme_id bigint NOT NULL,
    _from_version varchar(50) NOT NULL,
    _to_version varchar(50) NOT NULL,
    _migration_type varchar(100) NOT NULL, -- 'ChangeType', 'Rename', 'Split', 'Merge', 'Custom'
    _field_name varchar(250) NULL, -- имя поля (NULL для комплексных миграций)
    _migration_data jsonb NOT NULL, -- детали миграции
    _applied_at timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
    _applied_by bigint NOT NULL, -- кто применил миграцию
    _status varchar(50) DEFAULT 'Pending' NOT NULL, -- 'Pending', 'InProgress', 'Completed', 'Failed', 'Rollback'
    _records_processed bigint DEFAULT 0 NOT NULL,
    _records_failed bigint DEFAULT 0 NOT NULL,
    _execution_time_ms bigint DEFAULT 0 NOT NULL,
    _error_message text NULL,
    
    CONSTRAINT PK__migrations PRIMARY KEY (_id),
    CONSTRAINT FK__migrations__schemes FOREIGN KEY (_scheme_id) REFERENCES _schemes (_id) ON DELETE CASCADE,
    CONSTRAINT FK__migrations__users FOREIGN KEY (_applied_by) REFERENCES _users (_id),
    CONSTRAINT CK__migrations__status CHECK (_status IN ('Pending', 'InProgress', 'Completed', 'Failed', 'Rollback')),
    CONSTRAINT CK__migrations__type CHECK (_migration_type IN ('ChangeType', 'Rename', 'Split', 'Merge', 'Custom'))
);

-- Индексы для быстрого поиска
CREATE INDEX IX__migrations__scheme_id ON _migrations(_scheme_id);
CREATE INDEX IX__migrations__status ON _migrations(_status);
CREATE INDEX IX__migrations__applied_at ON _migrations(_applied_at);
CREATE INDEX IX__migrations__versions ON _migrations(_scheme_id, _from_version, _to_version);
```

#### **3. Таблица `_migration_errors` - детальные ошибки:**
```sql
CREATE TABLE _migration_errors(
    _id bigint NOT NULL DEFAULT nextval('global_identity'),
    _migration_id bigint NOT NULL,
    _object_id bigint NULL, -- объект с ошибкой (может быть NULL)
    _structure_id bigint NULL, -- структура с ошибкой (может быть NULL)
    _old_value text NULL, -- старое значение
    _error_message text NOT NULL,
    _occurred_at timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
    
    CONSTRAINT PK__migration_errors PRIMARY KEY (_id),
    CONSTRAINT FK__migration_errors__migrations FOREIGN KEY (_migration_id) REFERENCES _migrations (_id) ON DELETE CASCADE,
    CONSTRAINT FK__migration_errors__objects FOREIGN KEY (_object_id) REFERENCES _objects (_id) ON DELETE SET NULL,
    CONSTRAINT FK__migration_errors__structures FOREIGN KEY (_structure_id) REFERENCES _structures (_id) ON DELETE SET NULL
);

-- Индексы
CREATE INDEX IX__migration_errors__migration_id ON _migration_errors(_migration_id);
CREATE INDEX IX__migration_errors__object_id ON _migration_errors(_object_id);
CREATE INDEX IX__migration_errors__occurred_at ON _migration_errors(_occurred_at);
```

#### **4. Примеры данных в `_migration_data` (JSONB):**

**Переименование поля:**
```json
{
  "type": "Rename",
  "old_name": "Name",
  "new_name": "Title"
}
```

**Смена типа с конвертором:**
```json
{
  "type": "ChangeType",
  "field_name": "Price",
  "old_type": "string",
  "new_type": "decimal",
  "converter": {
    "type": "lambda",
    "expression": "price => decimal.TryParse(price, out var p) ? p : 0m"
  }
}
```

**Декомпозиция поля (1 → N):**
```json
{
  "type": "Split",
  "source_field": "FullName",
  "target_fields": {
    "FirstName": {
      "converter": "name => name?.Split(' ').FirstOrDefault() ?? \"\""
    },
    "LastName": {
      "converter": "name => name?.Split(' ').Skip(1).FirstOrDefault() ?? \"\""
    }
  }
}
```

**Композиция полей (N → 1):**
```json
{
  "type": "Merge",
  "source_fields": ["FirstName", "LastName"],
  "target_field": "FullName",
  "merger": "fields => $\"{fields[0]} {fields[1]}\".Trim()"
}
```

#### **5. VIEW для удобного просмотра миграций:**
```sql
CREATE VIEW v_migration_history AS
SELECT 
    m._id as migration_id,
    s._name as scheme_name,
    s._version as current_version,
    m._from_version,
    m._to_version,
    m._migration_type,
    m._field_name,
    m._status,
    m._records_processed,
    m._records_failed,
    m._execution_time_ms,
    m._applied_at,
    u._name as applied_by_user,
    m._error_message,
    (SELECT COUNT(*) FROM _migration_errors me WHERE me._migration_id = m._id) as error_count
FROM _migrations m
JOIN _schemes s ON m._scheme_id = s._id
LEFT JOIN _users u ON m._applied_by = u._id
ORDER BY m._applied_at DESC;
```

### **Структура файлов:**
```
redb.Core/
├── Models/
│   ├── Migration/
│   │   ├── MigrationResult.cs
│   │   ├── MigrationOptions.cs
│   │   ├── MigrationConfig.cs
│   │   ├── FieldMigration.cs
│   │   └── ConversionError.cs
│   └── Versioning/
│       ├── SchemeVersion.cs
│       └── MigrationHistory.cs
├── Providers/
│   ├── ISchemeSyncProvider.cs (обновить)
│   └── Migration/
│       ├── IDataConverter.cs
│       └── LambdaConverter.cs
└── Attributes/
    └── SchemeVersionAttribute.cs

redb.Core.Postgres/
├── Providers/
│   └── PostgresSchemeSyncProvider.cs (обновить)
├── Models/
│   ├── _RSchemeVersion.cs
│   └── _RMigration.cs
└── sql/
    └── redbPostgre.sql (обновить)
```

### **Ключевые C# модели:**

#### **1. Модели БД (EF Core):**
```csharp
// Обновленная модель схемы с версионированием
public class _RScheme
{
    public long _id { get; set; }
    public long? _id_parent { get; set; }
    public string _name { get; set; } = "";
    public string? _alias { get; set; }
    public string? _name_space { get; set; }
    public string _version { get; set; } = "1.0"; // НОВОЕ
    public DateTime _created_at { get; set; } = DateTime.UtcNow; // НОВОЕ
    public DateTime _updated_at { get; set; } = DateTime.UtcNow; // НОВОЕ
}

// Модель миграции
public class _RMigration
{
    public long _id { get; set; }
    public long _scheme_id { get; set; }
    public string _from_version { get; set; } = "";
    public string _to_version { get; set; } = "";
    public string _migration_type { get; set; } = ""; // ChangeType, Rename, Split, Merge, Custom
    public string? _field_name { get; set; }
    public string _migration_data { get; set; } = "{}"; // JSON
    public DateTime _applied_at { get; set; } = DateTime.UtcNow;
    public long _applied_by { get; set; }
    public string _status { get; set; } = "Pending"; // Pending, InProgress, Completed, Failed, Rollback
    public long _records_processed { get; set; } = 0;
    public long _records_failed { get; set; } = 0;
    public long _execution_time_ms { get; set; } = 0;
    public string? _error_message { get; set; }
    
    // Navigation properties
    public _RScheme Scheme { get; set; } = null!;
    public _RUser AppliedByUser { get; set; } = null!;
    public List<_RMigrationError> Errors { get; set; } = new();
}

// Модель ошибки миграции
public class _RMigrationError
{
    public long _id { get; set; }
    public long _migration_id { get; set; }
    public long? _object_id { get; set; }
    public long? _structure_id { get; set; }
    public string? _old_value { get; set; }
    public string _error_message { get; set; } = "";
    public DateTime _occurred_at { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public _RMigration Migration { get; set; } = null!;
    public _RObject? Object { get; set; }
    public _RStructure? Structure { get; set; }
}
```

#### **2. Бизнес-модели:**
```csharp
// Основная конфигурация миграции
public class MigrationConfig<TProps> where TProps : class, new()
{
    public string FromVersion { get; set; } = "";
    public string ToVersion { get; set; } = "";
    public List<FieldMigration> FieldMigrations { get; set; } = new();
    
    // Fluent API
    public MigrationConfig<TProps> RenameField(string oldName, string newName);
    public MigrationConfig<TProps> ChangeFieldType<TOld, TNew>(string fieldName, Func<TOld?, TNew?> converter);
    public MigrationConfig<TProps> SplitField<TSource>(string sourceField, Dictionary<string, Func<TSource?, object?>> targetConverters);
    public MigrationConfig<TProps> MergeFields<TTarget>(string[] sourceFields, string targetField, Func<object?[], TTarget?> merger);
}

// Детали одной миграции поля
public class FieldMigration
{
    public MigrationType Type { get; set; }
    public string? FieldName { get; set; }
    public string? OldName { get; set; }
    public string? NewName { get; set; }
    public Type? OldType { get; set; }
    public Type? NewType { get; set; }
    public object? Converter { get; set; } // Func<TOld?, TNew?> или другие типы
    public Dictionary<string, object>? TargetConverters { get; set; } // для Split
    public string[]? SourceFields { get; set; } // для Merge
    public string? TargetField { get; set; } // для Merge
    public object? Merger { get; set; } // для Merge
}

// Типы миграций
public enum MigrationType
{
    Rename,
    ChangeType,
    Split,
    Merge,
    Custom
}

// Стратегии обработки ошибок
public enum MigrationStrategy
{
    Transactional,      // Откат при любой ошибке
    ContinueOnError,    // Продолжать несмотря на ошибки
    StopOnError,        // Остановиться при первой ошибке
    StopOnThreshold     // Остановиться при превышении порога ошибок
}

// Настройки миграции
public class MigrationOptions
{
    public MigrationStrategy Strategy { get; set; } = MigrationStrategy.Transactional;
    public int BatchSize { get; set; } = 1000;
    public int ErrorThreshold { get; set; } = 10; // для StopOnThreshold
    public bool LogDetailedErrors { get; set; } = true;
    public TimeSpan? Timeout { get; set; }
    public IProgress<MigrationProgress>? Progress { get; set; }
}

// Прогресс миграции
public class MigrationProgress
{
    public long TotalRecords { get; set; }
    public long ProcessedRecords { get; set; }
    public long FailedRecords { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public string? CurrentOperation { get; set; }
    public double PercentComplete => TotalRecords > 0 ? (double)ProcessedRecords / TotalRecords * 100 : 0;
}
```

// Результат миграции
public class MigrationResult
{
    public bool Success { get; set; }
    public long SchemeId { get; set; }
    public int ProcessedRecords { get; set; }
    public int FailedRecords { get; set; }
    public List<ConversionError> Errors { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public string? ErrorMessage { get; set; }
}

// Расширенный интерфейс
public interface ISchemeSyncProvider
{
    // Существующие методы
    Task<long> SyncSchemeAsync<TProps>(string? schemeName = null, string? alias = null, bool strictDeleteExtra = true) where TProps : class;
    
    // Новые методы с миграциями
    Task<MigrationResult> SyncSchemeWithMigrationAsync<TProps>(MigrationConfig<TProps> migrationConfig, string? schemeName = null, MigrationOptions? options = null) where TProps : class, new();
    Task<bool> NeedsMigrationAsync<TProps>(string? schemeName = null) where TProps : class;
    Task<string> GetCurrentSchemeVersionAsync(long schemeId);
}
```

---

## 🎯 **ПРИМЕРЫ ИСПОЛЬЗОВАНИЯ**

### **Простая миграция:**
```csharp
var migrationConfig = new MigrationConfig<Product>()
    .RenameField("Name", "Title")
    .ChangeFieldType<string, decimal>("Price", price => 
        decimal.TryParse(price, out var p) ? p : 0m);

var result = await redb.SyncSchemeWithMigrationAsync(migrationConfig, "Product");
```

### **Автоматическое определение версий:**
```csharp
[SchemeVersion("1.2")]
public class Product
{
    public string Title { get; set; } = "";
    public decimal Price { get; set; }
}

// Автоматически определит что нужна миграция с 1.1 на 1.2
if (await redb.NeedsMigrationAsync<Product>())
{
    var config = CreateMigrationConfig();
    await redb.SyncSchemeWithMigrationAsync(config, "Product");
}
```

### **Интеграция с существующим кодом:**
```csharp
// Заменяет обычный SyncSchemeAsync при необходимости
public async Task<long> EnsureProductScheme()
{
    if (await redb.NeedsMigrationAsync<Product>())
    {
        var migration = CreateProductMigration();
        var result = await redb.SyncSchemeWithMigrationAsync(migration, "Product");
        return result.SchemeId;
    }
    
    return await redb.SyncSchemeAsync<Product>("Product");
}
```

---

## ⏱️ **ВРЕМЕННЫЕ РАМКИ**

### **ДЕТАЛЬНАЯ ОЦЕНКА:**

- **Этап 1: Базовая инфраструктура БД** - **3-4 дня**
  - 1.1. Расширение `_schemes` - 0.5 дня
  - 1.2. Создание `_migrations` - 1 день  
  - 1.3. Создание `_migration_errors` - 0.5 дня
  - 1.4. C# модели EF Core - 1 день
  - 1.5. Базовые интерфейсы - 0.5 дня

- **Этап 2: Конфигурация миграций** - **2-3 дня**
  - 2.1. `MigrationConfig<TProps>` Fluent API - 1.5 дня
  - 2.2. `FieldMigration` и енумы - 0.5 дня
  - 2.3. Система версионирования - 1 день

- **Этап 3: Расширение ISchemeSyncProvider** - **2 дня**
  - 3.1. Новые методы в интерфейсе - 0.5 дня
  - 3.2. Реализация в PostgresSchemeSyncProvider - 1.5 дня

- **Этап 4: Обработка миграций** - **4-5 дней**
  - 4.1. Методы применения миграций - 2 дня
  - 4.2. Пакетная обработка данных - 1.5 дня
  - 4.3. Управление структурами БД - 1 день

- **Этап 5: Версионирование и журналирование** - **2-3 дня**
  - 5.1. Работа с версиями схем - 1 день
  - 5.2. Журналирование миграций - 1 день
  - 5.3. Валидация миграций - 1 день

- **Этап 6: Интеграция с RedbService** - **1 день**
  - 6.1-6.3. Обновление интерфейсов и реализации - 1 день

- **Этап 7: Тестирование** - **3-4 дня**
  - 7.1. Тестовые модели - 0.5 дня
  - 7.2. Unit тесты - 1.5 дня
  - 7.3. Integration тесты - 2 дня

**Общее время:** **17-22 дня**

### **КРИТИЧЕСКИЙ ПУТЬ:**
1. **Этап 1** → **Этап 2** → **Этап 3** → **Этап 4** → **Этап 5**
2. **Этап 6** и **Этап 7** могут выполняться частично параллельно

### **ПРИОРИТЕТЫ:**
- **🔥 ВЫСОКИЙ:** Этапы 1-4 (основная функциональность)
- **🟡 СРЕДНИЙ:** Этап 5 (журналирование и валидация)  
- **🟢 НИЗКИЙ:** Этапы 6-7 (интеграция и тестирование)

---

## 🎯 **КРИТЕРИИ ГОТОВНОСТИ**

### **Функциональные:**
- ✅ Все типы миграций работают (Rename, ChangeType, Split, Merge)
- ✅ Лямбда-конверторы выполняются корректно
- ✅ Пакетная обработка для больших данных
- ✅ Версионирование схем и журналирование
- ✅ Обратная совместимость с существующим API

### **Нефункциональные:**
- ✅ Производительность: обработка 100K записей за разумное время
- ✅ Надежность: откат при ошибках, сохранение данных
- ✅ Мониторинг: прогресс и детальная статистика
- ✅ Документация: примеры использования

### **Тестирование:**
- ✅ Unit тесты покрывают 90%+ кода
- ✅ Integration тесты для всех сценариев
- ✅ Тесты производительности на больших данных
- ✅ Тесты обратной совместимости

**План готов к реализации! 🚀**
