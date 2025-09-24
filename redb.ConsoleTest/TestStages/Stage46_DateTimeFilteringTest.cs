using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.Configuration;
using redb.Core.Models;
using redb.Core.Models.Attributes;
using redb.Core.Models.Configuration;
using redb.Core.Models.Contracts;
using redb.Core.Models.Entities;
using redb.Core.Providers;
using redb.Core.Query;
using System;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// 🐛 Этап 46: Тестирование проблемы с фильтрацией DateTime в TreeQueryAsync
    /// Воспроизводит различное поведение при фильтрации по DateTime с промежуточными фильтрами и без
    /// </summary>
    public class Stage46_DateTimeFilteringTest : BaseTestStage
    {
        public override string Name => "Тест фильтрации DateTime в TreeQueryAsync";
        public override string Description => "Воспроизведение проблемы различного поведения DateTime фильтров";
        public override int Order => 46;

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🐛 === ТЕСТ ФИЛЬТРАЦИИ DATETIME В TREEQUERYASYNC ===");
            logger.LogInformation("Воспроизводим проблему различного поведения DateTime фильтров");

            // ===== ПОДГОТОВКА ДАННЫХ =====
            logger.LogInformation("📋 Подготовка тестовых данных");

            // Инициализируем RedbObjectFactory
            var schemeSyncProvider = redb as ISchemeSyncProvider;
            if (schemeSyncProvider != null)
            {
                RedbObjectFactory.Initialize(schemeSyncProvider);
                logger.LogInformation("✅ RedbObjectFactory инициализирован");
            }

            // Синхронизируем схемы
            await redb.SyncSchemeAsync<MyDate>();
            await redb.SyncSchemeAsync<TestParent>();
            logger.LogInformation("✅ Схемы синхронизированы");

            // Создаем родительский объект через фабрику
            var parentObj = await RedbObjectFactory.CreateAsync(new TestParent { Name = "Parent for DateTime testing" });

            var parentId = await redb.SaveAsync(parentObj);
            logger.LogInformation($"✅ Родительский объект создан с ID: {parentId}");

            // Создаем тестовый объект с DateTime через фабрику
            var testDate = new DateTime(2023, 12, 1, 10, 30, 45, 123); // Фиксированное время для теста

            // Создаем через фабрику как дочерний объект
            var childObj = await RedbObjectFactory.CreateChildAsync(parentObj, new MyDate
            {
                Value = testDate,
                Test = 2
            });

            // Сохраняем дочерний объект
            var childId = await redb.SaveAsync(childObj);
            logger.LogInformation($"✅ Дочерний объект с DateTime создан с ID: {childId}");
            logger.LogInformation($"📅 Тестовое время: {testDate:yyyy-MM-dd HH:mm:ss.fff}");

            // ===== ТЕСТ 1: С ПРОМЕЖУТОЧНЫМ ФИЛЬТРОМ (РАБОТАЕТ) =====
            logger.LogInformation("");
            logger.LogInformation("🧪 === ТЕСТ 1: С ПРОМЕЖУТОЧНЫМ ФИЛЬТРОМ ===");

            try
            {
                var query1 = await redb.TreeQueryAsync<MyDate>(parentId, 1);
                logger.LogInformation("🔍 Создан TreeQuery с parentId");

                // Получаем все объекты
                var dayList1 = await query1.ToListAsync();
                logger.LogInformation($"📊 Всего объектов найдено: {dayList1.Count}");

                if (dayList1.Count == 0)
                {
                    logger.LogWarning("⚠️ Не найдено объектов в дереве - проверьте создание данных");
                    return;
                }

                // Промежуточный фильтр по Test
                var filteredQuery1 = query1.Where(d => d.Test == 2);
                var dayList2 = await filteredQuery1.ToListAsync();
                logger.LogInformation($"📊 После фильтра Test == 2: {dayList2.Count} объектов");

                // Фильтр по DateTime
                var finalQuery1 = filteredQuery1.Where(d => d.Value == testDate);
                var dayList3 = await finalQuery1.ToListAsync();
                logger.LogInformation($"📊 После фильтра Value == testDate: {dayList3.Count} объектов");

                if (dayList3.Count > 0)
                {
                    logger.LogInformation("✅ ТЕСТ 1 ПРОШЕЛ: DateTime фильтр с промежуточным фильтром работает");
                    var foundObj = dayList3[0];
                    logger.LogInformation($"🔍 Найденный объект: ID={foundObj.id}, Value={foundObj.properties.Value:yyyy-MM-dd HH:mm:ss.fff}");
                }
                else
                {
                    logger.LogError("❌ ТЕСТ 1 ПРОВАЛЕН: DateTime фильтр с промежуточным фильтром НЕ работает");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Ошибка в тесте 1");
                throw;
            }

            // ===== ТЕСТ 2: БЕЗ ПРОМЕЖУТОЧНОГО ФИЛЬТРА (НЕ РАБОТАЕТ) =====
            logger.LogInformation("");
            logger.LogInformation("🧪 === ТЕСТ 2: БЕЗ ПРОМЕЖУТОЧНОГО ФИЛЬТРА ===");

            try
            {
                var query2 = await redb.TreeQueryAsync<MyDate>(parentId, 1);
                logger.LogInformation("🔍 Создан TreeQuery с parentId");

                // Получаем все объекты
                var dayListAll = await query2.ToListAsync();
                logger.LogInformation($"📊 Всего объектов найдено: {dayListAll.Count}");

                // Сразу фильтр по DateTime (без промежуточного)
                var directDateQuery = query2.Where(d => d.Value == testDate);
                var dayListFiltered = await directDateQuery.ToListAsync();
                logger.LogInformation($"📊 После фильтра Value == testDate: {dayListFiltered.Count} объектов");

                if (dayListFiltered.Count > 0)
                {
                    logger.LogInformation("✅ ТЕСТ 2 ПРОШЕЛ: DateTime фильтр без промежуточного фильтра работает");
                    var foundObj = dayListFiltered[0];
                    logger.LogInformation($"🔍 Найденный объект: ID={foundObj.id}, Value={foundObj.properties.Value:yyyy-MM-dd HH:mm:ss.fff}");
                }
                else
                {
                    logger.LogError("❌ ТЕСТ 2 ПРОВАЛЕН: DateTime фильтр без промежуточного фильтра НЕ работает");
                    logger.LogError("🐛 ПРОБЛЕМА ВОСПРОИЗВЕДЕНА: Различное поведение в зависимости от наличия промежуточных фильтров");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Ошибка в тесте 2");
                throw;
            }

            // ===== ТЕСТ 3: ОБЫЧНЫЙ QUERY С ПРОМЕЖУТОЧНЫМ ФИЛЬТРОМ =====
            logger.LogInformation("");
            logger.LogInformation("🧪 === ТЕСТ 3: ОБЫЧНЫЙ QUERY С ПРОМЕЖУТОЧНЫМ ФИЛЬТРОМ ===");

            try
            {
                var query3 = await redb.QueryAsync<MyDate>();
                logger.LogInformation("🔍 Создан обычный Query");

                // Получаем все объекты
                var allObjects = await query3.ToListAsync();
                logger.LogInformation($"📊 Всего объектов найдено: {allObjects.Count}");

                if (allObjects.Count == 0)
                {
                    logger.LogWarning("⚠️ Не найдено объектов для обычного Query");
                }
                else
                {
                    // Промежуточный фильтр по Test
                    query3 = query3.Where(d => d.Test == 2);
                    var filteredByTest = await query3.ToListAsync();
                    logger.LogInformation($"📊 После фильтра Test == 2: {filteredByTest.Count} объектов");

                    // Фильтр по DateTime
                    query3 = query3.Where(d => d.Value == testDate);
                    var filteredByDateTime = await query3.ToListAsync();
                    logger.LogInformation($"📊 После фильтра Value == testDate: {filteredByDateTime.Count} объектов");

                    if (filteredByDateTime.Count > 0)
                    {
                        logger.LogInformation("✅ ТЕСТ 3 ПРОШЕЛ: DateTime фильтр с промежуточным фильтром работает в обычном Query");
                        var foundObj = filteredByDateTime[0];
                        logger.LogInformation($"🔍 Найденный объект: ID={foundObj.id}, Value={foundObj.properties.Value:yyyy-MM-dd HH:mm:ss.fff}");
                    }
                    else
                    {
                        logger.LogError("❌ ТЕСТ 3 ПРОВАЛЕН: DateTime фильтр с промежуточным фильтром НЕ работает в обычном Query");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Ошибка в тесте 3");
                throw;
            }

            // ===== ТЕСТ 4: ОБЫЧНЫЙ QUERY БЕЗ ПРОМЕЖУТОЧНОГО ФИЛЬТРА =====
            logger.LogInformation("");
            logger.LogInformation("🧪 === ТЕСТ 4: ОБЫЧНЫЙ QUERY БЕЗ ПРОМЕЖУТОЧНОГО ФИЛЬТРА ===");

            try
            {
                var query4 = await redb.QueryAsync<MyDate>();
                logger.LogInformation("🔍 Создан обычный Query");

                // Получаем все объекты
                var allObjects2 = await query4.ToListAsync();
                logger.LogInformation($"📊 Всего объектов найдено: {allObjects2.Count}");

                // Сразу фильтр по DateTime (без промежуточного)
                query4 = query4.Where(d => d.Value == testDate);
                var filteredByDateTime2 = await query4.ToListAsync();
                logger.LogInformation($"📊 После фильтра Value == testDate: {filteredByDateTime2.Count} объектов");

                if (filteredByDateTime2.Count > 0)
                {
                    logger.LogInformation("✅ ТЕСТ 4 ПРОШЕЛ: DateTime фильтр без промежуточного фильтра работает в обычном Query");
                    var foundObj = filteredByDateTime2[0];
                    logger.LogInformation($"🔍 Найденный объект: ID={foundObj.id}, Value={foundObj.properties.Value:yyyy-MM-dd HH:mm:ss.fff}");
                }
                else
                {
                    logger.LogError("❌ ТЕСТ 4 ПРОВАЛЕН: DateTime фильтр без промежуточного фильтра НЕ работает в обычном Query");
                    logger.LogError("🐛 ПРОБЛЕМА ЕСТЬ И В ОБЫЧНЫХ ЗАПРОСАХ: Различное поведение не только в TreeQuery");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Ошибка в тесте 4");
                throw;
            }

            logger.LogInformation("");
            logger.LogInformation("🏁 === ТЕСТ ЗАВЕРШЕН ===");
            logger.LogInformation("📊 Результат: Сравнение поведения TreeQuery vs обычный Query для DateTime фильтрации");
        }
    }

    /// <summary>
    /// Модель для тестирования DateTime фильтрации
    /// </summary>
    [RedbScheme]
    public class MyDate
    {
        public DateTime Value { get; set; }

        public int Test { get; set; } = 2;
    }

    /// <summary>
    /// Родительский объект для тестов
    /// </summary>
    [RedbScheme]
    public class TestParent
    {
        public string Name { get; set; } = "";
    }
}
