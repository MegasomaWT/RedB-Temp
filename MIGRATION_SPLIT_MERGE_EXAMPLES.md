# 🔄 ПРИМЕРЫ ДЕКОМПОЗИЦИИ И КОМПОЗИЦИИ ПОЛЕЙ

## 1️⃣ **ДЕКОМПОЗИЦИЯ: ОДНО ПОЛЕ → НЕСКОЛЬКО ПОЛЕЙ**

### 📝 **Сценарий: Разделение FullName на FirstName + LastName**

#### **Было (v1.0):**
```csharp
public class Person
{
    public string FullName { get; set; } = "";  // "John Doe"
    public string Email { get; set; } = "";
    public int Age { get; set; }
}
```

#### **Стало (v1.1):**
```csharp
[SchemeVersion("1.1")]
public class Person
{
    [SplitFrom("FullName", version: "1.0", separator: " ", maxParts: 2)]
    public string FirstName { get; set; } = "";  // "John"
    
    [SplitFrom("FullName", version: "1.0", separator: " ", maxParts: 2, partIndex: 1)]
    public string LastName { get; set; } = "";   // "Doe"
    
    public string Email { get; set; } = "";      // без изменений
    public int Age { get; set; }                 // без изменений
}
```

### 🛠️ **Реализация декомпозиции:**

```csharp
public class SplitFromAttribute : Attribute
{
    public string SourceField { get; }
    public string Version { get; }
    public string Separator { get; set; } = " ";
    public int MaxParts { get; set; } = 2;
    public int PartIndex { get; set; } = 0;  // Какую часть брать (0 = первая, 1 = вторая)
    
    public SplitFromAttribute(string sourceField, string version)
    {
        SourceField = sourceField;
        Version = version;
    }
}

// Конвертор для разделения
public class FieldSplitter
{
    public static object?[] SplitField(object? value, string separator = " ", int maxParts = 2)
    {
        if (value == null) return new object?[maxParts];
        
        var parts = value.ToString()?.Split(separator, maxParts) ?? new string[0];
        var result = new object?[maxParts];
        
        for (int i = 0; i < maxParts; i++)
        {
            result[i] = i < parts.Length ? parts[i] : "";
        }
        
        return result;
    }
}
```

### 🚀 **Выполнение декомпозиции:**

```csharp
public async Task DecomposeFullNameField(long schemeId, int batchSize = 1000)
{
    var offset = 0;
    var errors = new List<ConversionError>();
    
    // 1. Создаем новые структуры
    await CreateStructureAsync("FirstName", typeof(string), schemeId);
    await CreateStructureAsync("LastName", typeof(string), schemeId);
    
    while (true)
    {
        // 2. Получаем пакет записей FullName
        var batch = await _context.Database.SqlQueryRaw<FullNameRecord>(@"
            SELECT v._id as ValueId, v._id_object as ObjectId, v._String as FullNameValue
            FROM _values v
            INNER JOIN _structures s ON s._id = v._id_structure
            WHERE s._name = 'FullName' AND s._id_scheme = {0}
            ORDER BY v._id
            OFFSET {1} LIMIT {2}", 
            schemeId, offset, batchSize).ToListAsync();
            
        if (!batch.Any()) break;
        
        foreach (var record in batch)
        {
            try
            {
                // 3. Разделяем FullName на части
                var parts = FieldSplitter.SplitField(record.FullNameValue, " ", 2);
                var firstName = parts[0]?.ToString() ?? "";
                var lastName = parts[1]?.ToString() ?? "";
                
                // 4. Вставляем FirstName
                await _context.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO _values (_id, _id_object, _id_structure, _String)
                    VALUES (nextval('global_identity'), {0}, 
                           (SELECT _id FROM _structures WHERE _name = 'FirstName' AND _id_scheme = {1}),
                           {2})",
                    record.ObjectId, schemeId, firstName);
                
                // 5. Вставляем LastName
                await _context.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO _values (_id, _id_object, _id_structure, _String)
                    VALUES (nextval('global_identity'), {0}, 
                           (SELECT _id FROM _structures WHERE _name = 'LastName' AND _id_scheme = {1}),
                           {2})",
                    record.ObjectId, schemeId, lastName);
            }
            catch (Exception ex)
            {
                errors.Add(new ConversionError
                {
                    ObjectId = record.ObjectId,
                    FieldName = "FullName",
                    OriginalValue = record.FullNameValue,
                    ErrorMessage = ex.Message,
                    Type = ConversionErrorType.CustomValidation
                });
            }
        }
        
        offset += batchSize;
        Console.WriteLine($"Decomposed {offset} records, {errors.Count} errors");
    }
    
    // 6. Удаляем старое поле FullName
    await DeleteStructureAsync("FullName", schemeId);
}
```

