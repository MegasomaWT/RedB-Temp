using redb.Core.Providers;
using redb.Core.DBModels;
using redb.Core.Utils;
using redb.Core.Serialization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using redb.Core.Models.Contracts;
using redb.Core.Models.Entities;
using redb.Core.Models.Configuration;


namespace redb.Core.Postgres.Providers
{
    /// <summary>
    /// PostgreSQL реализация провайдера для работы с объектами
    /// </summary>
    public class PostgresObjectStorageProvider : IObjectStorageProvider
    {
        private readonly RedbContext _context;
        private readonly IRedbObjectSerializer _serializer;
        private readonly IPermissionProvider _permissionProvider;
        private readonly IRedbSecurityContext _securityContext;
        private readonly ISchemeSyncProvider _schemeSync;
        private readonly RedbServiceConfiguration _configuration;

        public PostgresObjectStorageProvider(
            RedbContext context, 
            IRedbObjectSerializer serializer,
            IPermissionProvider permissionProvider,
            IRedbSecurityContext securityContext,
            ISchemeSyncProvider schemeSync,
            RedbServiceConfiguration configuration)
        {
            _context = context;
            _serializer = serializer;
            _permissionProvider = permissionProvider;
            _securityContext = securityContext;
            _schemeSync = schemeSync;
            _configuration = configuration ?? new RedbServiceConfiguration();
        }

        // ===== БАЗОВЫЕ МЕТОДЫ (используют _securityContext и конфигурацию) =====
        
        /// <summary>
        /// Загрузить объект из EAV по ID (использует _securityContext и config.DefaultCheckPermissionsOnLoad)
        /// </summary>
        public async Task<RedbObject<TProps>> LoadAsync<TProps>(long objectId, int depth = 10) where TProps : class, new()
        {
            var effectiveUser = _securityContext.GetEffectiveUser();
            return await LoadAsync<TProps>(objectId, effectiveUser, depth);
        }

        /// <summary>
        /// Загрузить объект из EAV (использует _securityContext и config.DefaultCheckPermissionsOnLoad)
        /// </summary>
        public async Task<RedbObject<TProps>> LoadAsync<TProps>(IRedbObject obj, int depth = 10) where TProps : class, new()
        {
            var effectiveUser = _securityContext.GetEffectiveUser();
            return await LoadAsync<TProps>(obj.Id, effectiveUser, depth);
        }

        /// <summary>
        /// Загрузить объект из EAV с явно указанным пользователем (использует config.DefaultCheckPermissionsOnLoad)
        /// </summary>
        public async Task<RedbObject<TProps>> LoadAsync<TProps>(IRedbObject obj, IRedbUser user, int depth = 10) where TProps : class, new()
        {
            return await LoadAsync<TProps>(obj.Id, user, depth);
        }

        // ===== ПЕРЕГРУЗКИ С ЯВНЫМ ПОЛЬЗОВАТЕЛЕМ =====
        
        /// <summary>
        /// ОСНОВНОЙ МЕТОД загрузки - все остальные LoadAsync его вызывают
        /// </summary>
        public async Task<RedbObject<TProps>> LoadAsync<TProps>(long objectId, IRedbUser user, int depth = 10) where TProps : class, new()
        {
            // Проверка прав доступа по конфигурации
            if (_configuration.DefaultCheckPermissionsOnLoad)
            {
                var canRead = await _permissionProvider.CanUserSelectObject(objectId, user.Id);
                if (!canRead)
                {
                    throw new UnauthorizedAccessException($"Пользователь {user.Id} не имеет прав на чтение объекта {objectId}");
                }
            }

            // Выполнение PostgreSQL функции get_object_json напрямую как строка
            var json = await _context.Database.SqlQueryRaw<string>(
                "SELECT get_object_json({0}, {1})::text AS \"Value\"", objectId, depth
            ).FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(json))
            {
                throw new InvalidOperationException($"Объект с ID {objectId} не найден");
            }

            // Десериализация JSON в RedbObject<TProps> через сериализатор
            return _serializer.Deserialize<TProps>(json);
        }


