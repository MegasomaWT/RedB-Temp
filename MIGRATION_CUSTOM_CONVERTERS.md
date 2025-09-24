# 🔧 КАСТОМНЫЕ КОНВЕРТОРЫ В МИГРАЦИЯХ

## 🎯 **ТИПЫ КАСТОМНЫХ КОНВЕРТОРОВ**

### 1. **ЛЯМБДА КОНВЕРТОРЫ (ПРОСТЫЕ)**
### 2. **КЛАСС КОНВЕРТОРЫ (СЛОЖНЫЕ)**
### 3. **СТАТИЧЕСКИЕ МЕТОДЫ**
### 4. **INLINE КОНВЕРТОРЫ**

---

## 1️⃣ **ЛЯМБДА КОНВЕРТОРЫ**

### **Пример: Конверсия статуса заказа**

```csharp
[SchemeVersion("1.1")]
public class Order
{
    public string OrderNumber { get; set; } = "";
    
    // Было: StatusCode (int) → Стало: Status (string)
    [MigratedFrom("StatusCode", version: "1.0", oldType: typeof(int), 
                  converter: "status => status switch { 0 => \"Pending\", 1 => \"Processing\", 2 => \"Shipped\", 3 => \"Delivered\", _ => \"Unknown\" }")]
    public string Status { get; set; } = "";
    
    // Было: PriceString → Стало: Price с валидацией
    [MigratedFrom("PriceString", version: "1.0", oldType: typeof(string),
                  converter: "price => decimal.TryParse(price?.ToString(), out var p) && p > 0 ? p : 0")]
    public decimal Price { get; set; }
}
```

### **Реализация лямбда конверторов:**

```csharp
public class LambdaConverter : IDataConverter
{
    private readonly string _lambdaExpression;
    private readonly Func<object?, object?> _compiledLambda;
    
    public LambdaConverter(string lambdaExpression)
    {
        _lambdaExpression = lambdaExpression;
        _compiledLambda = CompileLambda(lambdaExpression);
    }
    
    private Func<object?, object?> CompileLambda(string expression)
    {
        // Простой парсер для базовых лямбд
        if (expression.Contains("switch"))
        {
            return CompileSwitchExpression(expression);
        }
        
        if (expression.Contains("TryParse"))
        {
            return CompileTryParseExpression(expression);
        }
        
        // Fallback к динамической компиляции
        return CompileDynamicExpression(expression);
    }
    
    private Func<object?, object?> CompileSwitchExpression(string expression)
    {
        // "status => status switch { 0 => \"Pending\", 1 => \"Processing\", ... }"
        return value =>
        {
            if (value is int intValue)
            {
                return intValue switch
                {
                    0 => "Pending",
                    1 => "Processing", 
                    2 => "Shipped",
                    3 => "Delivered",
                    _ => "Unknown"
                };
            }
            return "Unknown";
        };
    }
    
    private Func<object?, object?> CompileTryParseExpression(string expression)
    {
        // "price => decimal.TryParse(price?.ToString(), out var p) && p > 0 ? p : 0"
        return value =>
        {
            if (decimal.TryParse(value?.ToString(), out var parsed) && parsed > 0)
                return parsed;
            return 0m;
        };
    }
    
    public async Task<object?> ConvertAsync(object? value, Type fromType, Type toType)
    {
        try
        {
            return _compiledLambda(value);
        }
        catch (Exception ex)
        {
            throw new ConversionException($"Lambda conversion failed: {ex.Message}");
        }
    }
}
```

---

## 2️⃣ **КЛАСС КОНВЕРТОРЫ (СЛОЖНЫЕ)**

### **Пример: Сложная конверсия адреса с геокодированием**

