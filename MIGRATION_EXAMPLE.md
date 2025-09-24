# 🔄 ПРИМЕР МИГРАЦИИ: СМЕНА ИМЕНИ И ТИПА ПОЛЯ

## 📝 **СЦЕНАРИЙ МИГРАЦИИ**

### **Было (версия 1.0):**
```csharp
public class Product
{
    public string Name { get; set; } = "";
    public string Price { get; set; } = "";  // Цена как строка
    public string Category { get; set; } = "";
}
```

### **Стало (версия 1.1):**
```csharp
[SchemeVersion("1.1")]
public class Product
{
    [MigratedFrom("Name", version: "1.0")]
    public string Title { get; set; } = "";  // Name → Title
    
    [MigratedFrom("Price", version: "1.0", oldType: typeof(string), converter: "StringToDecimal")]
    public decimal Cost { get; set; }  // Price (string) → Cost (decimal)
    
    public string Category { get; set; } = "";  // Без изменений
}
```

---

## 🎯 **КАК БУДЕТ РАБОТАТЬ МИГРАЦИЯ**

### **1. Автоматическое обнаружение изменений:**

```csharp
// При вызове SyncSchemeAsync система автоматически обнаружит изменения
var schemeId = await redb.SyncSchemeAsync<Product>();

// Внутри система выполнит:
// 1. Анализ атрибутов [MigratedFrom]
// 2. Сравнение с текущей структурой в БД
// 3. Создание плана миграции
// 4. Выполнение миграции данных
```

### **2. Детальный план миграции:**

```csharp
// Система создаст следующий план:
var migrationPlan = new MigrationPlan
{
    SchemeId = 1001,
    FromVersion = "1.0",
    ToVersion = "1.1",
    Changes = new[]
    {
        new FieldMigration
        {
            Type = MigrationType.Rename,
            OldName = "Name",
            NewName = "Title",
            FieldType = typeof(string)
        },
        new FieldMigration
        {
            Type = MigrationType.RenameAndConvert,
            OldName = "Price",
            NewName = "Cost",
            OldType = typeof(string),
            NewType = typeof(decimal),
            Converter = "StringToDecimal"
        }
    }
};
```

---

## 🛠️ **ПОШАГОВОЕ ВЫПОЛНЕНИЕ МИГРАЦИИ**

### **Шаг 1: Переименование поля Name → Title**

```sql
-- 1.1. Создаем новую структуру
INSERT INTO _structures (_id, _id_scheme, _name, _id_type, _order)
VALUES (nextval('global_identity'), 1001, 'Title', -9223372036854775701, 1);

-- 1.2. Копируем данные
INSERT INTO _values (_id, _id_object, _id_structure, _String)
SELECT nextval('global_identity'), _id_object, 
       (SELECT _id FROM _structures WHERE _name = 'Title' AND _id_scheme = 1001),
       _String
FROM _values v
INNER JOIN _structures s ON s._id = v._id_structure
WHERE s._name = 'Name' AND s._id_scheme = 1001;

-- 1.3. Удаляем старую структуру и данные
DELETE FROM _values WHERE _id_structure IN 
    (SELECT _id FROM _structures WHERE _name = 'Name' AND _id_scheme = 1001);
DELETE FROM _structures WHERE _name = 'Name' AND _id_scheme = 1001;
```

### **Шаг 2: Конверсия Price (string) → Cost (decimal)**

```sql
-- 2.1. Создаем новую структуру для decimal
INSERT INTO _structures (_id, _id_scheme, _name, _id_type, _order)
VALUES (nextval('global_identity'), 1001, 'Cost', -9223372036854775707, 2);

-- 2.2. Конвертируем данные через C# код
-- (выполняется пакетно по 1000 записей)
```

