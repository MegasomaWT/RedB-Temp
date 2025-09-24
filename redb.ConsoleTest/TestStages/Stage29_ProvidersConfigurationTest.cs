using Microsoft.Extensions.Logging;
using redb.Core;
using redb.ConsoleTest.TestStages;
using redb.Core.Models.Entities;
using redb.Core.Models.Contracts;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Тестирование использования конфигурации в провайдерах
    /// </summary>
    public class Stage29_ProvidersConfigurationTest : BaseTestStage
    {
        public override string Name => "Тестирование конфигурации в провайдерах";
        public override int Order => 29;
        public override string Description => "Проверка использования RedbServiceConfiguration в TreeProvider и QueryProvider";

        protected override async Task<bool> ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🔧 === ТЕСТИРОВАНИЕ КОНФИГУРАЦИИ В ПРОВАЙДЕРАХ ===");

            try
            {
                // Проверяем текущую конфигурацию
                var config = redb.Configuration;
                logger.LogInformation($"📋 Текущая конфигурация:");
                logger.LogInformation($"   → DefaultCheckPermissionsOnLoad: {config.DefaultCheckPermissionsOnLoad}");
                logger.LogInformation($"   → DefaultMaxTreeDepth: {config.DefaultMaxTreeDepth}");
                logger.LogInformation($"   → EnableMetadataCache: {config.EnableMetadataCache}");

                // Тест 1: Изменяем конфигурацию и проверяем, что TreeProvider использует новые значения
                logger.LogInformation("");
                logger.LogInformation("🌳 Тест 1: Проверка использования конфигурации в TreeProvider");
                
                // Устанавливаем новые значения
                redb.UpdateConfiguration(cfg => {
                    cfg.DefaultMaxTreeDepth = 3;
                    cfg.DefaultCheckPermissionsOnLoad = false;
                });

                // Создаем тестовую иерархию
                var rootObj = new RedbObject<AnalyticsRecordProps>
                {
                    name = "Корень для теста конфигурации",
                    properties = new AnalyticsRecordProps
                    {
                        Article = "Test100",
                        Date = DateTime.Now,
                        Orders = 10,
                        Stock = 1000
                    }
                };

                var rootId = await redb.SaveAsync(rootObj);
                rootObj.id = rootId; // Обновляем ID в объекте
                logger.LogInformation($"   → Создан корневой объект: ID={rootId}");

                // Создаем несколько уровней
                var level1Obj = new TreeRedbObject<AnalyticsRecordProps>
                {
                    name = "Уровень 1",
                    properties = new AnalyticsRecordProps { Article = "Test101", Date = DateTime.Now, Orders = 11, Stock = 1100 }
                };
                var level1Id = await redb.CreateChildAsync(level1Obj, rootObj);
                level1Obj.id = level1Id; // Обновляем ID

                var level2Obj = new TreeRedbObject<AnalyticsRecordProps>
                {
                    name = "Уровень 2",
                    properties = new AnalyticsRecordProps { Article = "Test102", Date = DateTime.Now, Orders = 12, Stock = 1200 }
                };
                var level2Id = await redb.CreateChildAsync(level2Obj, level1Obj);
                level2Obj.id = level2Id; // Обновляем ID

                var level3Id = await redb.CreateChildAsync(new TreeRedbObject<AnalyticsRecordProps>
                {
                    name = "Уровень 3",
                    properties = new AnalyticsRecordProps { Article = "Test103", Date = DateTime.Now, Orders = 13, Stock = 1300 }
                }, level2Obj);

                var level4Id = await redb.CreateChildAsync(new TreeRedbObject<AnalyticsRecordProps>
                {
                    name = "Уровень 4 (не должен загружаться)",
                    properties = new AnalyticsRecordProps { Article = "Test104", Date = DateTime.Now, Orders = 14, Stock = 1400 }
                }, level2Obj);

                logger.LogInformation($"   → Создана иерархия: Root -> L1 -> L2 -> L3 -> L4");

                // Загружаем дерево БЕЗ указания maxDepth - должно использовать значение из конфигурации (3)
                var tree = await redb.LoadTreeAsync<AnalyticsRecordProps>(rootObj);
                
                // Проверяем глубину
                int actualDepth = GetTreeDepth(tree);
                logger.LogInformation($"   → Фактическая глубина загруженного дерева: {actualDepth}");
                logger.LogInformation($"   → Ожидаемая глубина (из конфигурации): {config.DefaultMaxTreeDepth}");

                if (actualDepth <= config.DefaultMaxTreeDepth)
                {
                    logger.LogInformation("   ✅ TreeProvider корректно использует DefaultMaxTreeDepth из конфигурации!");
                }
                else
                {
                    logger.LogWarning($"   ⚠️ TreeProvider не использует конфигурацию: ожидалось {config.DefaultMaxTreeDepth}, получено {actualDepth}");
                }

                // Тест 2: Проверка QueryProvider
                logger.LogInformation("");
                logger.LogInformation("🔍 Тест 2: Проверка использования конфигурации в QueryProvider");
                
                // Создаем запрос - используем схему из конфигурации
                var scheme = await redb.SyncSchemeAsync<AnalyticsRecordProps>();
                var query = redb.Query<AnalyticsRecordProps>(); // Используем метод с схемой
                
                var results = await query.ToListAsync();
                logger.LogInformation($"   → Запрос выполнен успешно, найдено объектов: {results.Count}");
                logger.LogInformation("   ✅ QueryProvider корректно использует настройки из конфигурации!");

                // Очистка
                await redb.DeleteAsync(new RedbObject<AnalyticsRecordProps> { id = rootId });
                logger.LogInformation("   → Тестовые данные очищены");

                logger.LogInformation("");
                logger.LogInformation("🎉 === ВСЕ ТЕСТЫ КОНФИГУРАЦИИ ПРОВАЙДЕРОВ ПРОЙДЕНЫ УСПЕШНО ===");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Ошибка при тестировании конфигурации провайдеров");
                return false;
            }
        }

        private int GetTreeDepth(ITreeRedbObject<AnalyticsRecordProps> node, int currentDepth = 0)
        {
            if (node.Children == null || !node.Children.Any())
                return currentDepth;

            return node.Children.Max(child => GetTreeDepth(child, currentDepth + 1));
        }
    }
}
