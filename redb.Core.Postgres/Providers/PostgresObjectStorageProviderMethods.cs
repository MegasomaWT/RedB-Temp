using redb.Core.DBModels;
using redb.Core.Utils;
using System.Text.Json.Serialization;
using redb.Core.Postgres.Extensions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using System.Collections;

namespace redb.Core.Postgres.Providers
{
    /// <summary>
    /// Дополнительные методы для PostgresObjectStorageProvider под новую парадигму сохранения
    /// </summary>
    public partial class PostgresObjectStorageProvider
    {
        /// <summary>
        /// Сохранить простое поле (примитивный тип)
        /// </summary>
        private async Task SaveSimpleFieldAsync(long objectId, StructureMetadata structure, object? rawValue)
        {
            var processedValue = await ProcessNestedObjectsAsync(rawValue, structure.DbType, false, objectId);
            var valueRecord = new _RValue
            {
                Id = _context.GetNextKey(),
                IdObject = objectId,
                IdStructure = structure.Id
            };
            SetSimpleValueByType(valueRecord, structure.DbType, processedValue);
            _context.Set<_RValue>().Add(valueRecord);
        }

        /// <summary>
        /// Сохранить Class поле с UUID хешем в _Guid
        /// </summary>
        private async Task SaveClassFieldAsync(long objectId, StructureMetadata structure, object? rawValue, long schemeId)
        {
            if (rawValue == null) return;

            // ✅ Вычисляем UUID хеш бизнес-класса  
            var classHash = RedbHash.ComputeForProps(rawValue);

            // Создаем базовую запись Class поля с хешем в _Guid
            var classRecord = new _RValue
            {
                Id = _context.GetNextKey(),
                IdObject = objectId,
                IdStructure = structure.Id,
                Guid = classHash  // ✅ UUID хеш в _Guid поле
            };
            _context.Set<_RValue>().Add(classRecord);

            // ✅ Сохраняем дочерние поля Class объекта через _array_parent_id
            await SaveClassChildrenAsync(objectId, classRecord.Id, rawValue, structure.Id, schemeId);
        }

        /// <summary>
        /// Сохранить массив с базовой записью (хеш массива) + элементы через _array_parent_id + _array_index
        /// </summary>
        private async Task SaveArrayFieldAsync(long objectId, StructureMetadata structure, object? rawValue, long schemeId)
        {
            if (rawValue == null) return;
            if (rawValue is not IEnumerable enumerable || rawValue is string) return;
            
            // ✅ Создаем БАЗОВУЮ запись массива с хешем всего массива
            var arrayHash = RedbHash.ComputeForProps(rawValue);
            var baseArrayRecord = new _RValue
            {
                Id = _context.GetNextKey(),
                IdObject = objectId,
                IdStructure = structure.Id,
                Guid = arrayHash  // ✅ Хеш всего массива в _Guid
            };
            _context.Set<_RValue>().Add(baseArrayRecord);

            // ✅ Создаем элементы массива с _array_parent_id + _array_index
            await SaveArrayElementsAsync(objectId, structure, baseArrayRecord.Id, enumerable, schemeId);
        }

        /// <summary>
        /// Сохранить элементы массива с _array_parent_id + _array_index
        /// </summary>
        private async Task SaveArrayElementsAsync(long objectId, StructureMetadata structure, long arrayParentId, IEnumerable arrayValue, long schemeId)
        {
            int index = 0;
            foreach (var item in arrayValue)
            {
                if (item == null) 
                {
                    index++;
                    continue;
                }

                // Создаем запись элемента массива
                var elementRecord = new _RValue
                {
                    Id = _context.GetNextKey(),
                    IdObject = objectId,
                    IdStructure = structure.Id,
                    ArrayParentId = arrayParentId,  // ✅ Связь с базовой записью массива
                    ArrayIndex = index              // ✅ Позиция в массиве
                };

                // ✅ Если элемент - Class тип, создаем хеш элемента в _Guid
                if (PostgresObjectStorageProviderExtensions.IsClassType(structure.TypeSemantic))
                {
                    elementRecord.Guid = RedbHash.ComputeForProps(item);  // ✅ Хеш элемента
                    _context.Set<_RValue>().Add(elementRecord);
                    
                    // Сохраняем дочерние поля Class элемента через _array_parent_id
                    await SaveClassChildrenAsync(objectId, elementRecord.Id, item, structure.Id, schemeId);
                }
                else
                {
                    // Для простых типов и RedbObject ссылок
                    var processedValue = await ProcessNestedObjectsAsync(item, structure.DbType, false, objectId);
                    SetSimpleValueByType(elementRecord, structure.DbType, processedValue);
                    _context.Set<_RValue>().Add(elementRecord);
                }

                index++;
            }
        }

        /// <summary>
        /// ✅ НОВАЯ ПАРАДИГМА: Сохраняем дочерние поля Class объектов используя иерархические структуры схемы
        /// </summary>
        private async Task SaveClassChildrenAsync(long objectId, long parentRecordId, object classObject, long parentStructureId, long schemeId, int depth = 0)
        {
            if (classObject == null || depth > 5) return; // Защита от глубокой рекурсии

            // Получаем все public свойства Class объекта через рефлексию
            var classType = classObject.GetType();
            var properties = classType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !p.ShouldIgnoreForRedb())
                .ToArray();