```csharp
[SchemeVersion("1.2")]
public class Customer
{
    public string Name { get; set; } = "";
    
    // Сложная конверсия с внешним API
    [MigratedFrom("Address", version: "1.1", oldType: typeof(string), 
                  converter: typeof(AddressToCoordinatesConverter))]
    public GeoLocation Location { get; set; } = new();
    
    // Конверсия с бизнес-логикой
    [MigratedFrom("BirthDate", version: "1.1", oldType: typeof(DateTime),
                  converter: typeof(AgeGroupConverter))]
    public string AgeGroup { get; set; } = "";
}

public class GeoLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string FormattedAddress { get; set; } = "";
}
```

### **Реализация сложных конверторов:**

```csharp
public class AddressToCoordinatesConverter : IDataConverter
{
    private readonly IGeocodeService _geocodeService;
    private readonly ILogger<AddressToCoordinatesConverter> _logger;
    
    public AddressToCoordinatesConverter(IGeocodeService geocodeService, ILogger<AddressToCoordinatesConverter> logger)
    {
        _geocodeService = geocodeService;
        _logger = logger;
    }
    
    public bool CanConvert(Type fromType, Type toType) 
        => fromType == typeof(string) && toType == typeof(GeoLocation);
    
    public async Task<object?> ConvertAsync(object? value, Type fromType, Type toType)
    {
        if (value == null || string.IsNullOrEmpty(value.ToString()))
        {
            return new GeoLocation();
        }
        
        var address = value.ToString()!;
        
        try
        {
            // Кешируем результаты геокодирования
            var cached = await _geocodeService.GetCachedCoordinatesAsync(address);
            if (cached != null) return cached;
            
            // Вызываем внешний API (с retry и rate limiting)
            var coordinates = await _geocodeService.GeocodeAsync(address);
            
            var result = new GeoLocation
            {
                Latitude = coordinates.Lat,
                Longitude = coordinates.Lng,
                FormattedAddress = coordinates.FormattedAddress
            };
            
            // Кешируем результат
            await _geocodeService.CacheCoordinatesAsync(address, result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Geocoding failed for address '{address}': {ex.Message}");
            
            // Возвращаем пустую локацию вместо ошибки
            return new GeoLocation { FormattedAddress = address };
        }
    }
    
    public async Task<List<ConversionError>> ValidateAsync(IEnumerable<object?> values, Type fromType, Type toType)
    {
        var errors = new List<ConversionError>();
        var addresses = values.Where(v => !string.IsNullOrEmpty(v?.ToString())).ToList();
        
        // Проверяем лимиты API
        if (addresses.Count > 1000)
        {
            errors.Add(new ConversionError
            {
                ErrorMessage = $"Too many addresses to geocode: {addresses.Count}. API limit is 1000 per day.",
                Type = ConversionErrorType.CustomValidation
            });
        }
        
        return errors;
    }
}

public class AgeGroupConverter : IDataConverter
{
    public async Task<object?> ConvertAsync(object? value, Type fromType, Type toType)
    {
        if (value is not DateTime birthDate) return "Unknown";
        
        var age = DateTime.Now.Year - birthDate.Year;
        if (DateTime.Now.DayOfYear < birthDate.DayOfYear) age--;
        
        return age switch
        {
            < 18 => "Minor",
            >= 18 and < 25 => "Young Adult",
            >= 25 and < 40 => "Adult", 
            >= 40 and < 65 => "Middle Age",
            >= 65 => "Senior",
            _ => "Unknown"
        };
    }
}
```

---

## 3️⃣ **СТАТИЧЕСКИЕ МЕТОДЫ**

### **Пример: Конверторы через статические методы**