```csharp
// C# код конверсии (выполняется пакетами)
public async Task ConvertPriceToDecimal(long schemeId, int batchSize = 1000)
{
    var offset = 0;
    var errors = new List<ConversionError>();
    
    while (true)
    {
        // Получаем пакет строковых цен
        var batch = await _context.Database.SqlQueryRaw<PriceRecord>(@"
            SELECT v._id as ValueId, v._id_object as ObjectId, v._String as PriceString
            FROM _values v
            INNER JOIN _structures s ON s._id = v._id_structure
            WHERE s._name = 'Price' AND s._id_scheme = {0}
            ORDER BY v._id
            OFFSET {1} LIMIT {2}", 
            schemeId, offset, batchSize).ToListAsync();
            
        if (!batch.Any()) break;
        
        foreach (var record in batch)
        {
            try
            {
                // Конвертируем строку в decimal
                if (decimal.TryParse(record.PriceString, out var decimalPrice))
                {
                    // Вставляем в новую структуру Cost
                    await _context.Database.ExecuteSqlRawAsync(@"
                        INSERT INTO _values (_id, _id_object, _id_structure, _Double)
                        VALUES (nextval('global_identity'), {0}, 
                               (SELECT _id FROM _structures WHERE _name = 'Cost' AND _id_scheme = {1}),
                               {2})",
                        record.ObjectId, schemeId, decimalPrice);
                }
                else
                {
                    // Записываем ошибку конверсии
                    errors.Add(new ConversionError
                    {
                        ObjectId = record.ObjectId,
                        FieldName = "Price",
                        OriginalValue = record.PriceString,
                        ErrorMessage = $"Cannot convert '{record.PriceString}' to decimal",
                        Type = ConversionErrorType.InvalidFormat
                    });
                }
            }
            catch (Exception ex)
            {
                errors.Add(new ConversionError
                {
                    ObjectId = record.ObjectId,
                    FieldName = "Price", 
                    OriginalValue = record.PriceString,
                    ErrorMessage = ex.Message,
                    Type = ConversionErrorType.CustomValidation
                });
            }
        }
        
        offset += batchSize;
        
        // Отчет о прогрессе
        Console.WriteLine($"Processed {offset} records, {errors.Count} errors");
    }
    
    // Удаляем старую структуру Price
    await _context.Database.ExecuteSqlRawAsync(@"
        DELETE FROM _values WHERE _id_structure IN 
            (SELECT _id FROM _structures WHERE _name = 'Price' AND _id_scheme = {0});
        DELETE FROM _structures WHERE _name = 'Price' AND _id_scheme = {0};",
        schemeId);
}
```

---

## 📊 **ЖУРНАЛ МИГРАЦИИ**

### **Запись в таблице `_migrations`:**

```sql
INSERT INTO _migrations (
    _id, _id_scheme, _migration_name, _from_version, _to_version,
    _date_applied, _applied_by, _success, _processed_records, _failed_records,
    _execution_time_ms, _batch_size, _migration_details
) VALUES (
    nextval('global_identity'),
    1001,
    'ProductV1_0_to_V1_1',
    '1.0',
    '1.1', 
    NOW(),
    0, -- sys user
    true,
    15000, -- обработано записей
    23,    -- ошибок
    45000, -- 45 секунд
    1000,  -- размер пакета
    '{
        "changes": [
            {
                "field": "Name",
                "operation": "rename",
                "old_name": "Name",
                "new_name": "Title",
                "type": "String",
                "records_affected": 15000
            },
            {
                "field": "Price", 
                "operation": "rename_and_convert",
                "old_name": "Price",
                "new_name": "Cost",
                "old_type": "String",
                "new_type": "Decimal",
                "converter": "StringToDecimal",
                "records_affected": 15000,
                "conversion_errors": 23,
                "sample_errors": [
                    "Object 1021: Cannot convert ''abc'' to decimal",
                    "Object 1055: Cannot convert ''N/A'' to decimal",
                    "Object 1089: Cannot convert ''free'' to decimal"
                ]
            }
        ],
        "strategy": "continue_on_error",
        "backup_created": true
    }'::jsonb
);
```

---

## 🎯 **ВЫЗОВ МИГРАЦИИ**

