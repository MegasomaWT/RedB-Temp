# 🚀 **ПЛАН РЕАЛИЗАЦИИ: IL EMIT PROXY + CHANGE TRACKING ДЛЯ RedbObject<TProps>**

## 🎯 **АРХИТЕКТУРНАЯ КОНЦЕПЦИЯ**

### **Цель:** 
Создать прозрачный change tracking для бизнес-объектов TProps в RedbObject<TProps> без изменения пользовательского кода.

### **Ключевые принципы:**
1. **🎭 Прозрачность:** `obj.properties.Name = "..."` работает как обычно
2. **🔒 Thread-Safety:** ConcurrentDictionary для потокобезопасного трекинга
3. **💾 Ленивое сохранение:** Изменения накапливаются, сохранение по требованию  
4. **🎯 EAV оптимизация:** INSERT/UPDATE/DELETE только измененных свойств
5. **🏷️ Полная совместимость:** Сохранение всех атрибутов TProps

---

## 🏗️ **ЭТАП 1: БАЗОВАЯ ИНФРАСТРУКТУРА**

### **1.1 📊 Change Tracking Core**

```csharp
/// <summary>
/// Информация об изменении свойства для EAV операций
/// </summary>
public class PropertyChangeInfo
{
    public string PropertyName { get; set; } = "";
    public object? OriginalValue { get; set; }
    public object? CurrentValue { get; set; }
    public long OriginalValueHash { get; set; }     // ← long вместо string!
    public long CurrentValueHash { get; set; }      // ← long вместо string!
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public PropertyChangeType ChangeType { get; set; }
}

public enum PropertyChangeType
{
    Insert,    // null → not-null (INSERT INTO _values)
    Update,    // not-null → not-null (UPDATE _values)
    Delete     // not-null → null (DELETE FROM _values)
}

/// <summary>
/// Потокобезопасный трекер изменений
/// </summary>
public interface IChangeTracker
{
    bool HasChanges { get; }
    void TrackChange(string propertyName, object? originalValue, object? newValue);
    IReadOnlyCollection<PropertyChangeInfo> GetChanges();
    void AcceptChanges(); // Очистить после сохранения
    void RejectChanges(); // Откатить изменения
    long ComputeValueHash(object? value);  // ← long вместо string!
}
```

### **1.2 🔒 Thread-Safe Implementation**

```csharp
public class ThreadSafeChangeTracker : IChangeTracker
{
    private readonly ConcurrentDictionary<string, PropertyChangeInfo> _changes = new();
    private readonly object _lockObject = new();

    public bool HasChanges => !_changes.IsEmpty;

    public void TrackChange(string propertyName, object? originalValue, object? newValue)
    {
        lock (_lockObject)
        {
            var originalHash = ComputeValueHash(originalValue);
            var newHash = ComputeValueHash(newValue);
            
            // Если хеши одинаковы - изменений нет
            if (originalHash == newHash)
            {
                _changes.TryRemove(propertyName, out _);
                return;
            }

            var changeType = DetermineChangeType(originalValue, newValue);
            
            _changes.AddOrUpdate(propertyName, 
                // Добавляем новое изменение
                new PropertyChangeInfo
                {
                    PropertyName = propertyName,
                    OriginalValue = originalValue,
                    CurrentValue = newValue,
                    OriginalValueHash = originalHash,
                    CurrentValueHash = newHash,
                    ChangeType = changeType
                },
                // Обновляем существующее (сохраняя ПЕРВОНАЧАЛЬНОЕ значение!)
                (key, existing) => new PropertyChangeInfo
                {
                    PropertyName = propertyName,
                    OriginalValue = existing.OriginalValue, // ⚡ Важно: оригинал НЕ меняем!
                    CurrentValue = newValue,
                    OriginalValueHash = existing.OriginalValueHash,
                    CurrentValueHash = newHash,
                    ChangedAt = DateTime.UtcNow,
                    ChangeType = DetermineChangeType(existing.OriginalValue, newValue)
                });
        }
    }

    public long ComputeValueHash(object? value)
    {
        if (value == null) return 0L;
        
        // Специальная обработка массивов
        if (value is Array array)
        {
            return ComputeArrayHash(array);
        }
        
        // Для обычных значений - комбинируем hashcode значения и типа
        var valueHash = value.GetHashCode();
        var typeHash = value.GetType().GetHashCode();
        
        // Создаем 64-битный хеш из двух 32-битных
        return ((long)valueHash << 32) | (uint)typeHash;
    }

    private long ComputeArrayHash(Array array)
    {
        if (array.Length == 0) return 17L; // Пустой массив
        
        long hash = 0x1505L; // Seed для начального значения
        const long multiplier = 31L; // Простое число для комбинирования
        
        foreach (var item in array)
        {
            long itemHash;
            
            if (item == null)
            {
                itemHash = 0L;
            }
            else if (item is Array nestedArray)
            {
                // Рекурсивно обрабатываем вложенные массивы
                itemHash = ComputeArrayHash(nestedArray);
    }
    else
    {
                // Для обычных элементов
                var valueHash = item.GetHashCode();
                var typeHash = item.GetType().GetHashCode();
                itemHash = ((long)valueHash << 32) | (uint)typeHash;
            }
            
            // Комбинируем хеши с учетом позиции
            hash = hash * multiplier + itemHash;
        }
        
        return hash;
    }

    private static PropertyChangeType DetermineChangeType(object? original, object? current)
    {
        return (original, current) switch
        {
            (null, not null) => PropertyChangeType.Insert,
            (not null, null) => PropertyChangeType.Delete,
            (not null, not null) => PropertyChangeType.Update,
            (null, null) => PropertyChangeType.Update // Технически не должно случиться
        };
    }

    public IReadOnlyCollection<PropertyChangeInfo> GetChanges()
    {
        return _changes.Values.ToList().AsReadOnly();
    }

    public void AcceptChanges()
    {
        _changes.Clear();
    }

    public void RejectChanges()
    {
        _changes.Clear();
        // TODO: Восстановить исходные значения в объекте если нужно
    }
}
```

