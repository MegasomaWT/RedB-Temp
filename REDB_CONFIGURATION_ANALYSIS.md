# 🔧 Система конфигурации RedbService - Полное руководство

## 🎯 **Обзор системы конфигурации**

Система конфигурации RedbService обеспечивает гибкую настройку поведения всех компонентов через:
- ✅ **Интеграцию с Microsoft.Extensions.Configuration** (appsettings.json, переменные окружения)
- ✅ **Предопределенные профили** для разных сценариев
- ✅ **Fluent API (Builder Pattern)** для программной настройки
- ✅ **Hot-reload конфигурации** с мониторингом изменений
- ✅ **Валидацию настроек** с автоисправлением критических ошибок
- ✅ **Options Pattern** для стандартной интеграции с .NET DI

## 🔍 **Анализ решенных проблем**

### **1. ❌ Проблема: Непоследовательность настроек безопасности**

**Было:**
```csharp
// Разные значения по умолчанию в разных методах
LoadAsync<TProps>(..., bool checkPermissions = false)     // ❌ НЕ проверяет
SaveAsync<TProps>(..., bool checkPermissions = false)     // ❌ НЕ проверяет  
DeleteAsync(..., bool checkPermissions = true)            // ✅ Проверяет
```

**✅ Решение:**
```csharp
// Единые настройки через конфигурацию
public bool DefaultCheckPermissionsOnLoad { get; set; } = false;
public bool DefaultCheckPermissionsOnSave { get; set; } = false;  
public bool DefaultCheckPermissionsOnDelete { get; set; } = true;
```

### **2. ❌ Проблема: Удаленные объекты вызывают ошибки при повторном сохранении**

**Было:**
```csharp
var obj = new RedbObject<TestProps> { name = "Test" };
var id = await redb.SaveAsync(obj);           // obj.id = 12345
await redb.DeleteAsync(obj);                  // Удаляем из БД
await redb.SaveAsync(obj);                    // ❌ ОШИБКА! obj.id = 12345, но объекта нет
```

**✅ Решение:**
```csharp
// Стратегии автоматической обработки
public ObjectIdResetStrategy IdResetStrategy { get; set; } = ObjectIdResetStrategy.Manual;
public MissingObjectStrategy MissingObjectStrategy { get; set; } = MissingObjectStrategy.ThrowException;
```

### **3. ❌ Проблема: Разные значения глубины загрузки**

**Было:**
```csharp
LoadAsync<TProps>(..., int depth = 10)                    // Глубина 10
LoadTreeAsync<TProps>(..., int maxDepth = 10)             // Глубина 10  
GetDescendantsAsync<TProps>(..., int maxDepth = 50)       // Глубина 50 (!)
```

**✅ Решение:**
```csharp
// Единые настройки глубины
public int DefaultLoadDepth { get; set; } = 10;
public int DefaultMaxTreeDepth { get; set; } = 50;
```

## 🏗️ **Архитектура системы конфигурации**

### **1. Основные компоненты**

```csharp
// 🎯 Главный класс конфигурации
RedbServiceConfiguration

// 🏗️ Builder для программной настройки  
RedbServiceConfigurationBuilder

// 📋 Предопределенные конфигурации
PredefinedConfigurations

// ✅ Валидация настроек
ConfigurationValidator

// 🔄 JSON конвертеры для enum'ов
JsonConverters

// 🔌 Extension методы для IConfiguration
ConfigurationExtensions

// 🏭 DI регистрация
ServiceCollectionExtensions
```

### **2. Стратегии обработки удаленных объектов**

```csharp
/// <summary>
/// Стратегия обработки ID объекта после удаления
/// </summary>
public enum ObjectIdResetStrategy
{
    /// <summary>
    /// Ручной сброс ID (текущее поведение)
    /// Пользователь должен сам установить obj.id = 0
    /// </summary>
    Manual,
    
    /// <summary>
    /// Автоматический сброс ID при удалении через DeleteAsync(RedbObject)
    /// После удаления obj.id автоматически становится 0
    /// </summary>
    AutoResetOnDelete,
    
    /// <summary>
    /// Автоматическое создание нового объекта при попытке сохранить удаленный
    /// SaveAsync автоматически переключается на INSERT если объект не найден
    /// </summary>
    AutoCreateNewOnSave
}

/// <summary>
/// Стратегия обработки несуществующих объектов при UPDATE
/// </summary>
public enum MissingObjectStrategy
{
    /// <summary>
    /// Выбросить исключение (текущее поведение)
    /// </summary>
    ThrowException,
    
    /// <summary>
    /// Автоматически переключиться на INSERT
    /// </summary>
    AutoSwitchToInsert,
    
    /// <summary>
    /// Вернуть null/false без ошибки
    /// </summary>
    ReturnNull
}
```

