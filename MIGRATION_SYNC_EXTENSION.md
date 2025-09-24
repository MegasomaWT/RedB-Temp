# 🔧 РАСШИРЕНИЕ SyncStructuresFromTypeAsync С ЛЯМБДА-КОНВЕРТОРАМИ

## 🎯 **НОВЫЕ МЕТОДЫ НА ОСНОВЕ SyncStructuresFromTypeAsync**

Расширяем существующий метод синхронизации структур с поддержкой миграций и лямбда-конверторов.

---

## 📝 **РАСШИРЕННЫЙ ИНТЕРФЕЙС ISchemeSyncProvider**

```csharp
public interface ISchemeSyncProvider
{
    // Существующие методы
    Task<long> EnsureSchemeFromTypeAsync<TProps>(string? schemeName = null, string? alias = null) where TProps : class;
    Task SyncStructuresFromTypeAsync<TProps>(long schemeId, bool strictDeleteExtra = true) where TProps : class;
    Task<long> SyncSchemeAsync<TProps>(string? schemeName = null, string? alias = null, bool strictDeleteExtra = true) where TProps : class;
    
    // 🚀 НОВЫЕ МЕТОДЫ С МИГРАЦИЯМИ
    Task<MigrationResult> SyncStructuresWithMigrationAsync<TProps>(
        long schemeId, 
        MigrationConfig<TProps> migrationConfig,
        MigrationOptions? options = null) 
        where TProps : class, new();
    
    Task<MigrationResult> SyncSchemeWithMigrationAsync<TProps>(
        MigrationConfig<TProps> migrationConfig,
        string? schemeName = null, 
        string? alias = null,
        MigrationOptions? options = null) 
        where TProps : class, new();
}
```

---

## 🛠️ **КОНФИГУРАЦИЯ МИГРАЦИЙ**

```csharp
public class MigrationConfig<TProps> where TProps : class, new()
{
    public string FromVersion { get; set; } = "1.0";
    public string ToVersion { get; set; } = "1.1";
    public List<FieldMigration> FieldMigrations { get; set; } = new();
    
    // Методы для настройки миграций
    public MigrationConfig<TProps> RenameField(string oldName, string newName)
    {
        FieldMigrations.Add(new FieldMigration
        {
            Type = MigrationType.Rename,
            OldName = oldName,
            NewName = newName
        });
        return this;
    }
    
    public MigrationConfig<TProps> ChangeFieldType<TOld, TNew>(
        string fieldName, 
        Func<TOld?, TNew?> converter)
    {
        FieldMigrations.Add(new FieldMigration
        {
            Type = MigrationType.ChangeType,
            FieldName = fieldName,
            OldType = typeof(TOld),
            NewType = typeof(TNew),
            Converter = new LambdaConverter<TOld, TNew>(converter)
        });
        return this;
    }
    
    public MigrationConfig<TProps> SplitField<TSource>(
        string sourceField,
        Dictionary<string, Func<TSource?, object?>> targetConverters)
    {
        FieldMigrations.Add(new FieldMigration
        {
            Type = MigrationType.Split,
            SourceField = sourceField,
            TargetFields = targetConverters.Keys.ToArray(),
            SplitConverters = targetConverters.ToDictionary(
                kvp => kvp.Key, 
                kvp => new LambdaConverter<TSource, object>(kvp.Value)
            )
        });
        return this;
    }
    
    public MigrationConfig<TProps> MergeFields<TTarget>(
        string[] sourceFields,
        string targetField,
        Func<object?[], TTarget?> merger)
    {
        FieldMigrations.Add(new FieldMigration
        {
            Type = MigrationType.Merge,
            SourceFields = sourceFields,
            TargetField = targetField,
            MergeConverter = new LambdaConverter<object[], TTarget>(merger)
        });
        return this;
    }
}

public class FieldMigration
{
    public MigrationType Type { get; set; }
    public string? FieldName { get; set; }
    public string? OldName { get; set; }
    public string? NewName { get; set; }
    public Type? OldType { get; set; }
    public Type? NewType { get; set; }
    public string? SourceField { get; set; }
    public string[]? SourceFields { get; set; }
    public string[]? TargetFields { get; set; }
    public string? TargetField { get; set; }
    public IDataConverter? Converter { get; set; }
    public Dictionary<string, IDataConverter>? SplitConverters { get; set; }
    public IDataConverter? MergeConverter { get; set; }
}

public enum MigrationType
{
    Rename,
    ChangeType,
    Split,
    Merge,
    Custom
}
```

---

## 🚀 **РЕАЛИЗАЦИЯ В PostgresSchemeSyncProvider**

