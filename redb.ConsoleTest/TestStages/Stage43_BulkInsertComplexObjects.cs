using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.Models.Entities;
using redb.Core.Models.Contracts;
using redb.ConsoleTest.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    public class Stage43_BulkInsertComplexObjects : BaseTestStage
    {
        public override int Order => 43;
        public override string Name => "🚀 Массовая вставка сложных объектов";
        public override string Description => "Тестируем AddNewObjectsAsync со сложными объектами (бизнес-классы, массивы, вложенные объекты)";

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🚀 === ЭТАП 43: МАССОВАЯ ВСТАВКА СЛОЖНЫХ ОБЪЕКТОВ ===");
            logger.LogInformation("📋 Подготовка схем для сложных объектов...");

            // Создаем схемы заранее
            var mixedScheme = await redb.SyncSchemeAsync<MixedTestProps>();
            var metricsScheme = await redb.SyncSchemeAsync<AnalyticsMetricsProps>();
            logger.LogInformation($"✅ Схемы созданы: MixedTestProps (ID: {mixedScheme.Id}), AnalyticsMetricsProps (ID: {metricsScheme.Id})");

            // === СРАВНЕНИЕ ПРОИЗВОДИТЕЛЬНОСТИ ===
            logger.LogInformation($"📊 === СРАВНЕНИЕ: 100 SaveAsync VS 1000 AddNewObjectsAsync ===");

            // 1. ТЕСТ SaveAsync (100 объектов)
            logger.LogInformation($"🐌 Подготовка 100 объектов для SaveAsync...");
            var saveAsyncObjects = CreateComplexTestObjects(100, metricsScheme.Id);
            logger.LogInformation($"🐌 Тест SaveAsync на 100 объектов...");
            
            var stopwatchSaveAsync = Stopwatch.StartNew();
            var savedIds = new List<long>();
            foreach (var obj in saveAsyncObjects)
            {
                var savedId = await redb.SaveAsync(obj);
                savedIds.Add(savedId);
            }
            stopwatchSaveAsync.Stop();
            
            logger.LogInformation($"🐌 SaveAsync завершен за {stopwatchSaveAsync.ElapsedMilliseconds} мс");
            logger.LogInformation($"📊 SaveAsync скорость: {(double)100 / stopwatchSaveAsync.ElapsedMilliseconds * 1000:F2} объектов/сек");

            // 2. ТЕСТ AddNewObjectsAsync (1000 объектов)
            logger.LogInformation($"🚀 Подготовка 1000 объектов для AddNewObjectsAsync...");
            var bulkObjects = CreateComplexTestObjects(1000, metricsScheme.Id);
            logger.LogInformation($"🚀 Тест AddNewObjectsAsync на 1000 объектов...");

            var stopwatchBulk = Stopwatch.StartNew();
            var insertedIds = await redb.AddNewObjectsAsync<MixedTestProps>(bulkObjects.Cast<IRedbObject<MixedTestProps>>().ToList());
            stopwatchBulk.Stop();

            logger.LogInformation($"🚀 AddNewObjectsAsync завершен за {stopwatchBulk.ElapsedMilliseconds} мс");
            logger.LogInformation($"📊 AddNewObjectsAsync скорость: {(double)1000 / stopwatchBulk.ElapsedMilliseconds * 1000:F2} объектов/сек");

            // === СРАВНЕНИЕ РЕЗУЛЬТАТОВ ===
            var saveAsyncPerObj = (double)stopwatchSaveAsync.ElapsedMilliseconds / 100;
            var bulkPerObj = (double)stopwatchBulk.ElapsedMilliseconds / 1000;
            var speedup = saveAsyncPerObj / bulkPerObj;

            logger.LogInformation($"📈 === РЕЗУЛЬТАТЫ СРАВНЕНИЯ ===");
            logger.LogInformation($"🐌 SaveAsync (100 obj): {saveAsyncPerObj:F2} мс/объект");
            logger.LogInformation($"🚀 AddNewObjectsAsync (1000 obj): {bulkPerObj:F2} мс/объект");
            logger.LogInformation($"⚡ УСКОРЕНИЕ: в {speedup:F1}x раз быстрее!");
            
            if (speedup >= 3.0)
            {
                logger.LogInformation($"🏆 ОТЛИЧНО! Массовая вставка значительно быстрее!");
            }
            else if (speedup >= 1.5)
            {
                logger.LogInformation($"👍 ХОРОШО! Массовая вставка быстрее!");
            }
            else
            {
                logger.LogWarning($"⚠️ Массовая вставка работает медленнее ожидаемого");
            }

            // === ДЕТАЛЬНАЯ ПРОВЕРКА РЕЗУЛЬТАТОВ ===
            logger.LogInformation($"🔍 === ДЕТАЛЬНАЯ ПРОВЕРКА ОБЪЕКТОВ ===");
            
            // Проверяем один объект из каждого теста
            var saveAsyncTestId = savedIds.FirstOrDefault();
            var bulkTestId = insertedIds.FirstOrDefault();
            
            if (saveAsyncTestId > 0)
            {
                logger.LogInformation($"🐌 Проверка SaveAsync объекта ID={saveAsyncTestId}:");
                await CheckObjectDetails(redb, saveAsyncTestId, logger);
            }
            
            if (bulkTestId > 0)
            {
                logger.LogInformation($"🚀 Проверка AddNewObjectsAsync объекта ID={bulkTestId}:");
                await CheckObjectDetails(redb, bulkTestId, logger);
            }

            logger.LogInformation($"🎯 === ИТОГИ ЭТАПА 43 ===");
            logger.LogInformation($"✅ SaveAsync: {savedIds.Count} объектов за {stopwatchSaveAsync.ElapsedMilliseconds} мс");
            logger.LogInformation($"✅ AddNewObjectsAsync: {insertedIds.Count} объектов за {stopwatchBulk.ElapsedMilliseconds} мс");
            logger.LogInformation($"🏆 Полное заполнение данных как в Stage 5 протестировано!");
        }

        /// <summary>
        /// 🏗️ Создает полноценно заполненные сложные объекты как в Stage 5
        /// </summary>
        private List<RedbObject<MixedTestProps>> CreateComplexTestObjects(int count, long metricsSchemeId)
        {
            var objects = new List<RedbObject<MixedTestProps>>();

            for (int i = 0; i < count; i++)
            {
                // Создаем вложенные метрики (полное заполнение)
                var autoMetric = new RedbObject<AnalyticsMetricsProps>
                {
                    name = $"Вложенная метрика {i}",
                    note = $"🚀 STAGE 43 - вложенная метрика #{i}",
                    scheme_id = metricsSchemeId,
                    properties = new AnalyticsMetricsProps
                    {
                        AdvertId = 12000 + i,
                        Base = 1500 + (i % 100),
                        Baskets = 45 + (i % 20),
                        Association = 12 + (i % 8),
                        Costs = 2500.75 + (i * 10.25),
                        Rate = 95 + (i % 15)
                    }
                };

                var relatedMetrics = new RedbObject<AnalyticsMetricsProps>[]
                {
                    new RedbObject<AnalyticsMetricsProps>
                    {
                        name = $"Метрика 1 - Реклама ULTRA #{i}",
                        note = $"🚀 STAGE 43 - рекламная кампания {i}-1",
                        scheme_id = metricsSchemeId,
                        properties = new AnalyticsMetricsProps
                        {
                            AdvertId = 10001 + i,
                            Base = 150 + (i % 50),
                            Baskets = 25 + (i % 15),
                            Association = 5 + (i % 6),
                            Costs = 1250.5 + (i * 8.75),
                            Rate = 85 + (i % 20)
                        }
                    },
                    new RedbObject<AnalyticsMetricsProps>
                    {
                        name = $"Метрика 2 - Органика #{i}",
                        note = $"🚀 STAGE 43 - органический трафик {i}",
                        scheme_id = metricsSchemeId,
                        properties = new AnalyticsMetricsProps
                        {
                            AdvertId = 10002 + i,
                            Base = 300 + (i % 75),
                            Baskets = 45 + (i % 25),
                            Association = 12 + (i % 10),
                            Costs = (i % 3 == 0) ? 0 : 450.25 + (i * 3.5),
                            Rate = 92 + (i % 12)
                        }
                    },
                    new RedbObject<AnalyticsMetricsProps>
                    {
                        name = $"Метрика 3 - Социальные сети #{i}",
                        note = $"🚀 STAGE 43 - SMM кампания {i}",
                        scheme_id = metricsSchemeId,
                        properties = new AnalyticsMetricsProps
                        {
                            AdvertId = 10003 + i,
                            Base = 75 + (i % 30),
                            Baskets = 8 + (i % 12),
                            Association = 2 + (i % 4),
                            Costs = 450.25 + (i * 6.75),
                            Rate = 65 + (i % 25)
                        }
                    }
                };

                // Основной объект с МАКСИМАЛЬНЫМ заполнением как в Stage 5
                var complexObj = new RedbObject<MixedTestProps>
                {
                    name = $"Объект #{i} - Stage43 тест",
                    note = $"🚀 STAGE 43 - массовая вставка #{i} (полное заполнение как в Stage5)",
                    properties = new MixedTestProps
                    {
                        // Простые поля
                        Age = 30 + (i % 40),
                        Name = $"John Doe {i}",
                        Date = new DateTime(2025, 8, 30).AddDays(-i),
                        Article = $"ART-{i:D6}-BULK",
                        Stock = 100 + i * 5,
                        Tag = $"stage43-test-{i % 10}",
                        TestName = $"МАКСИМАЛЬНЫЙ ТЕСТ STAGE 43 #{i}",

                        // Массивы простых типов (как в Stage 5)
                        Tags1 = new string[] { $"stage43-{i}", $"bulk-{i % 5}", $"test-{i % 3}", $"advanced-{i % 7}", $"full-{i % 4}", $"complete-{i % 6}" },
                        Scores1 = new int[] { 85 + (i % 15), 92 + (i % 8), 78 + (i % 12), 96 + (i % 4), 88 + (i % 6), 94 + (i % 3) },
                        
                        Tags2 = (i % 2 == 0) ? new string[] { $"TAG2-{i}", $"SECOND-{i % 3}", $"EXTRA-{i % 4}", $"MORE-{i % 5}", $"FINAL-{i % 6}" } : new string[0],
                        Scores2 = (i % 3 == 0) ? new int[] { 33 + (i % 7), 22 + (i % 9), 11 + (i % 11), 44 + (i % 5), 55 + (i % 8) } : new int[0],

                        // Бизнес-классы с полной вложенностью (как в Stage 5)
                        Address1 = new Address
                        {
                            City = $"Moscow-{i % 10}",
                            Street = $"Main Street {123 + i}",
                            Details = new Details
                            {
                                Floor = 5 + (i % 15),
                                Building = $"Building {(char)('A' + (i % 5))} Advanced",
                                Tags1 = new string[] { $"moscow-{i}", $"main-street-{i % 7}", $"building-{(char)('a' + (i % 5))}-{i % 3}" },
                                Scores1 = new int[] { 95 + (i % 5), 87 + (i % 8), 92 + (i % 6) },
                                Tags2 = (i % 4 == 0) ? new string[] { $"addr1-{i}", $"premium-{i % 3}", $"center-{i % 5}" } : new string[0],
                                Scores2 = (i % 3 == 0) ? new int[] { 88 + (i % 7), 91 + (i % 4), 89 + (i % 6) } : new int[0]
                            }
                        },

                        Address2 = new Address
                        {
                            City = $"Moscow-Center-{i % 5}",
                            Street = $"Premium Street {200 + i}",
                            Details = new Details
                            {
                                Floor = 15 + (i % 10),
                                Building = $"Building {(char)('B' + (i % 4))} Advanced",
                                Tags1 = new string[] { $"address2-{i}", $"advanced-{i % 4}", $"building-{(char)('b' + (i % 4))}-{i % 2}", $"premium-{i % 3}", $"moscow-center-{i % 6}" },
                                Scores1 = new int[] { 98 - (i % 4), 97 - (i % 3), 96 - (i % 2), 95 - (i % 1), 94 + (i % 2) },
                                Tags2 = new string[] { $"ultra-{i}", $"mega-{i % 3}", $"super-{i % 4}", $"advanced-{i % 2}", $"final-{i % 5}" },
                                Scores2 = new int[] { 100 - (i % 4), 99 - (i % 3), 98 - (i % 2), 97 - (i % 1), 96 + (i % 2) }
                            }
                        },

                        // Массив бизнес-классов (расширенный как в Stage 5)
                        Contacts = new Contact[]
                        {
                            new Contact { Type = "email", Value = $"user{i}@stage43.com", Verified = i % 2 == 0 },
                            new Contact { Type = "phone", Value = $"+7-{(900 + i % 100):D3}-{((i + 123) % 1000):D3}-{((i * 7 + 45) % 100):D2}-{((i * 11 + 67) % 100):D2}", Verified = i % 3 == 0 },
                            new Contact { Type = "telegram", Value = $"@stage43_user_{i}", Verified = i % 4 == 0 },
                            new Contact { Type = "skype", Value = $"stage43.user.{i}.business", Verified = i % 5 == 0 },
                            new Contact { Type = "whatsapp", Value = $"+7-{(950 + i % 50):D3}-{((i + 555) % 1000):D3}-{((i * 3 + 77) % 100):D2}-{((i * 13 + 88) % 100):D2}", Verified = i % 6 == 0 }
                        },

                        // Вложенные RedbObject'ы (полное заполнение как в Stage 5)
                        AutoMetrics = autoMetric,
                        RelatedMetrics = relatedMetrics
                    }
                };

                objects.Add(complexObj);
            }

            return objects;
        }

        /// <summary>
        /// 🔍 Проверяет детали загруженного объекта
        /// </summary>
        private async Task CheckObjectDetails(IRedbService redb, long objectId, ILogger logger)
        {
            var loadedObj = await redb.LoadAsync<MixedTestProps>(objectId);
            if (loadedObj?.properties != null)
            {
                logger.LogInformation($"   📋 Name='{loadedObj.properties.Name}', Age={loadedObj.properties.Age}");
                logger.LogInformation($"   📊 Tags1: {loadedObj.properties.Tags1?.Length ?? 0}, Tags2: {loadedObj.properties.Tags2?.Length ?? 0}");
                logger.LogInformation($"   🎯 Scores1: {loadedObj.properties.Scores1?.Length ?? 0}, Scores2: {loadedObj.properties.Scores2?.Length ?? 0}");
                logger.LogInformation($"   🏠 Address1: {(loadedObj.properties.Address1 != null ? $"Floor={loadedObj.properties.Address1.Details?.Floor}" : "null")}");
                logger.LogInformation($"   🏢 Address2: {(loadedObj.properties.Address2 != null ? $"Floor={loadedObj.properties.Address2.Details?.Floor}" : "null")}");
                logger.LogInformation($"   📞 Contacts: {loadedObj.properties.Contacts?.Length ?? 0} элементов");
                logger.LogInformation($"   🔗 AutoMetrics: ID={loadedObj.properties.AutoMetrics?.Id ?? 0}");
                logger.LogInformation($"   📊 RelatedMetrics: {loadedObj.properties.RelatedMetrics?.Length ?? 0} элементов");
                
                if (loadedObj.properties.Address1?.Details?.Tags1?.Length > 0)
                {
                    logger.LogInformation($"   🏷️ Address1.Details.Tags1: [{string.Join(", ", loadedObj.properties.Address1.Details.Tags1)}]");
                }
            }
            else
            {
                logger.LogWarning($"   ❌ Объект ID={objectId} не загружен или пустой!");
            }
        }
    }
}
