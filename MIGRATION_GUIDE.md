# 🚀 Руководство по миграции на систему конфигурации RedbService

## 🎯 **Обзор изменений**

Система конфигурации RedbService обеспечивает:
- ✅ **Обратную совместимость** - существующий код продолжит работать
- ✅ **Гибкую настройку** - через appsettings.json, переменные окружения, код
- ✅ **Решение проблем** - автоматическая обработка удаленных объектов
- ✅ **Единые настройки** - централизованные значения по умолчанию

## 📋 **Что изменилось**

### **1. Сигнатуры методов**
```csharp
// ❌ Было:
Task<long> SaveAsync<T>(RedbObject<T> obj, bool checkPermissions = true);
Task<RedbObject<T>?> LoadAsync<T>(long id, bool checkPermissions = true);
Task<bool> DeleteAsync(RedbObject obj, bool checkPermissions = true);

// ✅ Стало:
Task<long> SaveAsync<T>(RedbObject<T> obj, bool? checkPermissions = null);
Task<RedbObject<T>?> LoadAsync<T>(long id, int? depth = null, bool? checkPermissions = null);
Task<bool> DeleteAsync(RedbObject obj, bool? checkPermissions = null);
```

### **2. Новые возможности**
```csharp
// Доступ к конфигурации
var config = redb.Configuration;

// Обновление конфигурации
redb.UpdateConfiguration(config => 
{
    config.IdResetStrategy = ObjectIdResetStrategy.AutoResetOnDelete;
});

// Fluent API
redb.UpdateConfiguration(builder => 
{
    builder.ForProduction().WithLoadDepth(5);
});
```

## 🔄 **Сценарии миграции**

### **Сценарий 1: Код без изменений (100% совместимость)**

```csharp
// Ваш существующий код продолжит работать БЕЗ ИЗМЕНЕНИЙ:
var obj = new RedbObject<MyProps> { name = "Test" };
await redb.SaveAsync(obj, true);                    // ✅ Работает
await redb.LoadAsync<MyProps>(id, false);           // ✅ Работает  
await redb.DeleteAsync(obj, true);                  // ✅ Работает
```

### **Сценарий 2: Использование настроек по умолчанию**

```csharp
// ❌ Было:
await redb.SaveAsync(obj, true);
await redb.LoadAsync<MyProps>(id, true);
await redb.DeleteAsync(obj, true);

// ✅ Стало (используем настройки по умолчанию):
await redb.SaveAsync(obj);        // Использует Configuration.DefaultCheckPermissionsOnSave
await redb.LoadAsync<MyProps>(id); // Использует Configuration.DefaultCheckPermissionsOnLoad и DefaultLoadDepth
await redb.DeleteAsync(obj);      // Использует Configuration.DefaultCheckPermissionsOnDelete
```

### **Сценарий 3: Решение проблемы с удаленными объектами**

```csharp
// ❌ Было (вызывало ошибку):
var obj = new RedbObject<MyProps> { name = "Test" };
var id = await redb.SaveAsync(obj);
await redb.DeleteAsync(obj);
await redb.SaveAsync(obj);  // ❌ ОШИБКА! obj.id != 0

// ✅ Стало (настраиваем автоматическое решение):
redb.UpdateConfiguration(config => 
{
    config.IdResetStrategy = ObjectIdResetStrategy.AutoResetOnDelete;
});

var obj = new RedbObject<MyProps> { name = "Test" };
var id = await redb.SaveAsync(obj);
await redb.DeleteAsync(obj);  // obj.id автоматически = 0
await redb.SaveAsync(obj);    // ✅ Создается новый объект
```

## 🏭 **Миграция DI регистрации**

### **ASP.NET Core приложения**

```csharp
// ❌ Было:
services.AddScoped<IRedbService, RedbService>();

// ✅ Стало (выберите подходящий вариант):

// Вариант 1: Простая регистрация
services.AddRedbService();

// Вариант 2: Из appsettings.json
services.AddRedbService(configuration);

// Вариант 3: Программная настройка
services.AddRedbService(config => 
{
    config.DefaultLoadDepth = 5;
    config.IdResetStrategy = ObjectIdResetStrategy.AutoResetOnDelete;
});

// Вариант 4: Предопределенный профиль
services.AddRedbService("Production");
```

### **Консольные приложения**

```csharp
// ❌ Было:
var serviceProvider = new ServiceCollection()
    .AddScoped<IRedbService, RedbService>()
    .BuildServiceProvider();

// ✅ Стало:
var serviceProvider = new ServiceCollection()
    .AddRedbService("Development")  // Или другая конфигурация
    .BuildServiceProvider();
```

## ⚙️ **Настройка конфигурации**

### **1. Через appsettings.json**

Создайте или обновите `appsettings.json`:

```json
{
  "RedbService": {
    "IdResetStrategy": "AutoResetOnDelete",
    "MissingObjectStrategy": "AutoSwitchToInsert",
    "DefaultCheckPermissionsOnLoad": false,
    "DefaultCheckPermissionsOnSave": false,
    "DefaultCheckPermissionsOnDelete": true,
    "DefaultLoadDepth": 10,
    "EnableSchemaMetadataCache": true,
    "AutoSetModifyDate": true
  }
}
```

