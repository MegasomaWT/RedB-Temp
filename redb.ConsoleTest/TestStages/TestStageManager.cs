using Microsoft.Extensions.Logging;
using redb.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Менеджер для управления этапами тестирования
    /// </summary>
    public class TestStageManager
    {
        private readonly List<ITestStage> _stages;

        public TestStageManager()
        {
            _stages = new List<ITestStage>
            {
                new Stage01_DatabaseConnection(),
                new Stage02_LoadExistingObject(),
                new Stage03_SchemaSync(),
                new Stage04_PermissionChecks(),
                new Stage05_CreateObject(),
                new Stage06_VerifyCreatedObject(),
                new Stage07_UpdateObject(),
                new Stage08_FinalVerification(),
                new Stage09_DatabaseAnalysis(),
                new Stage10_ComparativeAnalysis(),
                new Stage11_ObjectDeletion(),
                new Stage12_TreeFunctionality(),
                new Stage13_LinqQueries(),
                new Stage16_AdvancedLinq(),
                new Stage17_AdvancedLinqOperators(),
                new Stage18_SortingAndPagination(),
                new Stage19_DateTimeSorting(),
                
                // === ЭТАПЫ АРХИТЕКТУРЫ БЕЗОПАСНОСТИ ===
                new Stage20_CurrentSystemAnalysis(),
                new Stage21_DatabaseFunctionsAnalysis(),
                new Stage22_BasicInterfacesTest(),
                new Stage23_PermissionModelsTest(),
                new Stage24_AdvancedSecurityContext(),
                new Stage25_PolymorphicAPI(),
                
                // === ЭТАПЫ ПРОВАЙДЕРОВ ===
                new Stage26_UserProviderInterfacesTest(),
                new Stage27_ProvidersIntegrationTest(),
                
                // === ЭТАПЫ СИСТЕМЫ КОНФИГУРАЦИИ ===
                new Stage28_ConfigurationSystemTest(),
                new Stage29_ProvidersConfigurationTest(),
                
                // === ЭТАПЫ УЛУЧШЕНИЙ БЕЗОПАСНОСТИ ===
                new Stage30_TreeSecurityTests(),
                
                // === ЭТАПЫ РЕАЛИЗАЦИИ ПРОВАЙДЕРОВ ===
                new Stage31_UserProviderImplementation(),
                
                // === ЭТАПЫ ТЕСТИРОВАНИЯ ФАБРИКИ ===
                new Stage32_RedbObjectFactoryTest(),
                
                // === ЭТАПЫ ДРЕВОВИДНЫХ LINQ-ЗАПРОСОВ ===
                new Stage33_TreeLinqQueries(),
                new Stage34_SimpleTreeLinq(),
                
                // === ЭТАПЫ АТРИБУТОВ И АННОТАЦИЙ ===
                new Stage35_JsonIgnoreTest(),
                
                // === ЭТАПЫ ПОЛИМОРФНОГО API ===
                new Stage36_AdvancedPolymorphicAPI(),
                
                // === ЭТАПЫ НОВЫХ ВОЗМОЖНОСТЕЙ ===
                new Stage40_ChangeTrackingTest(),
                
                // === ЭТАПЫ НОВОЙ ПАРАДИГМЫ LINQ ===
                new Stage41_NewParadigmLINQTest(),
                
                // === ЭТАПЫ ТЕСТИРОВАНИЯ ПРОИЗВОДИТЕЛЬНОСТИ ===
                new Stage42_BulkInsertPerformanceTest(),
                new Stage43_BulkInsertComplexObjects(),
                
                // === ЭТАПЫ НОВОГО SaveAsync ===
                new Stage44_TestNewSaveAsync(),
                
                // === ФИНАЛЬНАЯ ДЕМОНСТРАЦИЯ УСПЕХА ===
                new Stage45_FinalSuccess(),
                
                // === ЭТАПЫ СПЕЦИАЛЬНЫХ ТЕСТОВ ===
                new Stage46_DateTimeFilteringTest()
                
                // === ЭТАПЫ УТИЛИТАРНЫХ МЕТОДОВ (ПРИМЕРЫ) ===
                // new Stage37_ResetIdsTest()  // Пример тестирования ResetIds (раскомментируйте при необходимости)
                // Добавляйте новые этапы здесь
            };
        }

        /// <summary>
        /// Получить все доступные этапы
        /// </summary>
        public IEnumerable<ITestStage> GetAllStages()
        {
            return _stages.OrderBy(s => s.Order);
        }

        /// <summary>
        /// Получить этап по номеру
        /// </summary>
        public ITestStage? GetStageByNumber(int stageNumber)
        {
            return _stages.FirstOrDefault(s => s.Order == stageNumber);
        }

        /// <summary>
        /// Получить этап по имени (частичное совпадение)
        /// </summary>
        public ITestStage? GetStageByName(string stageName)
        {
            return _stages.FirstOrDefault(s => 
                s.Name.Contains(stageName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Выполнить все этапы последовательно
        /// </summary>
        public async Task ExecuteAllStagesAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🚀 === ЗАПУСК ВСЕХ ЭТАПОВ ТЕСТИРОВАНИЯ ===");
            logger.LogInformation("Всего этапов: {count}", _stages.Count);
            
            var successCount = 0;
            var failedStages = new List<(ITestStage stage, Exception error)>();

            foreach (var stage in GetAllStages())
            {
                try
                {
                    await stage.ExecuteAsync(logger, redb);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failedStages.Add((stage, ex));
                    logger.LogError("❌ Этап {Order} ({Name}) завершился с ошибкой", stage.Order, stage.Name);
                }
            }

            logger.LogInformation("");
            logger.LogInformation("📊 === ИТОГИ ТЕСТИРОВАНИЯ ===");
            logger.LogInformation("✅ Успешно выполнено: {success}/{total} этапов", successCount, _stages.Count);
            
            if (failedStages.Any())
            {
                logger.LogInformation("❌ Завершились с ошибкой: {failed} этапов", failedStages.Count);
                foreach (var (stage, error) in failedStages)
                {
                    logger.LogError("  - Этап {Order}: {Name} - {Error}", stage.Order, stage.Name, error.Message);
                }
            }
            else
            {
                logger.LogInformation("🎉 === ВСЕ ЭТАПЫ ЗАВЕРШЕНЫ УСПЕШНО ===");
            }
        }

        /// <summary>
        /// Выполнить конкретные этапы
        /// </summary>
        public async Task ExecuteStagesAsync(ILogger logger, IRedbService redb, params int[] stageNumbers)
        {
            logger.LogInformation("🎯 === ЗАПУСК ВЫБРАННЫХ ЭТАПОВ ===");
            logger.LogInformation("Этапы к выполнению: [{stages}]", string.Join(", ", stageNumbers));

            var successCount = 0;
            var failedStages = new List<(int number, Exception error)>();

            foreach (var stageNumber in stageNumbers)
            {
                var stage = GetStageByNumber(stageNumber);
                if (stage == null)
                {
                    logger.LogWarning("⚠️ Этап {stageNumber} не найден", stageNumber);
                    continue;
                }

                try
                {
                    await stage.ExecuteAsync(logger, redb);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failedStages.Add((stageNumber, ex));
                    logger.LogError("❌ Этап {stageNumber} завершился с ошибкой", stageNumber);
                }
            }

            logger.LogInformation("");
            logger.LogInformation("📊 === ИТОГИ ВЫПОЛНЕНИЯ ===");
            logger.LogInformation("✅ Успешно выполнено: {success}/{total} этапов", successCount, stageNumbers.Length);
            
            if (failedStages.Any())
            {
                logger.LogInformation("❌ Завершились с ошибкой: {failed} этапов", failedStages.Count);
                foreach (var (number, error) in failedStages)
                {
                    logger.LogError("  - Этап {number}: {Error}", number, error.Message);
                }
            }
        }

        /// <summary>
        /// Показать список доступных этапов
        /// </summary>
        public void ShowAvailableStages(ILogger logger)
        {
            logger.LogInformation("📋 === ДОСТУПНЫЕ ЭТАПЫ ТЕСТИРОВАНИЯ ===");
            
            foreach (var stage in GetAllStages())
            {
                logger.LogInformation("  {Order:D2}. {Name}", stage.Order, stage.Name);
                if (!string.IsNullOrEmpty(stage.Description))
                {
                    logger.LogInformation("      {Description}", stage.Description);
                }
            }
            
            logger.LogInformation("");
            logger.LogInformation("💡 Использование:");
            logger.LogInformation("  dotnet run                    - выполнить все этапы");
            logger.LogInformation("  dotnet run --stages 1,3,13    - выполнить этапы 1, 3 и 13");
            logger.LogInformation("  dotnet run --list             - показать список этапов");
            logger.LogInformation("  dotnet run --help             - показать справку");
        }
    }
}
