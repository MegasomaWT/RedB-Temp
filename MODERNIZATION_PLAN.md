# 🚀 ПЛАН МОДЕРНИЗАЦИИ МЕТОДОВ С ID НА REDBOBJECT

## 📋 **ОБЩИЕ ПРИНЦИПЫ:**
- ✅ **Добавляем новые методы** с `RedbObject` параметрами
- ⚠️ **Сохраняем старые методы** для обратной совместимости (помечены как `[Obsolete]`)
- 🧪 **Обновляем тесты** на новые методы
- 🎯 **Приоритет**: часто используемые методы первыми

---

## 🔴 **ЭТАП 1: IPermissionProvider (ВЫСОКИЙ ПРИОРИТЕТ)**

### **Текущие методы с ID:**
```csharp
IQueryable<long> GetReadableObjectIds(long userId);
Task<bool> CanUserEditObject(long objectId, long userId);
Task<bool> CanUserSelectObject(long objectId, long userId);
Task<bool> CanUserInsertScheme(long schemeId, long userId);
Task<bool> CanUserDeleteObject(long objectId, long userId);
```

### **Новые методы с RedbObject:**
```csharp
// 🚀 НОВЫЕ КРАСИВЫЕ МЕТОДЫ:
IQueryable<long> GetReadableObjectIds(IRedbUser user);
Task<bool> CanUserEditObject(RedbObject obj, IRedbUser user);
Task<bool> CanUserSelectObject(RedbObject obj, IRedbUser user);
Task<bool> CanUserInsertScheme(RedbObject obj, IRedbUser user); // obj.scheme_id
Task<bool> CanUserDeleteObject(RedbObject obj, IRedbUser user);

// 🎯 МЕТОДЫ С КОНТЕКСТНЫМ ПОЛЬЗОВАТЕЛЕМ:
Task<bool> CanUserEditObject(RedbObject obj); // из SecurityContext
Task<bool> CanUserSelectObject(RedbObject obj);
Task<bool> CanUserDeleteObject(RedbObject obj);
```

### **Статус:** 
✅ **ЗАВЕРШЕН** - все новые методы добавлены и работают

### **Изменения в тестах:**
- ✅ **Stage04_PermissionChecks.cs** - уже использует новые методы
- ⚠️ **НЕ МЕНЯТЬ**: Stage20_CurrentSystemAnalysis.cs - там анализ старых методов

---

## 🔴 **ЭТАП 2: IObjectStorageProvider (СРЕДНИЙ ПРИОРИТЕТ)**

### **Текущие методы с ID:**
```csharp
Task<RedbObject<TProps>> LoadAsync<TProps>(long objectId, int depth = 10, long? userId = null, bool checkPermissions = false);
Task<bool> DeleteAsync(long objectId, long userId, bool checkPermissions = true);
Task<int> DeleteSubtreeAsync(long parentId, long userId, bool checkPermissions = true);
```

### **Анализ необходимости изменений:**
- ❌ **LoadAsync** - НЕ МЕНЯТЬ! Принимает ID по назначению (загрузка по ID)
- ❌ **DeleteAsync** - НЕ МЕНЯТЬ! Низкоуровневый метод, ID корректен
- ✅ **DeleteSubtreeAsync** - МОДЕРНИЗИРОВАТЬ! Можно передать родительский объект

### **Новые методы:**
```csharp
// 🚀 НОВЫЙ КРАСИВЫЙ МЕТОД:
Task<int> DeleteSubtreeAsync(RedbObject parentObj, IRedbUser user, bool checkPermissions = true);
Task<int> DeleteSubtreeAsync(RedbObject parentObj, bool checkPermissions = true); // с контекстом

// 🎯 НИЗКОУРОВНЕВЫЙ МЕТОД С ID - ОСТАВЛЯЕМ:
// Task<int> DeleteSubtreeAsync(long parentId, long userId, bool checkPermissions = true); [Obsolete]
```

### **Статус:** 
✅ **ЗАВЕРШЕН** - новые методы DeleteSubtreeAsync с RedbObject добавлены

---

## 🔴 **ЭТАП 3: IRedbService LoadAsync (СРЕДНИЙ ПРИОРИТЕТ)**

### **Текущие методы:**
```csharp
Task<RedbObject<T>?> LoadAsync<T>(long objectId, bool checkPermissions = true);
Task<RedbObject<T>?> LoadAsync<T>(long objectId, IRedbUser explicitUser, bool checkPermissions = false);
```