---

## 🎭 **ЭТАП 2: IL EMIT PROXY BUILDER**

### **2.1 🏗️ Генератор Proxy типов**

```csharp
public static class RedbProxyBuilder<TProps> where TProps : class, new()
{
    private static readonly Lazy<Type> _proxyType = new(CreateProxyType, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly AssemblyBuilder _assemblyBuilder;
    private static readonly ModuleBuilder _moduleBuilder;

    static RedbProxyBuilder()
    {
        var assemblyName = new AssemblyName($"RedbProxyAssembly_{typeof(TProps).Name}");
        _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        _moduleBuilder = _assemblyBuilder.DefineDynamicModule($"RedbProxyModule_{typeof(TProps).Name}");
    }

    public static Type GetProxyType() => _proxyType.Value;

    private static Type CreateProxyType()
    {
        var originalType = typeof(TProps);
        var typeName = $"{originalType.Name}_RedbProxy_{Guid.NewGuid():N}";
        
        var typeBuilder = _moduleBuilder.DefineType(
            typeName,
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed,
            originalType);

        // 📝 Копируем атрибуты класса
        CopyTypeAttributes(originalType, typeBuilder);

        // 🔧 Добавляем поле для change tracker
        var trackerField = typeBuilder.DefineField(
            "_changeTracker", 
            typeof(IChangeTracker), 
            FieldAttributes.Private | FieldAttributes.ReadOnly);

        // 🏗️ Создаем конструктор
        CreateConstructor(typeBuilder, trackerField);

        // 📊 Обрабатываем все public свойства
        var properties = originalType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .ToArray();

        foreach (var property in properties)
        {
            CreateTrackedProperty(typeBuilder, property, trackerField);
        }

        return typeBuilder.CreateType()!;
    }

    private static void CopyTypeAttributes(Type originalType, TypeBuilder typeBuilder)
    {
        foreach (var attr in originalType.GetCustomAttributesData())
        {
            var attrBuilder = CreateAttributeBuilder(attr);
            typeBuilder.SetCustomAttribute(attrBuilder);
        }
    }

    private static CustomAttributeBuilder CreateAttributeBuilder(CustomAttributeData attributeData)
    {
        var constructorArgs = attributeData.ConstructorArguments.Select(arg => arg.Value).ToArray();
        var namedPropertyInfos = attributeData.NamedArguments
            .Where(arg => arg.IsField == false)
            .Select(arg => arg.MemberInfo as PropertyInfo)
            .Where(pi => pi != null)
            .ToArray();
        var propertyValues = attributeData.NamedArguments
            .Where(arg => arg.IsField == false)
            .Select(arg => arg.TypedValue.Value)
            .ToArray();

        return new CustomAttributeBuilder(
            attributeData.Constructor, 
            constructorArgs,
            namedPropertyInfos!, 
            propertyValues);
    }
}
```

### **2.2 🔧 Генерация конструктора**

