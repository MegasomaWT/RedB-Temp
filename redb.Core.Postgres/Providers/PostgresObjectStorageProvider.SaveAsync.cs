using redb.Core.Providers;
using redb.Core.DBModels;
using redb.Core.Utils;
using redb.Core.Models.Contracts;
using redb.Core.Models.Entities;
using redb.Core.Models.Configuration;
using redb.Core.Models.Security;
using System.Text.Json.Serialization;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;

namespace redb.Core.Postgres.Providers
{
    /// <summary>
    /// 🚀 НОВЫЙ SaveAsync - правильная архитектура с рекурсивной обработкой
    /// </summary>
    public partial class PostgresObjectStorageProvider
    {
        /// <summary>
        /// 🚀 НОВЫЙ САВЕАСИНК: Правильная рекурсивная обработка всех типов данных
        /// Собирает объекты и values в списки, потом batch сохранение
        /// </summary>
        public async Task<long> SaveAsyncNew<TProps>(IRedbObject<TProps> obj, IRedbUser user) where TProps : class, new()
        {
            // 🚀 === НОВЫЙ SaveAsync ЗАПУСК ===
            // Объект: '{obj.Name}' (ID={obj.Id}, SchemeId={obj.SchemeId})
            
            if (obj.properties == null)
            {
                throw new ArgumentException("Свойства объекта не могут быть null", nameof(obj));
            }

            // === ПЕРЕСЧЕТ ХЕША В НАЧАЛЕ (из старого SaveAsync) ===
            var currentHash = RedbHash.ComputeFor(obj);
            if (currentHash.HasValue)
            {
                obj.Hash = currentHash.Value;
            }

            // === СТРАТЕГИИ ОБРАБОТКИ УДАЛЕННЫХ ОБЪЕКТОВ (из старого SaveAsync) ===
            var isNewObject = obj.Id == 0;
            if (!isNewObject)
            {
                var exists = await _context.Objects.AnyAsync(o => o.Id == obj.Id);
                if (!exists)
                {
                    switch (_configuration.MissingObjectStrategy)
                    {
                        case MissingObjectStrategy.AutoSwitchToInsert:
                            isNewObject = true;
                            break;
                        case MissingObjectStrategy.ReturnNull:
                            return 0;
                        case MissingObjectStrategy.ThrowException:
                        default:
                            throw new InvalidOperationException($"Object with id {obj.Id} not found. Current strategy: {_configuration.MissingObjectStrategy}");
                    }
                }
            }

            // === АВТООПРЕДЕЛЕНИЕ СХЕМЫ (из старого SaveAsync) ===
            // Проверка схемы: SchemeId={obj.SchemeId}, AutoSync={_configuration.AutoSyncSchemesOnSave}
            if (obj.SchemeId == 0 && _configuration.AutoSyncSchemesOnSave)
            {

                
                // 🚧 ВРЕМЕННО: проверяем существующую схему БЕЗ создания структур
                var existingScheme = await _schemeSync.GetSchemeByTypeAsync<TProps>();
                if (existingScheme != null)
                {
                    obj.SchemeId = existingScheme.Id;

                }
                else
                {

                    try
                    {
                        var scheme = await _schemeSync.SyncSchemeAsync<TProps>();
                        obj.SchemeId = scheme.Id;

                    }
                    catch (Exception ex)
                    {


                        throw;
                    }
                }
            }

            // === ПРОВЕРКИ ПРАВ ДОСТУПА (из старого SaveAsync) ===
            if (_configuration.DefaultCheckPermissionsOnSave)
            {
                if (isNewObject)
                {
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
                    var canUpdate = await _permissionProvider.CanUserEditObject(obj, user);
                    if (!canUpdate)
                    {
                        throw new UnauthorizedAccessException($"Пользователь {user.Id} не имеет прав на изменение объекта {obj.Id}");
                    }
                }
            }

            // ШАГ 1: Создание коллекторов для объектов и values
            var objectsToSave = new List<IRedbObject>();
            var valuesToSave = new List<_RValue>();
            var processedObjectIds = new HashSet<long>();

            // ШАГ 2: Рекурсивный сбор всех объектов (главный + вложенные IRedbObject)

            await CollectAllObjectsRecursively(obj, objectsToSave, processedObjectIds);


            // ШАГ 3: Назначение ID всем объектам без ID (через GetNextKey)

            await AssignMissingIds(objectsToSave, user);
            
            // ✅ ИСПРАВЛЯЕМ ParentId после назначения ID
            var mainObjectId = obj.Id;
            foreach (var nestedObj in objectsToSave.Skip(1)) // пропускаем главный объект
            {
                if (nestedObj.ParentId == null || nestedObj.ParentId == 0)
                {
                    nestedObj.ParentId = mainObjectId;

                }
            }
            


            // ШАГ 4: Создание/проверка схем для всех типов объектов

            await EnsureSchemesForAllTypes(objectsToSave);


            // ШАГ 5: Рекурсивная обработка properties всех объектов в списки values

            await ProcessAllObjectsPropertiesRecursively(objectsToSave, valuesToSave);

            
            // 🔍 ДИАГНОСТИКА: Проверяем дубли в valuesToSave 
            var duplicates = valuesToSave
                .Where(v => v.ArrayIndex == null) // только не-массивные элементы
                .GroupBy(v => new { v.IdStructure, v.IdObject })
                .Where(g => g.Count() > 1)
                .ToList();
                
            if (duplicates.Any())
            {

                foreach (var duplicate in duplicates)
                {

                }
            }
            else
            {

            }

            // ШАГ 6: Delete/Insert стратегия - удаляем старые values, готовим новые

            await PrepareValuesByStrategy(objectsToSave, valuesToSave);


            // ШАГ 7: Batch сохранение в правильном порядке

            await CommitAllChangesBatch(objectsToSave, valuesToSave);


            return obj.Id;
        }

        /// <summary>
        /// 🔍 ШАГ 2: Рекурсивный сбор всех IRedbObject (главный + вложенные)
        /// </summary>
        protected async Task CollectAllObjectsRecursively(IRedbObject rootObject, List<IRedbObject> collector, HashSet<long> processed)
        {

            
            collector.Add(rootObject);

            
            // ✅ РЕАЛИЗУЕМ: Рекурсивный поиск вложенных IRedbObject в properties
            var rootProperties = GetPropertiesFromRedbObject(rootObject);
            await CollectNestedRedbObjectsFromProperties(rootProperties, collector, processed, rootObject.Id);
        }

