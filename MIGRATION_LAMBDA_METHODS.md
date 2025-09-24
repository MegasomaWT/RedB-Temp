# 🔧 МЕТОДЫ МИГРАЦИИ С ЛЯМБДА-КОНВЕРТОРАМИ

## 🎯 **НОВЫЕ МЕТОДЫ В REDBSERVICE**

Вместо лямбд в атрибутах - красивые методы с callback'ами для конверсии.

---

## 📝 **ИНТЕРФЕЙС МЕТОДОВ МИГРАЦИИ**

```csharp
public interface IRedbService
{
    // Переименование поля
    Task<MigrationResult> RenameFieldAsync<TProps>(string oldName, string newName) 
        where TProps : class, new();
    
    // Смена типа с лямбда-конвертором
    Task<MigrationResult> ChangeFieldTypeAsync<TProps, TOld, TNew>(
        string fieldName, 
        Func<TOld?, TNew?> converter,
        MigrationOptions? options = null) 
        where TProps : class, new();
    
    // Декомпозиция: 1 → N полей
    Task<MigrationResult> SplitFieldAsync<TProps, TSource>(
        string sourceField,
        Dictionary<string, Func<TSource?, object?>> targetConverters,
        MigrationOptions? options = null)
        where TProps : class, new();
    
    // Композиция: N → 1 поле
    Task<MigrationResult> MergeFieldsAsync<TProps, TTarget>(
        string[] sourceFields,
        string targetField,
        Func<object?[], TTarget?> merger,
        MigrationOptions? options = null)
        where TProps : class, new();
    
    // Кастомная миграция с полным контролем
    Task<MigrationResult> CustomMigrationAsync<TProps>(
        string migrationName,
        Func<MigrationRecord, Task<object?>> converter,
        MigrationOptions? options = null)
        where TProps : class, new();
}
```

---

## 🚀 **ПРИМЕРЫ ИСПОЛЬЗОВАНИЯ**

### **1. Смена типа поля:**

```csharp
// Было: Price (string) → Стало: Price (decimal)
var result = await redb.ChangeFieldTypeAsync<Product, string, decimal>(
    fieldName: "Price",
    converter: priceString => 
    {
        if (decimal.TryParse(priceString, out var price) && price > 0)
            return price;
        return 0m;
    }
);

Console.WriteLine($"✅ Converted {result.ProcessedRecords} prices, {result.FailedRecords} errors");
```

### **2. Конверсия статуса с бизнес-логикой:**

```csharp
// StatusCode (int) → Status (string)
var result = await redb.ChangeFieldTypeAsync<Order, int, string>(
    fieldName: "StatusCode", 
    converter: statusCode => statusCode switch
    {
        0 => "Pending",
        1 => "Processing", 
        2 => "Shipped",
        3 => "Delivered",
        _ => "Unknown"
    }
);
```

### **3. Декомпозиция FullName:**

```csharp
// FullName → FirstName + LastName
var result = await redb.SplitFieldAsync<Person, string>(
    sourceField: "FullName",
    targetConverters: new Dictionary<string, Func<string?, object?>>
    {
        ["FirstName"] = fullName => fullName?.Split(' ', 2).FirstOrDefault() ?? "",
        ["LastName"] = fullName => 
        {
            var parts = fullName?.Split(' ', 2);
            return parts?.Length > 1 ? parts[1] : "";
        }
    }
);

Console.WriteLine($"✅ Split {result.ProcessedRecords} names");
```

### **4. Композиция адреса:**

```csharp
// Street + City + State + ZipCode → FullAddress
var result = await redb.MergeFieldsAsync<Customer, string>(
    sourceFields: new[] { "Street", "City", "State", "ZipCode" },
    targetField: "FullAddress",
    merger: fields => 
    {
        var street = fields[0]?.ToString() ?? "";
        var city = fields[1]?.ToString() ?? "";
        var state = fields[2]?.ToString() ?? "";
        var zip = fields[3]?.ToString() ?? "";
        
        return $"{street}, {city}, {state} {zip}".Trim(' ', ',');
    }
);
```

### **5. Сложная кастомная миграция:**

```csharp
// Конверсия адреса в координаты с внешним API
var result = await redb.CustomMigrationAsync<Customer>(
    migrationName: "AddressToCoordinates",
    converter: async record =>
    {
        var address = record.GetValue<string>("Address");
        if (string.IsNullOrEmpty(address)) 
            return new GeoLocation();
        
        try
        {
            // Вызов внешнего API
            var coords = await geocodeService.GeocodeAsync(address);
            return new GeoLocation
            {
                Latitude = coords.Lat,
                Longitude = coords.Lng,
                FormattedAddress = coords.FormattedAddress
            };
        }
        catch (Exception ex)
        {
            // Логируем ошибку, но не прерываем миграцию
            logger.LogWarning($"Geocoding failed for '{address}': {ex.Message}");
            return new GeoLocation { FormattedAddress = address };
        }
    },
    options: new MigrationOptions 
    { 
        Strategy = MigrationStrategy.ContinueOnError,
        BatchSize = 100  // Маленькие пакеты для API
    }
);
```