### **Анализ:**
❌ **НЕ МЕНЯТЬ!** Эти методы **правильно** принимают ID - они загружают объект ПО ID.

### **Статус:**
🟢 **ПРОПУСКАЕМ** - методы корректны

---

## 🟡 **ЭТАП 4: ITreeProvider (ВЫСОКИЙ ПРИОРИТЕТ)**

### **Текущие методы с ID:**
```csharp
Task<TreeRedbObject<TProps>> LoadTreeAsync<TProps>(long rootId, ...);
Task<IEnumerable<TreeRedbObject<TProps>>> GetChildrenAsync<TProps>(long parentId, ...);
Task<IEnumerable<TreeRedbObject<TProps>>> GetPathToRootAsync<TProps>(long objectId, ...);
Task<IEnumerable<TreeRedbObject<TProps>>> GetDescendantsAsync<TProps>(long parentId, ...);
Task MoveObjectAsync(long objectId, long? newParentId, long userId, bool checkPermissions = true);
Task<long> CreateChildAsync<TProps>(TreeRedbObject<TProps> obj, long parentId, bool checkPermissions = false);
```

### **Анализ изменений:**
- ✅ **LoadTreeAsync** - МОДЕРНИЗИРОВАТЬ! Можно передать корневой объект
- ✅ **GetChildrenAsync** - МОДЕРНИЗИРОВАТЬ! Можно передать родительский объект  
- ✅ **GetPathToRootAsync** - МОДЕРНИЗИРОВАТЬ! Можно передать объект
- ✅ **GetDescendantsAsync** - МОДЕРНИЗИРОВАТЬ! Можно передать родительский объект
- ✅ **MoveObjectAsync** - МОДЕРНИЗИРОВАТЬ! Можно передать объекты
- ✅ **CreateChildAsync** - МОДЕРНИЗИРОВАТЬ! Можно передать родительский объект

### **Новые методы с объектами:**
```csharp
// 🚀 НОВЫЕ КРАСИВЫЕ МЕТОДЫ С ОБЪЕКТАМИ:
Task<TreeRedbObject<TProps>> LoadTreeAsync<TProps>(RedbObject rootObj, ...);
Task<IEnumerable<TreeRedbObject<TProps>>> GetChildrenAsync<TProps>(RedbObject parentObj, ...);
Task<IEnumerable<TreeRedbObject<TProps>>> GetPathToRootAsync<TProps>(RedbObject obj, ...);
Task<IEnumerable<TreeRedbObject<TProps>>> GetDescendantsAsync<TProps>(RedbObject parentObj, ...);
Task MoveObjectAsync(RedbObject obj, RedbObject? newParent, IRedbUser user, bool checkPermissions = true);
Task MoveObjectAsync(RedbObject obj, RedbObject? newParent, bool checkPermissions = true); // с контекстом
Task<long> CreateChildAsync<TProps>(TreeRedbObject<TProps> obj, RedbObject parentObj, bool checkPermissions = false);

// 🎯 НИЗКОУРОВНЕВЫЕ МЕТОДЫ С ID - ОСТАВЛЯЕМ ДЛЯ СОВМЕСТИМОСТИ:
// (помечаем [Obsolete] но не удаляем)
```

### **Статус:** 
✅ **ЗАВЕРШЕН** - все новые методы с RedbObject добавлены и работают

---

## 🟡 **ЭТАП 5: IQueryableProvider (НИЗКИЙ ПРИОРИТЕТ)**

### **Текущие методы:**
```csharp
IRedbQueryable<TProps> Query<TProps>(long schemeId, long? userId = null, bool checkPermissions = false);
Task<IRedbQueryable<TProps>> QueryAsync<TProps>(string? schemeName = null, long? userId = null, bool checkPermissions = false);
```

### **Анализ:**
- ❌ **Query(schemeId)** - НЕ МЕНЯТЬ! Запрос по схеме ID корректен
- ✅ **userId параметры** - МОДЕРНИЗИРОВАТЬ! Заменить на IRedbUser