        public async Task<long> SaveAsync<TProps>(IRedbObject<TProps> obj) where TProps : class, new()
        {
            var effectiveUser = _securityContext.GetEffectiveUser();
            return await SaveAsync(obj, effectiveUser);
        }

        
        public async Task<bool> DeleteAsync<TProps>(IRedbObject<TProps> obj) where TProps : class, new()
        {
            var effectiveUser = _securityContext.GetEffectiveUser();
            return await DeleteAsync(obj, effectiveUser);
        }

        public async Task<bool> DeleteAsync<TProps>(IRedbObject<TProps> obj, IRedbUser user) where TProps : class, new()
        {
            // Проверка прав доступа по конфигурации
            if (_configuration.DefaultCheckPermissionsOnDelete)
            {
                var canDelete = await _permissionProvider.CanUserDeleteObject(obj, user);
                if (!canDelete)
                {
                    throw new UnauthorizedAccessException($"Пользователь {user.Id} не имеет прав на удаление объекта {obj.Id}");
                }
            }

            var objToDelete = await _context.Objects.FindAsync(obj.Id);
            if (objToDelete == null)
            {
                return false;
            }

            _context.Objects.Remove(objToDelete);
            await _context.SaveChangesAsync();

            // === СТРАТЕГИЯ СБРОСА ID ===
            if (_configuration.IdResetStrategy == Models.Configuration.ObjectIdResetStrategy.AutoResetOnDelete)
            {
                obj.ResetId(); // Автоматически сбрасываем ID
            }

            return true;
        }