```csharp
private static void CreateConstructor(TypeBuilder typeBuilder, FieldBuilder trackerField)
{
    var constructorBuilder = typeBuilder.DefineConstructor(
        MethodAttributes.Public,
        CallingConventions.Standard,
        new[] { typeof(IChangeTracker) });

    var il = constructorBuilder.GetILGenerator();

    // Вызываем конструктор базового класса
    var baseConstructor = typeof(TProps).GetConstructor(Type.EmptyTypes)
        ?? throw new InvalidOperationException($"Тип {typeof(TProps).Name} должен иметь параметрless конструктор");
    
    il.Emit(OpCodes.Ldarg_0);  // this
    il.Emit(OpCodes.Call, baseConstructor);

    // Устанавливаем поле _changeTracker
    il.Emit(OpCodes.Ldarg_0);  // this
    il.Emit(OpCodes.Ldarg_1);  // changeTracker parameter
    il.Emit(OpCodes.Stfld, trackerField);

    il.Emit(OpCodes.Ret);
}
```

### **2.3 📊 Генерация tracked свойств**

```csharp
private static void CreateTrackedProperty(TypeBuilder typeBuilder, PropertyInfo originalProperty, FieldBuilder trackerField)
{
    // Создаем backing field
    var backingField = typeBuilder.DefineField(
        $"_{originalProperty.Name.ToLowerInvariant()}_backing", 
        originalProperty.PropertyType, 
        FieldAttributes.Private);

    // 🔍 Создаем Getter
    var getterBuilder = typeBuilder.DefineMethod(
        $"get_{originalProperty.Name}",
        MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
        originalProperty.PropertyType,
        Type.EmptyTypes);

    var getterIL = getterBuilder.GetILGenerator();
    getterIL.Emit(OpCodes.Ldarg_0);      // this
    getterIL.Emit(OpCodes.Ldfld, backingField);  // return _field
    getterIL.Emit(OpCodes.Ret);

    // 📝 Создаем Setter с change tracking
    var setterBuilder = typeBuilder.DefineMethod(
        $"set_{originalProperty.Name}",
        MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
        null,
        new[] { originalProperty.PropertyType });

    var setterIL = setterBuilder.GetILGenerator();

    // Получаем старое значение
    setterIL.Emit(OpCodes.Ldarg_0);  // this
    setterIL.Emit(OpCodes.Ldfld, backingField);  // oldValue = _field
    var oldValueLocal = setterIL.DeclareLocal(originalProperty.PropertyType);
    setterIL.Emit(OpCodes.Stloc, oldValueLocal);

    // Устанавливаем новое значение
    setterIL.Emit(OpCodes.Ldarg_0);  // this
    setterIL.Emit(OpCodes.Ldarg_1);  // newValue
    setterIL.Emit(OpCodes.Stfld, backingField);  // _field = newValue

    // Вызываем change tracking: _changeTracker.TrackChange(propertyName, oldValue, newValue)
    setterIL.Emit(OpCodes.Ldarg_0);  // this
    setterIL.Emit(OpCodes.Ldfld, trackerField);  // _changeTracker
    setterIL.Emit(OpCodes.Ldstr, originalProperty.Name);  // propertyName
    
    // Подготавливаем oldValue для boxing
    setterIL.Emit(OpCodes.Ldloc, oldValueLocal);
    if (originalProperty.PropertyType.IsValueType)
        setterIL.Emit(OpCodes.Box, originalProperty.PropertyType);
    
    // Подготавливаем newValue для boxing
    setterIL.Emit(OpCodes.Ldarg_1);
    if (originalProperty.PropertyType.IsValueType)
        setterIL.Emit(OpCodes.Box, originalProperty.PropertyType);

    // Вызываем TrackChange
    var trackChangeMethod = typeof(IChangeTracker).GetMethod(nameof(IChangeTracker.TrackChange))!;
    setterIL.Emit(OpCodes.Callvirt, trackChangeMethod);

    setterIL.Emit(OpCodes.Ret);

    // 🏷️ Создаем Property Definition с атрибутами
    var propertyBuilder = typeBuilder.DefineProperty(
        originalProperty.Name, 
        PropertyAttributes.None, 
        originalProperty.PropertyType, 
        null);
    
    propertyBuilder.SetGetMethod(getterBuilder);
    propertyBuilder.SetSetMethod(setterBuilder);

    // Копируем все атрибуты свойства
    foreach (var attr in originalProperty.GetCustomAttributesData())
    {
        var attrBuilder = CreateAttributeBuilder(attr);
        propertyBuilder.SetCustomAttribute(attrBuilder);
    }
}
```

---

## 🔄 **ЭТАП 3: ИНТЕГРАЦИЯ В RedbObject<TProps>**

### **3.1 🎯 Модификация RedbObject**

