# 🔄 УЛУЧШЕНИЯ СИСТЕМЫ МИГРАЦИЙ REDB

## 📊 **ЖУРНАЛ ПРИМЕНЁННЫХ МИГРАЦИЙ**

### **Что будет в `_migrations`:**

```sql
CREATE TABLE _migrations (
    _id bigint PRIMARY KEY,
    _id_scheme bigint NOT NULL,
    _migration_name varchar(250) NOT NULL,        -- "ProductV1_0_to_V1_1" 
    _from_version varchar(50) NOT NULL,           -- "1.0"
    _to_version varchar(50) NOT NULL,             -- "1.1"
    _date_applied timestamp DEFAULT now(),
    _applied_by bigint NOT NULL,
    _success boolean DEFAULT true,
    _error_message text NULL,
    _execution_time_ms bigint NULL,
    _processed_records bigint NULL,               -- Сколько записей обработано
    _failed_records bigint NULL,                  -- Сколько записей с ошибками
    _migration_details jsonb NULL,                -- Детали миграции
    _batch_size int NULL,                         -- Размер пакета
    CONSTRAINT FK_migrations_schemes FOREIGN KEY (_id_scheme) REFERENCES _schemes (_id)
);
```

### **Пример записи в журнале:**
```json
{
  "migration_name": "ProductPriceStringToDecimal",
  "from_version": "1.0",
  "to_version": "1.1", 
  "processed_records": 150000,
  "failed_records": 23,
  "execution_time_ms": 45000,
  "migration_details": {
    "changes": [
      {
        "field": "Price",
        "old_type": "String", 
        "new_type": "Decimal",
        "converter": "StringToDecimalConverter",
        "validation_errors": 23,
        "sample_errors": ["'abc' cannot convert to decimal", "'N/A' invalid format"]
      }
    ],
    "batch_size": 1000,
    "strategy": "continue_on_error"
  }
}
```

---

## ⚡ **ТРАНЗАКЦИОННОСТЬ - ОПЦИОНАЛЬНАЯ**

### **Стратегии обработки:**

```csharp
public enum MigrationStrategy
{
    Transactional,      // Все или ничего (по умолчанию для малых данных)
    ContinueOnError,    // Игнорировать ошибки, продолжать
    StopOnError,        // Остановиться при первой ошибке
    StopOnThreshold     // Остановиться при превышении % ошибок
}

public class MigrationOptions
{
    public MigrationStrategy Strategy { get; set; } = MigrationStrategy.Transactional;
    public int BatchSize { get; set; } = 1000;
    public double ErrorThreshold { get; set; } = 0.05; // 5% ошибок максимум
    public bool CreateBackup { get; set; } = true;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromHours(2);
}
```

### **Логика выбора стратегии:**

```csharp
public async Task<MigrationResult> ApplyMigrationAsync(IMigration migration, MigrationOptions? options = null)
{
    options ??= new MigrationOptions();
    
    // Определяем стратегию автоматически
    var recordCount = await CountRecordsAsync(migration.SchemeId);
    
    if (recordCount < 10000 && options.Strategy == MigrationStrategy.Transactional)
    {
        // Малые данные - транзакционно
        return await ApplyTransactionalAsync(migration, options);
    }
    else
    {
        // Большие данные - пакетно с обработкой ошибок
        return await ApplyBatchedAsync(migration, options);
    }
}
```

---

## 📦 **ПАКЕТНАЯ ОБРАБОТКА**

### **Как работает:**

```csharp
public async Task<MigrationResult> ApplyBatchedAsync(IMigration migration, MigrationOptions options)
{
    var result = new MigrationResult();
    var batchSize = options.BatchSize;
    var offset = 0;
    
    while (true)
    {
        // Получаем пакет записей
        var batch = await GetRecordsBatchAsync(migration.SchemeId, offset, batchSize);
        if (!batch.Any()) break;
        
        // Обрабатываем пакет
        var batchResult = await ProcessBatchAsync(batch, migration, options);
        result.Merge(batchResult);
        
        // Проверяем стратегию
        if (options.Strategy == MigrationStrategy.StopOnError && batchResult.HasErrors)
        {
            result.ErrorMessage = "Stopped on first error";
            break;
        }
        
        if (options.Strategy == MigrationStrategy.StopOnThreshold)
        {
            var errorRate = (double)result.FailedRecords / result.ProcessedRecords;
            if (errorRate > options.ErrorThreshold)
            {
                result.ErrorMessage = $"Error rate {errorRate:P} exceeds threshold {options.ErrorThreshold:P}";
                break;
            }
        }
        
        offset += batchSize;
        
        // Прогресс
        await ReportProgressAsync(result.ProcessedRecords, result.TotalRecords);
    }
    
    return result;
}

private async Task<List<MigrationRecord>> GetRecordsBatchAsync(long schemeId, int offset, int batchSize)
{
    return await _context.Database
        .SqlQueryRaw<MigrationRecord>(@"
            SELECT o._id as ObjectId, v._id as ValueId, v._id_structure as StructureId, 
                   v._String, v._Long, v._Double, v._DateTime, v._Boolean, v._Guid
            FROM _objects o
            INNER JOIN _values v ON v._id_object = o._id  
            WHERE o._id_scheme = {0}
            ORDER BY o._id, v._id
            OFFSET {1} LIMIT {2}", 
            schemeId, offset, batchSize)
        .ToListAsync();
}
```