---

## 🛠️ **РЕАЛИЗАЦИЯ В REDBSERVICE**

### **Базовый метод смены типа:**

```csharp
public async Task<MigrationResult> ChangeFieldTypeAsync<TProps, TOld, TNew>(
    string fieldName, 
    Func<TOld?, TNew?> converter,
    MigrationOptions? options = null) 
    where TProps : class, new()
{
    options ??= new MigrationOptions();
    var result = new MigrationResult();
    var schemeId = await GetSchemeIdAsync<TProps>();
    
    // 1. Создаем новую структуру
    var newStructureId = await CreateStructureAsync(fieldName + "_new", typeof(TNew), schemeId);
    
    // 2. Конвертируем данные пакетами
    var offset = 0;
    while (true)
    {
        var batch = await GetFieldValuesBatchAsync<TOld>(schemeId, fieldName, offset, options.BatchSize);
        if (!batch.Any()) break;
        
        var batchResult = await ProcessConversionBatchAsync(batch, converter, newStructureId, options);
        result.Merge(batchResult);
        
        // Проверяем стратегию
        if (ShouldStopMigration(options, batchResult, result))
            break;
        
        offset += options.BatchSize;
        await ReportProgressAsync(result.ProcessedRecords, result.TotalRecords);
    }
    
    // 3. Переименовываем структуры
    if (result.Success || options.Strategy == MigrationStrategy.ContinueOnError)
    {
        await RenameStructureAsync(fieldName, fieldName + "_old", schemeId);
        await RenameStructureAsync(fieldName + "_new", fieldName, schemeId);
        
        if (options.DeleteOldStructure)
            await DeleteStructureAsync(fieldName + "_old", schemeId);
    }
    
    // 4. Записываем в журнал
    await LogMigrationAsync(schemeId, $"ChangeType_{fieldName}", result);
    
    return result;
}

private async Task<BatchResult> ProcessConversionBatchAsync<TOld, TNew>(
    List<FieldValueRecord<TOld>> batch,
    Func<TOld?, TNew?> converter,
    long newStructureId,
    MigrationOptions options)
{
    var batchResult = new BatchResult();
    
    foreach (var record in batch)
    {
        try
        {
            // Применяем лямбда-конвертор
            var convertedValue = converter(record.Value);
            
            // Сохраняем в новую структуру
            await SaveConvertedValueAsync(record.ObjectId, newStructureId, convertedValue);
            
            batchResult.ProcessedRecords++;
        }
        catch (Exception ex)
        {
            batchResult.Errors.Add(new ConversionError
            {
                ObjectId = record.ObjectId,
                FieldName = record.FieldName,
                OriginalValue = record.Value,
                ErrorMessage = ex.Message,
                Type = ConversionErrorType.CustomValidation
            });
            
            batchResult.FailedRecords++;
            
            if (options.Strategy == MigrationStrategy.StopOnError)
                break;
        }
    }
    
    return batchResult;
}
```

### **Метод декомпозиции:**

```csharp
public async Task<MigrationResult> SplitFieldAsync<TProps, TSource>(
    string sourceField,
    Dictionary<string, Func<TSource?, object?>> targetConverters,
    MigrationOptions? options = null)
    where TProps : class, new()
{
    options ??= new MigrationOptions();
    var result = new MigrationResult();
    var schemeId = await GetSchemeIdAsync<TProps>();
    
    // 1. Создаем новые структуры для каждого целевого поля
    var targetStructures = new Dictionary<string, long>();
    foreach (var targetField in targetConverters.Keys)
    {
        var structureId = await CreateStructureAsync(targetField, typeof(object), schemeId);
        targetStructures[targetField] = structureId;
    }
    
    // 2. Обрабатываем данные пакетами
    var offset = 0;
    while (true)
    {
        var batch = await GetFieldValuesBatchAsync<TSource>(schemeId, sourceField, offset, options.BatchSize);
        if (!batch.Any()) break;
        
        foreach (var record in batch)
        {
            try
            {
                // Применяем все конверторы для разделения
                foreach (var (targetField, converter) in targetConverters)
                {
                    var convertedValue = converter(record.Value);
                    var structureId = targetStructures[targetField];
                    
                    await SaveConvertedValueAsync(record.ObjectId, structureId, convertedValue);
                }
                
                result.ProcessedRecords++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ConversionError
                {
                    ObjectId = record.ObjectId,
                    FieldName = sourceField,
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
    }
    
    // 3. Удаляем исходное поле
    if (result.Success || options.Strategy == MigrationStrategy.ContinueOnError)
    {
        await DeleteStructureAsync(sourceField, schemeId);
    }
    
    await LogMigrationAsync(schemeId, $"Split_{sourceField}", result);
    return result;
}
```

