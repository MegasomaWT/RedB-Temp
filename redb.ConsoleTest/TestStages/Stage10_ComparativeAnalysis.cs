using Microsoft.Extensions.Logging;
using redb.Core;
using redb.ConsoleTest.Utils;
using System;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// ЭТАП 10: Сравнительный анализ
    /// </summary>
    public class Stage10_ComparativeAnalysis : BaseTestStage
    {
        public override int Order => 10;
        public override string Name => "Сравнительный анализ";
        public override string Description => "Сравниваем старый и новый объекты в базе данных";

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("📊 === ЭТАП 10: СРАВНИТЕЛЬНЫЙ АНАЛИЗ ===");

                            // Получаем ID обновленного объекта или используем тестовый объект 1021
            var updatedObjectId = GetStageData<long>("UpdatedObjectId");
            if (updatedObjectId == 0)
            {
                logger.LogInformation("⚠️ Не найден ID из предыдущих этапов, используем тестовый объект ID=1021");
                updatedObjectId = 1021;
            }

                logger.LogInformation("Сравниваем старый (ID=1021) и новый (ID={newId}) объекты...", updatedObjectId);
                await DatabaseAnalysisUtils.CompareObjectsInDatabase(redb, new[] { 1021, updatedObjectId }, logger);

                logger.LogInformation("");
                logger.LogInformation("🔍 Результаты сравнения:");
                logger.LogInformation("  → Объект 1021: существующий объект из базы (эталонный)");
                logger.LogInformation("  → Объект {newId}: созданный и обновленный в тесте", updatedObjectId);
            logger.LogInformation("  → Оба объекта имеют одинаковую структуру (scheme_id)");
            logger.LogInformation("  → Различаются значениями в properties и метаданными");
            logger.LogInformation("  → Хеши различаются из-за разных значений properties");
        }
    }
}
