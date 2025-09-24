using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.Postgres;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace redb.ConsoleTest.Utils
{
    /// <summary>
    /// Утилитарные методы для анализа базы данных (только для тестирования)
    /// </summary>
    public static class DatabaseAnalysisUtils
    {
        /// <summary>
        /// Проверка объекта в базе данных: поля _objects и связанные _values
        /// </summary>
        public static async Task CheckObjectInDatabase(IRedbService redb, long objectId, ILogger logger)
        {
            var redbService = (RedbService)redb;
            var context = GetRedbContext(redbService);

            // Получаем базовые поля объекта из _objects
            var objData = await context.Objects
                .Where(o => o.Id == objectId)
                .Select(o => new
                {
                    o.Id,
                    o.Name,
                    o.Note,
                    o.IdScheme,
                    o.IdOwner,
                    o.IdWhoChange,
                    o.DateCreate,
                    o.DateModify,
                    o.Hash,
                    o.Bool,
                    o.Key,
                    o.CodeInt,
                    o.CodeString,
                    o.CodeGuid
                })
                .FirstOrDefaultAsync();

            if (objData == null)
            {
                logger.LogWarning("Объект {objectId} не найден в _objects", objectId);
                return;
            }

            logger.LogInformation("Объект {id} в _objects:", objData.Id);
            logger.LogInformation("  Name: {name}", objData.Name);
            logger.LogInformation("  Note: {note}", objData.Note);
            logger.LogInformation("  Scheme: {scheme}, Owner: {owner}, WhoChange: {whoChange}",
                objData.IdScheme, objData.IdOwner, objData.IdWhoChange);
            logger.LogInformation("  Created: {created}, Modified: {modified}",
                objData.DateCreate, objData.DateModify);
            logger.LogInformation("  Hash: {hash}", objData.Hash);
            logger.LogInformation("  Bool: {bool}, Key: {key}", objData.Bool, objData.Key);
            logger.LogInformation("  CodeInt: {codeInt}, CodeString: {codeString}, CodeGuid: {codeGuid}",
                objData.CodeInt, objData.CodeString, objData.CodeGuid);

            // Получаем все значения из _values для этого объекта
            var values = await context.Values
                .Where(v => v.IdObject == objectId)
                .Join(context.Structures, v => v.IdStructure, s => s.Id, (v, s) => new
                {
                    StructureName = s.Name,
                    StructureType = s.TypeNavigation.DbType,
                    IsArray = s.IsArray,
                    StoreNull = s.StoreNull,
                    v.String,
                    v.Long,
                    v.Guid,
                    v.Double,
                    v.DateTime,
                    v.Boolean,
                    v.ByteArray,
                    v.ArrayParentId,
                    v.ArrayIndex
                })
                .ToListAsync();

            logger.LogInformation("Значения в _values ({count} записей):", values.Count);
            foreach (var val in values)
            {
                var actualValue = GetActualValue(val);
                logger.LogInformation("  {name} ({type}{array}): {value}",
                    val.StructureName,
                    val.StructureType,
                    val.IsArray == true ? "[]" : "",
                    actualValue ?? "<NULL>");
            }

            // ✅ ДОПОЛНИТЕЛЬНАЯ ДИАГНОСТИКА: Показываем все реляционные записи массивов
            var arrayRecords = values.Where(v => v.ArrayParentId != null || 
                                                 (v.ArrayParentId == null && v.Guid != null && 
                                                  v.String == null && v.Long == null && v.Double == null && 
                                                  v.DateTime == null && v.Boolean == null && v.ByteArray == null))
                                     .ToList();
            
            if (arrayRecords.Any())
            {
                logger.LogInformation("");
                logger.LogInformation("🔍 === ДЕТАЛЬНЫЙ АНАЛИЗ РЕЛЯЦИОННЫХ МАССИВОВ ({count} записей) ===", arrayRecords.Count);
                
                var baseRecords = arrayRecords.Where(r => r.ArrayParentId == null).ToList();
                var elementRecords = arrayRecords.Where(r => r.ArrayParentId != null).ToList();
                
                foreach (var baseRecord in baseRecords)
                {
                    logger.LogInformation("  📦 Базовая запись массива '{name}': Guid={guid}",
                        baseRecord.StructureName, baseRecord.Guid);
                    
                    // Нужно получить ID записи - но это сложно из анонимного типа
                    // Пока просто показываем элементы по имени структуры
                    var relatedElements = elementRecords.Where(e => e.StructureName == baseRecord.StructureName)
                                                       .OrderBy(e => e.ArrayIndex).ToList();
                    foreach (var element in relatedElements)
                    {
                        var elementValue = GetSimpleValue(element);
                        logger.LogInformation("    └─ [{index}] Parent:{parent} = {value}",
                            element.ArrayIndex, element.ArrayParentId, elementValue ?? "<NULL>");
                    }
                    
                    if (!relatedElements.Any())
                    {
                        logger.LogInformation("    └─ (элементы массива не найдены)");
                    }
                }
                
                // Показываем все элементы массивов
                if (elementRecords.Any())
                {
                    logger.LogInformation("  🔗 Все элементы массивов ({count}):", elementRecords.Count);
                    foreach (var element in elementRecords.OrderBy(e => e.StructureName).ThenBy(e => e.ArrayIndex))
                    {
                        var elementValue = GetSimpleValue(element);
                        logger.LogInformation("    └─ {name}[{index}] Parent:{parent} = {value}",
                            element.StructureName, element.ArrayIndex, element.ArrayParentId, elementValue ?? "<NULL>");
                    }
                }
            }
        }

        /// <summary>
        /// Сравнение нескольких объектов
        /// </summary>
        public static async Task CompareObjectsInDatabase(IRedbService redb, long[] objectIds, ILogger logger)
        {
            var redbService = (RedbService)redb;
            var context = GetRedbContext(redbService);

            foreach (var objectId in objectIds)
            {
                logger.LogInformation("--- Объект {id} ---", objectId);

                // Базовые поля
                var obj = await context.Objects.FindAsync(objectId);
                if (obj == null)
                {
                    logger.LogWarning("Объект {id} не найден", objectId);
                    continue;
                }

                logger.LogInformation("Базовые поля: name='{name}', scheme={scheme}, hash={hash}",
                    obj.Name, obj.IdScheme, obj.Hash);

                // Свойства (generic fields)
                var valueCount = await context.Values.CountAsync(v => v.IdObject == objectId);
                var propertyNames = await context.Values
                    .Where(v => v.IdObject == objectId)
                    .Join(context.Structures, v => v.IdStructure, s => s.Id, (v, s) => s.Name)
                    .ToListAsync();

                logger.LogInformation("Дженерик свойства ({count}): {names}",
                    valueCount, string.Join(", ", propertyNames));
            }
        }

        /// <summary>
        /// Проверка существования объекта в основной таблице
        /// </summary>
        public static async Task<bool> CheckObjectExists(IRedbService redb, long objectId)
        {
            var redbService = (RedbService)redb;
            var context = GetRedbContext(redbService);
            return await context.Objects.AnyAsync(o => o.Id == objectId);
        }

        /// <summary>
        /// Проверка наличия объекта в архиве удаленных
        /// </summary>
        public static async Task<bool> CheckObjectInArchive(IRedbService redb, long objectId)
        {
            var redbService = (RedbService)redb;
            var context = GetRedbContext(redbService);
            return await context.Database
                .SqlQueryRaw<long>("SELECT _id FROM _deleted_objects WHERE _id = {0}", objectId)
                .AnyAsync();
        }

        /// <summary>
        /// Показать детали архивной записи
        /// </summary>
        public static async Task ShowArchivedObjectDetails(IRedbService redb, long objectId, ILogger logger)
        {
            var redbService = (RedbService)redb;
            var context = GetRedbContext(redbService);

            logger.LogInformation("");
            logger.LogInformation("📋 === ДЕТАЛИ АРХИВНОЙ ЗАПИСИ ===");

            // Получаем архивную запись
            var archivedRecord = await context.Database
                .SqlQueryRaw<ArchivedObjectRecord>(@"
                    SELECT _id, _name, _note, _date_create, _date_modify, _date_delete, 
                           _values, _hash, _id_scheme, _id_owner, _id_who_change
                    FROM _deleted_objects 
                    WHERE _id = {0}", objectId)
                .FirstOrDefaultAsync();

            if (archivedRecord == null)
            {
                logger.LogWarning("Архивная запись для объекта {objectId} не найдена", objectId);
                return;
            }

            logger.LogInformation("Архивная запись объекта {id}:", archivedRecord._id);
            logger.LogInformation("  Name: {name}", archivedRecord._name);
            logger.LogInformation("  Note: {note}", archivedRecord._note);
            logger.LogInformation("  Scheme: {scheme}, Owner: {owner}, WhoChange: {whoChange}",
                archivedRecord._id_scheme, archivedRecord._id_owner, archivedRecord._id_who_change);
            logger.LogInformation("  Created: {created}, Modified: {modified}, Deleted: {deleted}",
                archivedRecord._date_create, archivedRecord._date_modify, archivedRecord._date_delete);
            logger.LogInformation("  Hash: {hash}", archivedRecord._hash);

            logger.LogInformation("");
            logger.LogInformation("📄 Архивированные _values (JSON):");
            if (string.IsNullOrEmpty(archivedRecord._values))
            {
                logger.LogInformation("  (нет значений)");
            }
            else
            {
                try
                {
                    // Форматируем JSON для лучшей читаемости
                    var jsonObj = System.Text.Json.JsonSerializer.Deserialize<object>(archivedRecord._values);
                    var formattedJson = System.Text.Json.JsonSerializer.Serialize(jsonObj, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    // Показываем первые 500 символов JSON для обзора
                    var preview = formattedJson.Length > 500 ? formattedJson.Substring(0, 500) + "..." : formattedJson;
                    logger.LogInformation("  JSON Preview ({length} chars):", formattedJson.Length);
                    logger.LogInformation("{preview}", preview);
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Ошибка парсинга JSON: {error}", ex.Message);
                    logger.LogInformation("  Raw Values: {values}", archivedRecord._values);
                }
            }
        }

        /// <summary>
        /// Извлечение актуального значения из записи _values
        /// </summary>
        private static object? GetActualValue(dynamic valueRecord)
        {
            // ✅ НОВАЯ ПАРАДИГМА: Диагностика реляционных массивов
            if (valueRecord.ArrayParentId != null)
            {
                // Это элемент массива - показываем его индекс и родителя
                var elementValue = GetSimpleValue(valueRecord);
                return $"<ArrayElement[{valueRecord.ArrayIndex ?? "?"}] Parent:{valueRecord.ArrayParentId}> = {elementValue}";
            }
            
            // Проверяем есть ли у записи только Guid (возможно базовая запись массива)
            if (valueRecord.Guid != null && IsOnlyGuidSet(valueRecord))
            {
                return $"<ArrayBase: {valueRecord.Guid}>";
            }

            // Обычные значения - проверяем все возможные столбцы и возвращаем не-null значение
            return GetSimpleValue(valueRecord);
        }

        /// <summary>
        /// Получить простое значение из записи (без массивной логики)
        /// </summary>
        private static object? GetSimpleValue(dynamic valueRecord)
        {
            if (valueRecord.String != null) return valueRecord.String;
            if (valueRecord.Long != null) return valueRecord.Long;
            if (valueRecord.Guid != null) return valueRecord.Guid;
            if (valueRecord.Double != null) return valueRecord.Double;
            if (valueRecord.DateTime != null) return valueRecord.DateTime;
            if (valueRecord.Boolean != null) return valueRecord.Boolean;
            if (valueRecord.ByteArray != null) return $"<ByteArray[{((byte[])valueRecord.ByteArray).Length}]>";

            return null;
        }

        /// <summary>
        /// Проверить, что в записи установлен только Guid (признак базовой записи массива)
        /// </summary>
        private static bool IsOnlyGuidSet(dynamic valueRecord)
        {
            return valueRecord.String == null && 
                   valueRecord.Long == null && 
                   valueRecord.Double == null && 
                   valueRecord.DateTime == null && 
                   valueRecord.Boolean == null && 
                   valueRecord.ByteArray == null &&
                   valueRecord.ArrayParentId == null;
        }

        /// <summary>
        /// Получение контекста БД через рефлексию (только для тестирования!)
        /// </summary>
        private static redb.Core.Postgres.RedbContext GetRedbContext(RedbService redbService)
        {
            var context = redbService.GetType().GetField("_context",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(redbService)
                as redb.Core.Postgres.RedbContext;

            if (context == null)
            {
                throw new InvalidOperationException("Не удалось получить RedbContext из RedbService");
            }

            return context;
        }
    }
}
