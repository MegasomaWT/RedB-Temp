using redb.Core.Providers;
using redb.Core.DBModels;
using redb.Core.Utils;
using redb.Core.Models.Contracts;
using redb.Core.Models.Entities;
using redb.Core.Models.Configuration;
using redb.Core.Models.Security;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using EFCore.BulkExtensions;

namespace redb.Core.Postgres.Providers
{
    /// <summary>
    /// 🚀 МАССОВАЯ ВСТАВКА - высокопроизводительное создание множества объектов
    /// </summary>
    public partial class PostgresObjectStorageProvider
    {
        /// <summary>
        /// 🚀 МАССОВАЯ ВСТАВКА: Создать множество новых объектов за одну операцию (БЕЗ проверки прав)
        /// Переиспользует логику из SaveAsyncNew + BulkInsert для максимальной производительности
        /// </summary>
        public async Task<List<long>> AddNewObjectsAsync<TProps>(List<IRedbObject<TProps>> objects) where TProps : class, new()
        {
            return await AddNewObjectsAsync(objects, _securityContext.CurrentUser);
        }

        /// <summary>
        /// 🚀 МАССОВАЯ ВСТАВКА с явным пользователем: Создать множество новых объектов (БЕЗ проверки прав)
        /// </summary>
        public async Task<List<long>> AddNewObjectsAsync<TProps>(List<IRedbObject<TProps>> objects, IRedbUser user) where TProps : class, new()
        {
            if (objects == null || !objects.Any())
            {
                return new List<long>();
            }




            // Валидация
            foreach (var obj in objects)
            {
                if (obj.properties == null)
                {
                    throw new ArgumentException($"Свойства объекта '{obj.Name}' не могут быть null", nameof(objects));
                }
            }

            // Начальная обработка хешей и схем для главных объектов

            foreach (var obj in objects)
            {
                // Пересчет хеша (из SaveAsyncNew)
                var currentHash = RedbHash.ComputeFor(obj);
                if (currentHash.HasValue)
                {
                    obj.Hash = currentHash.Value;
                }

                // Автоопределение схемы (из SaveAsyncNew, но БЕЗ проверок существующих объектов)
                if (obj.SchemeId == 0 && _configuration.AutoSyncSchemesOnSave)
                {

                    var existingScheme = await _schemeSync.GetSchemeByTypeAsync<TProps>();
                    if (existingScheme != null)
                    {
                        obj.SchemeId = existingScheme.Id;

                    }
                    else if (_configuration.AutoSyncSchemesOnSave)
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
            }

            // === ПЕРЕИСПОЛЬЗУЕМ ЛОГИКУ ИЗ SaveAsyncNew ===

            var objectsToSave = new List<IRedbObject>();
            var valuesToSave = new List<_RValue>();
            var processedObjectIds = new HashSet<long>();

            // ШАГ 2: Рекурсивный сбор всех объектов (главный + вложенные IRedbObject)

            foreach (var obj in objects)
            {
                await CollectAllObjectsRecursively(obj, objectsToSave, processedObjectIds);
            }


            // ШАГ 3: Назначение ID всем объектам без ID (через GetNextKey)

            await AssignMissingIds(objectsToSave, user);
            
            // ✅ Устанавливаем ParentId для вложенных объектов после назначения ID
            var mainObjectIds = objects.Select(o => o.Id).ToHashSet();
            foreach (var obj in objectsToSave)
            {
                if (!mainObjectIds.Contains(obj.Id))
                {
                    // Это вложенный объект - устанавливаем ParentId = ID главного объекта
                    obj.ParentId = objects.First().Id;

                }
            }

            // ШАГ 4: Создание/проверка схем для всех типов объектов

            await EnsureSchemesForAllTypes(objectsToSave);


            // ШАГ 5: Рекурсивная обработка properties всех объектов в списки values

            await ProcessAllObjectsPropertiesRecursively(objectsToSave, valuesToSave);

            
            // ШАГ 6: БЕЗ Delete стратегии - это НОВЫЕ объекты


            // ШАГ 7: BULK ВСТАВКА вместо обычного сохранения

            await CommitAllChangesBulk(objectsToSave, valuesToSave);


            // Возвращаем ID всех созданных главных объектов
            var resultIds = objects.Select(o => o.Id).ToList();

            
            return resultIds;
        }

        /// <summary>
        /// 🚀 ШАГ 7 (BULK): Массовое сохранение с BulkInsert вместо Add()
        /// </summary>
        private async Task CommitAllChangesBulk(List<IRedbObject> objects, List<_RValue> valuesList)
        {


            // Конфигурация для BulkInsert
            var bulkConfig = new BulkConfig
            {
                BatchSize = 1000,           // Размер пакета для разбивки
                SetOutputIdentity = false,   // ID уже установлены через GetNextKey
                PreserveInsertOrder = true,  // важен порядок: объекты → values
                BulkCopyTimeout = 60,        // Таймаут операции
                UseTempDB = false            // Прямо в основную БД
            };

            try
            {
                // 1. BULK INSERT объектов
                if (objects.Count > 0)
                {

                    
                    var objectRecords = objects.Select(obj =>
                    {
                        var record = new _RObject
                        {
                            Id = obj.Id,
                            IdScheme = obj.SchemeId,
                            IdParent = obj.ParentId,
                            IdOwner = obj.OwnerId,
                            IdWhoChange = obj.WhoChangeId,
                            Name = obj.Name,
                            Hash = obj.Hash,
                            
                            // ✅ ИСПРАВЛЕНИЕ: Все пропущенные служебные поля
                            DateBegin = obj.DateBegin,        // ❗ КРИТИЧНО: Поле которое пропадало!
                            DateComplete = obj.DateComplete,
                            Key = obj.Key,
                            CodeInt = obj.CodeInt,
                            CodeString = obj.CodeString,
                            CodeGuid = obj.CodeGuid,
                            Bool = obj.Bool,
                            Note = obj.Note
                        };
                        
                        // ✅ ИСПРАВЛЕНИЕ: Учитываем конфигурацию AutoSetModifyDate как в SaveAsync
                        if (_configuration.AutoSetModifyDate)
                        {
                            record.DateCreate = DateTime.Now;
                            record.DateModify = DateTime.Now;
                        }
                        else
                        {
                            // Используем значения из объекта
                            record.DateCreate = obj.DateCreate;
                            record.DateModify = obj.DateModify;
                        }
                        
                        return record;
                    }).ToList();

                    await _context.BulkInsertAsync(objectRecords, bulkConfig);

                }

                // 2. BULK INSERT values
                if (valuesList.Count > 0)
                {

                    await _context.BulkInsertAsync(valuesList, bulkConfig);

                }


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
