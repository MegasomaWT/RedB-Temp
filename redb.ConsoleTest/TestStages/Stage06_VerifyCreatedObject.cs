using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.Postgres;
using redb.Core.Providers;
using System;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// ЭТАП 6: Проверка созданного объекта
    /// </summary>
    public class Stage06_VerifyCreatedObject : BaseTestStage
    {
        public override int Order => 6;
        public override string Name => "Проверка созданного объекта";
        public override string Description => "Загружаем созданный объект для проверки корректности сохранения";

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🔍 === ЭТАП 6: ПРОВЕРКА СОЗДАННОГО ОБЪЕКТА ===");

                // Получаем ID созданного объекта из предыдущего этапа
                var stage5 = GetPreviousStage<Stage05_CreateObject>();
                if (stage5?.CreatedObjectId == 0)
                {
                    logger.LogError("❌ Не найден ID созданного объекта из этапа 5");
                    throw new InvalidOperationException("Не найден ID созданного объекта из этапа 5");
                }

                var createdObjectId = stage5.CreatedObjectId;
                logger.LogInformation("Загружаем созданный объект {newId} для проверки...", createdObjectId);
                
                var createdObj = await ((IObjectStorageProvider)redb).LoadAsync<AnalyticsRecordProps>(createdObjectId);
                logger.LogInformation("✅ Проверка пройдена: name='{name}', TestName='{testName}'", 
                    createdObj.name, createdObj.properties.TestName);

                // 🔬 АНАЛИЗ NULL → DEFAULT ПОСЛЕ ЧТЕНИЯ ИЗ БД
                logger.LogInformation("🔬 === АНАЛИЗ NULL → DEFAULT ПОСЛЕ ЧТЕНИЯ ===");
                logger.LogInformation($"   📊 Stock (non-nullable): {createdObj.properties.Stock}");
                logger.LogInformation($"   📊 Tag (nullable): {createdObj.properties.Tag ?? "NULL"}");
                logger.LogInformation($"   📊 TestName (nullable): {createdObj.properties.TestName ?? "NULL"}");
                logger.LogInformation($"   📊 Orders (nullable): {createdObj.properties.Orders?.ToString() ?? "NULL"}");
                logger.LogInformation($"   📊 TotalCart (nullable): {createdObj.properties.TotalCart?.ToString() ?? "NULL"}");
                
                // Проверяем различие между null и 0/default
                logger.LogInformation("🧪 АНАЛИЗ РАЗЛИЧИЯ null vs default:");
                logger.LogInformation($"   📊 Orders == null: {createdObj.properties.Orders == null}");
                logger.LogInformation($"   📊 Stock == 0: {createdObj.properties.Stock == 0}");
                logger.LogInformation($"   📊 Tag == null: {createdObj.properties.Tag == null}");
                logger.LogInformation($"   📊 TotalCart == null: {createdObj.properties.TotalCart == null}");
                
                // 🔬 АНАЛИЗ БИЗНЕС-КЛАССОВ ПОСЛЕ ЧТЕНИЯ
                var mixedObj = await ((IObjectStorageProvider)redb).LoadAsync<MixedTestProps>(createdObjectId);
                logger.LogInformation("🔬 АНАЛИЗ БИЗНЕС-КЛАССОВ ПОСЛЕ ЧТЕНИЯ:");
                logger.LogInformation($"   📊 Address1 (должен быть заполнен): {(mixedObj.properties.Address1 == null ? "NULL" : $"OK: {mixedObj.properties.Address1.City}")}");
                logger.LogInformation($"   📊 Address2 (должен быть заполнен): {(mixedObj.properties.Address2 == null ? "NULL" : $"OK: {mixedObj.properties.Address2.City}")}"); 
                logger.LogInformation($"   📊 Address3 (должен остаться null): {(mixedObj.properties.Address3 == null ? "NULL" : $"ПРОБЛЕМА: {mixedObj.properties.Address3.City}")}");

                // 🔬 ТЕСТ NULL TEST ОБЪЕКТА  
                logger.LogInformation("🧪 === АНАЛИЗ NULL TEST ОБЪЕКТА ИЗ БД ===");
                var nullTestId = GetStageData<long>("NullTestObjectId");
                if (nullTestId > 0)
                {
                    var nullTestObj = await ((IObjectStorageProvider)redb).LoadAsync<AnalyticsRecordProps>(nullTestId);
                    logger.LogInformation("📖 ПОСЛЕ ЧТЕНИЯ ИЗ БД:");
                    logger.LogInformation($"   📊 Orders (было null): {nullTestObj.properties.Orders?.ToString() ?? "NULL"}");
                    logger.LogInformation($"   📊 TotalCart (было null): {nullTestObj.properties.TotalCart?.ToString() ?? "NULL"}");
                    logger.LogInformation($"   📊 Tag (было null): {nullTestObj.properties.Tag ?? "NULL"}");
                    logger.LogInformation($"   📊 TestName (было Filled Name): {nullTestObj.properties.TestName ?? "NULL"}");
                    logger.LogInformation($"   📊 Stock (было 100): {nullTestObj.properties.Stock}");
                    logger.LogInformation($"   📊 AuctionMetrics (RedbObject было null): {(nullTestObj.properties.AuctionMetrics == null ? "NULL" : "NOT NULL")}");
                    
                    // 🚨 ПРОВЕРКА NULL → DEFAULT ПРОБЛЕМЫ
                    bool hasNullToDefaultIssue = false;
                    if (nullTestObj.properties.Orders != null) 
                    {
                        logger.LogWarning("❌ NULL → DEFAULT ПРОБЛЕМА: Orders было null, стало {value}", nullTestObj.properties.Orders);
                        hasNullToDefaultIssue = true;
                    }
                    if (nullTestObj.properties.TotalCart != null)
                    {
                        logger.LogWarning("❌ NULL → DEFAULT ПРОБЛЕМА: TotalCart было null, стало {value}", nullTestObj.properties.TotalCart);
                        hasNullToDefaultIssue = true;
                    }
                    if (nullTestObj.properties.Tag != null)
                    {
                        logger.LogWarning("❌ NULL → DEFAULT ПРОБЛЕМА: Tag было null, стало '{value}'", nullTestObj.properties.Tag);
                        hasNullToDefaultIssue = true;
                    }
                    if (nullTestObj.properties.AuctionMetrics != null)
                    {
                        logger.LogWarning("❌ NULL → DEFAULT ПРОБЛЕМА: AuctionMetrics было null, стало NOT NULL (ID: {id})", nullTestObj.properties.AuctionMetrics.id);
                        hasNullToDefaultIssue = true;
                    }
                    
                    // 🔬 ПРОВЕРКА NULL БИЗНЕС-КЛАССА В ОСНОВНОМ ОБЪЕКТЕ
                    if (mixedObj.properties.Address3 != null)
                    {
                        logger.LogWarning("❌ NULL → DEFAULT ПРОБЛЕМА: Address3 было null, стало NOT NULL (City: {city})", mixedObj.properties.Address3.City);
                        hasNullToDefaultIssue = true;
                    }
                    
                    if (!hasNullToDefaultIssue)
                    {
                        logger.LogInformation("✅ NULL значения сохранились корректно!");
                    }
                    else
                    {
                        logger.LogWarning("❌ ОБНАРУЖЕНА NULL → DEFAULT ПРОБЛЕМА В СЕРИАЛИЗАЦИИ!");
                    }
                }

                // Проверяем все поля
                logger.LogInformation("Детальная проверка полей:");
                logger.LogInformation("  ID: {id}", createdObj.id);
                logger.LogInformation("  Name: '{name}'", createdObj.name);
                logger.LogInformation("  Note: '{note}'", createdObj.note);
                logger.LogInformation("  Article: '{article}'", createdObj.properties.Article);
                logger.LogInformation("  Date: {date}", createdObj.properties.Date);
                logger.LogInformation("  Stock: {stock}", createdObj.properties.Stock);
                logger.LogInformation("  Orders: {orders}", createdObj.properties.Orders);
                logger.LogInformation("  TotalCart: {totalCart}", createdObj.properties.TotalCart);
                logger.LogInformation("  Tag: '{tag}'", createdObj.properties.Tag);
                logger.LogInformation("  TestName: '{testName}'", createdObj.properties.TestName);

            // Сохраняем объект для следующих этапов
            SetStageData("CreatedObject", createdObj);
        }
    }
}