            // Получаем структуры схемы для поиска
            var allStructures = await GetStructuresWithMetadataAsync(schemeId);

            foreach (var property in properties)
            {
                var rawValue = property.GetValue(classObject);
                if (rawValue == null) 
                {
                    continue;
                }

                // ✅ НОВАЯ ЛОГИКА: Ищем дочернюю структуру среди детей родительской структуры
                var structure = allStructures.FirstOrDefault(s => 
                    s.Name == property.Name && 
                    s.IdParent == parentStructureId);
                    
                if (structure == null) 
                {
                    continue; // Пропускаем поля без структур
                }

                // Создаем запись дочернего поля с _array_parent_id связью
                var childRecordId = _context.GetNextKey();
                var childRecord = new _RValue
                {
                    Id = childRecordId,
                    IdObject = objectId,
                    IdStructure = structure.Id,  // ✅ Теперь используем реальную структуру
                    ArrayParentId = parentRecordId,  // ✅ Связь с родительским Class полем
                    ArrayIndex = (int)(childRecordId % 1000000)  // ✅ ИСПРАВЛЕНО: Используем уникальный ID как индекс
                };

                // Определяем тип поля и сохраняем соответственно
                if (property.PropertyType.IsArray || property.PropertyType.GetInterfaces().Contains(typeof(System.Collections.IEnumerable)))
                {
                    // Дочерний массив - создаем хеш и элементы
                    if (rawValue is System.Collections.IEnumerable enumerable && !(rawValue is string))
                    {
                        var arrayHash = RedbHash.ComputeForProps(rawValue);
                        childRecord.Guid = arrayHash;
                        _context.Set<_RValue>().Add(childRecord);
                        
                        // Сохраняем элементы дочернего массива
                        int index = 0;
                        foreach (var item in enumerable)
                        {
                            if (item != null)
                            {
                                var elementRecord = new _RValue
                                {
                                    Id = _context.GetNextKey(),
                                    IdObject = objectId,
                                    IdStructure = structure.Id,
                                    ArrayParentId = childRecord.Id,  // Связь с базовой записью массива
                                    ArrayIndex = index
                                };
                                
                                // Определяем тип элемента массива
                                if (IsBusinessClassProperty(item.GetType()))
                                {
                                    var itemHash = RedbHash.ComputeForProps(item);
                                    elementRecord.Guid = itemHash;
                                    _context.Set<_RValue>().Add(elementRecord);
                                    
                                    // Рекурсивно сохраняем поля элемента массива
                                    await SaveClassChildrenAsync(objectId, elementRecord.Id, item, structure.Id, schemeId, depth + 1);
                                }
                                else
                                {
                                    // Простой элемент массива
                                    SetSimpleValueByType(elementRecord, GetDbTypeForValue(item), item);
                                    _context.Set<_RValue>().Add(elementRecord);
                                }
                            }
                            index++;
                        }
                    }
                }
                else if (IsBusinessClassProperty(property.PropertyType))
                {
                    // Вложенный бизнес-класс - рекурсивно сохраняем
                    var nestedHash = RedbHash.ComputeForProps(rawValue);
                    childRecord.Guid = nestedHash;
                    _context.Set<_RValue>().Add(childRecord);
                    
                    // Рекурсивно сохраняем поля вложенного класса
                    await SaveClassChildrenAsync(objectId, childRecord.Id, rawValue, structure.Id, schemeId, depth + 1);
                }
                else
                {
                    // Простое поле - сохраняем значение
                    SetSimpleValueByType(childRecord, GetDbTypeForValue(rawValue), rawValue);
                    _context.Set<_RValue>().Add(childRecord);
                }
            }
        }

        /// <summary>
        /// Получить схему ID для объекта
        /// </summary>
        private async Task<long> GetSchemeIdForObject(long objectId)
        {
            var obj = await _context.Set<_RObject>()
                .Where(o => o.Id == objectId)
                .FirstOrDefaultAsync();
            return obj?.IdScheme ?? throw new InvalidOperationException($"Объект {objectId} не найден");
        }

        /// <summary>
        /// Определить является ли тип бизнес-классом
        /// </summary>
        private static bool IsBusinessClassProperty(Type type)
        {
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal)) return false;
            if (type == typeof(DateTime) || type == typeof(Guid) || type == typeof(TimeSpan) || type == typeof(byte[])) return false;
            if (Nullable.GetUnderlyingType(type) != null) return false;
            if (type.IsArray) return false;
            if (type.IsEnum) return false;
            if (type.Namespace?.StartsWith("System") == true) return false;
            return type.IsClass;
        }

        /// <summary>
        /// Получить DB тип для значения
        /// </summary>
        private static string GetDbTypeForValue(object value)
        {
            return value switch
            {
                int or long => "Long",
                double or float or decimal => "Double", 
                bool => "Boolean",
                DateTime => "DateTime",
                Guid => "Guid",
                byte[] => "ByteArray",
                _ => "String"
            };
        }
    }
}