        public async Task<long> SaveAsync<TProps>(IRedbObject<TProps> obj, IRedbUser user) where TProps : class, new()
        {
            if (obj.properties == null)
            {
                throw new ArgumentException("Свойства объекта не могут быть null", nameof(obj));
            }

            // === СТРАТЕГИИ ОБРАБОТКИ УДАЛЕННЫХ ОБЪЕКТОВ (как было в RedbService) ===
            var isNewObject = obj.Id == 0;
            if (!isNewObject)
            {
                // Проверяем, существует ли объект (быстрая проверка без загрузки данных)
                var exists = await _context.Objects.AnyAsync(o => o.Id == obj.Id);
                
                if (!exists)
                {
                    // Применяем стратегию обработки несуществующих объектов
                    switch (_configuration.MissingObjectStrategy)
                    {
                        case MissingObjectStrategy.AutoSwitchToInsert:
                            isNewObject=!isNewObject;
                            break;
                        case MissingObjectStrategy.ReturnNull:
                            return 0;
                        case MissingObjectStrategy.ThrowException:
                        default:
                            throw new InvalidOperationException($"Object with id {obj.Id} not found. Current strategy: {_configuration.MissingObjectStrategy}");
                    }
                }
            }

            // 🚀 АВТООПРЕДЕЛЕНИЕ СХЕМЫ: Если scheme_id = 0, определяем по имени класса автоматически
            if (obj.SchemeId == 0 && _configuration.AutoSyncSchemesOnSave)
            {
                // Используем упрощенный метод с автоопределением имени и алиаса из атрибута
                var scheme = await _schemeSync.SyncSchemeAsync<TProps>();
                obj.SchemeId = scheme.Id;
            }

            // Проверка прав доступа по конфигурации
            if (_configuration.DefaultCheckPermissionsOnSave)
            {
                if (isNewObject)
                {
                    // Для проверки прав на создание нужно получить схему
                    var scheme = await _context.Schemes.FindAsync(obj.SchemeId);
                    if (scheme != null)
                    {
                        var schemeContract = RedbScheme.FromEntity(scheme);
                        var canInsert = await _permissionProvider.CanUserInsertScheme(schemeContract, user);
                        if (!canInsert)
                        {
                            throw new UnauthorizedAccessException($"Пользователь {user.Id} не имеет прав на создание объектов в схеме {obj.SchemeId}");
                        }
                    }
                }
                else
                {
                    // Для проверки прав на редактирование используем входящий объект напрямую
                    var canUpdate = await _permissionProvider.CanUserEditObject(obj, user);
                    if (!canUpdate)
                    {
                        throw new UnauthorizedAccessException($"Пользователь {user.Id} не имеет прав на изменение объекта {obj.Id}");
                    }
                }
            }

            if (isNewObject)
            {
                // INSERT
                // 🎯 Генерируем ID только если его еще нет (TreeProvider мог уже установить)
                if (obj.Id == 0)
                {
                    obj.Id = _context.GetNextKey();
                }
                
                // === ПРИМЕНЕНИЕ НАСТРОЕК АУДИТА (как было в RedbService) ===
                obj.OwnerId = user.Id;  // Устанавливаем владельца
                obj.WhoChangeId = user.Id;  // Устанавливаем кто изменил
                
                if (_configuration.AutoSetModifyDate)
                {
                    obj.DateCreate = DateTime.Now;
                    obj.DateModify = DateTime.Now;
                }

                // === ПРИМЕНЕНИЕ НАСТРОЕК ХЕШИРОВАНИЯ ===
                if (_configuration.AutoRecomputeHash)
                {
                    obj.RecomputeHashForType();
                }

                // Создание записи в _objects
                var objectRecord = new _RObject
                {
                    Id = obj.Id,
                    IdParent = obj.ParentId,
                    IdScheme = obj.SchemeId,
                    IdOwner = obj.OwnerId,
                    IdWhoChange = obj.WhoChangeId,
                    DateCreate = obj.DateCreate,
                    DateModify = obj.DateModify,
                    DateBegin = obj.DateBegin,
                    DateComplete = obj.DateComplete,
                    Key = obj.Key,
                    CodeInt = obj.CodeInt,
                    CodeString = obj.CodeString,
                    CodeGuid = obj.CodeGuid,
                    Name = obj.Name,
                    Note = obj.Note,
                    Bool = obj.Bool,
                    Hash = obj.Hash
                };

                _context.Objects.Add(objectRecord);
            }
            else
            {
                // UPDATE
                var existingObject = await _context.Objects.FindAsync(obj.Id);
                if (existingObject == null)
                {
                    // === ПРИМЕНЕНИЕ СТРАТЕГИИ ОБРАБОТКИ ОТСУТСТВУЮЩИХ ОБЪЕКТОВ ===
                    switch (_configuration.MissingObjectStrategy)
                    {
                        case MissingObjectStrategy.AutoSwitchToInsert:
                            // Переключаемся на INSERT с заданным ID (уважаем выбор программиста)
                            // НЕ сбрасываем obj.Id - программист задал конкретный ID
                            return await SaveAsync(obj, user);
                            
                        case MissingObjectStrategy.ReturnNull:
                            return 0;
                            
                        case MissingObjectStrategy.ThrowException:
                        default:
                            throw new InvalidOperationException($"Объект с ID {obj.Id} не найден для обновления. Текущая стратегия: {_configuration.MissingObjectStrategy}");
                    }
                }

                // === ПРИМЕНЕНИЕ НАСТРОЕК АУДИТА (как было в RedbService) ===
                obj.WhoChangeId = user.Id;  // Устанавливаем кто изменил
                
                            if (_configuration.AutoSetModifyDate)
            {
                obj.DateModify = DateTime.Now;
            }

                // === ПРИМЕНЕНИЕ НАСТРОЕК ХЕШИРОВАНИЯ ===
                if (_configuration.AutoRecomputeHash)
                {
                    obj.Hash = RedbHash.ComputeFor(obj);
                }

                // Обновление полей
                existingObject.IdParent = obj.ParentId;
                existingObject.IdWhoChange = obj.WhoChangeId;
                existingObject.DateModify = obj.DateModify;
                existingObject.DateBegin = obj.DateBegin;
                existingObject.DateComplete = obj.DateComplete;
                existingObject.Key = obj.Key;
                existingObject.CodeInt = obj.CodeInt;
                existingObject.CodeString = obj.CodeString;
                existingObject.CodeGuid = obj.CodeGuid;
                existingObject.Name = obj.Name;
                existingObject.Note = obj.Note;
                existingObject.Bool = obj.Bool;
                existingObject.Hash = obj.Hash;
            }

            // Сохранение свойств в _values
            await SavePropertiesAsync(obj.Id, obj.SchemeId, obj.properties);

            await _context.SaveChangesAsync();
            return obj.Id;
        }