### 📊 **Результат декомпозиции:**

**До:**
```
Object 1001: FullName="John Doe"
Object 1002: FullName="Jane Smith"  
Object 1003: FullName="Bob"         // только имя
Object 1004: FullName=""            // пустое
```

**После:**
```
Object 1001: FirstName="John", LastName="Doe"
Object 1002: FirstName="Jane", LastName="Smith"
Object 1003: FirstName="Bob", LastName=""
Object 1004: FirstName="", LastName=""
```

---

## 2️⃣ **КОМПОЗИЦИЯ: НЕСКОЛЬКО ПОЛЕЙ → ОДНО ПОЛЕ**

### 📝 **Сценарий: Объединение Address полей в FullAddress**

#### **Было (v1.0):**
```csharp
public class Customer
{
    public string Name { get; set; } = "";
    public string Street { get; set; } = "";     // "123 Main St"
    public string City { get; set; } = "";       // "New York"
    public string State { get; set; } = "";      // "NY"
    public string ZipCode { get; set; } = "";    // "10001"
}
```

#### **Стало (v1.1):**
```csharp
[SchemeVersion("1.1")]
public class Customer
{
    public string Name { get; set; } = "";
    
    [MergedFrom(new[] { "Street", "City", "State", "ZipCode" }, version: "1.0", 
                template: "{0}, {1}, {2} {3}")]
    public string FullAddress { get; set; } = "";  // "123 Main St, New York, NY 10001"
}
```

### 🛠️ **Реализация композиции:**

```csharp
public class MergedFromAttribute : Attribute
{
    public string[] SourceFields { get; }
    public string Version { get; }
    public string Template { get; set; } = "{0}";  // Шаблон объединения
    public string Separator { get; set; } = " ";   // Простой разделитель (если не template)
    
    public MergedFromAttribute(string[] sourceFields, string version)
    {
        SourceFields = sourceFields;
        Version = version;
    }
}

// Конвертор для объединения
public class FieldMerger
{
    public static object? MergeFields(object?[] values, string template = "{0}", string separator = " ")
    {
        if (values == null || values.Length == 0) return "";
        
        // Если есть шаблон - используем его
        if (template.Contains("{"))
        {
            try
            {
                var stringValues = values.Select(v => v?.ToString() ?? "").ToArray();
                return string.Format(template, stringValues);
            }
            catch
            {
                // Fallback к простому объединению
            }
        }
        
        // Простое объединение через разделитель
        return string.Join(separator, values.Where(v => !string.IsNullOrEmpty(v?.ToString())));
    }
}
```

### 🚀 **Выполнение композиции:**

```csharp
public async Task ComposeAddressFields(long schemeId, int batchSize = 1000)
{
    var offset = 0;
    var errors = new List<ConversionError>();
    
    // 1. Создаем новую структуру
    await CreateStructureAsync("FullAddress", typeof(string), schemeId);
    
    while (true)
    {
        // 2. Получаем пакет записей со всеми адресными полями
        var batch = await _context.Database.SqlQueryRaw<AddressRecord>(@"
            SELECT DISTINCT o._id as ObjectId,
                   MAX(CASE WHEN s._name = 'Street' THEN v._String END) as Street,
                   MAX(CASE WHEN s._name = 'City' THEN v._String END) as City,
                   MAX(CASE WHEN s._name = 'State' THEN v._String END) as State,
                   MAX(CASE WHEN s._name = 'ZipCode' THEN v._String END) as ZipCode
            FROM _objects o
            LEFT JOIN _values v ON v._id_object = o._id
            LEFT JOIN _structures s ON s._id = v._id_structure
            WHERE o._id_scheme = {0} 
              AND s._name IN ('Street', 'City', 'State', 'ZipCode')
            GROUP BY o._id
            ORDER BY o._id
            OFFSET {1} LIMIT {2}", 
            schemeId, offset, batchSize).ToListAsync();
            
        if (!batch.Any()) break;
        
        foreach (var record in batch)
        {
            try
            {
                // 3. Объединяем поля в FullAddress
                var addressParts = new object?[] { 
                    record.Street, 
                    record.City, 
                    record.State, 
                    record.ZipCode 
                };
                
                var fullAddress = FieldMerger.MergeFields(addressParts, "{0}, {1}, {2} {3}");
                
                // 4. Вставляем FullAddress
                await _context.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO _values (_id, _id_object, _id_structure, _String)
                    VALUES (nextval('global_identity'), {0}, 
                           (SELECT _id FROM _structures WHERE _name = 'FullAddress' AND _id_scheme = {1}),
                           {2})",
                    record.ObjectId, schemeId, fullAddress);
            }
            catch (Exception ex)
            {
                errors.Add(new ConversionError
                {
                    ObjectId = record.ObjectId,
                    FieldName = "Address",
                    OriginalValue = $"{record.Street}, {record.City}, {record.State}, {record.ZipCode}",
                    ErrorMessage = ex.Message,
                    Type = ConversionErrorType.CustomValidation
                });
            }
        }
        
        offset += batchSize;
        Console.WriteLine($"Composed {offset} records, {errors.Count} errors");
    }
    
    // 5. Удаляем старые поля
    await DeleteStructureAsync("Street", schemeId);
    await DeleteStructureAsync("City", schemeId);
    await DeleteStructureAsync("State", schemeId);
    await DeleteStructureAsync("ZipCode", schemeId);
}
```

