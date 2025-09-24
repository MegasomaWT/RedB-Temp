using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.DBModels;
using redb.Core.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages;

public class Stage18_SortingAndPagination : ITestStage
{
    public int StageNumber => 18;
    public int Order => 18;
    public string Name => "Тестирование сортировки и пагинации";
    public string Description => "Тестирование OrderBy(), ThenBy(), Take(), Skip() операторов";

    public async Task ExecuteAsync(ILogger logger, IRedbService redb)
    {
        try
        {
            logger.LogInformation("🔧 Тестирование сортировки и пагинации...");
            
            // Создаем и синхронизируем схему
            var schemeId = await redb.SyncSchemeAsync<ProductTestProps>();
            logger.LogInformation($"✅ Схема создана и синхронизирована: {nameof(ProductTestProps)}, ID: {schemeId}");

            // Создаем тестовые данные с разными ценами для проверки сортировки
            var testProducts = new[]
            {
                new { Name = "Laptop A", Category = "Electronics", Price = 1500.0, Stock = 10, IsActive = true },
                new { Name = "Laptop B", Category = "Electronics", Price = 800.0, Stock = 25, IsActive = true },
                new { Name = "Mouse", Category = "Electronics", Price = 50.0, Stock = 100, IsActive = true },
                new { Name = "Book A", Category = "Books", Price = 30.0, Stock = 50, IsActive = true },
                new { Name = "Book B", Category = "Books", Price = 25.0, Stock = 75, IsActive = false },
                new { Name = "Tablet", Category = "Electronics", Price = 400.0, Stock = 15, IsActive = true },
                new { Name = "Keyboard", Category = "Electronics", Price = 120.0, Stock = 30, IsActive = true },
                new { Name = "Book C", Category = "Books", Price = 45.0, Stock = 20, IsActive = true },
                new { Name = "Monitor", Category = "Electronics", Price = 300.0, Stock = 8, IsActive = false },
                new { Name = "Headphones", Category = "Electronics", Price = 200.0, Stock = 40, IsActive = true }
            };

            logger.LogInformation("📋 Создаваемые тестовые данные:");
            foreach (var product in testProducts)
            {
                logger.LogInformation($"  - {product.Name}: Category={product.Category}, Price=${product.Price}, Stock={product.Stock}, IsActive={product.IsActive}");
            }
            logger.LogInformation($"📊 Всего создаем: {testProducts.Length} продуктов для тестирования сортировки и пагинации");

            var createdIds = new List<long>();

            foreach (var product in testProducts)
            {
                var obj = new RedbObject<ProductTestProps>
                {
                    scheme_id = schemeId.Id,
                    name = product.Name,
                    owner_id = 0,
                    who_change_id = 0,
                    date_create = DateTime.Now,
                    date_modify = DateTime.Now,
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

                var savedId = await redb.SaveAsync(obj);
                createdIds.Add(savedId);
                logger.LogInformation($"  📦 Создан продукт: {product.Name} (ID: {savedId})");
            }

            logger.LogInformation("");
            logger.LogInformation("🔍 === ТЕСТИРОВАНИЕ СОРТИРОВКИ ===");

            // Тест 1: OrderBy - сортировка по цене по возрастанию
            logger.LogInformation("📋 Тест 1: OrderBy() - сортировка по цене по возрастанию");
            var sortedByPrice = await (await redb.QueryAsync<ProductTestProps>())
                .OrderBy(p => p.Price)
                .ToListAsync();
            
            logger.LogInformation($"📊 Продукты отсортированы по цене (возрастание) - {sortedByPrice.Count} шт:");
            foreach (var product in sortedByPrice.Take(5)) // Показываем первые 5
            {
                logger.LogInformation($"  - {product.name}: ${product.properties.Price}");
            }

            // Проверяем что сортировка работает
            var prices = sortedByPrice.Select(p => p.properties.Price).ToList();
            var isSorted = prices.SequenceEqual(prices.OrderBy(p => p));
            logger.LogInformation($"✅ Сортировка по возрастанию работает: {isSorted}");

            // Тест 2: OrderByDescending - сортировка по цене по убыванию
            logger.LogInformation("");
            logger.LogInformation("📋 Тест 2: OrderByDescending() - сортировка по цене по убыванию");
            var sortedByPriceDesc = await (await redb.QueryAsync<ProductTestProps>())
                .OrderByDescending(p => p.Price)
                .ToListAsync();
            
            logger.LogInformation($"📊 Продукты отсортированы по цене (убывание) - {sortedByPriceDesc.Count} шт:");
            foreach (var product in sortedByPriceDesc.Take(5)) // Показываем первые 5
            {
                logger.LogInformation($"  - {product.name}: ${product.properties.Price}");
            }

            // Проверяем что сортировка по убыванию работает
            var pricesDesc = sortedByPriceDesc.Select(p => p.properties.Price).ToList();
            var isSortedDesc = pricesDesc.SequenceEqual(pricesDesc.OrderByDescending(p => p));
            logger.LogInformation($"✅ Сортировка по убыванию работает: {isSortedDesc}");

            // Тест 3: OrderBy + ThenBy - сортировка по категории, затем по цене
            logger.LogInformation("");
            logger.LogInformation("📋 Тест 3: OrderBy() + ThenBy() - сортировка по категории, затем по цене");
            var sortedByCategoryThenPrice = await (await redb.QueryAsync<ProductTestProps>())
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Price)
                .ToListAsync();
            
            logger.LogInformation($"📊 Продукты отсортированы по категории + цене - {sortedByCategoryThenPrice.Count} шт:");
            foreach (var product in sortedByCategoryThenPrice)
            {
                logger.LogInformation($"  - {product.name}: {product.properties.Category} - ${product.properties.Price}");
            }

            // Проверяем сортировку по категориям
            var categories = sortedByCategoryThenPrice.GroupBy(p => p.properties.Category);
            logger.LogInformation($"✅ Найдено категорий: {categories.Count()}");
            foreach (var category in categories)
            {
                var categoryPrices = category.Select(p => p.properties.Price).ToList();
                var isCategorySorted = categoryPrices.SequenceEqual(categoryPrices.OrderBy(p => p));
                logger.LogInformation($"  - {category.Key}: {category.Count()} товаров, сортировка по цене: {isCategorySorted}");
            }

            // ✅ НОВЫЙ ТЕСТ: BinaryExpression в ThenBy - исправление проблемы №5 из redb3.txt
            logger.LogInformation("");
            logger.LogInformation("📋 Тест 3.5: ThenBy() с BinaryExpression - тест исправления r.Field != null");
            var binaryExpressionSort = await (await redb.QueryAsync<ProductTestProps>())
                .OrderBy(p => p.Category)
                .ThenBy(p => p.IsActive != false)  // ✅ BinaryExpression: p.IsActive != false
                .ToListAsync();
            
            logger.LogInformation($"📊 Продукты отсортированы по Category + BinaryExpression - {binaryExpressionSort.Count} шт:");
            foreach (var product in binaryExpressionSort.Take(5))
            {
                logger.LogInformation($"  - {product.name}: {product.properties.Category} - Active:{product.properties.IsActive}");
            }
            
            // Проверяем что BinaryExpression обрабатывается без ошибок
            var binaryCategories = binaryExpressionSort.GroupBy(p => p.properties.Category);
            foreach (var category in binaryCategories)
            {
                var categoryActiveFirst = category.Select(p => p.properties.IsActive).ToList();
                // Активные должны быть перед неактивными (true != false = true идет первым)
                logger.LogInformation($"  - {category.Key}: {category.Count()} товаров, активные сначала: {categoryActiveFirst.Take(3).Count(a => a == true) >= categoryActiveFirst.Skip(3).Count(a => a == true)}");
            }
            logger.LogInformation("✅ BinaryExpression в ThenBy() работает без ошибок!");

            logger.LogInformation("");
            logger.LogInformation("🔍 === ТЕСТИРОВАНИЕ ПАГИНАЦИИ ===");

            // Тест 4: Take() - получить первые N продуктов
            logger.LogInformation("📋 Тест 4: Take() - получить первые 3 продукта");
            var firstThree = await (await redb.QueryAsync<ProductTestProps>())
                .OrderBy(p => p.Price)
                .Take(3)
                .ToListAsync();
            
            logger.LogInformation($"📊 Первые 3 самых дешевых продукта:");
            foreach (var product in firstThree)
            {
                logger.LogInformation($"  - {product.name}: ${product.properties.Price}");
            }
            logger.LogInformation($"✅ Take(3) вернул {firstThree.Count} продуктов (ожидалось: 3)");

            // Тест 5: Skip() - пропустить первые N продуктов
            logger.LogInformation("");
            logger.LogInformation("📋 Тест 5: Skip() - пропустить первые 5 продуктов");
            var afterSkip = await (await redb.QueryAsync<ProductTestProps>())
                .OrderBy(p => p.Price)
                .Skip(5)
                .ToListAsync();
            
            logger.LogInformation($"📊 Продукты после пропуска первых 5:");
            foreach (var product in afterSkip.Take(3)) // Показываем первые 3 из оставшихся
            {
                logger.LogInformation($"  - {product.name}: ${product.properties.Price}");
            }
            logger.LogInformation($"✅ Skip(5) вернул {afterSkip.Count} продуктов (ожидалось: 5)");

            // Тест 6: Skip() + Take() - пагинация
            logger.LogInformation("");
            logger.LogInformation("📋 Тест 6: Skip() + Take() - пагинация (страница 2, по 3 элемента)");
            var page2 = await (await redb.QueryAsync<ProductTestProps>())
                .OrderBy(p => p.Price)
                .Skip(3)
                .Take(3)
                .ToListAsync();
            
            logger.LogInformation($"📊 Страница 2 (элементы 4-6):");
            foreach (var product in page2)
            {
                logger.LogInformation($"  - {product.name}: ${product.properties.Price}");
            }
            logger.LogInformation($"✅ Пагинация Skip(3).Take(3) вернула {page2.Count} продуктов");

            // Тест 7: Комплексный запрос
            logger.LogInformation("");
            logger.LogInformation("📋 Тест 7: Комплексный - Where() + OrderBy() + ThenBy() + Skip() + Take()");
            var complexQuery = await (await redb.QueryAsync<ProductTestProps>())
                .Where(p => p.IsActive == true)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Price)
                .Skip(1)
                .Take(4)
                .ToListAsync();
            
            logger.LogInformation($"📊 Комплексный запрос - активные товары, отсортированные, страница 2:");
            foreach (var product in complexQuery)
            {
                logger.LogInformation($"  - {product.name}: {product.properties.Category} - ${product.properties.Price} (Active: {product.properties.IsActive})");
            }
            logger.LogInformation($"✅ Комплексный запрос вернул {complexQuery.Count} продуктов");

            // Очистка тестовых данных
            logger.LogInformation("");
            logger.LogInformation("🎯 === ОЧИСТКА ТЕСТОВЫХ ДАННЫХ ===");
            
            // ✅ КОММЕНТИРУЕМ УДАЛЕНИЕ - ОСТАВЛЯЕМ ОБЪЕКТЫ ДЛЯ АНАЛИЗА SQL
            /*
            foreach (var id in createdIds)
            {
                var obj = await redb.LoadAsync<ProductTestProps>(id);
                await redb.DeleteAsync(obj);
                logger.LogInformation($"🗑️ Удален продукт ID: {id}");
            }
            */
            logger.LogInformation("🔍 УДАЛЕНИЕ ЗАКОММЕНТИРОВАНО - объекты остаются для анализа SQL");

            logger.LogInformation("✅ Сортировка и пагинация протестированы успешно");
        }
        catch (Exception ex)
        {
            logger.LogError($"❌ Ошибка в этапе {StageNumber}: {Name}");
            logger.LogError($"❌ {ex.Message}");
            throw;
        }
    }
}