### **3. Приоритеты контекста безопасности**

```csharp
/// <summary>
/// Приоритеты контекста безопасности
/// </summary>
public enum SecurityContextPriority
{
    /// <summary>
    /// Уровень 1: Явно указанный пользователь (высший приоритет)
    /// </summary>
    ExplicitUser = 1,
    
    /// <summary>
    /// Уровень 2: Контекст безопасности (автоматически)
    /// </summary>
    SecurityContext = 2,
    
    /// <summary>
    /// Уровень 3: Системный контекст (для системных операций)
    /// </summary>
    SystemContext = 3,
    
    /// <summary>
    /// Уровень 4: Дефолтный admin (если ничего не указано)
    /// </summary>
    DefaultAdmin = 4
}
```

## 📋 **Полная структура RedbServiceConfiguration**

```csharp
public class RedbServiceConfiguration
{
    // === НАСТРОЙКИ УДАЛЕНИЯ ОБЪЕКТОВ ===
    
    /// <summary>
    /// Стратегия обработки ID после удаления объекта
    /// </summary>
    public ObjectIdResetStrategy IdResetStrategy { get; set; } = ObjectIdResetStrategy.Manual;
    
    /// <summary>
    /// Стратегия обработки несуществующих объектов при UPDATE
    /// </summary>
    public MissingObjectStrategy MissingObjectStrategy { get; set; } = MissingObjectStrategy.ThrowException;

    // === НАСТРОЙКИ БЕЗОПАСНОСТИ ПО УМОЛЧАНИЮ ===
    
    /// <summary>
    /// Проверять права доступа по умолчанию при загрузке объектов
    /// </summary>
    public bool DefaultCheckPermissionsOnLoad { get; set; } = false;
    
    /// <summary>
    /// Проверять права доступа по умолчанию при сохранении объектов
    /// </summary>
    public bool DefaultCheckPermissionsOnSave { get; set; } = false;
    
    /// <summary>
    /// Проверять права доступа по умолчанию при удалении объектов
    /// </summary>
    public bool DefaultCheckPermissionsOnDelete { get; set; } = true;

    // === НАСТРОЙКИ СХЕМ ===
    
    /// <summary>
    /// Строго удалять лишние поля при синхронизации схем по умолчанию
    /// </summary>
    public bool DefaultStrictDeleteExtra { get; set; } = true;
    
    /// <summary>
    /// Автоматически синхронизировать схемы при сохранении объектов
    /// </summary>
    public bool AutoSyncSchemesOnSave { get; set; } = true;

    // === НАСТРОЙКИ ЗАГРУЗКИ ===
    
    /// <summary>
    /// Глубина загрузки связанных объектов по умолчанию
    /// </summary>
    public int DefaultLoadDepth { get; set; } = 10;
    
    /// <summary>
    /// Максимальная глубина дерева при работе с иерархическими структурами
    /// </summary>
    public int DefaultMaxTreeDepth { get; set; } = 50;

    // === НАСТРОЙКИ ПРОИЗВОДИТЕЛЬНОСТИ ===
    
    /// <summary>
    /// Включить кеширование метаданных схем
    /// </summary>
    public bool EnableSchemaMetadataCache { get; set; } = true;
    
    /// <summary>
    /// Время жизни кеша метаданных схем в минутах
    /// </summary>
    public int SchemaMetadataCacheLifetimeMinutes { get; set; } = 30;

    // === НАСТРОЙКИ ВАЛИДАЦИИ ===
    
    /// <summary>
    /// Включить валидацию схем при их создании/изменении
    /// </summary>
    public bool EnableSchemaValidation { get; set; } = true;
    
    /// <summary>
    /// Включить валидацию данных при сохранении объектов
    /// </summary>
    public bool EnableDataValidation { get; set; } = true;

    // === НАСТРОЙКИ АУДИТА ===
    
    /// <summary>
    /// Автоматически устанавливать дату изменения при сохранении
    /// </summary>
    public bool AutoSetModifyDate { get; set; } = true;
    
    /// <summary>
    /// Автоматически пересчитывать MD5 хеш при сохранении
    /// </summary>
    public bool AutoRecomputeHash { get; set; } = true;

    // === НАСТРОЙКИ КОНТЕКСТА БЕЗОПАСНОСТИ ===
    
    /// <summary>
    /// Приоритет контекста безопасности по умолчанию
    /// </summary>
    public SecurityContextPriority DefaultSecurityPriority { get; set; } = SecurityContextPriority.SecurityContext;
    
    /// <summary>
    /// ID системного пользователя для операций без проверки прав
    /// </summary>
    public long SystemUserId { get; set; } = 0;

    // === НАСТРОЙКИ СЕРИАЛИЗАЦИИ ===
    
    /// <summary>
    /// Настройки JSON сериализации для массивов
    /// </summary>
    public JsonSerializationOptions JsonOptions { get; set; } = new JsonSerializationOptions();
}
```

