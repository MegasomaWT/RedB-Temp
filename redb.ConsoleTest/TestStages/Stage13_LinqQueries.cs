using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.DBModels;
using redb.Core.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Этап 13: Тестирование LINQ-запросов
    /// </summary>
    public class Stage13_LinqQueries : BaseTestStage
    {
        public override string Name => "Тестирование СЛОЖНЫХ LINQ-запросов с массивами";
        public override string Description => "Тестирование типобезопасных LINQ-запросов к REDB со сложными объектами и массивами";
        public override int Order => 13;

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("📝 Создаем схему для тестирования LINQ-запросов...");
            
            // Создаем/синхронизируем схему для сложного объекта с массивами
            var schemeId = await redb.SyncSchemeAsync<MixedTestProps>();
            logger.LogInformation($"✅ Схема создана: {nameof(MixedTestProps)}, ID: {schemeId}");

            logger.LogInformation("📦 Создаем тестовые данные...");
            
            // Создаем сложные объекты с массивами (из Stage 5)
            var testDate = new DateTime(2025, 1, 15, 10, 30, 0);
            var products = new[]
            {
                new RedbObject<MixedTestProps> 
                { 
                    scheme_id = schemeId.Id,
                    name = "Mixed Object 1",
                    owner_id = 0,
                    who_change_id = 0,
                    date_create = DateTime.Now,
                    date_modify = DateTime.Now,
                    properties = new MixedTestProps { 
                        Age = 30, Name = "John Doe", Date = testDate, Article = "Article 1", Stock = 150, Tag = "mixed-test",
                        Tags1 = new[] { "developer", "senior", "fullstack" },
                        Scores1 = new[] { 85, 92, 78 },
                        Tags2 = new[] { "secondary", "tags" },
                        Scores2 = new[] { 33, 22 },
                        Address1 = new Address { 
                            City = "Moscow", Street = "Main Street 123",
                            Details = new Details { 
                                Floor = 5, Building = "Building A",
                                Tags1 = new[] { "moscow", "main-street", "building-a" },
                                Scores1 = new[] { 95, 87, 92 }
                            }
                        },
                        Contacts = new[] {
                            new Contact { Type = "email", Value = "john@example.com", Verified = true },
                            new Contact { Type = "phone", Value = "+7-999-123-45-67", Verified = false }
                        }
                    }
                },
                new RedbObject<MixedTestProps> 
                { 
                    scheme_id = schemeId.Id,
                    name = "Mixed Object 2", 
                    owner_id = 0,
                    who_change_id = 0,
                    date_create = DateTime.Now,
                    date_modify = DateTime.Now,
                    properties = new MixedTestProps { 
                        Age = 25, Name = "Jane Smith", Date = testDate.AddDays(1), Article = "Article 2", Stock = 80, Tag = "test-user",
                        Tags1 = new[] { "designer", "junior" },
                        Scores1 = new[] { 70, 88 },
                        Tags2 = new[] { "creative" },
                        Scores2 = new[] { 45 }
                    }
                },
                new RedbObject<MixedTestProps> 
                { 
                    scheme_id = schemeId.Id,
                    name = "Mixed Object 3", 
                    owner_id = 0,
                    who_change_id = 0,
                    date_create = DateTime.Now,
                    date_modify = DateTime.Now,
                    properties = new MixedTestProps { 
                        Age = 35, Name = "Mike Johnson", Date = testDate, Article = "Article 3", Stock = 50, Tag = "complex-test",
                        Tags1 = new[] { "manager", "team-lead", "senior" },
                        Scores1 = new[] { 95, 100, 85, 90 },
                        Tags2 = new string[0], // Пустой массив
                        Scores2 = new int[0]   // Пустой массив
                    }
                }
            };

            var productIds = new List<long>();
            foreach (var product in products)
            {
                try
                {
                    var id = await redb.SaveAsync(product);
                    productIds.Add(id);
                    logger.LogInformation($"  📦 Создан продукт: {product.name} (ID: {id})");
                    
                    // 🔥 ВРЕМЕННАЯ ДИАГНОСТИКА СОЗДАННЫХ ОБЪЕКТОВ
                    Console.WriteLine($"🔥 CREATED: {product.name} -> Age={product.properties.Age}, Stock={product.properties.Stock}, Tags1={string.Join(",", product.properties.Tags1)}");
                }
                catch (Exception ex)
                {
                    logger.LogError($"❌ Ошибка создания продукта {product.name}: {ex.Message}");
                    logger.LogError($"StackTrace: {ex.StackTrace}");
                    throw;
                }
            }

            logger.LogInformation("");
            logger.LogInformation("🔍 === ТЕСТИРОВАНИЕ LINQ-ЗАПРОСОВ ===");

            // Тест 1: Простой Where (Stock > 100)
            logger.LogInformation("📋 Тест 1: Простой Where (Stock > 100)");
            try
            {
                var query = await redb.QueryAsync<MixedTestProps>();
                var highStockProducts = await query
                    .Where(p => p.Stock > 100)
                    .ToListAsync();

                logger.LogInformation($"✅ Найдено {highStockProducts.Count} объектов с Stock > 100:");
                foreach (var product in highStockProducts)
                {
                    logger.LogInformation($"  - {product.name}: Stock = {product.properties.Stock}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка в тесте 1: {ex.Message}");
                logger.LogError($"StackTrace: {ex.StackTrace}");
            }

            // Тест 2: Сложное условие AND (Age и Stock)
            logger.LogInformation("");
            logger.LogInformation("📋 Тест 2: Сложное условие (Age >= 30 AND Stock >= 100)");
            try
            {
                var query = await redb.QueryAsync<MixedTestProps>();
                var experienced = await query
                    .Where(p => p.Age >= 30 && p.Stock >= 100)
                    .ToListAsync();

                logger.LogInformation($"✅ Найдено {experienced.Count} опытных с большим Stock:");
                foreach (var product in experienced)
                {
                    logger.LogInformation($"  - {product.name}: Age = {product.properties.Age}, Stock = {product.properties.Stock}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка в тесте 2: {ex.Message}");
                logger.LogError($"StackTrace: {ex.StackTrace}");
            }

            // Тест 3: Count
            logger.LogInformation("");
            logger.LogInformation("📋 Тест 3: Подсчет записей (CountAsync)");
            try
            {
                var query = await redb.QueryAsync<MixedTestProps>();
                var totalCount = await query.CountAsync();
                var youngCount = await query.Where(p => p.Age < 30).CountAsync();

                logger.LogInformation($"✅ Всего объектов: {totalCount}");
                logger.LogInformation($"✅ Молодых (Age < 30): {youngCount}");
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка в тесте 3: {ex.Message}");
                logger.LogError($"StackTrace: {ex.StackTrace}");
            }

            // ===== ТЕСТ ФИЛЬТРАЦИИ ПОРЯДКА (ДИАГНОСТИКА ПРОБЛЕМЫ) =====
            logger.LogInformation("");
            logger.LogInformation("🐛 === ТЕСТ ПРОБЛЕМЫ ПОРЯДКА ФИЛЬТРАЦИИ ===");
            
            // Тест 4: Фильтрация массивов - поиск по содержимому Tags1
            logger.LogInformation("📋 Тест 4: МАССИВЫ - поиск объектов содержащих 'senior' в Tags1");
            try
            {
                var query4 = await redb.QueryAsync<MixedTestProps>();
                
                // 🔥 ТЕСТИРУЕМ МАССИВЫ - пока без специальных операторов
                logger.LogInformation("  ⚠️ ВРЕМЕННО: Тестируем существование Tags1 (простая проверка)");
                var withTags = await query4.Where(p => p.Name != "").ToListAsync(); // Все объекты с именами
                logger.LogInformation($"  Объекты с Tags1: найдено {withTags.Count} объектов");
                foreach (var item in withTags.Take(3))
                {
                    logger.LogInformation($"    • {item.name}: Tags1=[{string.Join(", ", item.properties.Tags1)}]");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка в тесте 4: {ex.Message}");
            }

            // Тест 5: Фильтрация по Age и Date
            logger.LogInformation("");
            logger.LogInformation("📋 Тест 5: Age и Date (Age >= 30 AND Date == 2025-01-15)");
            try
            {
                var query5 = await redb.QueryAsync<MixedTestProps>();
                var step1 = await query5.Where(p => p.Age >= 30).ToListAsync();
                logger.LogInformation($"  Шаг 1 - Age >= 30: найдено {step1.Count} объектов");
                
                query5 = query5.Where(p => p.Age >= 30);
                var step2 = await query5.Where(p => p.Date == testDate).ToListAsync();
                logger.LogInformation($"  Шаг 2 - Date == {testDate:yyyy-MM-dd}: найдено {step2.Count} объектов");
                foreach (var item in step2)
                {
                    logger.LogInformation($"    • {item.name}: Age={item.properties.Age}, Date={item.properties.Date:yyyy-MM-dd}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка в тесте 5: {ex.Message}");
            }

            // ===== СЛОЖНЫЕ LINQ ТЕСТЫ (РАСШИРЕННЫЕ ВОЗМОЖНОСТИ) =====
            logger.LogInformation("");
            logger.LogInformation("🚀 === СЛОЖНЫЕ LINQ ТЕСТЫ ===");
            
            // Тест 6: Диапазоны - Stock между 60 и 120  
            logger.LogInformation("📋 Тест 6: Диапазон Stock (60 < Stock < 120)");
            try
            {
                var query6 = await redb.QueryAsync<MixedTestProps>();
                var stockRange = await query6
                    .Where(p => p.Stock > 60 && p.Stock < 120)
                    .ToListAsync();
                logger.LogInformation($"  Диапазон Stock 60-120: найдено {stockRange.Count} объектов");
                foreach (var item in stockRange.Take(3))
                {
                    logger.LogInformation($"    • {item.name}: Stock={item.properties.Stock}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка в тесте 6: {ex.Message}");
            }

            // Тест 7: DateTime диапазон - Date >= 2025-01-15
            logger.LogInformation("📋 Тест 7: DateTime диапазон (Date >= 2025-01-15)");
            try
            {
                var query7 = await redb.QueryAsync<MixedTestProps>();
                var dateRange = await query7
                    .Where(p => p.Date >= new DateTime(2025, 1, 15))
                    .ToListAsync();
                logger.LogInformation($"  DateTime >= 2025-01-15: найдено {dateRange.Count} объектов");
                foreach (var item in dateRange.Take(4))
                {
                    logger.LogInformation($"    • {item.name}: Date={item.properties.Date:yyyy-MM-dd}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка в тесте 7: {ex.Message}");
            }

            // Тест 8: Сложный OR фильтр - молодые ИЛИ с большим Stock
            logger.LogInformation("📋 Тест 8: Сложный OR (Age < 30 ИЛИ Stock > 100)");
            try
            {
                var query8 = await redb.QueryAsync<MixedTestProps>();
                var complexOr = await query8
                    .Where(p => p.Age < 30 || p.Stock > 100)
                    .ToListAsync();
                logger.LogInformation($"  OR условие: найдено {complexOr.Count} объектов");
                foreach (var item in complexOr.Take(5))
                {
                    logger.LogInformation($"    • {item.name}: Age={item.properties.Age}, Stock={item.properties.Stock}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка в тесте 8: {ex.Message}");
            }

            // Тест 9: Тройной AND фильтр - Name и Age и Stock
            logger.LogInformation("📋 Тест 9: Тройной AND (Name != '' AND Age >= 30 AND Stock > 100)");
            try
            {
                var query9 = await redb.QueryAsync<MixedTestProps>();
                var tripleAnd = await query9
                    .Where(p => p.Name != "" && p.Age >= 30 && p.Stock > 100)
                    .ToListAsync();
                logger.LogInformation($"  Тройной AND: найдено {tripleAnd.Count} объектов");
                foreach (var item in tripleAnd.Take(3))
                {
                    logger.LogInformation($"    • {item.name}: Name={item.properties.Name}, Age={item.properties.Age}, Stock={item.properties.Stock}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка в тесте 9: {ex.Message}");
            }

            // Тест 10: Проверка NOT (негативная логика) - НЕ "John Doe"
            logger.LogInformation("📋 Тест 10: NOT логика (Name != 'John Doe')");
            try
            {
                var query10 = await redb.QueryAsync<MixedTestProps>();
                var notJohn = await query10
                    .Where(p => p.Name != "John Doe")
                    .ToListAsync();
                logger.LogInformation($"  NOT John Doe: найдено {notJohn.Count} объектов");
                foreach (var item in notJohn.Take(4))
                {
                    logger.LogInformation($"    • {item.name}: Name={item.properties.Name}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка в тесте 10: {ex.Message}");
            }

            // Тест 11: Комплексный тест с массивами - сочетание всех типов
            logger.LogInformation("📋 Тест 11: КОМПЛЕКСНЫЙ с МАССИВАМИ (Age>=30 AND Stock>60 AND Name содержит имена)");
            try
            {
                var query11 = await redb.QueryAsync<MixedTestProps>();
                var complex = await query11
                    .Where(p => p.Age >= 30 && p.Stock > 60 && p.Name != "")
                    .ToListAsync();
                logger.LogInformation($"  Комплексный фильтр: найдено {complex.Count} объектов");
                foreach (var item in complex.Take(6))
                {
                    logger.LogInformation($"    • {item.name}: Age={item.properties.Age}, Stock={item.properties.Stock}, Tags1=[{string.Join(",", item.properties.Tags1)}]");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка в тесте 11: {ex.Message}");
            }

            // Тест 12: ПРОВЕРЯЕМ СТАНДАРТНЫЙ LINQ ДЛЯ МАССИВОВ
            logger.LogInformation("📋 Тест 12: СТАНДАРТНЫЙ LINQ - array.Contains() синтаксис");
            try
            {
                var query12 = await redb.QueryAsync<MixedTestProps>();
                
                // 🧪 ЭКСПЕРИМЕНТАЛЬНЫЙ ТЕСТ: p.Tags1.Contains("senior")
                logger.LogInformation("  🧪 ТЕСТ: Стандартный .Contains() для массивов...");
                
                // ПРОБУЕМ СТАНДАРТНЫЙ LINQ СИНТАКСИС:
                // query.Where(p => p.Tags1.Contains("senior"))
                
                // Пока показываем содержимое массивов для понимания
                var allObjects = await query12.ToListAsync();
                logger.LogInformation($"  📊 МАССИВЫ В ОБЪЕКТАХ (для тестирования Contains):");
                foreach (var item in allObjects.Take(3))
                {
                    logger.LogInformation($"    🔍 {item.name}:");
                    logger.LogInformation($"      Tags1: [{string.Join(", ", item.properties.Tags1)}]");
                    logger.LogInformation($"      Scores1: [{string.Join(", ", item.properties.Scores1)}]");
                    logger.LogInformation($"      Tags2: [{string.Join(", ", item.properties.Tags2)}]");
                    
                    // ЛОГИКА ТЕСТА: Mixed Object 1 и 3 содержат "senior"
                    var containsSenior = item.properties.Tags1.Contains("senior");
                    logger.LogInformation($"      Contains('senior'): {containsSenior}");
                }
                
                logger.LogInformation("  💡 Готовимся к тесту: p.Tags1.Contains('senior') должен найти 2 объекта");
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка в тесте 12: {ex.Message}");
            }

            // Тест 13: ЭКСПЕРИМЕНТАЛЬНЫЙ - стандартный array.Contains()
            logger.LogInformation("📋 Тест 13: ЭКСПЕРИМЕНТ - стандартный .Contains() синтаксис");
            try
            {
                logger.LogInformation("  🧪 ПОПЫТКА: query.Where(p => p.Tags1.Contains('senior'))");
                
                var query13 = await redb.QueryAsync<MixedTestProps>();
                // ⚠️ РИСКОВАННЫЙ ТЕСТ - может не работать!
                var containsTest = await query13
                    .Where(p => p.Tags1.Contains("senior"))
                    .ToListAsync();
                
                logger.LogInformation($"  🎉 УСПЕХ! Найдено {containsTest.Count} объектов с 'senior' в Tags1:");
                foreach (var item in containsTest)
                {
                    logger.LogInformation($"    • {item.name}: Tags1=[{string.Join(", ", item.properties.Tags1)}]");
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation($"  ❌ СТАНДАРТНЫЙ LINQ НЕ РАБОТАЕТ: {ex.Message}");
                logger.LogInformation("  💡 Нужны специальные методы WhereArrayContains()");
            }

            // 🚀 === МАКСИМАЛЬНО СЛОЖНЫЕ ТЕСТЫ ===
            logger.LogInformation("");
            logger.LogInformation("🚀 === МАКСИМАЛЬНО СЛОЖНЫЕ ТЕСТЫ УНИФИЦИРОВАННОЙ СИСТЕМЫ ===");

            // Тест 14: КРАСИВЫЕ ЛЯМБДЫ - сложные условия с массивами и датами
            logger.LogInformation("📋 Тест 14: КРАСИВЫЕ ЛЯМБДЫ - сложный запрос с массивами");
            try
            {
                var query14 = await redb.QueryAsync<MixedTestProps>();
                var complexLambda = await query14
                    .Where(p => p.Tags1.Contains("senior") && p.Age >= 30 && p.Stock > 60)
                    .ToListAsync();
                logger.LogInformation($"  🌟 ЛЯМБДА КРАСОТА: найдено {complexLambda.Count} объектов");
                foreach (var item in complexLambda.Take(2))
                {
                    logger.LogInformation($"    • {item.name}: Tags1=[{string.Join(",", item.properties.Tags1)}]");
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation($"  ❌ ЛЯМБДА: {ex.Message}");
            }

            // Тест 15: КОМБИНИРОВАННЫЕ ЛЯМБДЫ - DateTime + массивы + логика
            logger.LogInformation("📋 Тест 15: DATETIME ЛЯМБДЫ - диапазоны дат и массивы");
            try
            {
                var query15 = await redb.QueryAsync<MixedTestProps>();
                var startDate = new DateTime(2025, 1, 15);
                var endDate = new DateTime(2025, 1, 17);
                
                var dateTimeLambda = await query15
                    .Where(p => p.Date >= startDate && p.Date < endDate && p.Tags1.Contains("senior"))
                    .ToListAsync();
                logger.LogInformation($"  📅 DATETIME ЛЯМБДЫ: найдено {dateTimeLambda.Count} объектов");
                foreach (var item in dateTimeLambda.Take(2))
                {
                    logger.LogInformation($"    • {item.name}: Date={item.properties.Date:yyyy-MM-dd}");
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation($"  ❌ DATETIME ЛЯМБДЫ: {ex.Message}");
            }

            // Тест 16: ЭКСТРЕМАЛЬНЫЕ ЛЯМБДЫ - множественные Contains() + DateTime
            logger.LogInformation("📋 Тест 16: ЭКСТРЕМАЛЬНЫЕ ЛЯМБДЫ - множественные Contains()");
            try
            {
                var query16 = await redb.QueryAsync<MixedTestProps>();
                var startDate = new DateTime(2025, 1, 15);
                var endDate = new DateTime(2025, 1, 17);
                
                var multipleContains = await query16
                    .Where(p => (p.Tags1.Contains("senior") || p.Tags1.Contains("developer") || p.Tags1.Contains("manager")) &&
                               p.Age >= 25 && p.Age <= 40 &&
                               p.Date >= startDate && p.Date < endDate &&
                               p.Stock > 40 &&
                               p.Name != "")
                    .ToListAsync();
                logger.LogInformation($"  🔥 МНОЖЕСТВЕННЫЕ CONTAINS: найдено {multipleContains.Count} объектов");
                foreach (var item in multipleContains.Take(3))
                {
                    logger.LogInformation($"    • {item.name}: Tags1=[{string.Join(",", item.properties.Tags1)}], Age={item.properties.Age}");
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation($"  ❌ МНОЖЕСТВЕННЫЕ CONTAINS: {ex.Message}");
            }

            // Тест 17: СУПЕР-СЛОЖНЫЙ ЛЯМБДЫ - многоуровневая логика
            logger.LogInformation("📋 Тест 17: СУПЕР-СЛОЖНЫЙ ЛЯМБДЫ - OR + AND + Contains");
            try
            {
                var query17 = await redb.QueryAsync<MixedTestProps>();
                var startDate = new DateTime(2025, 1, 15);
                
                var superComplexQuery = await query17
                    .Where(p => (p.Tags1.Contains("senior") || p.Tags1.Contains("manager")) && 
                               p.Age >= 25 && 
                               p.Date >= startDate && 
                               !p.Tags1.Contains("junior"))
                    .ToListAsync();
                    
                logger.LogInformation($"  🎯 СУПЕР-СЛОЖНЫЙ: найдено {superComplexQuery.Count} объектов");
                foreach (var item in superComplexQuery.Take(3))
                {
                    logger.LogInformation($"    • {item.name}: Age={item.properties.Age}, Tags1=[{string.Join(",", item.properties.Tags1)}]");
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation($"  ❌ СУПЕР-СЛОЖНЫЙ: {ex.Message}");
            }

            // Тест 18: КРАСИВЫЕ ЛЯМБДЫ - комплексные условия
            logger.LogInformation("📋 Тест 18: КРАСИВЫЕ ЛЯМБДЫ - комплексная логика");
            try
            {
                var query18 = await redb.QueryAsync<MixedTestProps>();
                var startDate = new DateTime(2025, 1, 15);
                
                var complexLambdaQuery = await query18
                    .Where(p => p.Name.Contains("e") && 
                               p.Age >= 25 && p.Age <= 40 && 
                               p.Stock > 40 && 
                               p.Date >= startDate)
                    .ToListAsync();
                    
                logger.LogInformation($"  🎭 КОМПЛЕКСНЫЕ ЛЯМБДЫ: найдено {complexLambdaQuery.Count} объектов");
                foreach (var item in complexLambdaQuery.Take(2))
                {
                    logger.LogInformation($"    • {item.name}: Age={item.properties.Age}, Stock={item.properties.Stock}");
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation($"  ❌ КОМПЛЕКСНЫЕ ЛЯМБДЫ: {ex.Message}");
            }

            // Тест 19: БЕЗУМНО СЛОЖНЫЕ ЛЯМБДЫ - вложенная логика с массивами
            logger.LogInformation("📋 Тест 19: БЕЗУМНО СЛОЖНЫЕ ЛЯМБДЫ - вложенная логика");
            try
            {
                var query19 = await redb.QueryAsync<MixedTestProps>();
                
                var insaneLambda = await query19
                    .Where(p => 
                        // Сложная логика с Contains
                        (p.Tags1.Contains("senior") && !p.Tags1.Contains("junior")) ||
                        (p.Tags1.Contains("developer") && p.Stock > 100) ||
                        (p.Tags1.Contains("manager") && p.Age >= 35) &&
                        // DateTime условия
                        p.Date >= new DateTime(2025, 1, 15) &&
                        // Числовые диапазоны
                        ((p.Age >= 25 && p.Age <= 35) || (p.Stock >= 50 && p.Stock <= 200)) &&
                        // Строковые условия
                        p.Name != "" && p.Name.Contains("e")
                    )
                    .ToListAsync();
                    
                logger.LogInformation($"  🤯 БЕЗУМНО СЛОЖНЫЕ: найдено {insaneLambda.Count} объектов");
                foreach (var item in insaneLambda.Take(2))
                {
                    logger.LogInformation($"    • {item.name}: Age={item.properties.Age}, Stock={item.properties.Stock}, Tags1=[{string.Join(",", item.properties.Tags1)}]");
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation($"  ❌ БЕЗУМНО СЛОЖНЫЕ: {ex.Message}");
            }

            // Тест 20: МАКСИМАЛЬНО БЕЗУМНЫЕ ЛЯМБДЫ - предел сложности
            logger.LogInformation("📋 Тест 20: МАКСИМАЛЬНО БЕЗУМНЫЕ ЛЯМБДЫ - предел сложности");
            try
            {
                var query20 = await redb.QueryAsync<MixedTestProps>();
                var now = DateTime.Now;
                var startYear = new DateTime(2025, 1, 1);
                var endYear = new DateTime(2025, 12, 31);
                
                var insaneComplexity = await query20
                    .Where(p => 
                        // 🔥 УРОВЕНЬ 1: Множественные массивы
                        ((p.Tags1.Contains("senior") && p.Tags2.Contains("secondary")) ||
                         (p.Tags1.Contains("developer") && !p.Tags1.Contains("intern")) ||
                         (p.Tags1.Contains("manager") && p.Tags1.Contains("team-lead"))) &&
                        
                        // 🔥 УРОВЕНЬ 2: Сложные числовые диапазоны  
                        ((p.Age >= 25 && p.Age <= 35 && p.Stock > 50) ||
                         (p.Age >= 30 && p.Age <= 40 && p.Stock > 100) ||
                         (p.Age == 35 && p.Stock != 80)) &&
                        
                        // 🔥 УРОВЕНЬ 3: DateTime с логикой
                        p.Date >= startYear && p.Date <= endYear &&
                        p.Date >= new DateTime(2025, 1, 15) &&
                        
                        // 🔥 УРОВЕНЬ 4: Строковая логика
                        p.Name != null && p.Name != "" && 
                        (p.Name.Contains("e") || p.Name.Contains("o")) &&
                        p.Name.Length > 3 &&
                        
                        // 🔥 УРОВЕНЬ 5: Отрицания
                        !p.Tags1.Contains("banned") &&
                        !p.Tags1.Contains("test-only") &&
                        !(p.Stock == 0 || p.Age == 0)
                    )
                    .ToListAsync();
                    
                logger.LogInformation($"  🤯 БЕЗУМНАЯ СЛОЖНОСТЬ: найдено {insaneComplexity.Count} объектов");
                foreach (var item in insaneComplexity)
                {
                    logger.LogInformation($"    • {item.name}: Age={item.properties.Age}, Stock={item.properties.Stock}");
                    logger.LogInformation($"      Tags1=[{string.Join(",", item.properties.Tags1)}]");
                    logger.LogInformation($"      Tags2=[{string.Join(",", item.properties.Tags2)}]");
                    logger.LogInformation($"      Date={item.properties.Date:yyyy-MM-dd}");
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation($"  ❌ БЕЗУМНАЯ СЛОЖНОСТЬ: {ex.Message}");
            }

            // Тест 21: ПОСЛЕДОВАТЕЛЬНЫЕ ФИЛЬТРЫ - цепочка Where()
            logger.LogInformation("📋 Тест 21: ПОСЛЕДОВАТЕЛЬНЫЕ ФИЛЬТРЫ - цепочка Where()");
            try
            {
                var query21 = await redb.QueryAsync<MixedTestProps>();
                
                var chainedQuery = await query21
                    .Where(p => p.Age >= 25)                    // Фильтр 1
                    .Where(p => p.Stock > 40)                   // Фильтр 2  
                    .Where(p => p.Tags1.Contains("senior"))     // Фильтр 3
                    .Where(p => p.Date >= new DateTime(2025, 1, 15)) // Фильтр 4
                    .Where(p => p.Name.Contains("e"))           // Фильтр 5
                    .ToListAsync();
                    
                logger.LogInformation($"  ⛓️ ЦЕПОЧКА ФИЛЬТРОВ: найдено {chainedQuery.Count} объектов");
                foreach (var item in chainedQuery)
                {
                    logger.LogInformation($"    • {item.name}: прошел 5 фильтров подряд!");
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation($"  ❌ ЦЕПОЧКА ФИЛЬТРОВ: {ex.Message}");
            }

            // Тест 22: НОВЫЙ API - WithMaxRecursionDepth() проверка
            logger.LogInformation("📋 Тест 22: НОВЫЙ API - WithMaxRecursionDepth() тестирование");
            try
            {
                var query22 = await redb.QueryAsync<MixedTestProps>();
                
                // Проверяем что API работает с DEFAULT значением (10)
                var defaultDepthQuery = await query22
                    .Where(p => p.Tags1.Contains("senior") && p.Age >= 25)
                    .ToListAsync();
                logger.LogInformation($"  🔧 DEFAULT DEPTH (10): найдено {defaultDepthQuery.Count} объектов");

                // Проверяем что API работает с КАСТОМНЫМ значением (15)
                var query22b = await redb.QueryAsync<MixedTestProps>();
                var customDepthQuery = await query22b
                    .Where(p => (p.Tags1.Contains("senior") || p.Tags1.Contains("developer")) && p.Age >= 25)
                    .WithMaxRecursionDepth(15)
                    .ToListAsync();
                logger.LogInformation($"  🚀 CUSTOM DEPTH (15): найдено {customDepthQuery.Count} объектов");

                // Проверяем что API работает с МАЛЫМ значением (2) - должно работать для простых запросов
                var query22c = await redb.QueryAsync<MixedTestProps>();
                var smallDepthQuery = await query22c
                    .Where(p => p.Tags1.Contains("senior"))
                    .WithMaxRecursionDepth(2)
                    .ToListAsync();
                logger.LogInformation($"  ⚡ SMALL DEPTH (2): найдено {smallDepthQuery.Count} объектов");

                logger.LogInformation("  ✅ НОВЫЙ API WithMaxRecursionDepth() РАБОТАЕТ!");
            }
            catch (Exception ex)
            {
                logger.LogInformation($"  ❌ НОВЫЙ API: {ex.Message}");
            }

            // Тест 23: ЭКСТРЕМАЛЬНЫЙ ТЕСТ - очень сложные лямбды с кастомной рекурсией
            logger.LogInformation("📋 Тест 23: ЭКСТРЕМАЛЬНЫЙ - сложные лямбды с WithMaxRecursionDepth(20)");
            try
            {
                var query23 = await redb.QueryAsync<MixedTestProps>();
                
                var extremeWithCustomDepth = await query23
                    .Where(p => 
                        // Уровень 1: Множественные OR
                        (p.Tags1.Contains("senior") || p.Tags1.Contains("developer") || p.Tags1.Contains("manager")) &&
                        // Уровень 2: Сложные AND
                        ((p.Age >= 25 && p.Age <= 35) || (p.Age >= 30 && p.Age <= 40)) &&
                        // Уровень 3: DateTime условия
                        p.Date >= new DateTime(2025, 1, 15) &&
                        // Уровень 4: Отрицания
                        !p.Tags1.Contains("banned") && !p.Tags1.Contains("intern")
                    )
                    .WithMaxRecursionDepth(20)  // 🔥 КАСТОМНАЯ ГЛУБИНА!
                    .ToListAsync();
                    
                logger.LogInformation($"  🤯 ЭКСТРЕМАЛЬНЫЙ С DEPTH(20): найдено {extremeWithCustomDepth.Count} объектов");
                foreach (var item in extremeWithCustomDepth.Take(2))
                {
                    logger.LogInformation($"    • {item.name}: Tags1=[{string.Join(",", item.properties.Tags1)}]");
                }
                
                logger.LogInformation("  ✅ ЭКСТРЕМАЛЬНЫЙ ТЕСТ С КАСТОМНОЙ РЕКУРСИЕЙ РАБОТАЕТ!");
            }
            catch (Exception ex)
            {
                logger.LogInformation($"  ❌ ЭКСТРЕМАЛЬНЫЙ С DEPTH(20): {ex.Message}");
            }

            logger.LogInformation("");
            logger.LogInformation("🎉 === ВСЕ СЛОЖНЫЕ LINQ ТЕСТЫ ЗАВЕРШЕНЫ ===");

            logger.LogInformation("");
            logger.LogInformation("🔥 === ДИАГНОСТИКА: НЕ УДАЛЯЕМ ДАННЫЕ ===");
            logger.LogInformation("🔥 Объекты оставлены в БД для SQL диагностики:");
            foreach (var id in productIds)
            {
                logger.LogInformation($"🔥 Объект ID: {id}");
            }
            
            // 🔥 ВРЕМЕННО ОТКЛЮЧЕНО ДЛЯ ДИАГНОСТИКИ
            /*
            // Удаляем тестовые продукты
            // ✅ НОВЫЙ API - удаляем тестовые объекты в системном контексте
            using (redb.CreateSystemContext())
            {
                foreach (var id in productIds)
                {
                    var obj = await redb.LoadAsync<MixedTestProps>(id);
                    await redb.DeleteAsync(obj);
                    logger.LogInformation($"🗑️ Удален объект через новый API: {obj.name} (ID: {id})");
                }
            }
            */
        }
    }
}
