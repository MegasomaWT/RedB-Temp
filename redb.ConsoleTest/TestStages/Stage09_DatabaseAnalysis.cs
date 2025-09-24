using Microsoft.Extensions.Logging;
using redb.Core;
using redb.ConsoleTest.Utils;
using System;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// ЭТАП 9: Анализ данных в базе
    /// </summary>
    public class Stage09_DatabaseAnalysis : BaseTestStage
    {
        public override int Order => 9;
        public override string Name => "Анализ данных в базе";
        public override string Description => "Проверяем как данные сохранены в таблицах _objects и _values";

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🔍 === ЭТАП 9: АНАЛИЗ ДАННЫХ В БАЗЕ ===");

                            // Получаем ID обновленного объекта или используем тестовый объект 1021
            var updatedObjectId = GetStageData<long>("UpdatedObjectId");
            if (updatedObjectId == 0)
            {
                logger.LogInformation("⚠️ Не найден ID из предыдущих этапов, используем тестовый объект ID=1021");
                updatedObjectId = 1021;
            }

                logger.LogInformation("Проверяем как данные сохранены в таблицах _objects и _values...");
                await DatabaseAnalysisUtils.CheckObjectInDatabase(redb, updatedObjectId, logger);

                logger.LogInformation("");
                logger.LogInformation("📊 Анализ показывает:");
                logger.LogInformation("  → Базовые поля объекта хранятся в таблице _objects");
            logger.LogInformation("  → Каждое свойство из properties хранится как отдельная запись в _values");
            logger.LogInformation("  → Тип данных определяется через связь с _structures и _types");
            logger.LogInformation("  → MD5 хеш автоматически рассчитывается на основе properties");
        }
    }
}