```csharp
public class PostgresSchemeSyncProvider : ISchemeSyncProvider
{
    private readonly RedbContext _context;
    private readonly Dictionary<string, long> _typeCache = new();
    private readonly ILogger<PostgresSchemeSyncProvider>? _logger;
    
    // Существующие методы остаются без изменений...
    
    // 🚀 НОВЫЙ МЕТОД С МИГРАЦИЯМИ
    public async Task<MigrationResult> SyncStructuresWithMigrationAsync<TProps>(
        long schemeId, 
        MigrationConfig<TProps> migrationConfig,
        MigrationOptions? options = null) 
        where TProps : class, new()
    {
        options ??= new MigrationOptions();
        var result = new MigrationResult();
        
        _logger?.LogInformation($"🚀 Starting migration for scheme {schemeId}: v{migrationConfig.FromVersion} → v{migrationConfig.ToVersion}");
        
        try
        {
            // 1. Применяем миграции данных ПЕРЕД синхронизацией структур
            foreach (var migration in migrationConfig.FieldMigrations)
            {
                var migrationResult = await ApplyFieldMigrationAsync(schemeId, migration, options);
                result.Merge(migrationResult);
                
                if (!migrationResult.Success && options.Strategy == MigrationStrategy.StopOnError)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Migration failed at step: {migration.Type}";
                    return result;
                }
            }
            
            // 2. Синхронизируем структуры (обновленная версия)
            await SyncStructuresFromTypeAsync<TProps>(schemeId, options.StrictDeleteExtra);
            
            // 3. Записываем в журнал миграции
            await LogMigrationAsync(schemeId, migrationConfig, result);
            
            result.Success = true;
            _logger?.LogInformation($"✅ Migration completed: {result.ProcessedRecords} records, {result.FailedRecords} errors");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger?.LogError(ex, $"❌ Migration failed for scheme {schemeId}");
        }
        
        return result;
    }
    
    public async Task<MigrationResult> SyncSchemeWithMigrationAsync<TProps>(
        MigrationConfig<TProps> migrationConfig,
        string? schemeName = null, 
        string? alias = null,
        MigrationOptions? options = null) 
        where TProps : class, new()
    {
        var schemeId = await EnsureSchemeFromTypeAsync<TProps>(schemeName, alias);
        return await SyncStructuresWithMigrationAsync(schemeId, migrationConfig, options);
    }
    
    private async Task<MigrationResult> ApplyFieldMigrationAsync(
        long schemeId, 
        FieldMigration migration, 
        MigrationOptions options)
    {
        return migration.Type switch
        {
            MigrationType.Rename => await ApplyRenameMigrationAsync(schemeId, migration, options),
            MigrationType.ChangeType => await ApplyChangeTypeMigrationAsync(schemeId, migration, options),
            MigrationType.Split => await ApplySplitMigrationAsync(schemeId, migration, options),
            MigrationType.Merge => await ApplyMergeMigrationAsync(schemeId, migration, options),
            _ => throw new NotSupportedException($"Migration type {migration.Type} not supported")
        };
    }
    
    private async Task<MigrationResult> ApplyChangeTypeMigrationAsync(
        long schemeId, 
        FieldMigration migration, 
        MigrationOptions options)
    {
        var result = new MigrationResult();
        var fieldName = migration.FieldName!;
        var converter = migration.Converter!;
        
        _logger?.LogInformation($"🔄 Converting field '{fieldName}': {migration.OldType?.Name} → {migration.NewType?.Name}");
        
        // 1. Создаем новую структуру
        var newStructureId = await CreateTempStructureAsync(fieldName + "_new", migration.NewType!, schemeId);
        
        // 2. Конвертируем данные пакетами
        var offset = 0;
        while (true)
        {
            var batch = await GetFieldValuesBatchAsync(schemeId, fieldName, offset, options.BatchSize);
            if (!batch.Any()) break;
            
            foreach (var record in batch)
            {
                try
                {
                    // Применяем лямбда-конвертор
                    var convertedValue = await converter.ConvertAsync(record.Value, migration.OldType!, migration.NewType!);
                    
                    // Сохраняем в новую структуру
                    await SaveConvertedValueAsync(record.ObjectId, newStructureId, convertedValue, migration.NewType!);
                    
                    result.ProcessedRecords++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ConversionError
                    {
                        ObjectId = record.ObjectId,
                        FieldName = fieldName,
                        OriginalValue = record.Value,
                        ErrorMessage = ex.Message,
                        Type = ConversionErrorType.CustomValidation
                    });
                    
                    result.FailedRecords++;
                    
                    if (options.Strategy == MigrationStrategy.StopOnError)
                        break;
                }
            }
            
            offset += options.BatchSize;
            
            // Прогресс
            if (offset % (options.BatchSize * 10) == 0)
            {
                _logger?.LogInformation($"  Processed {offset} records for field '{fieldName}'");
            }
        }
        
        // 3. Переименовываем структуры
        if (result.FailedRecords == 0 || options.Strategy == MigrationStrategy.ContinueOnError)
        {
            await RenameStructureAsync(fieldName, fieldName + "_old", schemeId);
            await RenameStructureAsync(fieldName + "_new", fieldName, schemeId);
            
            if (options.DeleteOldStructures)
            {
                await DeleteStructureAsync(fieldName + "_old", schemeId);
            }
        }
        
        return result;
    }
    
    // Аналогично для других типов миграций...
}
```