### **Новые методы:**
```csharp
// 🚀 НОВЫЕ МЕТОДЫ С ПОЛЬЗОВАТЕЛЕМ:
IRedbQueryable<TProps> Query<TProps>(long schemeId, IRedbUser user, bool checkPermissions = false);
Task<IRedbQueryable<TProps>> QueryAsync<TProps>(string? schemeName = null, IRedbUser user = null, bool checkPermissions = false);

// 🎯 МЕТОДЫ С КОНТЕКСТНЫМ ПОЛЬЗОВАТЕЛЕМ:
IRedbQueryable<TProps> Query<TProps>(long schemeId, bool checkPermissions = true); // из SecurityContext
Task<IRedbQueryable<TProps>> QueryAsync<TProps>(string? schemeName = null, bool checkPermissions = true);
```

### **Статус:** 
✅ **ЗАВЕРШЕН** - все новые методы с IRedbUser добавлены и работают

---

## 🔴 **ЭТАП 6: Legacy методы IRedbService **

### **Методы низкоуровневые не!! удалению:**
```csharp

Task<T?> GetById<T>(long id) where T : class;
Task<int> DeleteById<T>(long id) where T : class;
IQueryable<T> GetAll<T>() where T : class;
```

### **Статус:** 
🟢 **ПРОПУСКАЕМ** - низкоуровневые методы оставляем

### **Замена в тестах:**
- Найти использование этих методов и заменить на новые

---

## 📋 **ПЛАН ВЫПОЛНЕНИЯ:**

### **🎯 ГЛАВНЫЙ ПРИНЦИП: API ПЕРВЫМ, ТЕСТЫ ПОТОМ!**

### **Шаг 1: Модернизация API (БЕЗ АНАЛИЗА ТЕСТОВ)**
1. ✅ **IPermissionProvider** - добавить новые методы с RedbObject
2. ✅ **IObjectStorageProvider** - добавить DeleteSubtreeAsync с RedbObject
3. ✅ **ITreeProvider** - добавить новые методы с RedbObject  
4. ✅ **IQueryableProvider** - добавить методы с IRedbUser
5. 🟢 **Удалить legacy методы** - пропускаем (низкоуровневые методы оставляем)

### **Шаг 2: Компиляция и выявление ошибок**
```bash
dotnet build
```
✅ **ЗАВЕРШЕН** - компилятор показал 37 ошибок, все исправлены!

### **Шаг 3: Исправление тестов по ошибкам компиляции**
✅ **ЗАВЕРШЕН** - исправлены неоднозначные вызовы `QueryAsync` в 5 файлах:
- Stage13_LinqQueries.cs - 3 вызова
- Stage16_AdvancedLinq.cs - 7 вызовов  
- Stage17_AdvancedLinqOperators.cs - 18 вызовов
- Stage18_SortingAndPagination.cs - 7 вызовов
- Stage19_DateTimeSorting.cs - 3 вызова

### **Шаг 4: Пометка устаревших методов**
```csharp
[Obsolete("Используйте CanUserEditObject(RedbObject obj, IRedbUser user)")]
Task<bool> CanUserEditObject(long objectId, long userId);
```

### **Шаг 5: Финальная проверка**
```bash
dotnet build
dotnet run --stages 1,2,3  # Тестируем базовые функции
```

---

## 🎯 **ОЖИДАЕМЫЙ РЕЗУЛЬТАТ:**

### **До модернизации:**
```csharp
// ❌ СТАРЫЙ КОД:
var canEdit = await redb.CanUserEditObject(1021, 12345);
await redb.MoveObjectAsync(1021, 2000, 12345, true);
var query = redb.Query<Product>(1001, 12345, true);
```

### **После модернизации:**
```csharp
// ✅ НОВЫЙ КРАСИВЫЙ КОД:
var obj = await redb.LoadAsync<Product>(1021);
var canEdit = await redb.CanUserEditObject(obj); // из контекста
await redb.MoveObjectAsync(obj, parentObj); // красиво!
var query = redb.Query<Product>(1001, true); // из контекста
```

---

## ⚠️ **ВАЖНЫЕ ЗАМЕЧАНИЯ:**

1. **НЕ МЕНЯТЬ методы, которые логически работают с ID** (LoadAsync, GetChildrenAsync и т.д.)
2. **СОХРАНИТЬ обратную совместимость** - старые методы помечаем `[Obsolete]`
3. **ТЕСТЫ ОБНОВЛЯТЬ** только там, где есть реальное улучшение
4. **Stage20** НЕ ТРОГАТЬ - там анализ старой архитектуры
5. **Приоритет**: сначала часто используемые методы

**Готов начать реализацию по этому плану?** 🚀