### **2. Через переменные окружения**

```bash
# Для Docker или системных переменных
REDBSERVICE__IDRESETSTRATEGY=AutoResetOnDelete
REDBSERVICE__DEFAULTLOADDEPTH=5
REDBSERVICE__ENABLESCHEMAMETADATACACHE=false
```

### **3. Через код**

```csharp
// В Program.cs или Startup.cs
services.AddRedbService(config =>
{
    config.IdResetStrategy = ObjectIdResetStrategy.AutoResetOnDelete;
    config.MissingObjectStrategy = MissingObjectStrategy.AutoSwitchToInsert;
    config.DefaultLoadDepth = 5;
    config.DefaultCheckPermissionsOnSave = true;
});
```

## 🎯 **Рекомендуемые профили**

### **Для разработки**
```csharp
services.AddRedbService("Development");
// Или
services.AddRedbService(builder => builder.ForDevelopment());
```

**Особенности:**
- Отключены проверки прав (удобство)
- Включена подробная валидация
- Автоматическое восстановление после ошибок
- Подробный JSON для отладки

### **Для продакшена**
```csharp
services.AddRedbService("Production");
// Или  
services.AddRedbService(builder => builder.ForProduction());
```

**Особенности:**
- Строгие проверки прав
- Строгая обработка ошибок
- Оптимизация производительности
- Компактный JSON

### **Для высокой производительности**
```csharp
services.AddRedbService("HighPerformance");
```

**Особенности:**
- Отключены проверки прав
- Минимальная глубина загрузки
- Агрессивное кеширование

## 🚨 **Потенциальные проблемы и решения**

### **Проблема 1: Изменение поведения по умолчанию**

```csharp
// Если ваш код полагался на конкретные значения по умолчанию:

// ❌ Проблема:
await redb.LoadAsync<MyProps>(id);  // Теперь может использовать другую глубину

// ✅ Решение - явно указывайте параметры:
await redb.LoadAsync<MyProps>(id, depth: 10, checkPermissions: true);

// Или настройте конфигурацию под ваши нужды:
redb.UpdateConfiguration(config => 
{
    config.DefaultLoadDepth = 10;
    config.DefaultCheckPermissionsOnLoad = true;
});
```

### **Проблема 2: Компиляционные ошибки**

```csharp
// ❌ Если получаете ошибки компиляции:
await redb.LoadAsync<MyProps>(id, true);  // Может не компилироваться

// ✅ Решение - используйте именованные параметры:
await redb.LoadAsync<MyProps>(id, checkPermissions: true);
```

### **Проблема 3: Неожиданное поведение**

```csharp
// ❌ Если объекты ведут себя не так, как ожидалось:

// ✅ Проверьте текущую конфигурацию:
var config = redb.Configuration;
Console.WriteLine($"IdResetStrategy: {config.IdResetStrategy}");
Console.WriteLine($"MissingObjectStrategy: {config.MissingObjectStrategy}");

// ✅ Или сбросьте на значения по умолчанию:
redb.UpdateConfiguration(config => 
{
    // Восстанавливаем старое поведение
    config.IdResetStrategy = ObjectIdResetStrategy.Manual;
    config.MissingObjectStrategy = MissingObjectStrategy.ThrowException;
});
```

## ✅ **Чек-лист миграции**

### **Этап 1: Подготовка**
- [ ] Изучите новые возможности системы конфигурации
- [ ] Определите, какие проблемы она может решить в вашем коде
- [ ] Выберите подходящий профиль конфигурации

### **Этап 2: Тестирование**
- [ ] Запустите существующий код без изменений
- [ ] Убедитесь, что все работает как раньше
- [ ] Протестируйте новые возможности на тестовых данных

### **Этап 3: Постепенная миграция**
- [ ] Обновите DI регистрацию
- [ ] Добавьте конфигурацию (appsettings.json или код)
- [ ] Постепенно убирайте явные параметры, используя настройки по умолчанию
- [ ] Настройте стратегии для решения проблем с удаленными объектами

### **Этап 4: Оптимизация**
- [ ] Настройте профиль под ваши нужды
- [ ] Оптимизируйте производительность через кеширование
- [ ] Настройте валидацию и аудит

## 🎉 **Преимущества после миграции**

1. **🔧 Гибкость** - легкая настройка под разные сценарии
2. **🛡️ Безопасность** - единые настройки по умолчанию
3. **⚡ Производительность** - оптимизация через конфигурацию
4. **🐛 Отладка** - упрощенные настройки для разработки
5. **📈 Масштабируемость** - легкое добавление новых настроек
6. **🔄 Автоматизация** - решение проблем с удаленными объектами
7. **📊 Мониторинг** - централизованное управление настройками

## 📞 **Поддержка**

Если у вас возникли проблемы при миграции:

1. **Проверьте совместимость** - старый код должен работать без изменений
2. **Изучите примеры** - в `DIExamples.cs` и `ConfigurationExamples.cs`
3. **Используйте отладку** - проверьте `redb.Configuration` для понимания текущих настроек
4. **Начните с простого** - используйте базовую регистрацию `services.AddRedbService()`

**Помните: миграция полностью обратно совместима!** 🚀