### 📊 **Результат композиции:**

**До:**
```
Object 1001: Street="123 Main St", City="New York", State="NY", ZipCode="10001"
Object 1002: Street="456 Oak Ave", City="Los Angeles", State="CA", ZipCode=""
Object 1003: Street="", City="Chicago", State="IL", ZipCode="60601"
```

**После:**
```
Object 1001: FullAddress="123 Main St, New York, NY 10001"
Object 1002: FullAddress="456 Oak Ave, Los Angeles, CA "
Object 1003: FullAddress=", Chicago, IL 60601"
```

---

## 🎯 **ВЫЗОВ МИГРАЦИЙ**

### **Декомпозиция:**
```csharp
// Автоматическая декомпозиция при синхронизации
var result = await redb.SyncSchemeAsync<Person>();

Console.WriteLine($"✅ FullName decomposed into FirstName + LastName");
Console.WriteLine($"   Processed: {result.ProcessedRecords} records");
Console.WriteLine($"   Errors: {result.FailedRecords} records");
```

### **Композиция:**
```csharp
// Автоматическая композиция при синхронизации
var result = await redb.SyncSchemeAsync<Customer>();

Console.WriteLine($"✅ Address fields merged into FullAddress");
Console.WriteLine($"   Processed: {result.ProcessedRecords} records");
Console.WriteLine($"   Errors: {result.FailedRecords} records");
```

### **С настройками:**
```csharp
var options = new MigrationOptions
{
    Strategy = MigrationStrategy.ContinueOnError,
    BatchSize = 2000,
    ErrorThreshold = 0.05  // 5% ошибок максимум
};

var result = await redb.SyncSchemeAsync<Person>(options: options);
```

---

## 📊 **ЖУРНАЛ МИГРАЦИЙ**

### **Декомпозиция в журнале:**
```json
{
  "migration_name": "PersonV1_0_to_V1_1_Decompose",
  "changes": [
    {
      "field": "FullName",
      "operation": "split",
      "target_fields": ["FirstName", "LastName"],
      "separator": " ",
      "max_parts": 2,
      "records_affected": 10000,
      "split_errors": 15,
      "sample_errors": [
        "Object 1055: Empty FullName cannot be split",
        "Object 1089: FullName 'Dr. John Smith Jr.' has too many parts"
      ]
    }
  ]
}
```

### **Композиция в журнале:**
```json
{
  "migration_name": "CustomerV1_0_to_V1_1_Compose", 
  "changes": [
    {
      "field": "FullAddress",
      "operation": "merge",
      "source_fields": ["Street", "City", "State", "ZipCode"],
      "template": "{0}, {1}, {2} {3}",
      "records_affected": 8000,
      "merge_warnings": 23,
      "sample_warnings": [
        "Object 1021: Missing City field",
        "Object 1034: Empty ZipCode field"
      ]
    }
  ]
}
```

---

## 💡 **ДОПОЛНИТЕЛЬНЫЕ ВОЗМОЖНОСТИ**

### **Сложная декомпозиция с regex:**
```csharp
[SplitFrom("PhoneNumber", version: "1.0", pattern: @"(\d{3})-(\d{3})-(\d{4})")]
public string AreaCode { get; set; } = "";  // группа 1

[SplitFrom("PhoneNumber", version: "1.0", pattern: @"(\d{3})-(\d{3})-(\d{4})", groupIndex: 2)]
public string Exchange { get; set; } = "";  // группа 2

[SplitFrom("PhoneNumber", version: "1.0", pattern: @"(\d{3})-(\d{3})-(\d{4})", groupIndex: 3)]
public string Number { get; set; } = "";    // группа 3
```

### **Условная композиция:**
```csharp
[MergedFrom(new[] { "FirstName", "MiddleName", "LastName" }, version: "1.0",
            template: "{0} {1} {2}", 
            condition: "MiddleName IS NOT NULL")]
public string FullNameWithMiddle { get; set; } = "";
```

**Декомпозиция и композиция выполняются автоматически и безопасно!** 🎯