```csharp
[SchemeVersion("1.3")]
public class Product
{
    // Конверсия через статический метод
    [MigratedFrom("CategoryCode", version: "1.2", oldType: typeof(int),
                  converter: nameof(CategoryConverters.CodeToName))]
    public string CategoryName { get; set; } = "";
    
    // Конверсия с параметрами
    [MigratedFrom("Weight", version: "1.2", oldType: typeof(double),
                  converter: nameof(UnitConverters.PoundsToKilograms),
                  converterParams: new object[] { 2 })] // 2 знака после запятой
    public decimal WeightKg { get; set; }
}

public static class CategoryConverters
{
    private static readonly Dictionary<int, string> CategoryMap = new()
    {
        { 1, "Electronics" },
        { 2, "Clothing" },
        { 3, "Books" },
        { 4, "Home & Garden" },
        { 5, "Sports" }
    };
    
    public static string CodeToName(object? value)
    {
        if (value is int code && CategoryMap.TryGetValue(code, out var name))
            return name;
        return "Other";
    }
}

public static class UnitConverters
{
    public static decimal PoundsToKilograms(object? value, int decimals = 2)
    {
        if (value is double pounds)
        {
            var kg = pounds * 0.453592;
            return Math.Round((decimal)kg, decimals);
        }
        return 0;
    }
}
```

---

## 4️⃣ **INLINE КОНВЕРТОРЫ С КОНТЕКСТОМ**

### **Пример: Конверторы с доступом к другим полям**

```csharp
[SchemeVersion("1.4")]
public class Employee
{
    public string Name { get; set; } = "";
    public decimal BaseSalary { get; set; }
    public string Department { get; set; } = "";
    
    // Конвертор с доступом к контексту объекта
    [MigratedFrom("BonusPercent", version: "1.3", oldType: typeof(int),
                  converter: typeof(SalaryBonusConverter))]
    public decimal TotalSalary { get; set; }
}

public class SalaryBonusConverter : IDataConverter, IContextualConverter
{
    public async Task<object?> ConvertAsync(object? value, Type fromType, Type toType, ConversionContext context)
    {
        if (value is not int bonusPercent) return 0m;
        
        // Получаем доступ к другим полям объекта
        var baseSalary = context.GetFieldValue<decimal>("BaseSalary");
        var department = context.GetFieldValue<string>("Department");
        
        // Бизнес-логика: разные департаменты имеют разные множители
        var departmentMultiplier = department switch
        {
            "Sales" => 1.2m,
            "Engineering" => 1.1m,
            "Management" => 1.3m,
            _ => 1.0m
        };
        
        var bonus = baseSalary * (bonusPercent / 100m) * departmentMultiplier;
        return baseSalary + bonus;
    }
}

public class ConversionContext
{
    private readonly Dictionary<string, object?> _fieldValues;
    
    public ConversionContext(Dictionary<string, object?> fieldValues)
    {
        _fieldValues = fieldValues;
    }
    
    public T GetFieldValue<T>(string fieldName)
    {
        if (_fieldValues.TryGetValue(fieldName, out var value) && value is T typedValue)
            return typedValue;
        return default(T)!;
    }
}
```

---

## 🚀 **ВЫПОЛНЕНИЕ КАСТОМНЫХ КОНВЕРТОРОВ**

### **Регистрация конверторов:**

```csharp
public class MigrationConverterRegistry
{
    private readonly Dictionary<string, IDataConverter> _converters = new();
    private readonly IServiceProvider _serviceProvider;
    
    public MigrationConverterRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        RegisterBuiltInConverters();
    }
    
    private void RegisterBuiltInConverters()
    {
        // Лямбда конверторы
        RegisterLambda("status => status switch { 0 => \"Pending\", 1 => \"Processing\", 2 => \"Shipped\", 3 => \"Delivered\", _ => \"Unknown\" }");
        RegisterLambda("price => decimal.TryParse(price?.ToString(), out var p) && p > 0 ? p : 0");
        
        // Статические методы
        RegisterStatic(nameof(CategoryConverters.CodeToName), CategoryConverters.CodeToName);
        RegisterStatic(nameof(UnitConverters.PoundsToKilograms), UnitConverters.PoundsToKilograms);
        
        // Сложные конверторы
        RegisterType<AddressToCoordinatesConverter>();
        RegisterType<AgeGroupConverter>();
        RegisterType<SalaryBonusConverter>();
    }
    
    public void RegisterLambda(string expression)
    {
        _converters[expression] = new LambdaConverter(expression);
    }
    
    public void RegisterStatic(string name, Func<object?, object?> method)
    {
        _converters[name] = new StaticMethodConverter(method);
    }
    
    public void RegisterType<T>() where T : class, IDataConverter
    {
        var converter = _serviceProvider.GetRequiredService<T>();
        _converters[typeof(T).Name] = converter;
    }
    
    public IDataConverter GetConverter(string name)
    {
        return _converters.TryGetValue(name, out var converter) 
            ? converter 
            : throw new InvalidOperationException($"Converter '{name}' not found");
    }
}
```

