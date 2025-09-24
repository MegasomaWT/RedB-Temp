using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.DBModels;
using redb.Core.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Этап 17: Тестирование дополнительных LINQ операторов (All, Select, Distinct)
    /// </summary>
    public class Stage17_AdvancedLinqOperators : BaseTestStage
    {
        public override string Name => "Тестирование дополнительных LINQ операторов";
        public override string Description => "Тестирование All(), Select(), Distinct() операторов";
        public override int Order => 17;

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🔧 Тестирование дополнительных LINQ операторов...");

            // Создаем схему для тестирования
            var schemeId = await redb.SyncSchemeAsync<ProductTestProps>();
            logger.LogInformation($"✅ Схема создана: {nameof(ProductTestProps)}, ID: {schemeId}");

            // Создаем тестовые данные
            var testProducts = new[]
            {
                new { Name = "Expensive Laptop", Category = "Electronics", Price = 2000.0, Stock = 5, IsActive = true },
                new { Name = "Cheap Laptop", Category = "Electronics", Price = 500.0, Stock = 15, IsActive = true },
                new { Name = "Gaming Mouse", Category = "Electronics", Price = 80.0, Stock = 50, IsActive = true },
                new { Name = "Programming Book", Category = "Books", Price = 45.0, Stock = 100, IsActive = true },
                new { Name = "Old Book", Category = "Books", Price = 10.0, Stock = 2, IsActive = false },
                new { Name = "Tablet", Category = "Electronics", Price = 300.0, Stock = 20, IsActive = true },
                // Добавляем НАСТОЯЩИЙ дубликат для тестирования Distinct() - точно такой же как Cheap Laptop
                new { Name = "Cheap Laptop", Category = "Electronics", Price = 500.0, Stock = 15, IsActive = true }
            };

            logger.LogInformation("📋 Создаваемые тестовые данные:");
            foreach (var product in testProducts)
            {
                logger.LogInformation($"  - {product.Name}: Category={product.Category}, Price=${product.Price}, Stock={product.Stock}, IsActive={product.IsActive}");
            }
            logger.LogInformation($"📊 Всего создаем: {testProducts.Length} продуктов (включая 1 полный дубликат 'Cheap Laptop' для тестирования Distinct)");

            var createdIds = new List<long>();

            foreach (var product in testProducts)
            {
                var obj = new RedbObject<ProductTestProps>
                {
                    scheme_id = schemeId.Id,
                    owner_id = 0,
                    who_change_id = 0,
                    name = product.Name,
                    note = "Тестовый продукт для дополнительных LINQ операторов",
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
            logger.LogInformation("🔍 === ПРОВЕРКА СОХРАНЕННЫХ ДАННЫХ ===");
            
            // Загружаем все созданные объекты и проверяем их свойства
            var allSavedProducts = await (await redb.QueryAsync<ProductTestProps>()).ToListAsync();
            logger.LogInformation($"📊 Загружено из БД: {allSavedProducts.Count} продуктов");
            
            foreach (var product in allSavedProducts)
            {
                logger.LogInformation($"  - {product.name}: Category={product.properties.Category}, Price=${product.properties.Price}, Stock={product.properties.Stock}, IsActive={product.properties.IsActive}");
                logger.LogInformation($"    🔑 Hash: {product.hash}, ID: {product.id}");
            }

            logger.LogInformation("");
            logger.LogInformation("🔍 === ПРОВЕРКА РАБОТЫ WHERE() ===");
            
            // Простой тест Where() - должно найти только активные продукты
            var activeProducts = await (await redb.QueryAsync<ProductTestProps>())
                .Where(p => p.IsActive == true)
                .ToListAsync();
            logger.LogInformation($"📊 Активных продуктов (Where IsActive=true): {activeProducts.Count}");
            foreach (var product in activeProducts)
            {
                logger.LogInformation($"  - {product.name}: IsActive={product.properties.IsActive}");
            }
            
            // Тест Where() по цене - должно найти только дорогие продукты  
            var expensiveProducts = await (await redb.QueryAsync<ProductTestProps>())
                .Where(p => p.Price > 1000)
                .ToListAsync();
            logger.LogInformation($"📊 Дорогих продуктов (Where Price > 1000): {expensiveProducts.Count}");
            foreach (var product in expensiveProducts)
            {
                logger.LogInformation($"  - {product.name}: Price=${product.properties.Price}");
            }

            logger.LogInformation("");
            logger.LogInformation("🔍 === ТЕСТИРОВАНИЕ ДОПОЛНИТЕЛЬНЫХ LINQ ОПЕРАТОРОВ ===");

            // Тест 1: AllAsync() - проверка что все записи удовлетворяют условию
            logger.LogInformation("📋 Тест 1: AllAsync() - проверка что все записи удовлетворяют условию");
            
            try
            {
                // Сначала проверим сколько у нас записей для отладки
                var totalCount = await (await redb.QueryAsync<ProductTestProps>()).CountAsync();
                var activeCount = await (await redb.QueryAsync<ProductTestProps>())
                    .Where(p => p.IsActive == true).CountAsync();
                logger.LogInformation($"📊 Всего продуктов: {totalCount}, активных: {activeCount}");

                // Проверяем что все активные продукты имеют цену > 0
                logger.LogInformation("🔍 ТЕСТ: Все активные продукты имеют цену > 0");
                var activeQuery = (await redb.QueryAsync<ProductTestProps>())
                    .Where(p => p.IsActive == true);
                var allActiveHavePrice = await activeQuery.AllAsync(p => p.Price > 0);
                logger.LogInformation($"✅ Все активные продукты имеют цену > 0: {allActiveHavePrice}");

                // Проверяем что все продукты дорогие (должно быть false)
                var allExpensive = await (await redb.QueryAsync<ProductTestProps>())
                    .AllAsync(p => p.Price > 1000);
                logger.LogInformation($"✅ Все продукты дорогие (>$1000): {allExpensive}");

                // Проверяем что все электронные товары активны (должно быть true - все Electronics активны)
                var electronicsCount = await (await redb.QueryAsync<ProductTestProps>())
                    .Where(p => p.Category == "Electronics").CountAsync();
                var allElectronicsActive = await (await redb.QueryAsync<ProductTestProps>())
                    .Where(p => p.Category == "Electronics")
                    .AllAsync(p => p.IsActive == true);
                logger.LogInformation($"✅ Вся электроника активна ({electronicsCount} товаров): {allElectronicsActive}");

                // Проверяем что все книги имеют низкую цену (должно быть true)
                var booksCount = await (await redb.QueryAsync<ProductTestProps>())
                    .Where(p => p.Category == "Books").CountAsync();
                var allBooksCheap = await (await redb.QueryAsync<ProductTestProps>())
                    .Where(p => p.Category == "Books")
                    .AllAsync(p => p.Price < 100);
                logger.LogInformation($"✅ Все книги дешевые (<$100) ({booksCount} книг): {allBooksCheap}");

                // Дополнительная проверка: все продукты имеют положительную цену (должно быть true)
                logger.LogInformation("🔍 ТЕСТ: Все продукты имеют положительную цену (должно быть TRUE)");
                var allQuery = await redb.QueryAsync<ProductTestProps>();
                var allHavePositivePrice = await allQuery.AllAsync(p => p.Price > 0);
                logger.LogInformation($"✅ Все продукты имеют положительную цену: {allHavePositivePrice}");
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка в тесте AllAsync(): {ex.Message}");
            }

            // Тест 2: Select() - проекция полей
            logger.LogInformation("");
            logger.LogInformation("📋 Тест 2: Select() - проекция полей");
            
            try
            {
                // Тест 1: Выбор только имен продуктов
                var productNames = await (await redb.QueryAsync<ProductTestProps>())
                    .Select(p => p.name)
                    .ToListAsync();
                logger.LogInformation($"📊 Имена продуктов ({productNames.Count}): {string.Join(", ", productNames)}");
                
                // Тест 2: Выбор анонимного объекта
                var productSummary = await (await redb.QueryAsync<ProductTestProps>())
                    .Where(p => p.IsActive == true)
                    .Select(p => new { Name = p.name, Price = p.properties.Price })
                    .ToListAsync();
                logger.LogInformation($"📊 Активные продукты - краткая информация ({productSummary.Count}):");
                foreach (var item in productSummary)
                {
                    logger.LogInformation($"  - {item.Name}: ${item.Price}");
                }
                
                // Тест 3: Выбор только цен
                var prices = await (await redb.QueryAsync<ProductTestProps>())
                    .Select(p => p.properties.Price)
                    .ToListAsync();
                logger.LogInformation($"📊 Все цены: {string.Join(", ", prices.Select(p => $"${p}"))}");
                
                logger.LogInformation("✅ Select() протестирован успешно");
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка в тесте Select(): {ex.Message}");
            }

            // Тест 3: Distinct() - уникальные значения
            logger.LogInformation("");
            logger.LogInformation("📋 Тест 3: Distinct() - уникальные значения");
            
            try
            {
                // Тестируем Distinct() - должно убрать дубликаты
                var allProducts = await (await redb.QueryAsync<ProductTestProps>())
                    .ToListAsync();
                logger.LogInformation($"📊 Всего продуктов без Distinct(): {allProducts.Count}");
                
                var distinctProducts = await (await redb.QueryAsync<ProductTestProps>())
                    .Distinct()
                    .ToListAsync();
                logger.LogInformation($"📊 Уникальных продуктов с Distinct(): {distinctProducts.Count}");
                
                // Проверяем что Distinct() работает корректно
                if (distinctProducts.Count < allProducts.Count)
                {
                    logger.LogInformation($"✅ Distinct() убрал дубликаты: {allProducts.Count} → {distinctProducts.Count} (убрано {allProducts.Count - distinctProducts.Count} дубликатов)");
                }
                else if (distinctProducts.Count == allProducts.Count)
                {
                    logger.LogInformation("ℹ️ Distinct() не изменил количество - дубликатов не было");
                }
                else
                {
                    logger.LogWarning("⚠️ Distinct() вернул больше записей чем общий запрос - возможная ошибка");
                }
                
                // Тестируем Distinct() с фильтром
                var distinctActiveProducts = await (await redb.QueryAsync<ProductTestProps>())
                    .Where(p => p.IsActive == true)
                    .Distinct()
                    .ToListAsync();
                logger.LogInformation($"📊 Уникальных активных продуктов: {distinctActiveProducts.Count}");
                
                logger.LogInformation("✅ Distinct() протестирован успешно");
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка в тесте Distinct(): {ex.Message}");
            }

            // ✅ КРИТИЧНЫЙ ТЕСТ: OrderBy ЛОМАЕТ ФИЛЬТРАЦИЮ - воспроизведение проблемы №4 из redb3.txt
            logger.LogInformation("");
            logger.LogInformation("📋 Тест 4: 🚨 КРИТИЧНЫЙ - OrderBy ломает фильтрацию (точная проблема из redb3.txt)");
            
            try
            {
                // ШАГ 0: Проверяем СКОЛЬКО ВСЕГО объектов в схеме (для понимания масштаба)
                var allQuery = await redb.QueryAsync<ProductTestProps>();
                var allObjects = await allQuery.ToListAsync();
                logger.LogInformation($"📊 ОБЩАЯ СТАТИСТИКА: всего объектов в схеме {allObjects.Count}");
                
                var categoryStats = allObjects.GroupBy(o => o.properties.Category).ToList();
                foreach (var category in categoryStats.Take(5))
                {
                    logger.LogInformation($"   📂 Category '{category.Key}': {category.Count()} объектов");
                }
                
                // ШАГ 1: Создаем РАБОТАЮЩИЙ фильтр (используем >= который работает!)
                var limitedQuery = (await redb.QueryAsync<ProductTestProps>())
                    .Where(p => p.Price >= 1999);  // ✅ РАБОТАЮЩИЙ ФИЛЬТР: Price >= 1999 (должен найти объекты)
                
                logger.LogInformation("🔍 Создан РАБОТАЮЩИЙ запрос Price >= 1999 (должен найти дорогие объекты)");
                
                // ШАГ 2: Проверяем что РАБОТАЮЩИЙ фильтр находит дорогие объекты (Price >= 1999)
                var test1 = await limitedQuery.ToListAsync();
                logger.LogInformation($"📊 ТЕСТ 1 (до OrderBy): найдено {test1.Count} объектов");
                if (test1.Count > 0)
                {
                    logger.LogInformation($"   🔍 Первый найденный: {test1[0].name} - Price=${test1[0].properties.Price}");
                    logger.LogInformation($"   🔍 Всего дорогих объектов (Price >= 1999): {test1.Count}");
                }
                
                // ШАГ 3: 🚨 КРИТИЧНЫЙ МОМЕНТ - добавляем OrderBy (точно как в примере из redb3.txt)
                logger.LogInformation("");
                logger.LogInformation("🚨 ПРИМЕНЯЕМ OrderBy - ПРОВЕРЯЕМ НЕ СТАНЕТ ЛИ ОБЪЕКТОВ БОЛЬШЕ...");
                logger.LogInformation("   📝 По примеру: recordsQuery = recordsQuery.OrderBy(r => r.Price);");
                var recordsQuery = limitedQuery.OrderBy(r => r.Price);  // ← ТОЧНО ПО ПРИМЕРУ ИЗ redb3.txt!
                
                // ШАГ 4: Проверяем что стало после OrderBy
                var test2 = await recordsQuery.ToListAsync();
                logger.LogInformation($"📊 ТЕСТ 2 (после OrderBy): найдено {test2.Count} объектов");
                
                // ШАГ 5: 🔥 ДЕТАЛЬНЫЙ АНАЛИЗ
                if (test2.Count == test1.Count && test1.Count >= 1)
                {
                    logger.LogInformation($"✅ ПРОБЛЕМА №4 ИСПРАВЛЕНА: OrderBy сохранил фильтрацию ({test2.Count} объектов)!");
                    foreach (var obj in test2)
                    {
                        logger.LogInformation($"   ✅ После OrderBy: {obj.name} - Category={obj.properties.Category}, Price=${obj.properties.Price}");
                    }
                }
                else if (test2.Count > test1.Count)
                {
                    logger.LogError($"❌ ПРОБЛЕМА №4 ВОСПРОИЗВЕДЕНА: OrderBy СЛОМАЛ фильтрацию!");
                    logger.LogError($"   📊 До OrderBy: {test1.Count} объектов (фильтр Price>=1999 работал)");
                    logger.LogError($"   📊 После OrderBy: {test2.Count} объектов (ПОЯВИЛИСЬ ЛИШНИЕ!)");
                    logger.LogError($"   🚨 ТОЧНО как в примере: 'было {test1.Count} стало {test2.Count} из-за OrderBy'");
                    
                    // Показываем лишние объекты (которые НЕ соответствуют фильтру Price >= 1999)
                    var invalidObjects = test2.Where(o => o.properties.Price < 1999).ToList();
                    logger.LogError($"   🔍 Лишних объектов (Price < 1999): {invalidObjects.Count}");
                    foreach (var obj in invalidObjects.Take(5))
                    {
                        logger.LogError($"     - {obj.name}: Price=${obj.properties.Price} (НЕ >= 1999!)");
                    }
                    
                    // Статистика по всем объектам после OrderBy
                    var validObjects = test2.Where(o => o.properties.Price >= 1999).Count();
                    logger.LogError($"   📊 Корректных объектов (Price >= 1999): {validObjects} из {test2.Count}");
                }
                else if (test1.Count == 0 && test2.Count == 0)
                {
                    logger.LogWarning("⚠️ Фильтр Price>=1999 не нашел дорогие объекты - возможно проблема с данными или фильтрацией");
                }
                else
                {
                    logger.LogWarning($"⚠️ Неожиданный результат: было {test1.Count}, стало {test2.Count}");
                }
                
                logger.LogInformation("✅ Критичный тест OrderBy завершен");
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка в критичном тесте OrderBy: {ex.Message}");
            }

            logger.LogInformation("");
                            logger.LogInformation("🎯 === ДОПОЛНИТЕЛЬНАЯ ДИАГНОСТИКА PRICE ФИЛЬТРАЦИИ ===");
            
            try 
            {
                logger.LogInformation("🔍 ТЕСТИРУЕМ ИСПРАВЛЕНИЕ PostgresFacetFilterBuilder с типизацией значений...");
                
                // 🔍 ТЕСТ ИСПРАВЛЕНИЯ: Фильтр Price == 2000 с логированием типов
                logger.LogInformation("🧪 ИСПРАВЛЕННЫЙ ТЕСТ: Price == 2000 (с типизацией значений)");
                var fixedEqualityTest = await (await redb.QueryAsync<ProductTestProps>())
                    .Where(p => p.Price == 2000)  // Должно работать после исправления!
                    .ToListAsync();
                logger.LogInformation($"   📊 Результат с исправленным кодом: {fixedEqualityTest.Count} объектов");
                
                // 🔍 ДЕТАЛЬНАЯ ДИАГНОСТИКА ПРОБЛЕМЫ С PRICE - загружаем все продукты
                var diagnosticProducts = await (await redb.QueryAsync<ProductTestProps>()).ToListAsync();
                var expensiveLaptop = diagnosticProducts.FirstOrDefault(p => p.name == "Expensive Laptop");
                if (expensiveLaptop != null)
                {
                    logger.LogInformation($"🔍 Expensive Laptop найден:");
                    logger.LogInformation($"   💰 Price: {expensiveLaptop.properties.Price} ({expensiveLaptop.properties.Price.GetType().Name})");
                    logger.LogInformation($"   🆔 ID: {expensiveLaptop.Id}");
                    
                    // Тестируем разные способы сравнения
                    var directFilter = await (await redb.QueryAsync<ProductTestProps>())
                        .Where(p => p.Price == expensiveLaptop.properties.Price)  // Используем точное значение из объекта
                        .ToListAsync();
                    logger.LogInformation($"🧪 Фильтр Price == {expensiveLaptop.properties.Price} (точное значение): {directFilter.Count} объектов");
                    
                    // ✅ ИСПРАВЛЕННЫЙ ТЕСТ: integer сравнение  
                    var integerFilter = await (await redb.QueryAsync<ProductTestProps>())
                        .Where(p => p.Price == 2000)  // integer 2000 вместо double
                        .ToListAsync();
                    logger.LogInformation($"🧪 Фильтр Price == 2000 (integer): {integerFilter.Count} объектов");
                    
                    // Проверяем диапазон (по-прежнему должен работать)
                    var rangeFilter = await (await redb.QueryAsync<ProductTestProps>())
                        .Where(p => p.Price >= 1999 && p.Price <= 2001)  // тоже integer
                        .ToListAsync();
                    logger.LogInformation($"🧪 Фильтр Price BETWEEN 1999-2001: {rangeFilter.Count} объектов");
                }
                else
                {
                    logger.LogError("❌ Expensive Laptop не найден в diagnosticProducts!");
                    logger.LogInformation($"🔍 Найдено продуктов для диагностики: {diagnosticProducts.Count}");
                    foreach (var prod in diagnosticProducts.Take(3))
                    {
                        logger.LogInformation($"   - {prod.name}: Price=${prod.properties.Price}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка в диагностике Price: {ex.Message}");
            }
            
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
    }
}
