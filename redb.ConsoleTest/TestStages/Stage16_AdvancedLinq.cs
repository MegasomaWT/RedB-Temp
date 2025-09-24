using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.DBModels;
using redb.Core.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using redb.ConsoleTest.TestStages;  // ✅ Для доступа к BulkTestProps из Stage42

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Этап 16: Тестирование расширенных LINQ операторов
    /// </summary>
    public class Stage16_AdvancedLinq : BaseTestStage
    {
        public override string Name => "Тестирование расширенных LINQ операторов";
        public override string Description => "Тестирование Any(), WhereIn() и других расширенных LINQ операторов";
        public override int Order => 16;

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🔧 Тестирование расширенных LINQ операторов...");

            // Создаем схему для тестирования
            var schemeId = await redb.SyncSchemeAsync<ProductTestProps>();
            logger.LogInformation($"✅ Схема создана: AdvancedLinqTestProps, ID: {schemeId}");

            // Создаем тестовые данные
            var testProducts = new[]
            {
                new { Name = "Gaming Laptop", Category = "Electronics", Price = 1500.0, Stock = 10, IsActive = true },
                new { Name = "Office Laptop", Category = "Electronics", Price = 800.0, Stock = 25, IsActive = true },
                new { Name = "Programming Book", Category = "Books", Price = 50.0, Stock = 100, IsActive = true },
                new { Name = "Old Phone", Category = "Electronics", Price = 200.0, Stock = 5, IsActive = false },
                new { Name = "Tablet", Category = "Electronics", Price = 400.0, Stock = 15, IsActive = true }
            };

            var createdIds = new List<long>();

            foreach (var product in testProducts)
            {
                var obj = new RedbObject<ProductTestProps>
                {
                    scheme_id = schemeId.Id,
                    owner_id = 0,
                    who_change_id = 0,
                    name = product.Name,
                    note = "Тестовый продукт для расширенных LINQ операторов",
                    properties = new ProductTestProps
                    {
                        Category = product.Category,
                        Price = product.Price,
                        Stock = product.Stock,
                        IsActive = product.IsActive,
                        TestDate = DateTime.Now,  // ✅ ИСПРАВЛЕНИЕ: инициализируем TestDate
                        TestValue = 2
                    }
                };

                await redb.SaveAsync(obj);
                createdIds.Add(obj.id);
                logger.LogInformation($"  📦 Создан продукт: {product.Name} (ID: {obj.id})");
            }

            logger.LogInformation("");
            logger.LogInformation("🔍 === ТЕСТИРОВАНИЕ РАСШИРЕННЫХ LINQ ОПЕРАТОРОВ ===");

            // Тест 1: Any() без параметров
            logger.LogInformation("📋 Тест 1: Any() - проверка существования записей");
            var hasAnyProducts = await (await redb.QueryAsync<ProductTestProps>())
                .AnyAsync();
            logger.LogInformation($"✅ Есть ли продукты: {hasAnyProducts}");

            // Тест 2: Any() с условием
            logger.LogInformation("📋 Тест 2: Any(predicate) - проверка существования с условием");
            var hasExpensiveProducts = await (await redb.QueryAsync<ProductTestProps>())
                .AnyAsync(p => p.Price > 1000);
            logger.LogInformation($"✅ Есть ли дорогие продукты (>$1000): {hasExpensiveProducts}");

            var hasCheapProducts = await (await redb.QueryAsync<ProductTestProps>())
                .AnyAsync(p => p.Price < 10);
            logger.LogInformation($"✅ Есть ли дешевые продукты (<$10): {hasCheapProducts}");

            // Тест 3: WhereIn() - фильтрация по списку значений
            logger.LogInformation("📋 Тест 3: WhereIn() - фильтрация по списку категорий");
            var categories = new[] { "Electronics", "Books" };
            
            // 🔬 ДИАГНОСТИКА: сравним с простыми поисками
            logger.LogInformation("🔍 ДИАГНОСТИКА ПЕРЕД WhereIn:");
            var electronicsOnly = await (await redb.QueryAsync<ProductTestProps>())
                .Where(p => p.Category == "Electronics")
                .ToListAsync();
            var booksOnly = await (await redb.QueryAsync<ProductTestProps>())
                .Where(p => p.Category == "Books")
                .ToListAsync();
            logger.LogInformation($"   📊 Category == Electronics: {electronicsOnly.Count} объектов");
            logger.LogInformation($"   📊 Category == Books: {booksOnly.Count} объектов");
            logger.LogInformation($"   📊 Общий список для WhereIn: [{string.Join(", ", categories)}]");
            
            // 🔬 ДИАГНОСТИКА: логируем что генерируется
            var whereInQuery = (await redb.QueryAsync<ProductTestProps>()).WhereIn(p => p.Category, categories);
            logger.LogInformation("🔍 ДИАГНОСТИКА WhereIn: выполняем запрос...");
            
            var productsInCategories = await whereInQuery.ToListAsync();
            logger.LogInformation($"✅ Найдено продуктов в категориях [Electronics, Books]: {productsInCategories.Count}");
            
            if (productsInCategories.Count == 0 && (electronicsOnly.Count > 0 || booksOnly.Count > 0))
            {
                logger.LogError("❌ КРИТИЧНАЯ ПРОБЛЕМА: WhereIn не работает для строковых полей!");
                logger.LogError("   🔍 Отдельные поиски находят объекты, но WhereIn - нет");
                logger.LogError("   📋 Возможные причины:");
                logger.LogError("     1. Неправильная генерация $in JSON для строк");
                logger.LogError("     2. SQL type_info проблема для String типа");
                logger.LogError("     3. Ошибка в _format_json_array_for_in для строк");
            }
            foreach (var product in productsInCategories)
            {
                logger.LogInformation($"  - {product.name}: Category = {product.properties.Category}");
            }

            // Тест 4: WhereIn() с числовыми значениями
            logger.LogInformation("📋 Тест 4: WhereIn() - фильтрация по списку цен");
            var targetPrices = new[] { 50.0, 200.0, 400.0 };
            var productsWithTargetPrices = await (await redb.QueryAsync<ProductTestProps>())
                .WhereIn(p => p.Price, targetPrices)
                .ToListAsync();
            logger.LogInformation($"✅ Найдено продуктов с ценами [50, 200, 400]: {productsWithTargetPrices.Count}");
            foreach (var product in productsWithTargetPrices)
            {
                logger.LogInformation($"  - {product.name}: Price = ${product.properties.Price}");
            }

            // Тест 5: Комбинирование операторов
            logger.LogInformation("📋 Тест 5: Комбинирование Any() и Where()");
            var hasActiveElectronics = await (await redb.QueryAsync<ProductTestProps>())
                .Where(p => p.Category == "Electronics")
                .AnyAsync(p => p.IsActive == true);
            logger.LogInformation($"✅ Есть ли активная электроника: {hasActiveElectronics}");

            // 🧪 ТЕСТ ИСПРАВЛЕНИЯ LIMIT ОГРАНИЧЕНИЯ - используем объекты из Stage42
            logger.LogInformation("");
            logger.LogInformation("🧪 === ТЕСТ >100 ОБЪЕКТОВ (ИСПРАВЛЕНИЕ LIMIT) ===");
            await TestLimitRemovalWithBulkObjects(logger, redb);

            logger.LogInformation("");
            logger.LogInformation("🎯 === ОЧИСТКА ТЕСТОВЫХ ДАННЫХ ===");
            
            // ✅ КОММЕНТИРУЕМ УДАЛЕНИЕ - ОСТАВЛЯЕМ ОБЪЕКТЫ ДЛЯ АНАЛИЗА SQL
            /*
            // Удаляем созданные объекты
            foreach (var id in createdIds)
            {
                var obj = await redb.LoadAsync<ProductTestProps>(id);
                await redb.DeleteAsync(obj);
                logger.LogInformation($"🗑️ Удален продукт ID: {id}");
            }
            */
            logger.LogInformation("🔍 УДАЛЕНИЕ ЗАКОММЕНТИРОВАНО - объекты остаются для анализа SQL");
        }

        /// <summary>
        /// 🧪 ТЕСТИРУЕТ ИСПРАВЛЕНИЕ СКРЫТОГО ЛИМИТА 100
        /// </summary>
        private async Task TestLimitRemovalWithBulkObjects(ILogger logger, IRedbService redb)
        {
            try
            {
                // 🔍 Запрос БЕЗ Take() - должен вернуть ВСЕ объекты (не только 100)
                logger.LogInformation("🔍 Запрос БЕЗ Take(): query.ToListAsync()...");
                var queryWithoutLimit = await redb.QueryAsync<BulkTestProps>();
                var allBulkObjects = await queryWithoutLimit.ToListAsync();
                
                logger.LogInformation($"📊 БЕЗ Take(): найдено {allBulkObjects.Count} объектов");
                
                // 🎯 Запрос С Take(50) - должен вернуть только 50
                logger.LogInformation("🎯 Запрос С Take(50): query.Take(50).ToListAsync()...");
                var queryWithLimit = await redb.QueryAsync<BulkTestProps>();
                var limitedBulkObjects = await queryWithLimit.Take(50).ToListAsync();
                
                logger.LogInformation($"📊 С Take(50): найдено {limitedBulkObjects.Count} объектов");
                
                // ✅ ПРОВЕРЯЕМ РЕЗУЛЬТАТЫ
                if (allBulkObjects.Count > 100)
                {
                    logger.LogInformation($"🎉 ИСПРАВЛЕНИЕ РАБОТАЕТ! ToListAsync() вернул {allBulkObjects.Count} объектов (>100)!");
                    
                    if (limitedBulkObjects.Count == 50)
                    {
                        logger.LogInformation($"✅ Take(50) работает корректно: {limitedBulkObjects.Count} объектов");
                        logger.LogInformation($"🏆 УСПЕХ! Скрытый лимит 100 полностью устранен!");
                    }
                    else
                    {
                        logger.LogWarning($"⚠️ Take(50) вернул {limitedBulkObjects.Count} объектов вместо 50");
                    }
                }
                else if (allBulkObjects.Count == 100)
                {
                    logger.LogError($"❌ ИСПРАВЛЕНИЕ НЕ РАБОТАЕТ! ToListAsync() всё ещё ограничен 100 объектами");
                    logger.LogError($"🔧 Нужно проверить исправления в PostgresQueryProvider и SQL");
                }
                else
                {
                    logger.LogInformation($"🤔 В базе только {allBulkObjects.Count} объектов BulkTestProps");
                    logger.LogInformation($"💡 Запустите сначала Stage42 для создания 1000 тестовых объектов");
                }
                
                // 📋 Показываем детали нескольких объектов
                if (allBulkObjects.Count > 0)
                {
                    logger.LogInformation($"📋 Примеры найденных объектов:");
                    for (int i = 0; i < Math.Min(3, allBulkObjects.Count); i++)
                    {
                        var obj = allBulkObjects[i];
                        logger.LogInformation($"   • #{i + 1}: ID={obj.Id}, Name='{obj.properties?.Name}', Active={obj.properties?.IsActive}");
                    }
                    
                    if (allBulkObjects.Count > 3)
                    {
                        logger.LogInformation($"   ... и ещё {allBulkObjects.Count - 3} объектов");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка при тестировании лимитов: {ex.Message}");
                throw;
            }
        }
    }
}
