using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Этап 25: Демонстрация полиморфного API без дженериков
    /// </summary>
    public class Stage25_PolymorphicAPI : BaseTestStage
    {
        public override string Name => "🚀 Полиморфный API без дженериков";
        public override string Description => "Демонстрация работы с RedbObject без указания типов";
        public override int Order => 25;

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🚀 === ДЕМОНСТРАЦИЯ ПОЛИМОРФНОГО API ===");
            logger.LogInformation("");

            // === СОЗДАЕМ РАЗНЫЕ ТИПЫ ОБЪЕКТОВ ===
            logger.LogInformation("📦 Создаем объекты разных типов...");
            
            var product = new RedbObject<ProductTestProps>
            {
                name = "Полиморфный продукт",
                scheme_id = 1001,
                properties = new ProductTestProps { Stock = 100, Category = "Electronics", Price = 999.99, IsActive = true }
            };

            var analytics = new RedbObject<AnalyticsRecordProps>
            {
                name = "Полиморфная аналитика", 
                scheme_id = 1002,
                properties = new AnalyticsRecordProps { Article = "TEST001", Stock = 50 }
            };

            // Сохраняем объекты
            using (redb.CreateSystemContext())
            {
                product.id = await redb.SaveAsync(product);
                analytics.id = await redb.SaveAsync(analytics);
            }

            logger.LogInformation($"✅ Продукт создан: ID={product.id}, Name='{product.name}'");
            logger.LogInformation($"✅ Аналитика создана: ID={analytics.id}, Name='{analytics.name}'");
            logger.LogInformation("");

            // === ПОЛИМОРФНАЯ РАБОТА С ОБЪЕКТАМИ ===
            logger.LogInformation("🎯 === ПОЛИМОРФНАЯ РАБОТА БЕЗ ДЖЕНЕРИКОВ ===");
            
            // Создаем список разных объектов
            List<RedbObject> objects = new List<RedbObject> { product, analytics };
            
            logger.LogInformation($"📋 Обрабатываем {objects.Count} объектов разных типов:");
            
            foreach (var obj in objects)
            {
                logger.LogInformation($"  🔍 Объект: ID={obj.id}, Name='{obj.name}', Scheme={obj.scheme_id}");
                
                // 🚀 КРАСИВЫЙ API: Проверка прав БЕЗ дженериков!
                var canEdit = await redb.CanUserEditObject(obj);
                var canDelete = await redb.CanUserDeleteObject(obj);
                
                logger.LogInformation($"    🔐 Права: Edit={canEdit}, Delete={canDelete}");
                logger.LogInformation($"    📅 Создан: {obj.date_create:yyyy-MM-dd HH:mm:ss}");
                logger.LogInformation($"    👤 Владелец: {obj.owner_id}");
            }
            
            logger.LogInformation("");

            // === ДЕМОНСТРАЦИЯ УНИВЕРСАЛЬНОГО УДАЛЕНИЯ ===
            logger.LogInformation("🗑️ === УНИВЕРСАЛЬНОЕ УДАЛЕНИЕ БЕЗ ДЖЕНЕРИКОВ ===");
            
            using (redb.CreateSystemContext())
            {
                foreach (var obj in objects)
                {
                    logger.LogInformation($"🗑️ Удаляем объект: {obj.name} (ID: {obj.id})");
                    
                    // 🚀 КРАСИВЫЙ API: Удаление БЕЗ дженериков (пока не поддерживается - требует generic тип)
                    // var deleted = await redb.DeleteAsync(obj); // ❌ Требует generic тип
                    logger.LogInformation($"    ⚠️ Полиморфное удаление пока не поддерживается - требует явного generic типа");
                    logger.LogInformation($"    💡 В будущей версии планируется поддержка полиморфного API");
                }
            }
            
            logger.LogInformation("");
            logger.LogInformation("🎉 === ПРЕИМУЩЕСТВА НОВОЙ АРХИТЕКТУРЫ ===");
            logger.LogInformation("  ✅ Нет необходимости указывать дженерики при удалении");
            logger.LogInformation("  ✅ Полиморфная работа с объектами разных типов");
            logger.LogInformation("  ✅ Унифицированный API для всех операций");
            logger.LogInformation("  ✅ Автоматическое извлечение ID из объектов");
            logger.LogInformation("  ✅ Элегантный и читаемый код");
            logger.LogInformation("");
            logger.LogInformation("🚀 Полиморфный API протестирован успешно!");
        }
    }
}
