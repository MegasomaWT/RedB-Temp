# 🔧 Конфигурация RedbService через IConfiguration

Полная интеграция системы конфигурации RedbService с Microsoft.Extensions.Configuration для поддержки `appsettings.json`, переменных окружения и других источников конфигурации.

## 🚀 Быстрый старт

### 1. Базовая настройка в Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Автоматическая загрузка из appsettings.json
builder.Services.AddRedbServiceConfiguration(builder.Configuration);

var app = builder.Build();
```

### 2. Базовый appsettings.json

```json
{
  "RedbService": {
    "DefaultCheckPermissionsOnLoad": false,
    "DefaultCheckPermissionsOnSave": false,
    "DefaultCheckPermissionsOnDelete": true,
    "DefaultLoadDepth": 10,
    "EnableSchemaMetadataCache": true
  }
}
```

## 📋 Способы конфигурации

### 1. Использование предопределенных профилей

```json
{
  "RedbService": {
    "Profile": "Development",
    "Overrides": {
      "DefaultLoadDepth": 15,
      "EnableSchemaMetadataCache": false
    }
  }
}
```

**Доступные профили:**
- `Development` - для разработки
- `Production` - для продакшена
- `HighPerformance` - для высокой производительности
- `BulkOperations` - для массовых операций
- `Debug` - для отладки
- `IntegrationTesting` - для тестирования
- `DataMigration` - для миграции данных

### 2. Полная конфигурация

```json
{
  "RedbService": {
    "IdResetStrategy": "AutoCreateNewOnSave",
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
    "DefaultSecurityPriority": "Context",
    "SystemUserId": 0,
    "JsonOptions": {
      "WriteIndented": false,
      "UseUnsafeRelaxedJsonEscaping": true
    }
  }
}
```

### 3. Переменные окружения

```bash
# Переопределение через environment variables
REDBSERVICE__DEFAULTCHECKPERMISSIONSONLOAD=true
REDBSERVICE__DEFAULTLOADDEPTH=5
REDBSERVICE__ENABLESCHEMAMETADATACACHE=false
REDBSERVICE__JSONOPTIONS__WRITEINDENTED=true
```

## 🛠️ Методы регистрации в DI

### 1. Базовая регистрация

```csharp
// Загрузка из appsettings.json
services.AddRedbServiceConfiguration(configuration);

// С валидацией
services.AddValidatedRedbServiceConfiguration(configuration);
```

### 2. Комбинированная настройка

```csharp
// appsettings.json + программная настройка
services.AddRedbServiceConfiguration(configuration, builder =>
{
    builder.WithLoadDepth(5)
           .WithStrictSecurity();
});
```

### 3. Предопределенные профили

```csharp
// Использование профиля
services.AddRedbServiceConfiguration("Production");

// Профиль + дополнительная настройка
services.AddRedbServiceConfiguration("Development", builder =>
{
    builder.WithoutCache()
           .WithPrettyJson();
});
```

### 4. Программная конфигурация

```csharp
// Только через builder
services.AddRedbServiceConfiguration(builder =>
{
    builder.ForProduction()
           .WithLoadDepth(3)
           .WithMetadataCache(enabled: true, lifetimeMinutes: 120);
});
```

### 5. Мониторинг изменений

```csharp
// Hot-reload конфигурации
services.AddRedbServiceConfigurationMonitoring(configuration);

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
    }
}
```

## 🔍 Валидация конфигурации

### 1. Автоматическая валидация

```csharp
// Валидация при регистрации
services.AddValidatedRedbServiceConfiguration(configuration, throwOnValidationError: true);
```

### 2. Валидация для конкретных сценариев

```csharp
// Валидация для продакшена
services.AddSingleton<IValidateOptions<RedbServiceConfiguration>>(
    new ScenarioBasedConfigurationValidator(ConfigurationScenario.Production));
```

### 3. Валидация с автоисправлением

```csharp
// Автоматическое исправление критических ошибок
services.AddSingleton<IValidateOptions<RedbServiceConfiguration>>(
    new RedbServiceConfigurationValidatorWithAutoFix(autoFixCriticalErrors: true));