        private async Task SavePropertiesAsync<TProps>(long objectId, long schemeId, TProps properties) where TProps : class
        {
            // Получение структур схемы с типами
            var structures = await _context.Structures
                .Where(s => s.IdScheme == schemeId)
                .Select(s => new { s.Id, s.Name, s.TypeNavigation.DbType, s.IsArray })
                .ToListAsync();

            // Удаление существующих значений
            var existingValues = await _context.Set<_RValue>()
                .Where(v => v.IdObject == objectId)
                .ToListAsync();
            _context.Set<_RValue>().RemoveRange(existingValues);

            // 🚀 ПРЯМАЯ РАБОТА С ОБЪЕКТОМ через рефлексию
            var propertiesType = typeof(TProps);

            foreach (var structure in structures)
            {
                var property = propertiesType.GetProperty(structure.Name);
                if (property == null) continue;
                
                // 🚫 ИГНОРИРУЕМ поля с атрибутом [JsonIgnore]
                if (property.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Any()) continue;

                var rawValue = property.GetValue(properties);
                if (rawValue == null) continue;

                var valueRecord = new _RValue
                {
                    Id = _context.GetNextKey(),
                    IdObject = objectId,
                    IdStructure = structure.Id
                };

                // 🎯 КЛЮЧЕВАЯ МАГИЯ: Автосохранение вложенных объектов
                var dbType = structure.DbType ?? "String"; // Fallback для null
                
                var processedValue = await ProcessNestedObjectsAsync(rawValue, dbType, structure.IsArray ?? false, objectId);
                
                // SetValueByType работает с уже обработанными значениями
                SetValueByType(valueRecord, dbType, processedValue, structure.IsArray ?? false);
                _context.Set<_RValue>().Add(valueRecord);
            }
        }

        /// <summary>
        /// 🚀 АВТОСОХРАНЕНИЕ: Обрабатывает вложенные RedbObject, сохраняя их рекурсивно
        /// </summary>
        private async Task<object?> ProcessNestedObjectsAsync(object rawValue, string dbType, bool isArray, long parentObjectId = 0)
        {
            if (rawValue == null) return null;

            // Обработка массивов
            if (isArray && rawValue is System.Collections.IEnumerable enumerable && rawValue is not string)
            {
                var processedList = new List<object>();
                foreach (var item in enumerable)
                {
                    if (IsRedbObjectWithoutId(item))
                    {
                        var nestedObj = (IRedbObject)item;
                        // 🎯 УСТАНОВКА РОДИТЕЛЯ: Если у вложенного объекта нет родителя, устанавливаем базовый
                        if ((nestedObj.ParentId == 0 || nestedObj.ParentId == null) && parentObjectId > 0)
                        {
                            nestedObj.ParentId = parentObjectId;
                        }
                        var savedId = await SaveAsync((dynamic)item);
                        processedList.Add((long)savedId);
                    }
                    else if (IsRedbObjectWithId(item))
                    {
                        processedList.Add(((IRedbObject)item).Id);
                    }
                    else
                    {
                        processedList.Add(item);
                    }
                }
                return processedList;
            }

            // Обработка одиночных объектов
            if (IsRedbObjectWithoutId(rawValue))
            {
                var nestedObj = (IRedbObject)rawValue;
                // 🎯 УСТАНОВКА РОДИТЕЛЯ: Если у вложенного объекта нет родителя, устанавливаем базовый
                if ((nestedObj.ParentId == 0 || nestedObj.ParentId == null) && parentObjectId > 0)
                {
                    nestedObj.ParentId = parentObjectId;
                }
                var savedId = await SaveAsync((dynamic)rawValue);
                return (long)savedId;
            }
            
            if (IsRedbObjectWithId(rawValue))
            {
                return ((IRedbObject)rawValue).Id;
            }

            return rawValue;
        }

