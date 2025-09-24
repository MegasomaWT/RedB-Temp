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
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using EFCore.BulkExtensions;
using System;


namespace redb.Core.Postgres.Providers
{
    /// <summary>
    /// PostgreSQL реализация провайдера для работы с объектами
    /// </summary>
    public partial class PostgresObjectStorageProvider : IObjectStorageProvider
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
            // 🚀 ПЕРЕНАПРАВЛЯЕМ НА НОВЫЙ SaveAsync с правильной архитектурой
            return await SaveAsyncNew(obj, user);
        }

        /// <summary>
        /// Сохранение свойств с выбором стратегии на основе конфигурации
        /// </summary>
        private async Task SavePropertiesAsync<TProps>(long objectId, long schemeId, TProps properties) where TProps : class
        {
            var strategy = _configuration.EavSaveStrategy;

            switch (strategy)
            {
                case EavSaveStrategy.DeleteInsert:
                    await SavePropertiesWithDeleteInsert(objectId, schemeId, properties);
                    break;

                case EavSaveStrategy.ChangeTracking:
                    await SavePropertiesWithChangeTracking(objectId, schemeId, properties);
                    break;

                default:
                    throw new NotSupportedException($"Стратегия {strategy} не поддерживается");
            }
        }

        /// <summary>
        /// Стратегия DELETE + INSERT - удаляет все существующие values и создает новые
        /// </summary>
        private async Task SavePropertiesWithDeleteInsert<TProps>(long objectId, long schemeId, TProps properties) where TProps : class
        {
            // Получение структур схемы с расширенными данными включая _store_null
            var structures = await GetStructuresWithMetadataAsync(schemeId);

            // Удаление существующих значений
            await DeleteExistingValuesAsync(objectId);

            // Сохранение новых значений
            await SavePropertiesFromObjectAsync(objectId, schemeId, structures, properties);
        }

        /// <summary>
        /// Получить структуры схемы с полными метаданными включая _store_null
        /// </summary>
        private async Task<List<StructureMetadata>> GetStructuresWithMetadataAsync(long schemeId)
        {
            return await _context.Structures
                .Where(s => s.IdScheme == schemeId)
                .Select(s => new StructureMetadata
                {
                    Id = s.Id,
                    IdParent = s.IdParent,  // ✅ Добавляем IdParent
                    Name = s.Name,
                    DbType = s.TypeNavigation.DbType ?? "String",
                    IsArray = s.IsArray ?? false,
                    StoreNull = s.StoreNull ?? false,
                    TypeSemantic = s.TypeNavigation.Type1 ?? "string"
                })
                .ToListAsync();
        }

        /// <summary>
        /// Удалить все существующие values для объекта
        /// </summary>
        private async Task DeleteExistingValuesAsync(long objectId)
        {
            var existingValues = await _context.Set<_RValue>()
                .Where(v => v.IdObject == objectId)
                .ToListAsync();
            _context.Set<_RValue>().RemoveRange(existingValues);
        }

        /// <summary>
        /// Сохранить properties объекта согласно структурам схемы
        /// </summary>
        private async Task SavePropertiesFromObjectAsync<TProps>(long objectId, long schemeId, List<StructureMetadata> structures, TProps properties) where TProps : class
        {
            var propertiesType = typeof(TProps);

            foreach (var structure in structures)
            {
                var property = propertiesType.GetProperty(structure.Name);
                if (property == null) continue;

                // 🚫 ИГНОРИРУЕМ поля с атрибутом [JsonIgnore]
                if (property.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Any()) continue;

                var rawValue = property.GetValue(properties);

                // ✅ НОВАЯ NULL СЕМАНТИКА: проверяем _store_null
                if (!PostgresObjectStorageProviderExtensions.ShouldCreateValueRecord(rawValue, structure.StoreNull))
                    continue;

                // ✅ НОВАЯ АРХИТЕКТУРА: разные стратегии для разных типов полей
                if (structure.IsArray)
                {
                    await SaveArrayFieldAsync(objectId, structure, rawValue, schemeId);
                }
                else if (PostgresObjectStorageProviderExtensions.IsClassType(structure.TypeSemantic))
                {
                    await SaveClassFieldAsync(objectId, structure, rawValue, schemeId);
                }
                else
                {
                    await SaveSimpleFieldAsync(objectId, structure, rawValue);
                }
            }
        }

        /// <summary>
        /// Стратегия ChangeTracking - сравнивает с БД и обновляет только измененные properties
        /// </summary>
        private async Task SavePropertiesWithChangeTracking<TProps>(long objectId, long schemeId, TProps properties) where TProps : class
        {
            // Получение структур схемы из кеша
            var scheme = await _schemeSync.GetSchemeByIdAsync(schemeId);
            if (scheme == null)
                throw new InvalidOperationException($"Схема с ID {schemeId} не найдена");

            // Загружаем существующие values из БД
            var existingValues = await LoadExistingValuesAsync(objectId, scheme.Structures);

            // Извлекаем текущие свойства объекта
            var currentProperties = await ExtractCurrentPropertiesAsync(properties, scheme.Structures);

            // Определяем что нужно изменить
            await ApplyPropertyChangesAsync(objectId, existingValues, currentProperties);
        }

        /// <summary>
        /// Загрузить существующие values из БД для объекта
        /// </summary>
        private async Task<Dictionary<string, ExistingValueInfo>> LoadExistingValuesAsync(long objectId, IReadOnlyCollection<IRedbStructure> structures)
        {
            var structureIds = structures.Select(s => s.Id).ToList();

            // Загружаем существующие values с информацией о типах (аналогично SavePropertiesWithDeleteInsert)
            var existingValuesWithTypes = await (from v in _context.Set<_RValue>()
                                                 join s in _context.Structures on v.IdStructure equals s.Id
                                                 join t in _context.Types on s.IdType equals t.Id
                                                 where v.IdObject == objectId && structureIds.Contains(v.IdStructure)
                                                 select new { Value = v, Structure = s, DbType = t.DbType })
                                                .ToListAsync();

            var result = new Dictionary<string, ExistingValueInfo>();

            foreach (var item in existingValuesWithTypes)
            {
                var structure = structures.First(s => s.Id == item.Value.IdStructure);

                result[structure.Name] = new ExistingValueInfo
                {
                    ValueRecord = item.Value,
                    StructureId = structure.Id,
                    DbType = item.DbType,
                    IsArray = structure.IsArray ?? false,
                    ExtractedValue = ExtractValueFromRecord(item.Value, item.DbType, structure.IsArray ?? false)
                };
            }

            return result;
        }

        /// <summary>
        /// Извлечь текущие свойства объекта через рефлексию
        /// </summary>
        private async Task<Dictionary<string, CurrentPropertyInfo>> ExtractCurrentPropertiesAsync<TProps>(TProps properties, IReadOnlyCollection<IRedbStructure> structures) where TProps : class
        {
            var result = new Dictionary<string, CurrentPropertyInfo>();
            var propertiesType = typeof(TProps);

            // Получаем информацию о типах из БД для всех структур
            var structureIds = structures.Select(s => s.Id).ToList();
            var structureTypes = await (from s in _context.Structures
                                        join t in _context.Types on s.IdType equals t.Id
                                        where structureIds.Contains(s.Id)
                                        select new { StructureId = s.Id, DbType = t.DbType })
                                      .ToDictionaryAsync(x => x.StructureId, x => x.DbType);

            foreach (var structure in structures)
            {
                var property = propertiesType.GetProperty(structure.Name);
                if (property == null) continue;

                // Игнорируем поля с атрибутом [JsonIgnore]
                if (property.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Any()) continue;

                var rawValue = property.GetValue(properties);
                var dbType = structureTypes.GetValueOrDefault(structure.Id, "String");

                result[structure.Name] = new CurrentPropertyInfo
                {
                    Value = rawValue,
                    StructureId = structure.Id,
                    DbType = dbType,
                    IsArray = structure.IsArray ?? false
                };
            }

            return result;
        }

        /// <summary>
        /// Применить изменения - INSERT/UPDATE/DELETE только измененных свойств
        /// </summary>
        private async Task ApplyPropertyChangesAsync(long objectId, Dictionary<string, ExistingValueInfo> existing, Dictionary<string, CurrentPropertyInfo> current)
        {
            var allFieldNames = existing.Keys.Union(current.Keys).ToList();

            foreach (var fieldName in allFieldNames)
            {
                var hasExisting = existing.TryGetValue(fieldName, out var existingInfo);
                var hasCurrent = current.TryGetValue(fieldName, out var currentInfo);

                if (!hasExisting && hasCurrent && currentInfo.Value != null)
                {
                    // INSERT новое значение
                    await InsertNewValueAsync(objectId, currentInfo);
                }
                else if (hasExisting && (!hasCurrent || currentInfo.Value == null))
                {
                    // DELETE удаленное значение
                    _context.Set<_RValue>().Remove(existingInfo.ValueRecord);
                }
                else if (hasExisting && hasCurrent && currentInfo.Value != null && !ValuesAreEqual(existingInfo.ExtractedValue, currentInfo.Value))
                {
                    // UPDATE измененное значение
                    await UpdateExistingValueAsync(existingInfo.ValueRecord, currentInfo);
                }
                // else: значение не изменилось - пропускаем
            }
        }

        /// <summary>
        /// INSERT новое значение в _values
        /// </summary>
        private async Task InsertNewValueAsync(long objectId, CurrentPropertyInfo currentInfo)
        {
            var valueRecord = new _RValue
            {
                Id = _context.GetNextKey(),
                IdObject = objectId,
                IdStructure = currentInfo.StructureId
            };

            var processedValue = await ProcessNestedObjectsAsync(currentInfo.Value, currentInfo.DbType ?? "String", currentInfo.IsArray, objectId);
            SetSimpleValueByType(valueRecord, currentInfo.DbType ?? "String", processedValue);

            _context.Set<_RValue>().Add(valueRecord);
        }

        /// <summary>
        /// UPDATE существующее значение в _values
        /// </summary>
        private async Task UpdateExistingValueAsync(_RValue existingRecord, CurrentPropertyInfo currentInfo)
        {
            // Очищаем все поля
            ClearValueRecord(existingRecord);

            var processedValue = await ProcessNestedObjectsAsync(currentInfo.Value, currentInfo.DbType ?? "String", currentInfo.IsArray, existingRecord.IdObject);
            SetSimpleValueByType(existingRecord, currentInfo.DbType ?? "String", processedValue);
        }

        /// <summary>
        /// Сравнить два значения на равенство
        /// </summary>
        private bool ValuesAreEqual(object? existing, object? current)
        {
            if (existing == null && current == null) return true;
            if (existing == null || current == null) return false;

            // Для массивов делаем поэлементное сравнение
            if (existing is Array arrayA && current is Array arrayB)
            {
                if (arrayA.Length != arrayB.Length) return false;

                for (int i = 0; i < arrayA.Length; i++)
                {
                    if (!Equals(arrayA.GetValue(i), arrayB.GetValue(i)))
                        return false;
                }
                return true;
            }

            return Equals(existing, current);
        }

        /// <summary>
        /// Извлечь значение из _RValue записи
        /// </summary>
        private object? ExtractValueFromRecord(_RValue valueRecord, string? dbType, bool isArray)
        {
            if (isArray)
                return null;  // ✅ Массивы теперь хранятся реляционно, а не в JSON

            return dbType switch
            {
                "String" => valueRecord.String,
                "Long" => valueRecord.Long,
                "Double" => valueRecord.Double,
                "Boolean" => valueRecord.Boolean,
                "DateTime" => valueRecord.DateTime,
                "Guid" => valueRecord.Guid,
                "ByteArray" => valueRecord.ByteArray,
                _ => valueRecord.String
            };
        }

        /// <summary>
        /// Очистить все поля _RValue записи
        /// </summary>
        private void ClearValueRecord(_RValue valueRecord)
        {
            valueRecord.String = null;
            valueRecord.Long = null;
            valueRecord.Guid = null;
            valueRecord.Double = null;
            valueRecord.DateTime = null;
            valueRecord.Boolean = null;
            valueRecord.ByteArray = null;

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

        /// <summary>
        /// ✅ ОБНОВЛЕННАЯ ВЕРСИЯ: Убрали JSON массивы, только простые типы
        /// </summary>
        private static void SetSimpleValueByType(_RValue valueRecord, string dbType, object? processedValue)
        {
            if (processedValue == null) return;

            // ❌ МАССИВЫ НЕ ОБРАБАТЫВАЕМ - они идут через SaveArrayFieldAsync
            
            // Прямое присваивание типизированных значений
            switch (dbType)
            {
                case "String":
                case "Text":
                    valueRecord.String = processedValue?.ToString();
                    break;
                case "Long":
                case "bigint":
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
                case "ByteArray":
                    if (processedValue is byte[] byteArray)
                        valueRecord.ByteArray = byteArray;
                    break;
                case "Object":
                case "ListItem":
                    // RedbObject ссылки хранятся как Long ID
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

        // ===== 🚀 МАССОВЫЕ ОПЕРАЦИИ реализованы в PostgresObjectStorageProvider.AddNewObjects.cs ===== 

      
      
        /// <summary>
        /// 🚀 ПОЛНАЯ обработка properties (массивы, классы, объекты) - GetNextKey прозрачно ASYNC!
        /// </summary>
      
        /// <summary>
        /// 🔧 Обработка простого поля для массовой вставки (GetNextKey прозрачно)
        /// </summary>
      
  /// <summary>
  /// Преобразует IRedbStructure в StructureMetadata с получением информации о типах из кеша
  /// </summary>
  private async Task<List<StructureMetadata>> ConvertStructuresToMetadataAsync(IEnumerable<IRedbStructure> structures)
  {
      var result = new List<StructureMetadata>();

      foreach (var structure in structures)
      {
          // Получаем информацию о типе по IdType через кеш или БД
          var typeInfo = await GetTypeInfoAsync(structure.IdType);

          result.Add(new StructureMetadata
          {
              Id = structure.Id,
              IdParent = structure.IdParent,
              Name = structure.Name,
              DbType = typeInfo.DbType,
              IsArray = structure.IsArray ?? false,
              StoreNull = structure.StoreNull ?? false,
              TypeSemantic = typeInfo.TypeSemantic
          });
      }

      return result;
  }
        /// <summary>
        /// Получает информацию о типе по IdType
        /// </summary>
        private async Task<(string DbType, string TypeSemantic)> GetTypeInfoAsync(long typeId)
        {
            // Используем прямой запрос к БД для получения информации о типе
            var typeEntity = await _context.Set<_RType>().FindAsync(typeId);

            return typeEntity != null
                ? (typeEntity.DbType ?? "String", typeEntity.Type1 ?? "string")
                : ("String", "string");
        }

    }

    /// <summary>
    /// Информация о существующем значении из БД
    /// </summary>
    internal class ExistingValueInfo
    {
        public _RValue ValueRecord { get; set; } = null!;
        public long StructureId { get; set; }
        public string? DbType { get; set; }
        public bool IsArray { get; set; }
        public object? ExtractedValue { get; set; }
    }

    /// <summary>
    /// Информация о текущем свойстве объекта
    /// </summary>
    internal class CurrentPropertyInfo
    {
        public object? Value { get; set; }
        public long StructureId { get; set; }
        public string? DbType { get; set; }
        public bool IsArray { get; set; }
    }

    /// <summary>
    /// Метаданные структуры с расширенной информацией включая _store_null
    /// </summary>
    internal class StructureMetadata
    {
        public long Id { get; set; }
        public long? IdParent { get; set; }  // ✅ Добавляем поле для иерархии структур
        public string Name { get; set; } = string.Empty;
        public string DbType { get; set; } = "String";
        public bool IsArray { get; set; }
        public bool StoreNull { get; set; }
        public string TypeSemantic { get; set; } = "string";
    }
}