```

## 🌍 Конфигурация для разных сред

### appsettings.json (базовая)
```json
{
  "RedbService": {
    "Profile": "Development"
  }
}
```

### appsettings.Production.json
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

### appsettings.Development.json
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

## 📊 Использование в коде

### 1. Через IOptions

```csharp
public class MyService
{
    private readonly RedbServiceConfiguration _config;
    
    public MyService(IOptions<RedbServiceConfiguration> options)
    {
        _config = options.Value;
    }
}
```

### 2. Через IOptionsMonitor (с hot-reload)

```csharp
public class MyService
{
    private readonly IOptionsMonitor<RedbServiceConfiguration> _configMonitor;
    
    public MyService(IOptionsMonitor<RedbServiceConfiguration> configMonitor)
    {
        _configMonitor = configMonitor;
    }
    
    public void DoSomething()
    {
        var currentConfig = _configMonitor.CurrentValue;
        // Используем актуальную конфигурацию
    }
}
```

### 3. Прямое внедрение

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

## ⚙️ Настройки конфигурации

### Стратегии обработки объектов

| Параметр | Значения | Описание |
|----------|----------|----------|
| `IdResetStrategy` | `Manual`, `AutoResetOnDelete`, `AutoCreateNewOnSave` | Стратегия сброса ID после удаления |
| `MissingObjectStrategy` | `ThrowException`, `AutoSwitchToInsert`, `ReturnNull` | Обработка несуществующих объектов |

### Настройки безопасности

| Параметр | Тип | Описание |
|----------|-----|----------|
| `DefaultCheckPermissionsOnLoad` | `bool` | Проверка прав при загрузке |
| `DefaultCheckPermissionsOnSave` | `bool` | Проверка прав при сохранении |
| `DefaultCheckPermissionsOnDelete` | `bool` | Проверка прав при удалении |
| `DefaultSecurityPriority` | `System`, `Explicit`, `Context` | Приоритет контекста безопасности |
| `SystemUserId` | `long` | ID системного пользователя |

### Настройки производительности

| Параметр | Тип | Описание |
|----------|-----|----------|
| `DefaultLoadDepth` | `int` | Глубина загрузки объектов |
| `DefaultMaxTreeDepth` | `int` | Максимальная глубина дерева |
| `EnableSchemaMetadataCache` | `bool` | Включить кеширование метаданных |
| `SchemaMetadataCacheLifetimeMinutes` | `int` | Время жизни кеша в минутах |

### Настройки валидации

| Параметр | Тип | Описание |
|----------|-----|----------|
| `EnableSchemaValidation` | `bool` | Валидация схем |
| `EnableDataValidation` | `bool` | Валидация данных |

### Настройки JSON

| Параметр | Тип | Описание |
|----------|-----|----------|
| `JsonOptions.WriteIndented` | `bool` | Форматированный JSON |
| `JsonOptions.UseUnsafeRelaxedJsonEscaping` | `bool` | Упрощенное экранирование |

## 🎯 Примеры для разных сценариев

Смотрите файл `appsettings.examples.json` для подробных примеров конфигураций для различных сценариев использования.

## 🚨 Решение проблемы с удаленными объектами

```json
{
  "RedbService": {
    "IdResetStrategy": "AutoResetOnDelete",
    "MissingObjectStrategy": "AutoSwitchToInsert"
  }
}
```

Эта конфигурация автоматически решает проблему, когда после удаления объекта из БД попытка его пересохранить вызывает ошибку.

## 🔧 Отладка конфигурации

```csharp
// Получить описание текущей конфигурации
var description = configuration.GetRedbServiceConfigurationDescription();
Console.WriteLine($"RedbService configuration: {description}");

// Проверить валидность
var config = configuration.GetRedbServiceConfiguration();
var validation = ConfigurationValidator.Validate(config);
if (!validation.IsValid)
{
    foreach (var message in validation.GetAllMessages())
    {
        Console.WriteLine(message);
    }
}
```

Интеграция с `IConfiguration` делает RedbService гораздо более гибким и удобным для enterprise использования! 🚀