        /// <summary>
        /// Проверяет что объект это IRedbObject с Id = 0 (нужно сохранить)
        /// </summary>
        private static bool IsRedbObjectWithoutId(object? value)
        {
            if (value is IRedbObject redbObj)
            {
                return redbObj.Id == 0;
            }
            return false;
        }

        /// <summary>
        /// Проверяет что объект это IRedbObject с Id != 0 (уже сохранен)
        /// </summary>
        private static bool IsRedbObjectWithId(object? value)
        {
            if (value is IRedbObject redbObj)
            {
                return redbObj.Id != 0;
            }
            return false;
        }

        private static void SetValueByType(_RValue valueRecord, string dbType, object? processedValue, bool isArray)
        {
            if (processedValue == null)
            {
                // Все поля останутся NULL
                return;
            }

            if (isArray)
            {
                // Сериализуем обработанный массив (уже содержит ID вместо объектов)
                var jsonOptions = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = false
                };
                valueRecord.Array = JsonSerializer.Serialize(processedValue, jsonOptions);
                return;
            }

            // Прямое присваивание типизированных значений
            switch (dbType)
            {
                case "String":
                case "Text":
                    valueRecord.String = processedValue?.ToString();
                    break;
                case "Long":
                    if (processedValue is long longVal)
                        valueRecord.Long = longVal;
                    else if (processedValue is int intVal)
                        valueRecord.Long = intVal;
                    else if (long.TryParse(processedValue?.ToString(), out var parsedLong))
                        valueRecord.Long = parsedLong;
                    break;
                case "Double":
                    if (processedValue is double doubleVal)
                        valueRecord.Double = doubleVal;
                    else if (processedValue is float floatVal)
                        valueRecord.Double = floatVal;
                    else if (double.TryParse(processedValue?.ToString(), out var parsedDouble))
                        valueRecord.Double = parsedDouble;
                    break;
                case "Boolean":
                    if (processedValue is bool boolVal)
                        valueRecord.Boolean = boolVal;
                    else if (bool.TryParse(processedValue?.ToString(), out var parsedBool))
                        valueRecord.Boolean = parsedBool;
                    break;
                case "DateTime":
                    if (processedValue is DateTime dateTime)
                        valueRecord.DateTime = dateTime;
                    else if (DateTime.TryParse(processedValue?.ToString(), out var parsedDate))
                        valueRecord.DateTime = parsedDate;
                    break;
                case "Guid":
                    if (processedValue is Guid guidVal)
                        valueRecord.Guid = guidVal;
                    else if (Guid.TryParse(processedValue?.ToString(), out var parsedGuid))
                        valueRecord.Guid = parsedGuid;
                    break;
                case "ByteArray":
                    if (processedValue is byte[] byteArray)
                        valueRecord.ByteArray = byteArray;
                    break;
                case "Object":
                case "ListItem":
                    // 🚀 ПРИМЕЧАНИЕ: Эта ветка может не выполняться, т.к. Object имеет db_type="Long"
                    // Но оставим для совместимости
                    if (processedValue is long objectId)
                        valueRecord.Long = objectId;
                    break;
                default:
                    valueRecord.String = processedValue?.ToString();
                    break;
            }
        }



        // ===== LEGACY МЕТОДЫ (закомментированы) =====
        
        /*
        public async Task<int> DeleteSubtreeAsync(RedbObject parentObj, IRedbUser user, bool checkPermissions = true)
        {
            return await DeleteSubtreeAsync(parentObj.Id, user.Id, checkPermissions);
        }
        
        public async Task<int> DeleteSubtreeAsync(RedbObject parentObj, bool checkPermissions = true)
        {
            var effectiveUser = _securityContext.GetEffectiveUserWithPriority();
            return await DeleteSubtreeAsync(parentObj.Id, effectiveUser.UserId, effectiveUser.ShouldCheckPermissions && checkPermissions);
        }
        */
    }
}