## 🚀 **Предопределенные конфигурации**

### **1. Development - Разработка**
```csharp
var config = PredefinedConfigurations.Development;
// Или через builder
var config = new RedbServiceConfigurationBuilder().ForDevelopment().Build();
```

**Особенности:**
- ❌ Отключены проверки прав (для удобства)
- ✅ Включена подробная валидация
- ✅ Автоматическое восстановление после ошибок
- ✅ Подробный JSON для отладки
- ✅ Большая глубина загрузки для полной картины

### **2. Production - Продакшн**
```csharp
var config = PredefinedConfigurations.Production;
// Или через builder
var config = new RedbServiceConfigurationBuilder().ForProduction().Build();
```

**Особенности:**
- ✅ Строгие проверки прав
- ✅ Строгая обработка ошибок
- ✅ Оптимизация производительности
- ✅ Компактный JSON
- ✅ Агрессивное кеширование

### **3. HighPerformance - Высокая производительность**
```csharp
var config = PredefinedConfigurations.HighPerformance;
// Или через builder
var config = new RedbServiceConfigurationBuilder().ForHighPerformance().Build();
```

**Особенности:**
- ❌ Отключены проверки прав
- ✅ Минимальная глубина загрузки
- ✅ Агрессивное кеширование
- ❌ Отключены тяжелые операции (пересчет хеша)

### **4. BulkOperations - Массовые операции**
```csharp
var config = PredefinedConfigurations.BulkOperations;
// Или через builder
var config = new RedbServiceConfigurationBuilder().ForBulkOperations().Build();
```

**Особенности:**
- ❌ Отключены все проверки
- ❌ Отключена валидация
- ✅ Минимальные настройки для максимальной скорости
- ✅ Автоматическое восстановление

### **5. Debug - Отладка**
```csharp
var config = PredefinedConfigurations.Debug;
// Или через builder
var config = new RedbServiceConfigurationBuilder().ForDebug().Build();
```

**Особенности:**
- ❌ Отключено кеширование
- ✅ Подробное JSON
- ✅ Максимальная глубина для анализа
- ✅ Подробная валидация

### **6. IntegrationTesting - Интеграционное тестирование**
```csharp
var config = PredefinedConfigurations.IntegrationTesting;
// Или через builder
var config = new RedbServiceConfigurationBuilder().ForIntegrationTesting().Build();
```

**Особенности:**
- ❌ Отключено кеширование (изоляция тестов)
- ✅ Строгая обработка ошибок
- ✅ Подробное JSON для анализа

### **7. DataMigration - Миграция данных**
```csharp
var config = PredefinedConfigurations.DataMigration;
// Или через builder
var config = new RedbServiceConfigurationBuilder().ForDataMigration().Build();
```

**Особенности:**
- ✅ Максимальная толерантность к ошибкам
- ❌ Отключена валидация данных (могут быть некорректные)
- ❌ Не удаляем поля при миграции
- ✅ Системный приоритет

## 🔧 **Способы настройки конфигурации**

### **1. Через appsettings.json**

```json
{
  "RedbService": {
    "IdResetStrategy": "AutoResetOnDelete",
    "MissingObjectStrategy": "AutoSwitchToInsert",
    "DefaultCheckPermissionsOnLoad": false,
    "DefaultCheckPermissionsOnSave": false,
    "DefaultCheckPermissionsOnDelete": true,
    "DefaultLoadDepth": 10,
    "DefaultMaxTreeDepth": 50,
    "EnableSchemaMetadataCache": true,
    "SchemaMetadataCacheLifetimeMinutes": 30,
    "EnableSchemaValidation": true,
    "EnableDataValidation": true,
    "AutoSetModifyDate": true,
    "AutoRecomputeHash": true,
    "DefaultSecurityPriority": "SecurityContext",
    "SystemUserId": 0,
    "JsonOptions": {
      "WriteIndented": false,
      "UseUnsafeRelaxedJsonEscaping": true
    }
  }
}
```

### **2. Через профили в appsettings.json**

