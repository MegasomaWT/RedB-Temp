using Microsoft.Extensions.Logging;
using redb.Core;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Этап 1: Подключение к базе данных
    /// </summary>
    public class Stage01_DatabaseConnection : BaseTestStage
    {
        public override string Name => "Подключение к базе данных";
        public override string Description => "Проверка подключения к PostgreSQL и получение информации о базе данных";
        public override int Order => 1;

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("DB Type: {dbType}", redb.dbType);
            logger.LogInformation("DB Version: {version}", redb.dbVersion);
            logger.LogInformation("DB Size: {size} bytes", redb.dbSize);
            
            await Task.CompletedTask; // Заглушка для async
        }
    }
}