```csharp
public class RedbObject<TProps> : RedbObject, IRedbObject<TProps> where TProps : class, new()
{
    private TProps? _trackedProperties;
    private readonly ThreadSafeChangeTracker _changeTracker = new();
    
    public RedbObject() { }

    public RedbObject(TProps props)
    {
        // Создаем прокси и инициализируем значениями из props
        _trackedProperties = CreateTrackedProxy();
        CopyPropertiesFrom(props, _trackedProperties);
        _changeTracker.AcceptChanges(); // Сбрасываем tracking после инициализации
    }

    // 🎭 Умное свойство - всегда возвращает tracked proxy
    public TProps properties => _trackedProperties ??= CreateTrackedProxy();

    private TProps CreateTrackedProxy()
    {
        var proxyType = RedbProxyBuilder<TProps>.GetProxyType();
        return (TProps)Activator.CreateInstance(proxyType, _changeTracker)!;
    }

    private static void CopyPropertiesFrom(TProps source, TProps target)
    {
        var properties = typeof(TProps).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite);

        foreach (var prop in properties)
        {
            var value = prop.GetValue(source);
            prop.SetValue(target, value);
        }
    }

    // 📊 Change Tracking API
    public bool HasChanges => _changeTracker.HasChanges;
    public IReadOnlyCollection<PropertyChangeInfo> GetChanges() => _changeTracker.GetChanges();
    public void AcceptChanges() => _changeTracker.AcceptChanges();
    public void RejectChanges() => _changeTracker.RejectChanges();
}
```

---

## 💾 **ЭТАП 4: EAV PERSISTENCE ЛОГИКА**

### **4.1 🎯 Smart Save Implementation**

```csharp
public class RedbObject<TProps> : RedbObject, IRedbObject<TProps> where TProps : class, new()
{
    /// <summary>
    /// Умное сохранение - только измененные свойства в EAV структуру
    /// </summary>
    public async Task<long> SaveChangesAsync()
    {
        if (!HasChanges)
            return id;

        var changes = GetChanges();
        var scheme = await GetSchemeForTypeAsync();
        
        using var transaction = await BeginTransactionAsync();
        try
        {
            // Обеспечиваем что основной объект существует в _objects
            await EnsureObjectExistsAsync();
            
            // Применяем изменения свойств в _values
            foreach (var change in changes)
            {
                await ApplyPropertyChangeAsync(scheme, change);
            }

            await transaction.CommitAsync();
            
            // Очищаем change tracker после успешного сохранения
            _changeTracker.AcceptChanges();
            
            return id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ApplyPropertyChangeAsync(IRedbScheme scheme, PropertyChangeInfo change)
    {
        var structure = scheme.GetStructureByName(change.PropertyName);
        if (structure == null) 
        {
            throw new InvalidOperationException($"Структура '{change.PropertyName}' не найдена в схеме '{scheme.Name}'");
        }

        switch (change.ChangeType)
        {
            case PropertyChangeType.Insert:
                await InsertValueAsync(structure, change.CurrentValue);
                break;
                
            case PropertyChangeType.Update:
                await UpdateValueAsync(structure, change.CurrentValue);
                break;
                
            case PropertyChangeType.Delete:
                await DeleteValueAsync(structure);
                break;
        }
    }

    private async Task InsertValueAsync(IRedbStructure structure, object? value)
    {
        var sql = @"
            INSERT INTO _values (_id, _id_structure, _id_object, _String, _Long, _Guid, _Double, _DateTime, _Boolean, _ByteArray, _Array)
            VALUES (@Id, @IdStructure, @IdObject, @String, @Long, @Guid, @Double, @DateTime, @Boolean, @ByteArray, @Array)";

        var parameters = CreateValueParameters(structure, value);
        parameters["Id"] = await GetNextIdAsync();
        parameters["IdStructure"] = structure.Id;
        parameters["IdObject"] = id;

        await ExecuteSqlAsync(sql, parameters);
    }

    private async Task UpdateValueAsync(IRedbStructure structure, object? value)
    {
        var sql = @"
            UPDATE _values 
            SET _String = @String, _Long = @Long, _Guid = @Guid, _Double = @Double, 
                _DateTime = @DateTime, _Boolean = @Boolean, _ByteArray = @ByteArray, _Array = @Array
            WHERE _id_structure = @IdStructure AND _id_object = @IdObject";

        var parameters = CreateValueParameters(structure, value);
        parameters["IdStructure"] = structure.Id;
        parameters["IdObject"] = id;

        await ExecuteSqlAsync(sql, parameters);
    }

    private async Task DeleteValueAsync(IRedbStructure structure)
    {
        var sql = "DELETE FROM _values WHERE _id_structure = @IdStructure AND _id_object = @IdObject";
        
        var parameters = new Dictionary<string, object?>
        {
            ["IdStructure"] = structure.Id,
            ["IdObject"] = id
        };

        await ExecuteSqlAsync(sql, parameters);
    }

    private Dictionary<string, object?> CreateValueParameters(IRedbStructure structure, object? value)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["String"] = null,
            ["Long"] = null,
            ["Guid"] = null,
            ["Double"] = null,
            ["DateTime"] = null,
            ["Boolean"] = null,
            ["ByteArray"] = null,
            ["Array"] = null
        };

        if (value == null) return parameters;

        // Обработка массивов
        if (structure.IsArray && value is Array arrayValue)
        {
            parameters["Array"] = JsonSerializer.Serialize(arrayValue);
            return parameters;
        }

        // Маппинг по типам данных
        switch (structure.Type.DbType.ToLowerInvariant())
        {
            case "string":
                parameters["String"] = value?.ToString();
                break;
            case "long":
                parameters["Long"] = Convert.ToInt64(value);
                break;
            case "guid":
                parameters["Guid"] = value is Guid guid ? guid : Guid.Parse(value.ToString()!);
                break;
            case "double":
                parameters["Double"] = Convert.ToDouble(value);
                break;
            case "datetime":
                parameters["DateTime"] = Convert.ToDateTime(value);
                break;
            case "boolean":
                parameters["Boolean"] = Convert.ToBoolean(value);
                break;
            case "bytearray":
                parameters["ByteArray"] = value as byte[];
                break;
            default:
                parameters["String"] = value?.ToString();
                break;
        }

        return parameters;
    }
}
```