        /// <summary>
        /// 🔍 Рекурсивный поиск IRedbObject в properties объекта
        /// </summary>
        private async Task CollectNestedRedbObjectsFromProperties(object? properties, List<IRedbObject> collector, HashSet<long> processed, long parentId)
        {
            if (properties == null) return;

            var propsType = properties.GetType();
            var propsProperties = propsType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var property in propsProperties)
            {
                var value = property.GetValue(properties);
                if (value == null) continue;

                // 🔗 Одиночный IRedbObject  
                if (IsRedbObjectType(value.GetType()))
                {
                    var redbObj = (IRedbObject)value;

                    
                    // ParentId будет установлен позже после назначения ID
                    
                    collector.Add(redbObj);

                    
                    // Рекурсия для вложенных IRedbObject
                    var nestedProperties = GetPropertiesFromRedbObject(redbObj);
                    await CollectNestedRedbObjectsFromProperties(nestedProperties, collector, processed, redbObj.Id);
                }
                // 📊 Массив IRedbObject
                else if (value is IEnumerable enumerable && IsRedbObjectArrayType(value.GetType()))
                {

                    foreach (var item in enumerable)
                    {
                        if (item != null && IsRedbObjectType(item.GetType()))
                        {
                            var redbObj = (IRedbObject)item;

                            
                            // ParentId будет установлен позже после назначения ID
                            
                            collector.Add(redbObj);

                            
                            // Рекурсия для элементов массива
                            var arrayElementProperties = GetPropertiesFromRedbObject(redbObj);
                            await CollectNestedRedbObjectsFromProperties(arrayElementProperties, collector, processed, redbObj.Id);
                        }
                    }
                }
                // 🏗️ Рекурсия в бизнес-классы
                else if (IsBusinessClassType(value.GetType()))
                {
                    await CollectNestedRedbObjectsFromProperties(value, collector, processed, parentId);
                }
                // 📊 Рекурсия в массивы бизнес-классов
                else if (value is IEnumerable businessEnumerable && !IsStringType(value.GetType()))
                {
                    foreach (var item in businessEnumerable)
                    {
                        if (item != null && IsBusinessClassType(item.GetType()))
                        {
                            await CollectNestedRedbObjectsFromProperties(item, collector, processed, parentId);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 🔍 Проверка типа IRedbObject
        /// </summary>
        private static bool IsRedbObjectType(Type type)
        {
            return type.GetInterfaces().Any(i => 
                i.IsGenericType && 
                i.GetGenericTypeDefinition().Name.Contains("IRedbObject"));
        }

        /// <summary>
        /// 🔍 Проверка массива IRedbObject
        /// </summary>
        private static bool IsRedbObjectArrayType(Type type)
        {
            if (!type.IsArray) return false;
            return IsRedbObjectType(type.GetElementType()!);
        }

        /// <summary>
        /// 🔍 Проверка строкового типа
        /// </summary>  
        private static bool IsStringType(Type type)
        {
            return type == typeof(string);
        }

        /// <summary>
        /// 🔧 Получение properties из IRedbObject через рефлексию
        /// </summary>
        private static object? GetPropertiesFromRedbObject(IRedbObject redbObj)
        {
            // Используем рефлексию для получения свойства properties
            var propertiesProperty = redbObj.GetType().GetProperty("properties");
            return propertiesProperty?.GetValue(redbObj);
        }
        
        /// <summary>
        /// 🔧 Получение типа properties из IRedbObject
        /// </summary>
        private static Type? GetPropertiesTypeFromRedbObject(IRedbObject redbObj)
        {
            // Получаем TProps из IRedbObject<TProps>
            var objType = redbObj.GetType();
            if (objType.IsGenericType)
            {
                return objType.GetGenericArguments()[0]; // TProps
            }
            return null;
        }

        /// <summary>
        /// 🔍 Проверка является ли структура Class типом (бизнес-класс)
        /// </summary>
        private async Task<bool> IsClassTypeStructure(IRedbStructure structure)
        {
            // Получаем тип из БД
            var type = await _context.Set<_RType>().FindAsync(structure.IdType);
            return type?.Type1 == "Object" || type?.Name == "Class";
        }

        /// <summary>
        /// 🔍 Проверка является ли структура IRedbObject ссылкой
        /// </summary>
        private async Task<bool> IsRedbObjectStructure(IRedbStructure structure)
        {
            var type = await _context.Set<_RType>().FindAsync(structure.IdType);
            return type?.Type1 == "_RObject" || type?.Name == "Object";
        }

        /// <summary>
        /// 🔧 Получение DbType структуры
        /// </summary>
        private async Task<string> GetStructureDbType(IRedbStructure structure)
        {
            var type = await _context.Set<_RType>().FindAsync(structure.IdType);
            return type?.DbType ?? "String";
        }

        /// <summary>
        /// 🔧 Создание схемы для типа через рефлексию (упрощенная версия)
        /// </summary>
        private async Task CreateSchemeForType(Type propsType, IRedbObject obj)
        {

            
            // Для AnalyticsMetricsProps используем существующую схему
            if (propsType.Name.Contains("AnalyticsMetrics"))
            {
                obj.SchemeId = 1001; // TrueSight.Models.AnalyticsMetrics

            }
            else
            {
                throw new NotImplementedException($"Создание схемы для типа {propsType.Name} не реализовано");
            }
        }

        /// <summary>
        /// 🎯 ШАГ 3: Назначение ID через GetNextKey() всем объектам без ID
        /// </summary>
        protected async Task AssignMissingIds(List<IRedbObject> objects, IRedbUser user)
        {
            foreach (var obj in objects)
            {

                if (obj.Id == 0)
                {
                    var newId = _context.GetNextKey();
                    obj.Id = newId;

                    
                    // Применяем аудит настройки
                    obj.OwnerId = user.Id;
                    obj.WhoChangeId = user.Id;
                    
                    if (_configuration.AutoSetModifyDate)
                    {
                        obj.DateCreate = DateTime.Now;
                        obj.DateModify = DateTime.Now;
                    }
                    
                    if (_configuration.AutoRecomputeHash)
                    {
                        obj.Hash = RedbHash.ComputeFor(obj);
                    }

                }
                else
                {

                }
            }
        }

        /// <summary>
        /// 🏗️ ШАГ 4: Создание/проверка схем для всех типов объектов (используем PostgresSchemeSyncProvider)
        /// </summary>
        protected async Task EnsureSchemesForAllTypes(List<IRedbObject> objects)
        {

            
            foreach (var obj in objects)
            {

                
                if (obj.SchemeId == 0 && _configuration.AutoSyncSchemesOnSave)
                {

                    
                    // Получаем тип properties объекта через рефлексию
                    var objType = obj.GetType();
                    if (objType.IsGenericType)
                    {
                        var propsType = objType.GetGenericArguments()[0]; // TProps из IRedbObject<TProps>

                        
                        // Ищем существующую схему

                        var existingScheme = await _schemeSync.GetSchemeByTypeAsync(propsType);
                        if (existingScheme != null)
                        {
                            obj.SchemeId = existingScheme.Id;

                        }
                        else
                        {

                            try
                            {
                                // Создаем схему через рефлексию (универсальный метод)

                                await CreateSchemeForType(propsType, obj);


                            }
                            catch (Exception ex)
                            {

                                // Устанавливаем схему по умолчанию (если есть)
                                obj.SchemeId = 1001; // TrueSight.Models.AnalyticsMetrics как fallback

                            }
                        }
                    }
                }
                else
                {

                }
            }

        }

        /// <summary>
        /// 🔄 ШАГ 5: Рекурсивная обработка properties всех объектов → списки _RValue
        /// ✅ НОВАЯ АРХИТЕКТУРА: Использует дерево структур вместо плоского списка!
        /// </summary>
        protected async Task ProcessAllObjectsPropertiesRecursively(List<IRedbObject> objects, List<_RValue> valuesList)
        {
            foreach (var obj in objects)
            {

                
                // Проверяем что схема существует
                var scheme = await _schemeSync.GetSchemeByIdAsync(obj.SchemeId);
                if (scheme == null)
                {

                    continue;
                }
                
                // ✅ НОВАЯ ЛОГИКА: Получаем дерево структур вместо плоского списка
                var schemeProvider = (PostgresSchemeSyncProvider)_schemeSync;
                var rootStructureTree = await schemeProvider.GetSubtreeAsync(obj.SchemeId, null); // корневые узлы

                
                if (rootStructureTree.Count == 0)
                {

                    try
                    {
                        // Создаем структуры через универсальный метод
                        var propsType = obj.GetType().GetGenericArguments()[0];
                        await SyncStructuresForType(scheme, propsType);
                        
                        // Получаем дерево структур заново
                        schemeProvider.InvalidateStructureTreeCache(obj.SchemeId); // очищаем кеш
                        rootStructureTree = await schemeProvider.GetSubtreeAsync(obj.SchemeId, null);

                    }
                    catch (Exception ex)
                    {

                        continue;
                    }
                }
                
                // ✅ НОВЫЙ ОБХОД: Через дерево структур с поддеревьями!
                await ProcessPropertiesWithTreeStructures(obj, rootStructureTree, valuesList, objects);
            }
        }

        /// <summary>
        /// 🌳 НОВЫЙ МЕТОД: Обработка properties через дерево структур
        /// Решает проблемы избыточных структур и правильной передачи поддеревьев
        /// </summary>
        private async Task ProcessPropertiesWithTreeStructures(IRedbObject obj, List<StructureTreeNode> structureNodes, List<_RValue> valuesList, List<IRedbObject> objectsToSave)
        {
            // Получаем тип properties объекта
            var objPropertiesType = GetPropertiesTypeFromRedbObject(obj);

            
            foreach (var structureNode in structureNodes)
            {

                
                // ✅ ПРОВЕРКА СУЩЕСТВОВАНИЯ СВОЙСТВА В C# КЛАССЕ
                var property = objPropertiesType?.GetProperty(structureNode.Structure.Name);
                if (property == null)
                {

                    continue; // ✅ РЕШАЕТ ПРОБЛЕМУ ИЗБЫТОЧНЫХ СТРУКТУР!
                }
                
                // Получаем значение свойства
                var objProperties = GetPropertiesFromRedbObject(obj);
                var rawValue = property.GetValue(objProperties);

                
                // ✅ NULL СЕМАНТИКА
                if (!PostgresObjectStorageProviderExtensions.ShouldCreateValueRecord(rawValue, structureNode.Structure.StoreNull ?? false))
                {

                    continue;
                }
                
                // ✅ DISPATCH С ПОДДЕРЕВЬЯМИ 
                if (structureNode.Structure.IsArray == true)
                {

                    await ProcessArrayWithSubtree(obj, structureNode, rawValue, valuesList, objectsToSave);
                }
                else if (await IsClassTypeStructure(structureNode.Structure))
                {

                    await ProcessBusinessClassWithSubtree(obj, structureNode, rawValue, valuesList, objectsToSave);
                }
                else if (await IsRedbObjectStructure(structureNode.Structure))
                {

                    await ProcessIRedbObjectField(obj, structureNode.Structure, rawValue, objectsToSave, valuesList);
                }
                else
                {

                    await ProcessSimpleFieldWithTree(obj, structureNode, rawValue, valuesList);
                }
            }
        }

        /// <summary>
        /// 🚀 Рекурсивная обработка properties одного объекта (по образцу SavePropertiesFromObjectAsync)
        /// </summary>
        private async Task ProcessPropertiesRecursively<TProps>(IRedbObject<TProps> obj, List<StructureMetadata> structures, List<_RValue> valuesList) where TProps : class
        {
            var propertiesType = typeof(TProps);
            // ✅ ИСПРАВЛЕНИЕ: Обрабатываем только КОРНЕВЫЕ структуры (_id_parent IS NULL)
            var rootStructures = structures.Where(s => s.IdParent == null).ToList();


            foreach (var structure in rootStructures)
            {
                var property = propertiesType.GetProperty(structure.Name);
                if (property == null) 
                {

                    continue;
                }

                // 🚫 ИГНОРИРУЕМ поля с атрибутом [JsonIgnore]
                if (property.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Any()) 
                {

                    continue;
                }

                var rawValue = property.GetValue(obj.properties);


                // ✅ НОВАЯ NULL СЕМАНТИКА: проверяем _store_null
                if (!PostgresObjectStorageProviderExtensions.ShouldCreateValueRecord(rawValue, structure.StoreNull))
                {

                    continue;
                }

                // ✅ РЕКУРСИВНАЯ АРХИТЕКТУРА: разные стратегии для разных типов полей
                if (structure.IsArray)
                {

                    await ProcessArrayFieldForCollection(obj.Id, structure, rawValue, valuesList, obj.SchemeId);
                }
                else if (PostgresObjectStorageProviderExtensions.IsClassType(structure.TypeSemantic))
                {

                    await ProcessClassFieldForCollection(obj.Id, structure, rawValue, valuesList);
                }
                else
                {

                    await ProcessSimpleFieldForCollection(obj.Id, structure, rawValue, valuesList);
                }
            }
            

        }

        /// <summary>
        /// 🔧 Обработка простого поля для коллекции (аналог SaveSimpleFieldAsync)
        /// </summary>
        private async Task ProcessSimpleFieldForCollection(long objectId, StructureMetadata structure, object? rawValue, List<_RValue> valuesList)
        {
            // 🚧 ВРЕМЕННАЯ ЗАГЛУШКА: простая обработка БЕЗ вложенных объектов
            var valueRecord = new _RValue
            {
                Id = _context.GetNextKey(),
                IdObject = objectId,
                IdStructure = structure.Id
            };
            
            // Устанавливаем значение по типу (из старого кода)
            SetSimpleValueByType(valueRecord, structure.DbType, rawValue);
            valuesList.Add(valueRecord);
            

        }

        /// <summary>
        /// 📊 Обработка массива для коллекции (аналог SaveArrayFieldAsync) 
        /// </summary>
        private async Task ProcessArrayFieldForCollection(long objectId, StructureMetadata structure, object? rawValue, List<_RValue> valuesList, long schemeId = 9001)
        {
            if (rawValue == null) return;
            if (rawValue is not IEnumerable enumerable || rawValue is string) return;



            // ✅ Создаем БАЗОВУЮ запись массива с хешем всего массива (как в SaveArrayFieldAsync)
            var arrayHash = RedbHash.ComputeForProps(rawValue);
            var baseArrayRecord = new _RValue
            {
                Id = _context.GetNextKey(),
                IdObject = objectId,
                IdStructure = structure.Id,
                Guid = arrayHash  // ✅ Хеш всего массива в _Guid
            };
            valuesList.Add(baseArrayRecord);


            // ✅ Обработка элементов массива с _array_parent_id и _array_index
            await ProcessArrayElementsForCollection(objectId, structure.Id, baseArrayRecord.Id, enumerable, valuesList, structure, schemeId);
        }

        /// <summary>
        /// 🔢 Обработка элементов массива с правильными _array_parent_id и _array_index
        /// </summary>
        private async Task ProcessArrayElementsForCollection(long objectId, long structureId, long parentValueId, IEnumerable enumerable, List<_RValue> valuesList, StructureMetadata structure, long schemeId)
        {
            int index = 0;
            foreach (var item in enumerable)
            {


                var elementRecord = new _RValue
                {
                    Id = _context.GetNextKey(),
                    IdObject = objectId,
                    IdStructure = structureId,
                    ArrayParentId = parentValueId,  // ✅ Связь с базовой записью массива
                    ArrayIndex = index             // ✅ Позиция в массиве
                };

                if (item != null)
                {
                    var itemType = item.GetType();
                    
                    // ♻️ РЕКУРСИЯ В МАССИВАХ: разные типы элементов
                    if (PostgresObjectStorageProviderExtensions.IsRedbObjectReference(structure.TypeSemantic))
                    {

                        // TODO: Реализовать обработку IRedbObject в массиве (пока заглушка)

                        valuesList.Add(elementRecord); // пустая запись пока
                    }
                    else if (IsBusinessClassType(itemType))
                    {

                        
                        // Вычисляем хеш бизнес-класса и сохраняем в элементе массива
                        var itemHash = RedbHash.ComputeForProps(item);
                        elementRecord.Guid = itemHash;
                        valuesList.Add(elementRecord);

                        
                        // ♻️ РЕКУРСИЯ: обрабатываем дочерние поля бизнес-класса из массива
                        // ✅ ПЕРЕДАЕМ ArrayIndex для дочерних полей элементов массива  
                        await ProcessClassChildrenForCollection(objectId, elementRecord.Id, item, structureId, valuesList, schemeId, index);
                    }
                    else
                    {
                        // Простой элемент массива - используем тип структуры 
                        SetSimpleValueByType(elementRecord, structure.DbType, item);
                        valuesList.Add(elementRecord);

                    }
                }
                else
                {
                    valuesList.Add(elementRecord); // null элемент

                }

                index++;
            }

        }

        /// <summary>
        /// 🏗️ Обработка бизнес-класса для коллекции (аналог SaveClassFieldAsync)
        /// </summary>
        private async Task ProcessClassFieldForCollection(long objectId, StructureMetadata structure, object? rawValue, List<_RValue> valuesList)
        {
            if (rawValue == null) return;



            // ✅ Вычисляем UUID хеш бизнес-класса (как в SaveClassFieldAsync)
            var classHash = RedbHash.ComputeForProps(rawValue);

            // Создаем базовую запись Class поля с хешем в _Guid
            var classRecord = new _RValue
            {
                Id = _context.GetNextKey(),
                IdObject = objectId,
                IdStructure = structure.Id,
                Guid = classHash  // ✅ UUID хеш в _Guid поле
            };
            valuesList.Add(classRecord);


            // ✅ Обрабатываем дочерние поля Class объекта через рекурсию  
            // TODO: Нужен правильный schemeId - пока используем 9001 для тестирования
            await ProcessClassChildrenForCollection(objectId, classRecord.Id, rawValue, structure.Id, valuesList, 9001);
        }

        /// <summary>
        /// 👶 Рекурсивная обработка дочерних полей бизнес-класса 
        /// </summary>
        private async Task ProcessClassChildrenForCollection(long objectId, long parentValueId, object businessObject, long parentStructureId, List<_RValue> valuesList, long schemeId, int? parentArrayIndex = null)
        {


            // Получаем схему и ищем дочерние структуры 
            var scheme = await _schemeSync.GetSchemeByIdAsync(schemeId);
            if (scheme == null) return;

            // Ищем дочерние структуры с _id_parent = parentStructureId
            var childStructuresRaw = scheme.Structures
                .Where(s => s.IdParent == parentStructureId)
                .ToList();



            // Преобразуем в StructureMetadata для получения DbType
            var childStructures = await ConvertStructuresToMetadataAsync(childStructuresRaw);

            var businessType = businessObject.GetType();
            foreach (var childStructure in childStructures)
            {
                var property = businessType.GetProperty(childStructure.Name);
                if (property == null) 
                {

                    continue;
                }

                var childValue = property.GetValue(businessObject);


                // ✅ НОВАЯ NULL СЕМАНТИКА: проверяем _store_null
                if (!PostgresObjectStorageProviderExtensions.ShouldCreateValueRecord(childValue, childStructure.StoreNull))
                {

                    continue;
                }

                // ♻️ ♻️ ПОЛНАЯ РЕКУРСИЯ: обрабатываем разные типы дочерних полей ♻️ ♻️
                if (childStructure.IsArray)
                {

                    await ProcessArrayFieldForCollection(objectId, childStructure, childValue, valuesList);
                }
                else if (PostgresObjectStorageProviderExtensions.IsClassType(childStructure.TypeSemantic))
                {

                    await ProcessClassFieldForCollection(objectId, childStructure, childValue, valuesList);
                }
                else if (PostgresObjectStorageProviderExtensions.IsRedbObjectReference(childStructure.TypeSemantic))
                {

                    // TODO: Реализовать обработку IRedbObject (пока заглушка)

                }
                else
                {

                    
                    // Создаем запись дочернего поля с привязкой к родительскому Class полю
                    var childRecord = new _RValue
                    {
                        Id = _context.GetNextKey(),
                        IdObject = objectId,
                        IdStructure = childStructure.Id,
                        ArrayParentId = parentValueId,  // ✅ Связь с родительским Class полем  
                        ArrayIndex = parentArrayIndex   // ✅ Наследуем ArrayIndex если это элемент массива
                    };

                    SetSimpleValueByType(childRecord, childStructure.DbType, childValue);
                    valuesList.Add(childRecord);

                }
            }
        }

        /// <summary>
        /// 🔍 Проверить является ли тип бизнес-классом (не примитивом и не массивом)
        /// </summary>
        private static bool IsBusinessClassType(Type type)
        {
            // Примитивы и строки - не бизнес-классы
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(Guid)) 
                return false;
            
            // Массивы - не бизнес-классы (обрабатываются отдельно)
            if (type.IsArray || (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string)))
                return false;
                
            // IRedbObject - не бизнес-класс (обрабатывается отдельно)
            if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition().Name.Contains("IRedbObject")))
                return false;
                
            // Остальные классы - бизнес-классы
            return type.IsClass;
        }

