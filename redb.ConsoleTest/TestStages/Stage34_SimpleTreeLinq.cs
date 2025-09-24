using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.Models.Entities;
using redb.ConsoleTest.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// 🌳 Этап 34: Упрощенное тестирование древовидных LINQ-запросов
    /// Проверяет базовые древовидные операторы без сложной логики
    /// </summary>
    public class Stage34_SimpleTreeLinq : BaseTestStage
    {
        public override string Name => "Упрощенное тестирование древовидных LINQ";
        public override string Description => "Базовые древовидные операторы: создание TreeQuery и простые фильтры";
        public override int Order => 34;

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🌳 === УПРОЩЕННЫЙ ТЕСТ ДРЕВОВИДНЫХ LINQ ===");
            logger.LogInformation("Проверяем базовые возможности TreeQuery API");

            // ===== ТЕСТ 1: СОЗДАНИЕ ДРЕВОВИДНОГО ЗАПРОСА =====
            logger.LogInformation("📋 Тест 1: Создание TreeQuery (базовая функциональность)");
            
            try
            {
                // Создаем древовидный запрос (синхронный)
                var treeQuery = redb.TreeQuery<CategoryTestProps>();
                logger.LogInformation("✅ TreeQuery<CategoryTestProps>() создан успешно");

                // Создаем древовидный запрос (асинхронный)
                var asyncTreeQuery = await redb.TreeQueryAsync<CategoryTestProps>();
                logger.LogInformation("✅ TreeQueryAsync<CategoryTestProps>() создан успешно");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Ошибка создания TreeQuery");
                throw;
            }

            // ===== ТЕСТ 2: СОЗДАНИЕ ПРОСТЫХ ТЕСТОВЫХ ДАННЫХ =====
            logger.LogInformation("📋 Тест 2: Создание упрощенных тестовых данных");
            
            try
            {
                // Создаем схему для категорий
                await redb.SyncSchemeAsync<CategoryTestProps>();
                logger.LogInformation("✅ Схема CategoryTestProps синхронизирована");

                // Создаем 2 простые категории
                var rootCategory = new RedbObject<CategoryTestProps>
                {
                    name = "TestRoot",
                    parent_id = null,
                    properties = new CategoryTestProps
                    {
                        Name = "Тестовая корневая категория",
                        IsActive = true
                    }
                };

                var rootId = await redb.SaveAsync(rootCategory);
                logger.LogInformation($"✅ Создана корневая категория: TestRoot (ID: {rootId})");

                var childCategory = new RedbObject<CategoryTestProps>
                {
                    name = "TestChild", 
                    parent_id = rootId,
                    properties = new CategoryTestProps
                    {
                        Name = "Дочерняя категория",
                        IsActive = true
                    }
                };

                var childId = await redb.SaveAsync(childCategory);
                logger.LogInformation($"✅ Создана дочерняя категория: TestChild (ID: {childId})");

                // ===== ТЕСТ 3: ПРОСТОЕ ТЕСТИРОВАНИЕ COUNT =====
                logger.LogInformation("📋 Тест 3: CountAsync() для древовидного запроса");
                
                var query = await redb.TreeQueryAsync<CategoryTestProps>();
                var totalCount = await query.CountAsync();
                logger.LogInformation($"✅ Всего категорий в TreeQuery: {totalCount}");

                // ===== ТЕСТ 4: ПРОСТОЕ ТЕСТИРОВАНИЕ TOLIST =====
                logger.LogInformation("📋 Тест 4: ToListAsync() для древовидного запроса");
                
                var allCategories = await query.ToListAsync();
                logger.LogInformation($"✅ Загружено {allCategories.Count} категорий через TreeQuery:");
                
                foreach (var cat in allCategories)
                {
                    logger.LogInformation($"  - {cat.name}: {cat.properties.Name} (parent: {cat.parent_id})");
                }

                // ===== ОЧИСТКА =====
                logger.LogInformation("🗑️ Очистка тестовых данных");
                await redb.DeleteAsync(childCategory);
                await redb.DeleteAsync(rootCategory);
                logger.LogInformation("✅ Тестовые данные очищены");

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Ошибка в упрощенном тесте древовидных LINQ");
                throw;
            }

            logger.LogInformation("✅ === УПРОЩЕННЫЙ ТЕСТ ЗАВЕРШЕН УСПЕШНО ===");
        }
    }
}