---

## 🔧 **ЭТАП 5: ДОПОЛНИТЕЛЬНЫЕ ПРЕДЛОЖЕНИЯ ПО ТРЕКИНГУ**

### **5.1 📊 Расширенная статистика трекинга**

```csharp
public class AdvancedChangeTracker : ThreadSafeChangeTracker
{
    private readonly Dictionary<string, PropertyStatistics> _propertyStats = new();
    
    public class PropertyStatistics
    {
        public int ChangeCount { get; set; }
        public DateTime FirstChangeTime { get; set; }
        public DateTime LastChangeTime { get; set; }
        public TimeSpan AverageTimeBetweenChanges { get; set; }
        public List<object?> ValueHistory { get; set; } = new();
    }
    
    public override void TrackChange(string propertyName, object? originalValue, object? newValue)
    {
        base.TrackChange(propertyName, originalValue, newValue);
        
        // Обновляем статистику
        if (!_propertyStats.TryGetValue(propertyName, out var stats))
        {
            stats = new PropertyStatistics { FirstChangeTime = DateTime.UtcNow };
            _propertyStats[propertyName] = stats;
        }
        
        stats.ChangeCount++;
        stats.LastChangeTime = DateTime.UtcNow;
        stats.ValueHistory.Add(newValue);
        
        // Ограничиваем историю последними 10 значениями
        if (stats.ValueHistory.Count > 10)
        {
            stats.ValueHistory.RemoveAt(0);
        }
    }
    
    public PropertyStatistics? GetPropertyStatistics(string propertyName)
    {
        return _propertyStats.TryGetValue(propertyName, out var stats) ? stats : null;
    }
}
```

### **5.2 🔄 Cascading Change Tracking**

```csharp
public class CascadingChangeTracker : ThreadSafeChangeTracker
{
    private readonly HashSet<string> _cascadingProperties = new();
    
    /// <summary>
    /// Регистрирует свойство как каскадное (изменение влечет пересчет других)
    /// </summary>
    public void RegisterCascadingProperty(string propertyName, params string[] dependentProperties)
    {
        _cascadingProperties.Add(propertyName);
        // TODO: Реализовать логику каскадных обновлений
    }
    
    public override void TrackChange(string propertyName, object? originalValue, object? newValue)
    {
        base.TrackChange(propertyName, originalValue, newValue);
        
        // Если это каскадное свойство - пересчитываем зависимые
        if (_cascadingProperties.Contains(propertyName))
        {
            RecalculateDependentProperties(propertyName);
        }
    }
    
    private void RecalculateDependentProperties(string changedProperty)
    {
        // Логика пересчета зависимых свойств
        // Например, при изменении Price пересчитывать TotalPrice
    }
}
```

### **5.3 🎭 Conditional Change Tracking**