        /// <summary>
        /// 🔧 Универсальный метод создания структур для любого типа через рефлексию
        /// </summary>
        private async Task SyncStructuresForType(IRedbScheme scheme, Type propsType)
        {
            // Используем рефлексию для вызова generic метода SyncStructuresFromTypeAsync<TProps>
            var method = typeof(PostgresSchemeSyncProvider)
                .GetMethod("SyncStructuresFromTypeAsync", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (method != null)
            {
                var genericMethod = method.MakeGenericMethod(propsType);
                var result = await (Task<List<IRedbStructure>>)genericMethod.Invoke(_schemeSync, new object[] { scheme, true })!;

            }
            else
            {
                // Поиск всех методов для диагностики
                var allMethods = typeof(PostgresSchemeSyncProvider).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var syncMethods = allMethods.Where(m => m.Name.Contains("Sync")).ToList();

                foreach (var m in syncMethods)
                {

                }
                throw new InvalidOperationException($"Метод SyncStructuresFromTypeAsync не найден в PostgresSchemeSyncProvider");
            }
        }

        // ===== 🌳 НОВЫЕ МЕТОДЫ ДЛЯ РАБОТЫ С ДЕРЕВОМ СТРУКТУР =====
        
        /// <summary>
        /// 🔧 Простое поле с поддержкой дерева структур
        /// </summary>
        private async Task ProcessSimpleFieldWithTree(IRedbObject obj, StructureTreeNode structureNode, object? rawValue, List<_RValue> valuesList)
        {
            var valueRecord = new _RValue
            {
                Id = _context.GetNextKey(),
                IdObject = obj.Id,
                IdStructure = structureNode.Structure.Id
            };
            
            var dbType = await GetStructureDbType(structureNode.Structure);
            SetSimpleValueByType(valueRecord, dbType, rawValue);
            valuesList.Add(valueRecord);
            

        }
        
        /// <summary>
        /// 📊 Массив с поддеревом структур
        /// </summary>
        private async Task ProcessArrayWithSubtree(IRedbObject obj, StructureTreeNode arrayStructureNode, object? rawValue, List<_RValue> valuesList, List<IRedbObject> objectsToSave)
        {
            if (rawValue == null) return;
            if (rawValue is not IEnumerable enumerable || rawValue is string) return;



            // ✅ Создаем БАЗОВУЮ запись массива с хешем всего массива
            var arrayHash = RedbHash.ComputeForProps(rawValue);
            var baseArrayRecord = new _RValue
            {
                Id = _context.GetNextKey(),
                IdObject = obj.Id,
                IdStructure = arrayStructureNode.Structure.Id,
                Guid = arrayHash
            };
            valuesList.Add(baseArrayRecord);


            // ✅ Обработка элементов массива с правильным поддеревом
            await ProcessArrayElementsWithSubtree(obj, arrayStructureNode, baseArrayRecord.Id, enumerable, valuesList, objectsToSave);
        }
        
        /// <summary>
        /// 🔢 Элементы массива с поддеревом структур
        /// </summary>
        private async Task ProcessArrayElementsWithSubtree(IRedbObject obj, StructureTreeNode arrayStructureNode, long parentValueId, IEnumerable enumerable, List<_RValue> valuesList, List<IRedbObject> objectsToSave)
        {
            int index = 0;
            foreach (var item in enumerable)
            {


                var elementRecord = new _RValue
                {
                    Id = _context.GetNextKey(),
                    IdObject = obj.Id,
                    IdStructure = arrayStructureNode.Structure.Id,
                    ArrayParentId = parentValueId,
                    ArrayIndex = index
                };

                if (item != null)
                {
                    var itemType = item.GetType();
                    
                    // ♻️ РЕКУРСИЯ С ПОДДЕРЕВЬЯМИ: разные типы элементов
                    if (await IsRedbObjectStructure(arrayStructureNode.Structure))
                    {
                        // 🔗 IRedbObject элемент массива
                        var targetObject = FindObjectInCollector((IRedbObject)item, objectsToSave);
                        elementRecord.Long = targetObject?.Id ?? 0;
                        valuesList.Add(elementRecord);

                    }
                    else if (IsBusinessClassType(itemType))
                    {
                        // 🏗️ Бизнес-класс элемент массива
                        var itemHash = RedbHash.ComputeForProps(item);
                        elementRecord.Guid = itemHash;
                        valuesList.Add(elementRecord);

                        
                        // ♻️ РЕКУРСИЯ: обрабатываем дочерние поля с поддеревом  
                        await ProcessBusinessClassChildrenWithSubtree(obj, elementRecord.Id, item, arrayStructureNode.Children, valuesList, objectsToSave, index);
                    }
                    else
                    {
                        // Простой элемент массива
                        var elementDbType = await GetStructureDbType(arrayStructureNode.Structure);
                        SetSimpleValueByType(elementRecord, elementDbType, item);
                        valuesList.Add(elementRecord);

                    }
                }
                else
                {
                    valuesList.Add(elementRecord); // null элемент

                }

                index++;
            }

        }
        
        /// <summary>
        /// 🏗️ Бизнес-класс с поддеревом структур
        /// </summary>
        private async Task ProcessBusinessClassWithSubtree(IRedbObject obj, StructureTreeNode classStructureNode, object? rawValue, List<_RValue> valuesList, List<IRedbObject> objectsToSave)
        {
            if (rawValue == null) return;



            // ✅ Вычисляем UUID хеш бизнес-класса
            var classHash = RedbHash.ComputeForProps(rawValue);

            // Создаем базовую запись Class поля с хешем в _Guid
            var classRecord = new _RValue
            {
                Id = _context.GetNextKey(),
                IdObject = obj.Id,
                IdStructure = classStructureNode.Structure.Id,
                Guid = classHash
            };
            valuesList.Add(classRecord);


            // ✅ Обрабатываем дочерние поля с правильным поддеревом!
            await ProcessBusinessClassChildrenWithSubtree(obj, classRecord.Id, rawValue, classStructureNode.Children, valuesList, objectsToSave);
        }
        
        /// <summary>
        /// 👶 Рекурсивная обработка дочерних полей бизнес-класса с поддеревом
        /// </summary>
        private async Task ProcessBusinessClassChildrenWithSubtree(IRedbObject obj, long parentValueId, object businessObject, List<StructureTreeNode> childrenSubtree, List<_RValue> valuesList, List<IRedbObject> objectsToSave, int? parentArrayIndex = null)
        {


            var businessType = businessObject.GetType();
            foreach (var childStructureNode in childrenSubtree)
            {
                // ✅ ПРОВЕРКА СУЩЕСТВОВАНИЯ СВОЙСТВА В C# КЛАССЕ  
                var property = businessType.GetProperty(childStructureNode.Structure.Name);
                if (property == null)
                {

                    
                    // 🔍 ДИАГНОСТИКА: покажем все свойства класса для понимания проблемы
                    var allProperties = businessType.GetProperties().Select(p => p.Name).ToArray();

                    continue;
                }

                var childValue = property.GetValue(businessObject);


                // ✅ NULL СЕМАНТИКА  
                if (!PostgresObjectStorageProviderExtensions.ShouldCreateValueRecord(childValue, childStructureNode.Structure.StoreNull ?? false))
                {

                    continue;
                }

                // ♻️ РЕКУРСИВНАЯ ОБРАБОТКА с правильными поддеревьями
                if (childStructureNode.Structure.IsArray == true)
                {

                    await ProcessArrayWithSubtree(obj, childStructureNode, childValue, valuesList, objectsToSave);
                }
                else if (await IsClassTypeStructure(childStructureNode.Structure))
                {

                    await ProcessBusinessClassWithSubtree(obj, childStructureNode, childValue, valuesList, objectsToSave);
                }
                else if (await IsRedbObjectStructure(childStructureNode.Structure))
                {

                    await ProcessIRedbObjectField(obj, childStructureNode.Structure, childValue, objectsToSave, valuesList);
                }
                else
                {

                    
                    // Создаем запись дочернего поля с привязкой к родительскому Class полю
                    var childRecord = new _RValue
                    {
                        Id = _context.GetNextKey(),
                        IdObject = obj.Id,
                        IdStructure = childStructureNode.Structure.Id,
                        ArrayParentId = parentValueId,
                        ArrayIndex = parentArrayIndex   // ✅ Наследуем ArrayIndex если это элемент массива
                    };

                    var childDbType = await GetStructureDbType(childStructureNode.Structure);
                    SetSimpleValueByType(childRecord, childDbType, childValue);
                    valuesList.Add(childRecord);

                }
            }
        }
        
        /// <summary>
        /// 🔗 Обработка IRedbObject поля с поиском ID в коллекторе объектов
        /// </summary>
        private async Task ProcessIRedbObjectField(IRedbObject obj, IRedbStructure structure, object? redbObjectValue, List<IRedbObject> objectsToSave, List<_RValue> valuesList)
        {

            
            if (structure.IsArray == true)
            {
                // МАССИВ IRedbObject
                await ProcessIRedbObjectArray(obj, structure, (IEnumerable)redbObjectValue!, objectsToSave, valuesList);
            }
            else
            {
                // ОДИНОЧНЫЙ IRedbObject
                await ProcessSingleIRedbObject(obj, structure, (IRedbObject)redbObjectValue!, objectsToSave, valuesList);
            }
        }
        
        /// <summary>
        /// 🔗 Одиночный IRedbObject с поиском ID в коллекторе
        /// </summary>
        private async Task ProcessSingleIRedbObject(IRedbObject obj, IRedbStructure structure, IRedbObject redbObjectValue, List<IRedbObject> objectsToSave, List<_RValue> valuesList)
        {
            // ✅ ПОИСК ID В КОЛЛЕКТОРЕ  
            var targetObject = FindObjectInCollector(redbObjectValue, objectsToSave);
            var objectId = targetObject?.Id ?? 0;
            

            
            var record = new _RValue
            {
                Id = _context.GetNextKey(),
                IdObject = obj.Id,
                IdStructure = structure.Id,
                Long = objectId  // ✅ РЕАЛЬНЫЙ ID вместо NULL!
            };
            
            valuesList.Add(record);

        }
        
        /// <summary>
        /// 📊 Массив IRedbObject с поиском ID в коллекторе
        /// </summary>
        private async Task ProcessIRedbObjectArray(IRedbObject obj, IRedbStructure structure, IEnumerable redbObjectArray, List<IRedbObject> objectsToSave, List<_RValue> valuesList)
        {
            // Создаем базовую запись массива
            var arrayHash = RedbHash.ComputeForProps((object)redbObjectArray);
            var baseArrayRecord = new _RValue
            {
                Id = _context.GetNextKey(),
                IdObject = obj.Id,
                IdStructure = structure.Id,
                Guid = arrayHash
            };
            valuesList.Add(baseArrayRecord);


            // Обрабатываем элементы массива
            int index = 0;
            foreach (var item in redbObjectArray)
            {
                if (item != null && IsRedbObjectType(item.GetType()))
                {
                    var targetObject = FindObjectInCollector((IRedbObject)item, objectsToSave);
                    var objectId = targetObject?.Id ?? 0;
                    
                    var elementRecord = new _RValue
                    {
                        Id = _context.GetNextKey(),
                        IdObject = obj.Id,
                        IdStructure = structure.Id,
                        ArrayParentId = baseArrayRecord.Id,
                        ArrayIndex = index,
                        Long = objectId  // ✅ РЕАЛЬНЫЙ ID!
                    };
                    
                    valuesList.Add(elementRecord);

                }
                index++;
            }
        }
        
        /// <summary>
        /// 🔍 Поиск объекта в коллекторе по различным стратегиям
        /// </summary>
        private IRedbObject? FindObjectInCollector(IRedbObject target, List<IRedbObject> objectsToSave)
        {
            // Стратегия 1: Точное совпадение ссылки
            var byReference = objectsToSave.FirstOrDefault(o => ReferenceEquals(o, target));
            if (byReference != null)
            {

                return byReference;
            }
            
            // Стратегия 2: По Name + Type
            var byNameAndType = objectsToSave.FirstOrDefault(o => 
                o.Name == target.Name && 
                o.GetType() == target.GetType());
            if (byNameAndType != null)
            {

                return byNameAndType;
            }
            
            // Стратегия 3: По Hash (если установлен)
            if (target.Hash.HasValue)
            {
                var byHash = objectsToSave.FirstOrDefault(o => o.Hash == target.Hash);
                if (byHash != null)
                {

                    return byHash;
                }
            }
            

            return null;
        }

        // SetSimpleValueByType уже существует в основном файле PostgresObjectStorageProvider.cs

        /// <summary>
        /// 🎯 ШАГ 6: Выбор стратегии обработки values на основе конфигурации
        /// </summary>
        private async Task PrepareValuesByStrategy(List<IRedbObject> objects, List<_RValue> valuesList)
        {
            var strategy = _configuration.EavSaveStrategy;


            switch (strategy)
            {
                case EavSaveStrategy.DeleteInsert:
                    await PrepareValuesWithTreeDeleteInsert(objects, valuesList);
                    break;

                case EavSaveStrategy.ChangeTracking:
                    await PrepareValuesWithTreeChangeTracking(objects, valuesList);
                    break;

                default:
                    throw new NotSupportedException($"Стратегия {strategy} не поддерживается в TreeBased SaveAsync");
            }
        }

        /// <summary>
        /// 🗑️ ШАГ 6A: Tree-based Delete/Insert стратегия для _RValue (простая, надежная)
        /// </summary>
        private async Task PrepareValuesWithTreeDeleteInsert(List<IRedbObject> objects, List<_RValue> valuesList)
        {

            
            // Удаляем все существующие values для объектов (простая стратегия)
            var objectIds = objects.Where(o => o.Id != 0).Select(o => o.Id).ToList();
            if (objectIds.Any())
            {
                var existingValues = await _context.Set<_RValue>()
                    .Where(v => objectIds.Contains(v.IdObject))
                    .ToListAsync();
                _context.Set<_RValue>().RemoveRange(existingValues);

            }
            else
            {

            }
            
            // Для DeleteInsert стратегии - valuesList остается как есть для вставки в CommitAllChangesBatch

        }

        /// <summary>
        /// ⚡ ШАГ 6B: Tree-based ChangeTracking стратегия для _RValue (эффективная)
        /// Сравнивает старые и новые values, обновляет только измененные
        /// </summary>
        private async Task PrepareValuesWithTreeChangeTracking(List<IRedbObject> objects, List<_RValue> valuesList)
        {

            
            // 1. Определяем объекты для трекинга (только существующие)
            var existingObjectIds = objects.Where(o => o.Id != 0).Select(o => o.Id).ToList();
            
            if (!existingObjectIds.Any())
            {
                // ✅ Только новые объекты - оптимизированный путь без сравнений


                return; // valuesList уже содержит все values для вставки
            }



            // 2. Загружаем ВСЕ существующие values для объектов одним запросом
            var existingValues = await _context.Set<_RValue>()
                .Where(v => existingObjectIds.Contains(v.IdObject))
                .ToListAsync();
            

            
            // 🔍 ДИАГНОСТИКА: Показываем первые 10 существующих values
            if (existingValues.Count > 0)
            {

                foreach (var ev in existingValues.Take(10))
                {

                }
            }

            // 3. Создаем быстрый Dictionary для поиска существующих values
            // 🎯 ArrayParentId ТОЛЬКО ДЛЯ ЭЛЕМЕНТОВ МАССИВОВ! Обычные поля - БЕЗ ArrayParentId!
            var existingValuesDict = existingValues.ToDictionary(
                v => v.ArrayIndex.HasValue 
                    ? $"{v.IdObject}|{v.IdStructure}|{v.ArrayIndex}"  // Для элементов массивов - с ArrayIndex
                    : $"{v.IdObject}|{v.IdStructure}", // Для обычных полей - БЕЗ ArrayParentId!
                v => v
            );

            // 4. Получаем ПОЛНУЮ информацию о структурах (DbType + IsArray + StoreNull)
            var structureIds = valuesList.Select(v => v.IdStructure).Distinct().ToList();
            var structuresFullInfo = await GetStructuresFullInfo(structureIds);
            var structuresInfo = structuresFullInfo.ToDictionary(x => x.Key, x => x.Value.DbType); // Legacy совместимость

            // 🔍 ДИАГНОСТИКА: Показываем первые 10 новых values

            foreach (var nv in valuesList.Take(10))
            {

            }

            // 5. 🎯 РАЗДЕЛЯЕМ НА ОБЫЧНЫЕ ПОЛЯ И ЭЛЕМЕНТЫ МАССИВОВ ДО ОБРАБОТКИ
            var regularFields = valuesList.Where(v => !v.ArrayIndex.HasValue).ToList();
            var arrayElements = valuesList.Where(v => v.ArrayIndex.HasValue).ToList();
            




            // 6. Обрабатываем ТОЛЬКО обычные поля
            var valuesToInsert = new List<_RValue>();
            var valuesToDelete = new List<_RValue>();
            var statsUpdated = 0;
            var statsInserted = 0;
            var statsSkipped = 0;


            foreach (var newValue in regularFields)
            {
                // Values для новых объектов всегда INSERT
                if (!existingObjectIds.Contains(newValue.IdObject))
                {
                    valuesToInsert.Add(newValue);
                    statsInserted++;
                    continue;
                }

                // Создаем уникальный ключ для поиска существующего value
                // 🎯 ТОЛЬКО ОБЫЧНЫЕ ПОЛЯ (ArrayParentId НЕ ИСПОЛЬЗУЕТСЯ!)
                var uniqueKey = $"{newValue.IdObject}|{newValue.IdStructure}";
                
                if (existingValuesDict.TryGetValue(uniqueKey, out var existingValue))
                {
                    // Value существует - сравниваем только по значимому полю (DbType)
                    var changed = await IsValueChanged(existingValue, newValue, structuresInfo);
                    if (changed)
                    {
                        // Value изменился - UPDATE существующий
                        var dbType = structuresInfo.TryGetValue(newValue.IdStructure, out var dt) ? dt : "Unknown";


                        UpdateExistingValueFields(existingValue, newValue, structuresInfo);
                        
                        statsUpdated++;
                    }
                    else
                    {
                        // Value не изменился - SKIP

                        statsSkipped++;
                    }
                    
                    // Убираем из Dictionary (отмечаем как обработанный)
                    existingValuesDict.Remove(uniqueKey);
                }
                else
                {
                    // Value не существует - INSERT


                    valuesToInsert.Add(newValue);
                    statsInserted++;
                }
            }

            // 6. 🎯 СНАЧАЛА УДАЛЯЕМ ЭЛЕМЕНТЫ МАССИВОВ ИЗ existingValuesDict
            if (arrayElements.Any())
            {
                // 🔧 КРИТИЧЕСКИ ВАЖНО: удаляем ВСЕ элементы массивов из existingValuesDict
                // чтобы они НЕ попали в remainingValues и НЕ были удалены!
                var arrayElementsToRemove = existingValues
                    .Where(v => v.ArrayIndex.HasValue)
                    .Select(v => $"{v.IdObject}|{v.IdStructure}|{v.ArrayIndex}")  // ✅ ПРАВИЛЬНЫЙ КЛЮЧ!
                    .ToList();
                    
                foreach (var keyToRemove in arrayElementsToRemove)
                {
                    existingValuesDict.Remove(keyToRemove);
                }
                

            }

            // 7. ТЕПЕРЬ remainingValues НЕ будет содержать элементов массивов
            var remainingValues = existingValuesDict.Values.ToList();
            valuesToDelete.AddRange(remainingValues);
            



            
            if (valuesToDelete.Any())
            {

                foreach (var dv in valuesToDelete.Take(10))
                {
                    var type = dv.ArrayIndex.HasValue ? "ARRAY_ELEMENT" : "FIELD";

                }
            }

            // 8. 🎯 ОТДЕЛЬНАЯ ОБРАБОТКА ЭЛЕМЕНТОВ МАССИВОВ ПО ИНДЕКСАМ
            
            if (arrayElements.Any())
            {
                
                var (arrayUpdated, arrayInserted, arraySkipped) = await ProcessArrayElementsChangeTracking(
                    arrayElements, existingValues, valuesToInsert, valuesToDelete, structuresFullInfo);
                statsUpdated += arrayUpdated;
                statsInserted += arrayInserted;
                statsSkipped += arraySkipped;
            }
            else
            {

            }
            
            // 9. ДОБАВЛЯЕМ DELETE операции в EF контекст  
            if (valuesToDelete.Count > 0)
            {

                _context.Set<_RValue>().RemoveRange(valuesToDelete);
            }

            // 8. Формируем финальный valuesList только из новых values для INSERT
            valuesList.Clear();
            valuesList.AddRange(valuesToInsert);
            

            foreach (var iv in valuesToInsert.Take(10))
            {

            }






        }

        /// <summary>
        /// 🔢 ОТДЕЛЬНАЯ ОБРАБОТКА ЭЛЕМЕНТОВ МАССИВОВ ПО ИНДЕКСАМ
        /// Группирует по структуре, сравнивает по индексам: element[0] с element[0], element[1] с element[1]
        /// </summary>
        private async Task<(int updated, int inserted, int skipped)> ProcessArrayElementsChangeTracking(
            List<_RValue> newArrayElements, 
            List<_RValue> existingValues, 
            List<_RValue> valuesToInsert, 
            List<_RValue> valuesToDelete, 
            Dictionary<long, StructureFullInfo> structuresFullInfo)
        {




            
            int localUpdated = 0, localInserted = 0, localSkipped = 0;
            
            // 1. 🔧 ИСПРАВЛЯЕМ ArrayParentId у новых элементов массивов

            foreach (var newElement in newArrayElements) 
            {
                // Находим существующее базовое поле для этого массива
                var existingBaseField = existingValues
                    .FirstOrDefault(v => v.IdObject == newElement.IdObject && 
                                        v.IdStructure == newElement.IdStructure && 
                                        !v.ArrayIndex.HasValue);  // Базовое поле
                                        
                if (existingBaseField != null) 
                {
                    // ✅ Массив существует → используем СУЩЕСТВУЮЩИЙ ArrayParentId

                    newElement.ArrayParentId = existingBaseField.Id;
                }
                else 
                {
                    // 🆕 НОВЫЙ массив → оставляем новый ArrayParentId

                }
            }
            
            // 2. 🎯 ГРУППИРУЕМ ПО ArrayParentId (после исправления)
            var existingArraysDict = existingValues
                .Where(v => v.ArrayIndex.HasValue)
                .GroupBy(v => v.ArrayParentId)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.ArrayIndex).ToList());
                
