using Microsoft.Extensions.Logging;
using redb.Core;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Этап 3: Code-First синхронизация схемы
    /// </summary>
    public class Stage03_SchemaSync : BaseTestStage
    {
        public override string Name => "Code-First синхронизация схемы";
        public override string Description => "Проверяем/создаем схему и синхронизируем структуры";
        public override int Order => 3;

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("Проверяем/создаем схему и синхронизируем структуры: {scheme} (автоматически по имени класса)", 
                nameof(AnalyticsRecordProps));
            
            var schemeId = await redb.SyncSchemeAsync<AnalyticsRecordProps>();
            logger.LogInformation("✅ Scheme ID: {schemeId}, структуры синхронизированы", schemeId);
        }
    }
}