```json
{
  "RedbService": {
    "Profile": "Development",
    "Overrides": {
      "DefaultLoadDepth": 15,
      "EnableSchemaMetadataCache": false,
      "JsonOptions": {
        "WriteIndented": true
      }
    }
  }
}
```

### **3. Через переменные окружения**

```bash
# Переопределение через environment variables
REDBSERVICE__DEFAULTCHECKPERMISSIONSONLOAD=true
REDBSERVICE__DEFAULTLOADDEPTH=5
REDBSERVICE__ENABLESCHEMAMETADATACACHE=false
REDBSERVICE__JSONOPTIONS__WRITEINDENTED=true
```

### **4. Через Fluent API (Builder Pattern)**

```csharp
var config = new RedbServiceConfigurationBuilder()
    .WithIdResetStrategy(ObjectIdResetStrategy.AutoResetOnDelete)
    .WithMissingObjectStrategy(MissingObjectStrategy.AutoSwitchToInsert)
    .WithStrictSecurity()
    .WithLoadDepth(5)
    .WithMetadataCache(enabled: true, lifetimeMinutes: 60)
    .WithPrettyJson()
    .Build();
```

### **5. Через предопределенные конфигурации**

```csharp
// Готовые конфигурации
var devConfig = PredefinedConfigurations.Development;
var prodConfig = PredefinedConfigurations.Production;
var perfConfig = PredefinedConfigurations.HighPerformance;

// С дополнительной настройкой
var customConfig = new RedbServiceConfigurationBuilder(PredefinedConfigurations.Production)
    .WithLoadDepth(3)
    .WithoutCache()
    .Build();
```

## 🏭 **Регистрация в DI контейнере**

### **1. Базовая регистрация**

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Автоматическая загрузка из appsettings.json
builder.Services.AddRedbServiceConfiguration(builder.Configuration);

var app = builder.Build();
```

### **2. С валидацией**

```csharp
// С валидацией при регистрации
builder.Services.AddValidatedRedbServiceConfiguration(
    builder.Configuration, 
    throwOnValidationError: true);
```

### **3. Комбинированная настройка**

```csharp
// appsettings.json + программная настройка
builder.Services.AddRedbServiceConfiguration(builder.Configuration, configBuilder =>
{
    configBuilder.WithLoadDepth(5)
                 .WithStrictSecurity();
});
```

### **4. Только программная настройка**

```csharp
// Только через builder
builder.Services.AddRedbServiceConfiguration(configBuilder =>
{
    configBuilder.ForProduction()
                 .WithLoadDepth(3)
                 .WithMetadataCache(enabled: true, lifetimeMinutes: 120);
});
```

### **5. По имени профиля**

```csharp
// Использование профиля
builder.Services.AddRedbServiceConfiguration("Production");

// Профиль + дополнительная настройка
builder.Services.AddRedbServiceConfiguration("Development", configBuilder =>
{
    configBuilder.WithoutCache()
                 .WithPrettyJson();
});
```

### **6. Hot-reload конфигурации**

```csharp
// Мониторинг изменений конфигурации
builder.Services.AddRedbServiceConfigurationMonitoring(builder.Configuration);

// Использование в сервисе
public class MyService
{
    private readonly IRedbServiceConfigurationMonitor _configMonitor;
    
    public MyService(IRedbServiceConfigurationMonitor configMonitor)
    {
        _configMonitor = configMonitor;
        _configMonitor.ConfigurationChanged += OnConfigChanged;
    }
    
    private void OnConfigChanged(RedbServiceConfiguration newConfig)
    {
        // Реакция на изменение конфигурации
        Console.WriteLine($"Configuration updated: {newConfig.GetDescription()}");
    }
}
```

## 🔍 **Использование в коде**

### **1. Через IOptions (рекомендуется)**

```csharp
public class MyService
{
    private readonly RedbServiceConfiguration _config;
    
    public MyService(IOptions<RedbServiceConfiguration> options)
    {
        _config = options.Value;
    }
    
    public async Task DoSomething()
    {
        // Используем настройки из конфигурации
        var loadDepth = _config.DefaultLoadDepth;
        var checkPermissions = _config.DefaultCheckPermissionsOnLoad;
    }
}
```

### **2. Через IOptionsMonitor (с hot-reload)**

```csharp
public class MyService
{
    private readonly IOptionsMonitor<RedbServiceConfiguration> _configMonitor;
    
    public MyService(IOptionsMonitor<RedbServiceConfiguration> configMonitor)
    {
        _configMonitor = configMonitor;
    }
    