---

## 🎯 **ПРИМЕРЫ ИСПОЛЬЗОВАНИЯ**

### **1. Простая миграция с переименованием и сменой типа:**

```csharp
// Создаем конфигурацию миграции
var migrationConfig = new MigrationConfig<Product>()
    .RenameField("Name", "Title")
    .ChangeFieldType<string, decimal>("Price", priceStr => 
    {
        if (decimal.TryParse(priceStr, out var price) && price > 0)
            return price;
        return 0m;
    });

// Применяем миграцию
var result = await redb.SyncSchemeWithMigrationAsync(
    migrationConfig,
    schemeName: "Product",
    options: new MigrationOptions 
    { 
        Strategy = MigrationStrategy.ContinueOnError,
        BatchSize = 5000 
    }
);

Console.WriteLine($"✅ Migration completed: {result.ProcessedRecords} records, {result.FailedRecords} errors");
```

### **2. Сложная миграция с декомпозицией и композицией:**

```csharp
var migrationConfig = new MigrationConfig<Customer>()
    // Декомпозиция FullName
    .SplitField<string>("FullName", new Dictionary<string, Func<string?, object?>>
    {
        ["FirstName"] = fullName => fullName?.Split(' ', 2).FirstOrDefault() ?? "",
        ["LastName"] = fullName => 
        {
            var parts = fullName?.Split(' ', 2);
            return parts?.Length > 1 ? parts[1] : "";
        }
    })
    // Композиция адреса
    .MergeFields<string>(
        sourceFields: new[] { "Street", "City", "State", "ZipCode" },
        targetField: "FullAddress",
        merger: fields => 
        {
            var parts = fields.Select(f => f?.ToString()).Where(s => !string.IsNullOrEmpty(s));
            return string.Join(", ", parts);
        }
    )
    // Конверсия статуса
    .ChangeFieldType<int, string>("StatusCode", status => status switch
    {
        0 => "Active",
        1 => "Inactive", 
        2 => "Pending",
        _ => "Unknown"
    });

var result = await redb.SyncSchemeWithMigrationAsync(migrationConfig, "Customer");
```

### **3. Интеграция с существующим кодом:**

```csharp
// Можно использовать вместо обычного SyncSchemeAsync
public async Task<long> MigrateAndSyncProduct()
{
    // Если нужна миграция
    if (await NeedsMigrationAsync<Product>())
    {
        var migrationConfig = CreateProductMigrationConfig();
        var result = await redb.SyncSchemeWithMigrationAsync(migrationConfig, "Product");
        
        if (!result.Success)
        {
            throw new InvalidOperationException($"Migration failed: {result.ErrorMessage}");
        }
        
        return result.SchemeId;
    }
    
    // Обычная синхронизация без миграции
    return await redb.SyncSchemeAsync<Product>("Product");
}

private MigrationConfig<Product> CreateProductMigrationConfig()
{
    return new MigrationConfig<Product>()
        .RenameField("Name", "Title")
        .ChangeFieldType<string, decimal>("Price", price => 
        {
            // Сложная логика конверсии цены
            if (string.IsNullOrEmpty(price)) return 0m;
            
            // Убираем символы валют
            var cleanPrice = price.Replace("$", "").Replace("€", "").Replace("₽", "").Trim();
            
            if (decimal.TryParse(cleanPrice, out var result))
                return result;
            
            // Пытаемся парсить как число с запятой
            if (decimal.TryParse(cleanPrice.Replace(',', '.'), out result))
                return result;
                
            return 0m;
        });
}
```

### **4. Мониторинг миграции:**

```csharp
var options = new MigrationOptions
{
    Strategy = MigrationStrategy.ContinueOnError,
    BatchSize = 1000,
    ErrorThreshold = 0.05  // 5% ошибок максимум
};

var progress = new Progress<MigrationProgress>(p =>
{
    Console.WriteLine($"Migration progress: {p.CurrentStep} - {p.ProcessedRecords}/{p.TotalRecords} ({p.ProgressPercent:F1}%)");
    
    if (p.ErrorsCount > 0)
    {
        Console.WriteLine($"  Errors: {p.ErrorsCount} ({p.ErrorRate:P})");
    }
});

var result = await redb.SyncSchemeWithMigrationAsync(
    migrationConfig, 
    "Product", 
    options: options,
    progress: progress
);
```

---

## 💡 **ПРЕИМУЩЕСТВА ЭТОГО ПОДХОДА:**

1. **🔄 Интеграция с существующим кодом** - расширяет `SyncStructuresFromTypeAsync`
2. **🎯 Fluent API** - удобная настройка миграций через цепочку методов
3. **⚡ Пакетная обработка** - автоматически для больших данных
4. **🛡️ Безопасность** - транзакционность и откат при ошибках
5. **📊 Мониторинг** - прогресс и детальная статистика
6. **🔧 Гибкость** - любые лямбда-конверторы в методах

**Расширение SyncStructuresFromTypeAsync с лямбда-конверторами - идеальное решение!** 🎯