### **Использование в миграции:**

```csharp
public async Task ApplyCustomConversion(long schemeId, string fieldName, string converterName, int batchSize = 1000)
{
    var converter = _converterRegistry.GetConverter(converterName);
    var offset = 0;
    var errors = new List<ConversionError>();
    
    while (true)
    {
        var batch = await GetRecordsBatchAsync(schemeId, fieldName, offset, batchSize);
        if (!batch.Any()) break;
        
        foreach (var record in batch)
        {
            try
            {
                // Создаем контекст для конверторов, которым нужны другие поля
                var context = await CreateConversionContextAsync(record.ObjectId, schemeId);
                
                object? convertedValue;
                if (converter is IContextualConverter contextualConverter)
                {
                    convertedValue = await contextualConverter.ConvertAsync(
                        record.Value, record.OldType, record.NewType, context);
                }
                else
                {
                    convertedValue = await converter.ConvertAsync(
                        record.Value, record.OldType, record.NewType);
                }
                
                // Сохраняем конвертированное значение
                await SaveConvertedValueAsync(record.ObjectId, fieldName, convertedValue, record.NewType);
            }
            catch (Exception ex)
            {
                errors.Add(new ConversionError
                {
                    ObjectId = record.ObjectId,
                    FieldName = fieldName,
                    OriginalValue = record.Value,
                    ErrorMessage = ex.Message,
                    Type = ConversionErrorType.CustomValidation
                });
            }
        }
        
        offset += batchSize;
        Console.WriteLine($"Converted {offset} records with {converterName}, {errors.Count} errors");
    }
}
```

---

## 🎯 **ВЫЗОВ КАСТОМНЫХ МИГРАЦИЙ**

### **Автоматический вызов:**
```csharp
// Все кастомные конверторы применятся автоматически
var result = await redb.SyncSchemeAsync<Order>();

Console.WriteLine($"✅ Custom conversions applied:");
Console.WriteLine($"   StatusCode → Status: {result.ProcessedRecords} records");
Console.WriteLine($"   Geocoding errors: {result.FailedRecords} addresses");
```

### **С валидацией перед конверсией:**
```csharp
var options = new MigrationOptions
{
    Strategy = MigrationStrategy.StopOnThreshold,
    ErrorThreshold = 0.1,  // 10% ошибок максимум для геокодирования
    ValidateBeforeConversion = true
};

// Сначала валидация всех конверторов
var validationResult = await redb.ValidateMigrationAsync<Customer>(options);
if (validationResult.HasCriticalErrors)
{
    Console.WriteLine($"❌ Validation failed: {validationResult.ErrorMessage}");
    return;
}

// Затем применение
var result = await redb.SyncSchemeAsync<Customer>(options);
```

### **Мониторинг сложных конверсий:**
```csharp
var progress = new Progress<MigrationProgress>(p =>
{
    Console.WriteLine($"Converting {p.CurrentField}: {p.ProcessedRecords}/{p.TotalRecords} " +
                     $"({p.ProgressPercent:F1}%) - {p.ErrorsCount} errors");
    
    if (p.CurrentConverter == "AddressToCoordinatesConverter")
    {
        Console.WriteLine($"  API calls: {p.ApiCalls}, Cache hits: {p.CacheHits}");
    }
});

var result = await redb.SyncSchemeAsync<Customer>(progress: progress);
```

**Кастомные конверторы обеспечивают максимальную гибкость для сложных миграций!** 🎯