### **Метод композиции:**

```csharp
public async Task<MigrationResult> MergeFieldsAsync<TProps, TTarget>(
    string[] sourceFields,
    string targetField,
    Func<object?[], TTarget?> merger,
    MigrationOptions? options = null)
    where TProps : class, new()
{
    options ??= new MigrationOptions();
    var result = new MigrationResult();
    var schemeId = await GetSchemeIdAsync<TProps>();
    
    // 1. Создаем новую структуру для целевого поля
    var targetStructureId = await CreateStructureAsync(targetField, typeof(TTarget), schemeId);
    
    // 2. Получаем данные всех исходных полей пакетами
    var offset = 0;
    while (true)
    {
        var batch = await GetMultiFieldValuesBatchAsync(schemeId, sourceFields, offset, options.BatchSize);
        if (!batch.Any()) break;
        
        foreach (var record in batch)
        {
            try
            {
                // Собираем значения всех исходных полей
                var sourceValues = sourceFields.Select(field => record.GetValue(field)).ToArray();
                
                // Применяем лямбда-мерджер
                var mergedValue = merger(sourceValues);
                
                // Сохраняем результат
                await SaveConvertedValueAsync(record.ObjectId, targetStructureId, mergedValue);
                
                result.ProcessedRecords++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ConversionError
                {
                    ObjectId = record.ObjectId,
                    FieldName = string.Join("+", sourceFields),
                    OriginalValue = string.Join(", ", sourceFields.Select(f => record.GetValue(f))),
                    ErrorMessage = ex.Message,
                    Type = ConversionErrorType.CustomValidation
                });
                
                result.FailedRecords++;
                
                if (options.Strategy == MigrationStrategy.StopOnError)
                    break;
            }
        }
        
        offset += options.BatchSize;
    }
    
    // 3. Удаляем исходные поля
    if (result.Success || options.Strategy == MigrationStrategy.ContinueOnError)
    {
        foreach (var sourceField in sourceFields)
        {
            await DeleteStructureAsync(sourceField, schemeId);
        }
    }
    
    await LogMigrationAsync(schemeId, $"Merge_{string.Join("_", sourceFields)}_to_{targetField}", result);
    return result;
}
```

---

## 🎯 **ПОЛНЫЙ ПРИМЕР МИГРАЦИИ**

```csharp
public async Task MigrateProductCatalog()
{
    Console.WriteLine("🚀 Starting product catalog migration...");
    
    // 1. Переименование
    await redb.RenameFieldAsync<Product>("Name", "Title");
    
    // 2. Смена типа с валидацией
    var priceResult = await redb.ChangeFieldTypeAsync<Product, string, decimal>(
        fieldName: "Price",
        converter: priceStr => 
        {
            if (decimal.TryParse(priceStr, out var price) && price >= 0)
                return price;
            
            // Логируем проблемные значения
            Console.WriteLine($"⚠️ Invalid price: '{priceStr}', setting to 0");
            return 0m;
        },
        options: new MigrationOptions 
        { 
            Strategy = MigrationStrategy.ContinueOnError,
            BatchSize = 5000 
        }
    );
    
    // 3. Конверсия категорий
    await redb.ChangeFieldTypeAsync<Product, int, string>(
        fieldName: "CategoryId",
        converter: categoryId => categoryId switch
        {
            1 => "Electronics",
            2 => "Clothing", 
            3 => "Books",
            4 => "Home & Garden",
            _ => "Other"
        }
    );
    
    // 4. Декомпозиция размеров
    await redb.SplitFieldAsync<Product, string>(
        sourceField: "Dimensions",
        targetConverters: new Dictionary<string, Func<string?, object?>>
        {
            ["Width"] = dims => ParseDimension(dims, 0),
            ["Height"] = dims => ParseDimension(dims, 1),
            ["Depth"] = dims => ParseDimension(dims, 2)
        }
    );
    
    Console.WriteLine($"✅ Migration completed!");
    Console.WriteLine($"   Price conversion: {priceResult.ProcessedRecords} records, {priceResult.FailedRecords} errors");
}

private static decimal ParseDimension(string? dimensions, int index)
{
    if (string.IsNullOrEmpty(dimensions)) return 0;
    
    var parts = dimensions.Split('x', StringSplitOptions.RemoveEmptyEntries);
    if (index < parts.Length && decimal.TryParse(parts[index].Trim(), out var value))
        return value;
    
    return 0;
}
```

**Лямбда-конверторы в методах гораздо элегантнее атрибутов!** 🎯