```csharp
public class ConditionalChangeTracker : ThreadSafeChangeTracker
{
    private readonly Dictionary<string, Func<object?, object?, bool>> _changeConditions = new();
    
    /// <summary>
    /// Добавляет условие для отслеживания изменений свойства
    /// </summary>
    public void AddChangeCondition(string propertyName, Func<object?, object?, bool> condition)
    {
        _changeConditions[propertyName] = condition;
    }
    
    public override void TrackChange(string propertyName, object? originalValue, object? newValue)
    {
        // Проверяем условие отслеживания
        if (_changeConditions.TryGetValue(propertyName, out var condition))
        {
            if (!condition(originalValue, newValue))
                return; // Не отслеживаем это изменение
        }
        
        base.TrackChange(propertyName, originalValue, newValue);
    }
}

// Пример использования:
tracker.AddChangeCondition("Price", (old, @new) => 
{
    // Отслеживаем только если изменение больше 5%
    if (old is double oldPrice && @new is double newPrice)
        return Math.Abs(newPrice - oldPrice) / oldPrice > 0.05;
    return true;
});
```

### **5.4 📈 Performance Monitoring**

```csharp
public class MonitoredChangeTracker : ThreadSafeChangeTracker
{
    private readonly Dictionary<string, PerformanceCounters> _performanceCounters = new();
    
    public class PerformanceCounters
    {
        public long TotalChanges { get; set; }
        public TimeSpan TotalTrackingTime { get; set; }
        public TimeSpan AverageTrackingTime => TotalChanges > 0 
            ? TimeSpan.FromTicks(TotalTrackingTime.Ticks / TotalChanges) 
            : TimeSpan.Zero;
    }
    
    public override void TrackChange(string propertyName, object? originalValue, object? newValue)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            base.TrackChange(propertyName, originalValue, newValue);
        }
        finally
        {
            stopwatch.Stop();
            
            if (!_performanceCounters.TryGetValue(propertyName, out var counters))
            {
                counters = new PerformanceCounters();
                _performanceCounters[propertyName] = counters;
            }
            
            counters.TotalChanges++;
            counters.TotalTrackingTime = counters.TotalTrackingTime.Add(stopwatch.Elapsed);
        }
    }
    
    public PerformanceCounters? GetPerformanceCounters(string propertyName)
    {
        return _performanceCounters.TryGetValue(propertyName, out var counters) ? counters : null;
    }
}
```

---

## 🚀 **ЭТАП 6: ФИНАЛЬНАЯ ИНТЕГРАЦИЯ**

### **6.1 🎯 Factory для создания RedbObject**

```csharp
public static class RedbObjectFactory
{
    /// <summary>
    /// Создает RedbObject с расширенным change tracking
    /// </summary>
    public static RedbObject<TProps> CreateWithAdvancedTracking<TProps>() where TProps : class, new()
    {
        return new RedbObject<TProps>(new AdvancedChangeTracker());
    }
    
    /// <summary>
    /// Создает RedbObject с conditional tracking
    /// </summary>
    public static RedbObject<TProps> CreateWithConditionalTracking<TProps>(
        Dictionary<string, Func<object?, object?, bool>> conditions) where TProps : class, new()
    {
        var tracker = new ConditionalChangeTracker();
        foreach (var (property, condition) in conditions)
        {
            tracker.AddChangeCondition(property, condition);
        }
        
        return new RedbObject<TProps>(tracker);
    }
}
```

### **6.2 📊 Использование**

```csharp
// Стандартное использование
var product = new RedbObject<ProductProps>();
product.properties.Name = "iPhone 15";
product.properties.Price = 999.99;

Console.WriteLine($"Изменений: {product.GetChanges().Count}");  // 2
await product.SaveChangesAsync();  // INSERT в _values только для Name и Price

// Расширенное использование
var advancedProduct = RedbObjectFactory.CreateWithAdvancedTracking<ProductProps>();
advancedProduct.properties.Price = 1299.99;
var priceStats = ((AdvancedChangeTracker)advancedProduct._changeTracker).GetPropertyStatistics("Price");
Console.WriteLine($"Price изменялся {priceStats?.ChangeCount} раз");
```

---

## ✅ **ПЛАН РЕАЛИЗАЦИИ ПО ЭТАПАМ**

### **🎯 Фаза 1 (MVP):**
1. ✅ ThreadSafeChangeTracker базовый
2. ✅ IL Emit ProxyBuilder базовый  
3. ✅ Интеграция в RedbObject<TProps>
4. ✅ Базовая EAV persistence логика

### **🚀 Фаза 2 (Production):**
1. 📊 Добавить копирование атрибутов
2. 🔧 Оптимизация производительности IL генерации
3. 🧪 Unit тесты для всех компонентов
4. 📈 Базовый performance monitoring

