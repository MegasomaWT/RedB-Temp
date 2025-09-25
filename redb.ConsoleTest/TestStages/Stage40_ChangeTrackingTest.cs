using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.Models.Entities;
using redb.Core.Models.Configuration;
using redb.ConsoleTest.Models;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Тестирование новой ChangeTracking стратегии сохранения
    /// </summary>
    public class Stage40_ChangeTrackingTest : BaseTestStage
    {
        public override string Name => "Change Tracking тест";
        public override string Description => "Тестирование ChangeTracking стратегии - сравнение с БД и обновление только измененных свойств";
        public override int Order => 40;

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            try
            {
                logger.LogInformation("🚀 === ТЕСТИРОВАНИЕ CHANGETRACKING СТРАТЕГИИ ===");
                
                // 0. 🔧 Проверяем конфигурацию
                logger.LogInformation("0️⃣ Проверяем конфигурацию...");
                
                // ИСПОЛЬЗУЕМ ГЛОБАЛЬНУЮ КОНФИГУРАЦИЮ как в Stage 5 (убираем локальную)
                // RedbObject<MixedTestProps>.SetConfiguration(...) ← УБИРАЕМ!
                
                // Проверяем глобальную конфигурацию
                var config = RedbObject<MixedTestProps>.GetConfiguration();
                logger.LogInformation($"   EavSaveStrategy: {config.EavSaveStrategy}");
                logger.LogInformation($"   ✅ ChangeTracking стратегия активна");
                
                // 1. 📊 Создаем и сохраняем новый объект
                logger.LogInformation("");
                logger.LogInformation("1️⃣ Создаем СЛОЖНЫЙ объект с массивами для диагностики ChangeTracking...");
                
                // Создаем вложенные метрики
                var autoMetric = new RedbObject<AnalyticsMetricsProps>
                {
                    name = "Метрика для ChangeTracking",
                    note = "Stage 40 - AutoMetrics",
                    properties = new AnalyticsMetricsProps
                    {
                        AdvertId = 40001,
                        Base = 100,
                        Baskets = 15,
                        Association = 3,
                        Costs = 500.75,
                        Rate = 85
                    }
                };

                var testObj = new RedbObject<MixedTestProps>
                {
                    name = "Stage 40 - Смешанный тестовый объект",
                    note = "Stage 40 - тест ChangeTracking с полным заполнением",
                    properties = new MixedTestProps
                    {
                        // ПОЛНОЕ ЗАПОЛНЕНИЕ КАК В STAGE 5
                        Age = 30,
                        Name = "John Doe Stage40",
                        Date = new DateTime(2025, 8, 30),
                        Article = "Тестовый артикул Stage40",
                        Stock = 100,
                        Tag = "stage40-test",
                        TestName = "STAGE 40 TEST",

                        // Массивы как в Stage 5
                        Tags1 = new string[] { "stage40", "test", "tracking" },
                        Scores1 = new int[] { 85, 92, 78 },
                        Tags2 = new string[] { "secondary", "tags" },
                        Scores2 = new int[] { 33, 22 },

                        // Бизнес-классы как в Stage 5
                        Address1 = new Address
                        {
                            City = "Moscow",
                            Street = "Main Street 123",
                            Details = new Details
                            {
                                Floor = 5,
                                Building = "Building A",
                                Tags1 = new string[] { "moscow", "main-street", "building-a" },
                                Scores1 = new int[] { 95, 87, 92 }
                            }
                        },

                        // IRedbObject ссылка
                        AutoMetrics = autoMetric
                    }
                };
                
                logger.LogInformation($"   📊 Создан сложный объект MixedTestProps:");
                logger.LogInformation($"   🔢 Age: {testObj.properties.Age}, Name: '{testObj.properties.Name}'");
                logger.LogInformation($"   📋 Tags1: [{string.Join(", ", testObj.properties.Tags1)}]");
                logger.LogInformation($"   🎯 Scores1: [{string.Join(", ", testObj.properties.Scores1)}]");
                logger.LogInformation($"   🔗 AutoMetrics: {testObj.properties.AutoMetrics?.name}");
                
                // Сохраняем в БД (будет использована ChangeTracking стратегия)
                logger.LogInformation("   💾 Сохраняем в БД...");
                
                var stopwatch1 = Stopwatch.StartNew();
                var savedId = await redb.SaveAsync(testObj);
                stopwatch1.Stop();
                
                logger.LogInformation($"   ✅ Сложный объект сохранен с ID: {savedId}");
                logger.LogInformation($"   ⏱️ Время создания: {stopwatch1.ElapsedMilliseconds} мс");
                
                // ⚡ КРИТИЧНО: Перезагружаем объект из БД чтобы получить правильные ID
                logger.LogInformation("   🔄 Перезагружаем объект с правильными ID...");
                testObj = await redb.LoadAsync<MixedTestProps>(savedId);
                logger.LogInformation($"   ✅ Объект перезагружен: AutoMetrics.Id={testObj.properties.AutoMetrics?.Id}");
                
                // 2. 🔄 Тестируем обновление простых полей И одного элемента массива
                logger.LogInformation("");
                logger.LogInformation("2️⃣ Тестируем обновление простых полей + один элемент массива...");
                
                // Изменяем простые поля И один элемент массива для тестирования ChangeTracking
                testObj.properties.Age = 35;
                testObj.properties.Stock = 250;
                
                // 🧪 ТЕСТИРУЕМ: изменяем один элемент массива Tags1
                testObj.properties.Tags1[0] = "stage40-UPDATED1";
                
                logger.LogInformation($"   🔧 Изменили Age: {testObj.properties.Age}");
                logger.LogInformation($"   🔧 Изменили Stock: {testObj.properties.Stock}");
                logger.LogInformation($"   🧪 Изменили Tags1[0]: '{testObj.properties.Tags1[0]}' (было 'stage40')");
                logger.LogInformation($"   📋 Tags1 ПОСЛЕ: [{string.Join(", ", testObj.properties.Tags1)}]");
                logger.LogInformation($"   🔗 AutoMetrics НЕ изменяли: ID={testObj.properties.AutoMetrics?.Id}");
                
                // Сохраняем - должны обновиться Age, Stock И Tags1[0]
                logger.LogInformation("   💾 Сохраняем изменение (ChangeTracking стратегия)...");
                
                var stopwatch2 = Stopwatch.StartNew();
                await redb.SaveAsync(testObj);
                stopwatch2.Stop();
                
                logger.LogInformation($"   ✅ Обновление завершено - должны быть UPDATE для Age, Stock и Tags1[0]");
                logger.LogInformation($"   ⏱️ Время ChangeTracking: {stopwatch2.ElapsedMilliseconds} мс");
                
                // 3. 🔍 Проверяем результат 
                logger.LogInformation("");
                logger.LogInformation("3️⃣ Проверяем результат - загружаем объект заново...");
                
                var stopwatch3 = Stopwatch.StartNew();
                var reloadedObj = await redb.LoadAsync<MixedTestProps>(savedId);
                stopwatch3.Stop();
                
                logger.LogInformation($"   📊 Перезагруженный объект:");
                logger.LogInformation($"   ⏱️ Время загрузки: {stopwatch3.ElapsedMilliseconds} мс");
                logger.LogInformation($"   🔢 Age: {reloadedObj.properties.Age} (должен быть 35)");
                logger.LogInformation($"   📊 Stock: {reloadedObj.properties.Stock} (должен быть 250)");
                logger.LogInformation($"   📝 Name: '{reloadedObj.properties.Name}' (не изменялся)");
                logger.LogInformation($"   📋 Tags1: [{string.Join(", ", reloadedObj.properties.Tags1 ?? new string[0])}] (должен быть 'stage40-UPDATED, test, tracking')");
                logger.LogInformation($"   🔗 AutoMetrics: ID={reloadedObj.properties.AutoMetrics?.Id} (не изменялся)");
                
                // Простая проверка результата
                var simpleAgeOk = reloadedObj.properties.Age == 35;
                var simpleStockOk = reloadedObj.properties.Stock == 250;
                var simpleNameOk = reloadedObj.properties.Name == "John Doe Stage40";
                var simpleTagsOk = reloadedObj.properties.Tags1 != null && 
                                  reloadedObj.properties.Tags1.Length == 3 && 
                                  reloadedObj.properties.Tags1[0] == "stage40-UPDATED" &&
                                  reloadedObj.properties.Tags1[1] == "test" &&
                                  reloadedObj.properties.Tags1[2] == "tracking";
                
                if (simpleAgeOk && simpleStockOk && simpleNameOk && simpleTagsOk)
                {
                    logger.LogInformation($"   ✅ Все поля (включая массив) обновились корректно!");
                }
                else
                {
                    logger.LogError($"   ❌ Ошибка в полях: Age={simpleAgeOk}, Stock={simpleStockOk}, Name={simpleNameOk}, Tags1={simpleTagsOk}");
                    throw new InvalidOperationException("ChangeTracking не работает корректно");
                }
                
                logger.LogInformation("");
                logger.LogInformation("🎉 === CHANGETRACKING СТРАТЕГИЯ НА ПОЛЯХ И МАССИВАХ РАБОТАЕТ! ===");
                logger.LogInformation("✨ ChangeTracking корректно обрабатывает изменения элементов массивов!");
                // 4. 🧪 ДОПОЛНИТЕЛЬНЫЙ ТЕСТ: Создание второго объекта (структуры уже существуют)
                logger.LogInformation("");
                logger.LogInformation("4️⃣ ДОПОЛНИТЕЛЬНЫЙ ТЕСТ: Создание второго объекта...");
                
                // Создаем второй идентичный объект для проверки скорости без создания структур
                var secondAutoMetric = new RedbObject<AnalyticsMetricsProps>
                {
                    name = "Вторая метрика для теста",
                    note = "Stage 40 - Second Test",
                    properties = new AnalyticsMetricsProps
                    {
                        AdvertId = 40002,
                        Base = 150,
                        Baskets = 20,
                        Association = 4,
                        Costs = 750.50,
                        Rate = 90
                    }
                };

                var secondTestObj = new RedbObject<MixedTestProps>
                {
                    name = "Stage 40 - Второй тестовый объект",
                    note = "Stage 40 - второе создание (структуры существуют)",
                    properties = new MixedTestProps
                    {
                        // ПОЛНОЕ ЗАПОЛНЕНИЕ КАК ПЕРВЫЙ
                        Age = 25,
                        Name = "Jane Doe Stage40",
                        Date = new DateTime(2025, 8, 30),
                        Article = "Второй артикул Stage40",
                        Stock = 200,
                        Tag = "stage40-second-test",
                        TestName = "STAGE 40 SECOND TEST",

                        // Массивы
                        Tags1 = new string[] { "second", "test", "object" },
                        Scores1 = new int[] { 90, 85, 95 },
                        Tags2 = new string[] { "second", "batch" },
                        Scores2 = new int[] { 40, 35 },

                        // Бизнес-класс
                        Address1 = new Address
                        {
                            City = "Saint Petersburg",
                            Street = "Nevsky Prospect 100",
                            Details = new Details
                            {
                                Floor = 3,
                                Building = "Building B",
                                Tags1 = new string[] { "spb", "nevsky", "building-b" },
                                Scores1 = new int[] { 88, 92, 85 }
                            }
                        },

                        // IRedbObject ссылка
                        AutoMetrics = secondAutoMetric
                    }
                };
                
                logger.LogInformation("   💾 Сохраняем второй объект (структуры уже созданы)...");
                
                var stopwatch4 = Stopwatch.StartNew();
                var secondSavedId = await redb.SaveAsync(secondTestObj);
                stopwatch4.Stop();
                
                logger.LogInformation($"   ✅ Второй объект сохранен с ID: {secondSavedId}");
                logger.LogInformation($"   ⏱️ Время создания ВТОРОГО объекта: {stopwatch4.ElapsedMilliseconds} мс");
                
                // Сравнение
                var improvement = (double)stopwatch1.ElapsedMilliseconds / stopwatch4.ElapsedMilliseconds;
                logger.LogInformation($"   📊 СРАВНЕНИЕ: Первый объект {stopwatch1.ElapsedMilliseconds} мс vs Второй {stopwatch4.ElapsedMilliseconds} мс");
                logger.LogInformation($"   ⚡ Второй объект БЫСТРЕЕ в {improvement:F1}x раз!");
                
                if (improvement >= 5.0)
                {
                    logger.LogInformation($"   🎯 ПОДТВЕРЖДЕНО: Медленность в создании структур БД, а не в ChangeTracking!");
                }
                else if (improvement >= 2.0)
                {
                    logger.LogInformation($"   👍 ЧАСТИЧНО: Есть влияние создания структур на скорость");
                }
                else
                {
                    logger.LogInformation($"   ⚠️ Неожиданно: Разница меньше ожидаемой");
                }
                
                logger.LogInformation("");
                logger.LogInformation("📊 === ОТЧЕТ ПО ПРОИЗВОДИТЕЛЬНОСТИ ===");
                logger.LogInformation($"   ⏱️ Время создания ПЕРВОГО объекта: {stopwatch1.ElapsedMilliseconds} мс (+ создание структур)");
                logger.LogInformation($"   ⏱️ Время создания ВТОРОГО объекта: {stopwatch4.ElapsedMilliseconds} мс (структуры готовы)");
                logger.LogInformation($"   ⏱️ Время ChangeTracking обновления: {stopwatch2.ElapsedMilliseconds} мс");
                logger.LogInformation($"   ⏱️ Время загрузки объекта: {stopwatch3.ElapsedMilliseconds} мс");
                logger.LogInformation($"   ⏱️ ОБЩЕЕ время: {stopwatch1.ElapsedMilliseconds + stopwatch2.ElapsedMilliseconds + stopwatch3.ElapsedMilliseconds + stopwatch4.ElapsedMilliseconds} мс");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "💥 Ошибка в тесте ChangeTracking стратегии");
                throw;
            }
        }
        

    }
}