    public async Task DoSomething()
    {
        // Всегда актуальная конфигурация
        var currentConfig = _configMonitor.CurrentValue;
        var loadDepth = currentConfig.DefaultLoadDepth;
    }
}
```

### **3. Прямое внедрение**

```csharp
public class MyService
{
    private readonly RedbServiceConfiguration _config;
    
    public MyService(RedbServiceConfiguration config)
    {
        _config = config;
    }
}
```

## 🛠️ **Решение проблемы с удаленными объектами**

### **❌ Проблема:**
```csharp
var obj = new RedbObject<TestProps> { name = "Test" };
var id = await redb.SaveAsync(obj);           // obj.id = 12345
await redb.DeleteAsync(obj);                  // Удаляем из БД
await redb.SaveAsync(obj);                    // ❌ ОШИБКА! obj.id = 12345, но объекта нет
```

### **✅ Решение через конфигурацию:**

#### **Стратегия 1: AutoResetOnDelete**
```json
{
  "RedbService": {
    "IdResetStrategy": "AutoResetOnDelete"
  }
}
```
```csharp
await redb.DeleteAsync(obj);                  // obj.id автоматически = 0
await redb.SaveAsync(obj);                    // ✅ Создается новый объект
```

#### **Стратегия 2: AutoSwitchToInsert**
```json
{
  "RedbService": {
    "MissingObjectStrategy": "AutoSwitchToInsert"
  }
}
```
```csharp
await redb.SaveAsync(obj);                    // ✅ Автоматически создается новый объект
```

#### **Стратегия 3: Комбинированная**
```json
{
  "RedbService": {
    "IdResetStrategy": "AutoResetOnDelete",
    "MissingObjectStrategy": "AutoSwitchToInsert"
  }
}
```

## ✅ **Валидация конфигурации**

### **1. Автоматическая валидация**

```csharp
// Валидация при регистрации
builder.Services.AddValidatedRedbServiceConfiguration(
    builder.Configuration, 
    throwOnValidationError: true);
```

### **2. Валидация для конкретных сценариев**

```csharp
// Валидация для продакшена
builder.Services.AddSingleton<IValidateOptions<RedbServiceConfiguration>>(
    new ScenarioBasedConfigurationValidator(ConfigurationScenario.Production));
```

### **3. Валидация с автоисправлением**

```csharp
// Автоматическое исправление критических ошибок
builder.Services.AddSingleton<IValidateOptions<RedbServiceConfiguration>>(
    new RedbServiceConfigurationValidatorWithAutoFix(autoFixCriticalErrors: true));
```

### **4. Ручная валидация**

```csharp
var config = configuration.GetRedbServiceConfiguration();
var validationResult = ConfigurationValidator.Validate(config);

if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"Error: {error.PropertyName} - {error.Message}");
    }
    
    // Автоисправление критических ошибок
    if (validationResult.HasCriticalErrors)
    {
        config = ConfigurationValidator.FixCriticalErrors(config);
    }
}
```

## 🌍 **Конфигурация для разных сред**

### **appsettings.json (базовая)**
```json
{
  "RedbService": {
    "Profile": "Development"
  }
}
```

### **appsettings.Production.json**
```json
{
  "RedbService": {
    "Profile": "Production",
    "Overrides": {
      "DefaultLoadDepth": 3,
      "SchemaMetadataCacheLifetimeMinutes": 120
    }
  }
}
```

### **appsettings.Development.json**
```json
{
  "RedbService": {
    "Profile": "Development",
    "Overrides": {
      "EnableSchemaMetadataCache": false,
      "JsonOptions": {
        "WriteIndented": true
      }
    }
  }
}
```

## 🎉 **Преимущества системы конфигурации**

1. **🔧 Гибкость**: Разные настройки для разных сценариев
2. **🛡️ Безопасность**: Единые настройки безопасности по умолчанию  
3. **⚡ Производительность**: Оптимизация под конкретные задачи
4. **🐛 Отладка**: Упрощенные настройки для разработки
5. **📈 Масштабируемость**: Легкое добавление новых настроек
6. **🔄 Hot-reload**: Изменения конфигурации без перезапуска
7. **✅ Валидация**: Проверка настроек при старте
8. **📚 Документация**: Подробные примеры и руководства
9. **🏭 Enterprise-ready**: Интеграция с стандартными .NET подходами
10. **🎯 DevOps-friendly**: Настройки без перекомпиляции

## 🚀 **Заключение**

Система конфигурации RedbService превращает его в полноценное enterprise решение с поддержкой всех современных подходов к конфигурированию в .NET экосистеме. Теперь REDB готов к использованию в любых сценариях - от разработки до высоконагруженного продакшена! 🎯