---

## 🔧 **КОНВЕРТОРЫ В СТРУКТУРАХ**

### **Упрощенный подход - конвертор прямо в изменении структуры:**

```csharp
public async Task ChangeStructureTypeAsync(string fieldName, Type oldType, Type newType, 
    string? converterName = null, object? converterOptions = null)
{
    // 1. Создаем новую структуру
    var newStructure = await CreateStructureAsync(fieldName + "_new", newType);
    
    // 2. Конвертируем данные
    var converter = GetConverter(oldType, newType, converterName);
    await ConvertDataAsync(fieldName, fieldName + "_new", converter, converterOptions);
    
    // 3. Переименовываем структуры
    await RenameStructureAsync(fieldName, fieldName + "_old");
    await RenameStructureAsync(fieldName + "_new", fieldName);
    
    // 4. Удаляем старую структуру (опционально)
    if (options.DeleteOldStructure)
        await DeleteStructureAsync(fieldName + "_old");
}
```

### **Встроенные конверторы с опциями:**

```csharp
public class BuiltInConverters
{
    // Простые конверторы
    public static readonly Dictionary<(Type, Type), Func<object?, object?>> SimpleConverters = new()
    {
        { (typeof(string), typeof(decimal)), value => decimal.TryParse(value?.ToString(), out var d) ? d : null },
        { (typeof(string), typeof(int)), value => int.TryParse(value?.ToString(), out var i) ? i : null },
        { (typeof(string), typeof(DateTime)), value => DateTime.TryParse(value?.ToString(), out var dt) ? dt : null },
        { (typeof(decimal), typeof(string)), value => value?.ToString() },
        { (typeof(int), typeof(string)), value => value?.ToString() }
    };
    
    // Сложные конверторы (слияние/разделение)
    public static object? MergeFields(object?[] values, string separator = " ")
    {
        return string.Join(separator, values.Where(v => v != null).Select(v => v.ToString())).Trim();
    }
    
    public static object?[] SplitField(object? value, string separator = " ", int maxParts = 2)
    {
        return value?.ToString()?.Split(separator, maxParts) ?? new object?[maxParts];
    }
}
```

---

## 📝 **ВЕРСИИ МИГРАЦИЙ - УПРОЩЕННО**

### **Простое версионирование без сложных миграционных классов:**

```csharp
// Атрибут на классе модели
[SchemeVersion("1.1")]
public class Product
{
    [MigratedFrom("Name", version: "1.0")]
    public string Title { get; set; } = "";
    
    [MigratedFrom("Price", version: "1.0", oldType: typeof(string), converter: "StringToDecimal")]
    public decimal Price { get; set; }
    
    [MergedFrom(new[] { "FirstName", "LastName" }, version: "1.0", separator: " ")]
    public string FullName { get; set; } = "";
}

// Автоматическое применение при синхронизации
public async Task<long> SyncSchemeAsync<TProps>(string? schemeName = null)
{
    var schemeId = await EnsureSchemeFromTypeAsync<TProps>(schemeName);
    
    // Проверяем нужны ли миграции
    var currentVersion = await GetSchemeVersionAsync(schemeId);
    var targetVersion = GetTargetVersion<TProps>();
    
    if (currentVersion != targetVersion)
    {
        await ApplyAutoMigrationsAsync<TProps>(schemeId, currentVersion, targetVersion);
    }
    
    await SyncStructuresFromTypeAsync<TProps>(schemeId);
    return schemeId;
}
```

---

## 🎯 **УПРОЩЕННАЯ АРХИТЕКТУРА**

### **Основные принципы:**

1. **🔧 Конверторы встроены в изменение структур** - не нужны отдельные миграционные классы
2. **📝 Атрибуты на моделях** - автоматическое обнаружение изменений
3. **⚡ Опциональная транзакционность** - для больших данных пакетная обработка
4. **📊 Подробный журнал** - все детали в `_migrations` таблице

### **Пример использования:**

```csharp
// Просто синхронизируем схему - миграции применятся автоматически
var schemeId = await redb.SyncSchemeAsync<Product>();

// Или с опциями для больших данных
var options = new MigrationOptions 
{
    Strategy = MigrationStrategy.ContinueOnError,
    BatchSize = 5000,
    ErrorThreshold = 0.01 // 1% ошибок максимум
};

var result = await redb.SyncSchemeAsync<Product>(options: options);
if (!result.Success)
{
    Console.WriteLine($"Migration completed with {result.FailedRecords} errors");
}
```

**Миграции должны быть простыми и автоматическими!** 🎯