### **Простой вызов (автоматическая миграция):**

```csharp
// Самый простой способ - система сама определит что нужно мигрировать
var schemeId = await redb.SyncSchemeAsync<Product>();
Console.WriteLine($"Product scheme synced: {schemeId}");
```

### **Вызов с настройками для больших данных:**

```csharp
var options = new MigrationOptions
{
    Strategy = MigrationStrategy.ContinueOnError,  // Продолжать при ошибках
    BatchSize = 5000,                              // Большие пакеты
    ErrorThreshold = 0.02,                         // 2% ошибок максимум
    CreateBackup = true,                           // Создать бэкап
    Timeout = TimeSpan.FromHours(1)                // Таймаут 1 час
};

var result = await redb.SyncSchemeAsync<Product>(options: options);

if (result.Success)
{
    Console.WriteLine($"✅ Migration completed successfully!");
    Console.WriteLine($"   Processed: {result.ProcessedRecords} records");
    Console.WriteLine($"   Errors: {result.FailedRecords} records");
    Console.WriteLine($"   Time: {result.ExecutionTime.TotalSeconds:F1} seconds");
}
else
{
    Console.WriteLine($"❌ Migration failed: {result.ErrorMessage}");
    Console.WriteLine($"   Errors ({result.Errors.Count}):");
    
    foreach (var error in result.Errors.Take(10))
    {
        Console.WriteLine($"     Object {error.ObjectId}: {error.ErrorMessage}");
    }
}
```

### **Предварительная проверка (dry-run):**

```csharp
// Проверяем что будет мигрировано без выполнения
var migrationPlan = await redb.GetMigrationPlanAsync<Product>();

Console.WriteLine($"📋 Migration plan for Product (v{migrationPlan.FromVersion} → v{migrationPlan.ToVersion}):");
foreach (var change in migrationPlan.Changes)
{
    Console.WriteLine($"  - {change.Type}: {change.OldName} → {change.NewName} ({change.OldType?.Name} → {change.NewType.Name})");
}

Console.WriteLine($"📊 Estimated records to process: {migrationPlan.EstimatedRecords}");
Console.WriteLine($"⏱️ Estimated time: {migrationPlan.EstimatedDuration.TotalMinutes:F1} minutes");

// Подтверждение
Console.Write("Apply migration? (y/N): ");
if (Console.ReadLine()?.ToLower() == "y")
{
    var result = await redb.ApplyMigrationPlanAsync(migrationPlan, options);
}
```

---

## 🎉 **РЕЗУЛЬТАТ МИГРАЦИИ**

### **До миграции (в БД):**
```
_structures:
  - Name: "Name" (String)
  - Name: "Price" (String) 
  - Name: "Category" (String)

_values:
  - Object 1021: Name="Laptop", Price="1500.50", Category="Electronics"
  - Object 1022: Name="Mouse", Price="25.99", Category="Electronics"
  - Object 1023: Name="Book", Price="abc", Category="Books"  // ошибка конверсии
```

### **После миграции (в БД):**
```
_structures:
  - Name: "Title" (String)     // было Name
  - Name: "Cost" (Decimal)     // было Price (String)
  - Name: "Category" (String)  // без изменений

_values:
  - Object 1021: Title="Laptop", Cost=1500.50, Category="Electronics"
  - Object 1022: Title="Mouse", Cost=25.99, Category="Electronics"  
  - Object 1023: Title="Book", Cost=NULL, Category="Books"  // ошибка конверсии → NULL

_migrations:
  - ProductV1_0_to_V1_1: processed=3, failed=1, errors=["Object 1023: Cannot convert 'abc' to decimal"]
```

### **В C# коде:**
```csharp
// Теперь можно использовать новые поля
var products = await redb.QueryAsync<Product>();
foreach (var product in products)
{
    Console.WriteLine($"{product.Title}: ${product.Cost:F2}");  // Title вместо Name, Cost вместо Price
}
```

**Миграция выполнена автоматически с сохранением всех данных!** 🎯
