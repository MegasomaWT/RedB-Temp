# 🔄 АНАЛИЗ МИГРАЦИЙ ДЛЯ REDB EAV АРХИТЕКТУРЫ

## 📋 **СОДЕРЖАНИЕ**
1. [Текущее состояние](#текущее-состояние)
2. [Типы миграций](#типы-миграций)
3. [Архитектурные решения](#архитектурные-решения)
4. [Лучшие практики](#лучшие-практики)
5. [Реализация](#реализация)

---

## 🔍 **ТЕКУЩЕЕ СОСТОЯНИЕ**

### ✅ **Что уже есть:**
- **Code-First синхронизация** - `SyncSchemeAsync<TProps>()`
- **Автоматическое создание структур** - рефлексия C# типов
- **Архивирование удаленных объектов** - таблица `_deleted_objects`
- **Валидация изменений** - `IValidationProvider`
- **Обнаружение breaking changes** - анализ типов

### ❌ **Что отсутствует:**
- **Версионирование схем** - нет истории изменений
- **Миграция данных** - при смене типов данные теряются
- **Переименование полей** - старые поля удаляются
- **Слияние/разделение полей** - нет механизма
- **Откат миграций** - нет возможности вернуться назад
- **Пошаговые миграции** - все изменения сразу

---

## 🎯 **ТИПЫ МИГРАЦИЙ**

### 1. **СМЕНА ТИПА ДАННЫХ**
```csharp
// Было
public class Product { public string Price { get; set; } }

// Стало  
public class Product { public decimal Price { get; set; } }
```
**Проблемы:**
- Данные в `_values._String` нужно конвертировать в `_values._Double`
- Возможны ошибки конверсии ("abc" → decimal)
- Нужны конверторы на C#

### 2. **ПЕРЕИМЕНОВАНИЕ ПОЛЕЙ**
```csharp
// Было
public class Product { public string Name { get; set; } }

// Стало
public class Product { public string Title { get; set; } }
```
**Проблемы:**
- Сейчас: `Name` удаляется, `Title` создается заново
- Данные теряются
- Нужно: переименование структуры + сохранение данных

### 3. **СЛИЯНИЕ ПОЛЕЙ (N → 1)**
```csharp
// Было
public class Person { 
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

// Стало
public class Person { 
    public string FullName { get; set; }
}
```
**Проблемы:**
- Нужно объединить `FirstName + " " + LastName → FullName`
- Логика слияния может быть сложной
- Нужны кастомные конверторы

### 4. **РАЗДЕЛЕНИЕ ПОЛЕЙ (1 → N)**
```csharp
// Было
public class Person { 
    public string FullName { get; set; }
}

// Стало
public class Person { 
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
```
**Проблемы:**
- Нужно разделить `"John Doe" → FirstName="John", LastName="Doe"`
- Парсинг может быть неоднозначным
- Нужны умные алгоритмы разбора

### 5. **ВЕРСИОНИРОВАНИЕ И МЕТКИ**
```csharp
[Migration("v1.2.0")]
public class ProductV2 { 
    public decimal Price { get; set; }  // было string
    public string Title { get; set; }   // было Name
}
```
**Проблемы:**
- Как отследить какие миграции уже применены?
- Как не потерять новые данные при откате?
- Как обеспечить совместимость версий?

### 6. **ДОПОЛНИТЕЛЬНЫЕ ТИПЫ МИГРАЦИЙ**

#### **6.1. Изменение обязательности**
```csharp
// Было: optional
public string? Email { get; set; }

// Стало: required  
public string Email { get; set; } = "";
```

#### **6.2. Изменение массивности**
```csharp
// Было: одиночное значение
public string Tag { get; set; }

// Стало: массив
public string[] Tags { get; set; }
```

#### **6.3. Добавление вычисляемых полей**
```csharp
public class Order {
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    
    // Новое вычисляемое поле
    public decimal Total => Price * Quantity;
}
```

#### **6.4. Изменение связей**
```csharp
// Было: простая ссылка
public long CategoryId { get; set; }

// Стало: объектная ссылка
public Category Category { get; set; }
```

---

## 🏗️ **АРХИТЕКТУРНЫЕ РЕШЕНИЯ**

### 1. **СИСТЕМА ВЕРСИОНИРОВАНИЯ СХЕМ**

```sql
-- Новая таблица для версий схем
CREATE TABLE _scheme_versions (
    _id bigint PRIMARY KEY,
    _id_scheme bigint NOT NULL,
    _version varchar(50) NOT NULL,
    _migration_script text NULL,
    _date_applied timestamp DEFAULT now(),
    _applied_by bigint NOT NULL,
    _rollback_script text NULL,
    _is_breaking boolean DEFAULT false,
    CONSTRAINT FK_scheme_versions_schemes FOREIGN KEY (_id_scheme) REFERENCES _schemes (_id)
);

-- Таблица миграций
CREATE TABLE _migrations (
    _id bigint PRIMARY KEY,
    _name varchar(250) NOT NULL,
    _version varchar(50) NOT NULL,
    _date_applied timestamp DEFAULT now(),
    _applied_by bigint NOT NULL,
    _success boolean DEFAULT true,
    _error_message text NULL,
    _execution_time_ms bigint NULL
);
```

### 2. **КОНВЕРТОРЫ ДАННЫХ**

```csharp
public interface IDataConverter
{
    bool CanConvert(Type fromType, Type toType);
    Task<object?> ConvertAsync(object? value, Type fromType, Type toType);
    Task<List<ConversionError>> ValidateAsync(IEnumerable<object?> values, Type fromType, Type toType);
}

// Встроенные конверторы
public class StringToDecimalConverter : IDataConverter
{
    public bool CanConvert(Type fromType, Type toType) 
        => fromType == typeof(string) && toType == typeof(decimal);
    
    public async Task<object?> ConvertAsync(object? value, Type fromType, Type toType)
    {
        if (value == null) return null;
        if (decimal.TryParse(value.ToString(), out var result))
            return result;
        throw new ConversionException($"Cannot convert '{value}' to decimal");
    }
}
```

### 3. **МИГРАЦИОННЫЕ СКРИПТЫ**

```csharp
[Migration("1.0.0", "1.1.0")]
public class ProductPriceMigration : IMigration
{
    public string Description => "Convert Price from string to decimal";
    
    public async Task UpAsync(IMigrationContext context)
    {
        // 1. Создаем новую структуру
        await context.AddStructureAsync("Price", typeof(decimal));
        
        // 2. Конвертируем данные
        await context.ConvertDataAsync("Price", typeof(string), typeof(decimal), 
            new StringToDecimalConverter());
        
        // 3. Удаляем старую структуру
        await context.RemoveStructureAsync("Price", typeof(string));
    }
    
    public async Task DownAsync(IMigrationContext context)
    {
        // Откат: decimal → string
        await context.ConvertDataAsync("Price", typeof(decimal), typeof(string),
            new DecimalToStringConverter());
    }
}
```

### 4. **ПЕРЕИМЕНОВАНИЕ ПОЛЕЙ**

```csharp
[Migration("1.1.0", "1.2.0")]
public class ProductNameToTitleMigration : IMigration
{
    public async Task UpAsync(IMigrationContext context)
    {
        // Переименование без потери данных
        await context.RenameStructureAsync("Name", "Title");
    }
    
    public async Task DownAsync(IMigrationContext context)
    {
        await context.RenameStructureAsync("Title", "Name");
    }
}
```

### 5. **СЛИЯНИЕ И РАЗДЕЛЕНИЕ ПОЛЕЙ**

```csharp
[Migration("1.2.0", "1.3.0")]
public class PersonNameMigration : IMigration
{
    public async Task UpAsync(IMigrationContext context)
    {
        // Слияние: FirstName + LastName → FullName
        await context.MergeFieldsAsync(
            sourceFields: new[] { "FirstName", "LastName" },
            targetField: "FullName",
            merger: (values) => $"{values[0]} {values[1]}".Trim()
        );
    }
    
    public async Task DownAsync(IMigrationContext context)
    {
        // Разделение: FullName → FirstName + LastName
        await context.SplitFieldAsync(
            sourceField: "FullName",
            targetFields: new[] { "FirstName", "LastName" },
            splitter: (fullName) => {
                var parts = fullName?.Split(' ', 2) ?? new string[0];
                return new[] { 
                    parts.Length > 0 ? parts[0] : "",
                    parts.Length > 1 ? parts[1] : ""
                };
            }
        );
    }
}
```

---

## 🎯 **ЛУЧШИЕ ПРАКТИКИ**

### 1. **ПРИНЦИПЫ МИГРАЦИЙ**

#### **🔒 Безопасность данных**
- ✅ **Всегда создавать бэкап** перед миграцией
- ✅ **Транзакционность** - все или ничего
- ✅ **Валидация данных** перед конверсией
- ✅ **Откат при ошибках** - автоматический rollback

#### **📈 Производительность**
- ✅ **Пакетная обработка** - не по одной записи
- ✅ **Прогресс-бар** для длительных операций
- ✅ **Индексы** для ускорения поиска

#### **🔄 Совместимость**
- ✅ **Обратная совместимость** - старый код должен работать
- ✅ **Постепенный переход** - поддержка двух версий
- ✅ **Graceful degradation** - работа при отсутствии полей

### 2. **СТРАТЕГИИ МИГРАЦИЙ**

#### **🎯 Blue-Green Deployment**
```csharp
// Этап 1: Создаем новые структуры рядом со старыми
await context.AddStructureAsync("Title", typeof(string)); // новое поле
// Старое поле "Name" пока остается

// Этап 2: Заполняем новые поля данными
await context.CopyDataAsync("Name", "Title");

// Этап 3: Переключаем код на новые поля
// [Деплой нового кода]

// Этап 4: Удаляем старые поля (через несколько дней)
await context.RemoveStructureAsync("Name", typeof(string));
```

#### **🔄 Rolling Updates**
```csharp
// Поддерживаем оба поля одновременно
public class Product {
    [Obsolete("Use Title instead")]
    public string Name { get; set; }
    
    public string Title { get; set; }
}

// Автоматическая синхронизация
public string Name {
    get => Title;
    set => Title = value;
}
```

### 3. **ОБРАБОТКА ОШИБОК**

```csharp
public class MigrationResult
{
    public bool Success { get; set; }
    public List<ConversionError> Errors { get; set; } = new();
    public int ProcessedRecords { get; set; }
    public int FailedRecords { get; set; }
    public TimeSpan ExecutionTime { get; set; }
}

public class ConversionError
{
    public long ObjectId { get; set; }
    public string FieldName { get; set; }
    public object? OriginalValue { get; set; }
    public string ErrorMessage { get; set; }
    public ConversionErrorType Type { get; set; }
}

public enum ConversionErrorType
{
    InvalidFormat,      // "abc" → decimal
    ValueTooLarge,      // 999999999999999 → int
    RequiredFieldNull,  // null → required field
    CustomValidation    // бизнес-правила
}
```

---

## 🛠️ **РЕАЛИЗАЦИЯ**

### 1. **ИНТЕРФЕЙСЫ МИГРАЦИЙ**

```csharp
public interface IMigrationProvider
{
    Task<List<PendingMigration>> GetPendingMigrationsAsync<TProps>(string? schemeName = null);
    Task<MigrationResult> ApplyMigrationAsync(IMigration migration);
    Task<MigrationResult> RollbackMigrationAsync(string migrationName);
    Task<List<AppliedMigration>> GetAppliedMigrationsAsync();
}

public interface IMigrationContext
{
    // Структуры
    Task AddStructureAsync(string name, Type type, bool isRequired = false, bool isArray = false);
    Task RemoveStructureAsync(string name, Type type);
    Task RenameStructureAsync(string oldName, string newName);
    Task ChangeStructureTypeAsync(string name, Type oldType, Type newType, IDataConverter converter);
    
    // Данные
    Task<int> ConvertDataAsync(string fieldName, Type fromType, Type toType, IDataConverter converter);
    Task<int> CopyDataAsync(string sourceField, string targetField);
    Task<int> MergeFieldsAsync(string[] sourceFields, string targetField, Func<object?[], object?> merger);
    Task<int> SplitFieldAsync(string sourceField, string[] targetFields, Func<object?, object?[]> splitter);
    
    // Валидация
    Task<List<ConversionError>> ValidateConversionAsync(string fieldName, Type fromType, Type toType, IDataConverter converter);
    Task<long> CountRecordsAsync(long schemeId);
    
    // Транзакции
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
```

### 2. **АТРИБУТЫ И МЕТАДАННЫЕ**

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class MigrationAttribute : Attribute
{
    public string FromVersion { get; }
    public string ToVersion { get; }
    public bool IsBreaking { get; set; }
    public string Description { get; set; } = "";
    
    public MigrationAttribute(string fromVersion, string toVersion)
    {
        FromVersion = fromVersion;
        ToVersion = toVersion;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class MigratedFromAttribute : Attribute
{
    public string OldName { get; }
    public Type? OldType { get; set; }
    public string? ConverterType { get; set; }
    
    public MigratedFromAttribute(string oldName)
    {
        OldName = oldName;
    }
}

// Использование
public class Product
{
    [MigratedFrom("Name")]
    public string Title { get; set; } = "";
    
    [MigratedFrom("Price", OldType = typeof(string), ConverterType = nameof(StringToDecimalConverter))]
    public decimal Price { get; set; }
}
```

### 3. **АВТОМАТИЧЕСКОЕ ОБНАРУЖЕНИЕ МИГРАЦИЙ**

```csharp
public class AutoMigrationDetector
{
    public List<RequiredMigration> DetectMigrations<TProps>(long schemeId)
    {
        var migrations = new List<RequiredMigration>();
        var properties = typeof(TProps).GetProperties();
        
        foreach (var prop in properties)
        {
            var migratedFrom = prop.GetCustomAttribute<MigratedFromAttribute>();
            if (migratedFrom != null)
            {
                migrations.Add(new RequiredMigration
                {
                    Type = MigrationType.Rename,
                    OldName = migratedFrom.OldName,
                    NewName = prop.Name,
                    OldType = migratedFrom.OldType ?? prop.PropertyType,
                    NewType = prop.PropertyType,
                    ConverterType = migratedFrom.ConverterType
                });
            }
        }
        
        return migrations;
    }
}
```

### 4. **ПРИМЕР ИСПОЛЬЗОВАНИЯ**

```csharp
// 1. Определяем миграцию
[Migration("1.0.0", "1.1.0", Description = "Improve product model")]
public class ProductMigrationV1_1 : IMigration
{
    public async Task UpAsync(IMigrationContext context)
    {
        // Переименование
        await context.RenameStructureAsync("Name", "Title");
        
        // Смена типа
        await context.ChangeStructureTypeAsync("Price", typeof(string), typeof(decimal), 
            new StringToDecimalConverter());
        
        // Слияние полей
        await context.MergeFieldsAsync(
            new[] { "FirstName", "LastName" }, 
            "FullName",
            values => $"{values[0]} {values[1]}".Trim()
        );
    }
}

// 2. Применяем миграции
var migrationProvider = serviceProvider.GetService<IMigrationProvider>();
var pendingMigrations = await migrationProvider.GetPendingMigrationsAsync<Product>();

foreach (var migration in pendingMigrations)
{
    Console.WriteLine($"Applying migration: {migration.Name}");
    var result = await migrationProvider.ApplyMigrationAsync(migration);
    
    if (!result.Success)
    {
        Console.WriteLine($"Migration failed: {result.Errors.Count} errors");
        foreach (var error in result.Errors.Take(10))
        {
            Console.WriteLine($"  Object {error.ObjectId}: {error.ErrorMessage}");
        }
        break;
    }
}
```

---

## 🎯 **ЗАКЛЮЧЕНИЕ**

### **Приоритеты реализации:**

1. **🔥 Критично:**
   - Версионирование схем
   - Базовые конверторы (string ↔ number ↔ date)
   - Переименование полей
   - Система откатов

2. **📈 Важно:**
   - Слияние/разделение полей
   - Валидация данных
   - Прогресс миграций
   - Обработка ошибок

3. **✨ Желательно:**
   - Автоматическое обнаружение миграций
   - Blue-Green deployment
   - Метрики производительности

### **Архитектурные принципы:**
- ✅ **Безопасность данных** превыше всего
- ✅ **Обратная совместимость** для плавного перехода
- ✅ **Транзакционность** для целостности
- ✅ **Наблюдаемость** для контроля процесса

Эта система миграций обеспечит эволюцию схем данных без потери информации и с минимальными рисками для продакшена.