### **⭐ Фаза 3 (Advanced):**
1. 🎭 Расширенные типы change tracking
2. 🔄 Каскадные обновления
3. 📊 Детальная статистика и аналитика
4. 🎯 Conditional tracking условия

**Этот план обеспечит полную прозрачность change tracking с максимальной производительностью и гибкостью!** 🎉

---

## 📊 **ДЕТАЛЬНАЯ ОБРАБОТКА МАССИВОВ**

### **🎯 Проблемы с массивами в GetHashCode:**

```csharp
// ❌ ПРОБЛЕМА: Array.GetHashCode() возвращает хеш ссылки, а не содержимого!
var array1 = new[] { "Apple", "Google", "Microsoft" };
var array2 = new[] { "Apple", "Google", "Microsoft" };

Console.WriteLine(array1.GetHashCode());  // Например: 54267293
Console.WriteLine(array2.GetHashCode());  // Например: 18643596  ← РАЗНЫЕ!
Console.WriteLine(array1.Equals(array2)); // False ← ПРОБЛЕМА!

// ✅ РЕШЕНИЕ: Наша реализация ComputeArrayHash
var tracker = new ThreadSafeChangeTracker();
var hash1 = tracker.ComputeArrayHash(array1);  // Например: 1234567890123
var hash2 = tracker.ComputeArrayHash(array2);  // Например: 1234567890123  ← ОДИНАКОВЫЕ!
```

### **🔧 Различные типы массивов:**

```csharp
public long ComputeArrayHash(Array array)
{
    if (array.Length == 0) return 17L;
    
    long hash = 0x1505L;
    const long multiplier = 31L;
    
    foreach (var item in array)
    {
        long itemHash;
        
        if (item == null)
        {
            itemHash = 0L;
        }
        else if (item is Array nestedArray)
        {
            // 🔄 Вложенные массивы (многомерные)
            itemHash = ComputeArrayHash(nestedArray);
        }
        else if (item is string str)
        {
            // 📝 Строки - используем встроенный GetHashCode
            itemHash = str.GetHashCode();
        }
        else if (item is IRedbObject redbObj)
        {
            // 🎯 RedbObject - используем ID или специальный хеш
            itemHash = redbObj.id != 0 ? redbObj.id : ComputeValueHash(item);
        }
        else if (item.GetType().IsPrimitive)
        {
            // 🔢 Примитивные типы
            var valueHash = item.GetHashCode();
            var typeHash = item.GetType().GetHashCode();
            itemHash = ((long)valueHash << 32) | (uint)typeHash;
        }
        else
        {
            // 🎭 Сложные объекты - сериализуем или используем GetHashCode
            try
            {
                var jsonHash = JsonSerializer.Serialize(item).GetHashCode();
                var typeHash = item.GetType().GetHashCode();
                itemHash = ((long)jsonHash << 32) | (uint)typeHash;
            }
            catch
            {
                // Fallback к обычному GetHashCode
                var valueHash = item.GetHashCode();
                var typeHash = item.GetType().GetHashCode();
                itemHash = ((long)valueHash << 32) | (uint)typeHash;
            }
        }
        
        // Комбинируем с учетом позиции в массиве
        hash = hash * multiplier + itemHash;
    }
    
    return hash;
}
```

### **🎯 Примеры работы с разными массивами:**

```csharp
var tracker = new ThreadSafeChangeTracker();

// 1. 📝 Массив строк
var tags = new[] { "smartphone", "apple", "premium" };
var hash1 = tracker.ComputeValueHash(tags);
Console.WriteLine($"Tags hash: {hash1}");

// 2. 🔢 Массив чисел  
var prices = new[] { 999.99, 1299.99, 1499.99 };
var hash2 = tracker.ComputeValueHash(prices);
Console.WriteLine($"Prices hash: {hash2}");

// 3. 🎭 Массив объектов
var products = new[]
{
    new { Name = "iPhone", Price = 999.99 },
    new { Name = "iPad", Price = 799.99 }
};
var hash3 = tracker.ComputeValueHash(products);
Console.WriteLine($"Products hash: {hash3}");

// 4. 🔄 Многомерный массив
var matrix = new int[,] { {1, 2}, {3, 4} };
var hash4 = tracker.ComputeValueHash(matrix);
Console.WriteLine($"Matrix hash: {hash4}");

// 5. 🏗️ Массив RedbObject'ов (для EAV)
var redbProducts = new[]
{
    new RedbObject<ProductProps> { id = 101 },
    new RedbObject<ProductProps> { id = 102 }
};
var hash5 = tracker.ComputeValueHash(redbProducts);
Console.WriteLine($"RedbObjects hash: {hash5}");
```

