using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.Postgres;
using System;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// ЭТАП 4: Демонстрация опциональных проверок прав
    /// </summary>
    public class Stage04_PermissionChecks : BaseTestStage
    {
        public override int Order => 4;
        public override string Name => "Демонстрация опциональных проверок прав";
        public override string Description => "Проверяем права пользователя на редактирование объекта и демонстрируем опциональную проверку прав при операциях";

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🔐 === ЭТАП 4: ДЕМОНСТРАЦИЯ ОПЦИОНАЛЬНЫХ ПРОВЕРОК ПРАВ ===");
                
                const long testObjectId = 1021;

                // ✅ НОВЫЙ КРАСИВЫЙ API - работаем с объектами, а не с ID
                logger.LogInformation("Загружаем объект {objectId} для проверки прав...", testObjectId);
                var testObj = await redb.LoadAsync<AnalyticsRecordProps>(testObjectId);
                
                logger.LogInformation("Проверяем права текущего пользователя на редактирование объекта...");
                var canEdit = await redb.CanUserEditObject(testObj);
                logger.LogInformation("✅ Результат проверки прав через новый API: {canEdit}", canEdit ? "РАЗРЕШЕНО" : "ЗАПРЕЩЕНО");

                logger.LogInformation("");
                logger.LogInformation("📋 Демонстрируем опциональную проверку прав при операциях:");

                // Загрузка БЕЗ проверки прав (по умолчанию)
                logger.LogInformation("  → LoadAsync БЕЗ проверки прав (по умолчанию checkPermissions=false)");
                var objWithoutCheck = await redb.LoadAsync<AnalyticsRecordProps>(testObjectId);
                logger.LogInformation($"    ✅ Загружен: {objWithoutCheck.name}");

                // Загрузка С проверкой прав
                logger.LogInformation("  → LoadAsync С проверкой прав (checkPermissions=true)");
                try
                {
                    var objWithCheck = await redb.LoadAsync<AnalyticsRecordProps>(testObjectId);
                    logger.LogInformation($"    ✅ Загружен с проверкой прав: {objWithCheck.name}");
                }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogInformation($"    ❌ Доступ запрещен: {ex.Message}");
            }
        }
    }
}