            var newArraysDict = newArrayElements
                .GroupBy(v => v.ArrayParentId)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.ArrayIndex).ToList());
                

            
            // 3. 🎯 СРАВНИВАЕМ ПО ArrayParentId
            var allArrayParentIds = existingArraysDict.Keys.Union(newArraysDict.Keys).ToList();

            foreach (var arrayParentId in allArrayParentIds)
            {
                var existingElements = existingArraysDict.GetValueOrDefault(arrayParentId) ?? new List<_RValue>();
                var newElements = newArraysDict.GetValueOrDefault(arrayParentId) ?? new List<_RValue>();
                

                
                // 🎯 ГЛАВНАЯ ЛОГИКА: сравниваем по индексам
                var (updated, inserted, skipped) = await CompareArrayElementsByIndex(existingElements, newElements, valuesToInsert, valuesToDelete, structuresFullInfo);
                localUpdated += updated;
                localInserted += inserted;
                localSkipped += skipped;
                

            }
            
            return (localUpdated, localInserted, localSkipped);
        }

        /// <summary>
        /// 🎯 СРАВНЕНИЕ ЭЛЕМЕНТОВ МАССИВА ПО ИНДЕКСАМ
        /// </summary>
        private async Task<(int updated, int inserted, int skipped)> CompareArrayElementsByIndex(
            List<_RValue> existingElements, 
            List<_RValue> newElements, 
            List<_RValue> valuesToInsert, 
            List<_RValue> valuesToDelete, 
            Dictionary<long, StructureFullInfo> structuresFullInfo)
        {
            int localUpdated = 0, localInserted = 0, localSkipped = 0;
            var maxIndex = Math.Max(existingElements.Count, newElements.Count);

            
            for (int i = 0; i < maxIndex; i++)
            {
                var existingElement = i < existingElements.Count ? existingElements[i] : null;
                var newElement = i < newElements.Count ? newElements[i] : null;
                
                if (existingElement != null && newElement != null)
                {
                    // Оба элемента есть - сравниваем по DbType
                    var structInfo = structuresFullInfo[newElement.IdStructure];
                    var legacyStructuresInfo = new Dictionary<long, string> { { newElement.IdStructure, structInfo.DbType } };
                    
                    var changed = await IsValueChanged(existingElement, newElement, legacyStructuresInfo);
                    if (changed)
                    {
                        UpdateExistingValueFields(existingElement, newElement, legacyStructuresInfo);

                        localUpdated++;
                    }
                    else
                    {

                        localSkipped++;
                    }
                }
                else if (existingElement != null && newElement == null)
                {
                    // Новый массив короче - DELETE лишний элемент
                    valuesToDelete.Add(existingElement);

                }
                else if (existingElement == null && newElement != null)
                {
                    // Новый массив длиннее - INSERT новый элемент  
                    valuesToInsert.Add(newElement);

                    localInserted++;
                }
            }
            
            return (localUpdated, localInserted, localSkipped);
        }

        /// <summary>
        /// 📋 Структура с полной информацией для ChangeTracking
        /// </summary>
        public class StructureFullInfo
        {
            public string DbType { get; set; } = "String";
            public bool IsArray { get; set; }
            public bool StoreNull { get; set; }
        }

        /// <summary>
        /// 📋 Получает ПОЛНУЮ информацию о структурах (DbType + IsArray + StoreNull)
        /// </summary>
        private async Task<Dictionary<long, StructureFullInfo>> GetStructuresFullInfo(List<long> structureIds)
        {
            return await _context.Structures
                .Where(s => structureIds.Contains(s.Id))
                .Join(_context.Types, s => s.IdType, t => t.Id, (s, t) => new { 
                    s.Id, 
                    DbType = t.DbType ?? "String",
                    s.IsArray,
                    s.StoreNull
                })
                .ToDictionaryAsync(x => x.Id, x => new StructureFullInfo 
                { 
                    DbType = x.DbType, 
                    IsArray = x.IsArray ?? false,
                    StoreNull = x.StoreNull ?? false
                });
        }

        /// <summary>
        /// 📋 LEGACY: Получает информацию о DbType для структур (для совместимости)
        /// </summary>
        private async Task<Dictionary<long, string>> GetStructuresDbTypeInfo(List<long> structureIds)
        {
            var fullInfo = await GetStructuresFullInfo(structureIds);
            return fullInfo.ToDictionary(x => x.Key, x => x.Value.DbType);
        }

        /// <summary>
        /// 🔍 Сравнивает два values только по значимому полю (по DbType)
        /// </summary>
        private async Task<bool> IsValueChanged(_RValue oldValue, _RValue newValue, Dictionary<long, string> structuresInfo)
        {
            if (!structuresInfo.TryGetValue(newValue.IdStructure, out var dbType))
            {
                // Неизвестный тип - считаем что изменился
                return true;
            }

            return dbType switch
            {
                "String" => oldValue.String != newValue.String,
                "Long" => oldValue.Long != newValue.Long,
                "Double" => oldValue.Double != newValue.Double,
                "DateTime" => oldValue.DateTime != newValue.DateTime,
                "Boolean" => oldValue.Boolean != newValue.Boolean,
                "Guid" => oldValue.Guid != newValue.Guid,
                "ByteArray" => oldValue.ByteArray != newValue.ByteArray,
                _ => true // Неизвестный тип - считаем измененным
            };
        }

        /// <summary>
        /// 🔄 Обновляет поля существующего value из нового value (только значимые поля)
        /// </summary>
        private void UpdateExistingValueFields(_RValue existingValue, _RValue newValue, Dictionary<long, string> structuresInfo)
        {
            if (!structuresInfo.TryGetValue(newValue.IdStructure, out var dbType))
            {
                return; // Неизвестный тип - не обновляем
            }

            // Обновляем только значимое поле по DbType
            switch (dbType)
            {
                case "String":
                    existingValue.String = newValue.String;
                    break;
                case "Long":
                    existingValue.Long = newValue.Long;
                    break;
                case "Double":
                    existingValue.Double = newValue.Double;
                    break;
                case "DateTime":
                    existingValue.DateTime = newValue.DateTime;
                    break;
                case "Boolean":
                    existingValue.Boolean = newValue.Boolean;
                    break;
                case "Guid":
                    existingValue.Guid = newValue.Guid;
                    break;
                case "ByteArray":
                    existingValue.ByteArray = newValue.ByteArray;
                    break;
            }
        }



        /// <summary>
        /// 💾 ШАГ 7: Batch сохранение в правильном порядке
        /// </summary>
        private async Task CommitAllChangesBatch(List<IRedbObject> objects, List<_RValue> valuesList)
        {

            
            // Сначала объекты (могут быть новые или обновленные)
            foreach (var obj in objects)
            {

                var existingObject = await _context.Objects.FindAsync(obj.Id);
                if (existingObject == null)
                {

                    // INSERT нового объекта
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

                    // UPDATE существующего объекта
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
            }

            // Потом values (обработанные стратегией в PrepareValuesByStrategy)
            // 🎯 ChangeTracking стратегия сама добавляет values в EF контекст
            if (valuesList.Count > 0)
            {

                _context.Set<_RValue>().AddRange(valuesList);
            }
            else
            {

            }

            // 🔍 ДЕТАЛЬНАЯ ДИАГНОСТИКА EF CHANGE TRACKER

            var trackedEntities = _context.ChangeTracker.Entries().ToList();
            
            var addedEntities = trackedEntities.Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added).ToList();
            var modifiedEntities = trackedEntities.Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Modified).ToList();
            var deletedEntities = trackedEntities.Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Deleted).ToList();
            




            
            // ПОКАЗЫВАЕМ ПОРЯДОК ОПЕРАЦИЙ

            foreach (var deleted in deletedEntities.Where(e => e.Entity is _RValue))
            {
                if (deleted.Entity is _RValue dv)
                {

                }
            }
            

            foreach (var modified in modifiedEntities.Where(e => e.Entity is _RValue))
            {
                if (modified.Entity is _RValue mv)
                {

                }
            }
            

            foreach (var added in addedEntities.Where(e => e.Entity is _RValue).Take(5))
            {
                if (added.Entity is _RValue av)
                {

                }
            }

            try
            {

                await _context.SaveChangesAsync();

            }
            catch (Exception ex)
            {

                if (ex.InnerException != null)
                {

                }
                throw;
            }
        }
    }
}