### **⚡ Оптимизации для производительности:**

```csharp
public class OptimizedArrayHasher
{
    // Кеш хешей для неизменяемых массивов
    private static readonly ConcurrentDictionary<object, long> _arrayHashCache = new();
    
    public long ComputeArrayHashWithCache(Array array)
    {
        // Для readonly массивов можем кешировать результат
        if (array.IsReadOnly)
        {
            return _arrayHashCache.GetOrAdd(array, ComputeArrayHashInternal);
        }
        
        return ComputeArrayHashInternal(array);
    }
    
    private long ComputeArrayHashInternal(Array array)
    {
        // Быстрый путь для типизированных массивов
        return array switch
        {
            string[] stringArray => ComputeStringArrayHash(stringArray),
            int[] intArray => ComputeIntArrayHash(intArray),
            long[] longArray => ComputeLongArrayHash(longArray),
            double[] doubleArray => ComputeDoubleArrayHash(doubleArray),
            _ => ComputeGenericArrayHash(array) // Универсальный медленный путь
        };
    }
    
    private long ComputeStringArrayHash(string[] array)
    {
        long hash = 0x1505L;
        foreach (var str in array)
        {
            hash = hash * 31L + (str?.GetHashCode() ?? 0);
        }
        return hash;
    }
    
    private long ComputeIntArrayHash(int[] array)
    {
        long hash = 0x1505L;
        foreach (var value in array)
        {
            hash = hash * 31L + value;
        }
        return hash;
    }
    
    // Аналогично для других типов...
}
```

### **🔍 Отладка и диагностика:**

```csharp
public class DiagnosticChangeTracker : ThreadSafeChangeTracker
{
    public class ArrayHashInfo
    {
        public Array Array { get; set; } = null!;
        public long Hash { get; set; }
        public int ElementCount { get; set; }
        public Type ElementType { get; set; } = typeof(object);
        public TimeSpan ComputeTime { get; set; }
    }
    
    private readonly List<ArrayHashInfo> _arrayHashHistory = new();
    
    public override long ComputeValueHash(object? value)
    {
        if (value is Array array)
        {
            var stopwatch = Stopwatch.StartNew();
            var hash = ComputeArrayHash(array);
            stopwatch.Stop();
            
            // Сохраняем для диагностики
            _arrayHashHistory.Add(new ArrayHashInfo
            {
                Array = array,
                Hash = hash,
                ElementCount = array.Length,
                ElementType = array.GetType().GetElementType() ?? typeof(object),
                ComputeTime = stopwatch.Elapsed
            });
            
            return hash;
        }
        
        return base.ComputeValueHash(value);
    }
    
    public IReadOnlyList<ArrayHashInfo> GetArrayHashHistory() => _arrayHashHistory.AsReadOnly();
}
```

### **📊 Тестирование стабильности хешей:**

```csharp
[Test]
public void ArrayHash_SameContent_SameHash()
{
    var tracker = new ThreadSafeChangeTracker();
    
    var array1 = new[] { "A", "B", "C" };
    var array2 = new[] { "A", "B", "C" };
    
    var hash1 = tracker.ComputeValueHash(array1);
    var hash2 = tracker.ComputeValueHash(array2);
    
    Assert.AreEqual(hash1, hash2, "Массивы с одинаковым содержимым должны иметь одинаковые хеши");
}

[Test]
public void ArrayHash_DifferentOrder_DifferentHash()
{
    var tracker = new ThreadSafeChangeTracker();
    
    var array1 = new[] { "A", "B", "C" };
    var array2 = new[] { "C", "B", "A" };
    
    var hash1 = tracker.ComputeValueHash(array1);
    var hash2 = tracker.ComputeValueHash(array2);
    
    Assert.AreNotEqual(hash1, hash2, "Массивы с разным порядком должны иметь разные хеши");
}

[Test]
public void ArrayHash_Nested_Works()
{
    var tracker = new ThreadSafeChangeTracker();
    
    var nested1 = new object[] 
    { 
        new[] { 1, 2 }, 
        new[] { 3, 4 } 
    };
    
    var nested2 = new object[] 
    { 
        new[] { 1, 2 }, 
        new[] { 3, 4 } 
    };
    
    var hash1 = tracker.ComputeValueHash(nested1);
    var hash2 = tracker.ComputeValueHash(nested2);
    
    Assert.AreEqual(hash1, hash2, "Вложенные массивы должны корректно хешироваться");
}
```

**GetHashCode + специальная обработка массивов = оптимальное решение для нашего change tracker!** ⚡🎯
