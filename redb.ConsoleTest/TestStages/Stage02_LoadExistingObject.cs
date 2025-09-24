using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.Models.Entities;
using System;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Этап 2: Загрузка существующего объекта
    /// </summary>
    public class Stage02_LoadExistingObject : BaseTestStage
    {
        public override string Name => "Загрузка существующего объекта";
        public override string Description => "Создаем тестовый объект и загружаем его через контракты провайдера...";
        public override int Order => 2;

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            // 🚀 Сначала создаем тестовый объект для демонстрации LoadAsync
            logger.LogInformation("📦 Создаем тестовый объект AnalyticsRecordProps...");
            
            // ✅ Создаем схему явно вместо автоопределения
            var scheme = await redb.EnsureSchemeFromTypeAsync<AnalyticsRecordProps>();

            var testObj = new RedbObject<AnalyticsRecordProps>
            {
                scheme_id = 0,// scheme.Id, // Используем созданную схему
                name = "Тестовая запись для Stage02",
                note = "Создан в этапе 2 для демонстрации LoadAsync",
                properties = new AnalyticsRecordProps
                {
                    Article = "TEST-002",
                    Date = DateTime.Now,
                    Stock = 100,
                    Orders = 5,
                    Tag = "Stage02Test",
                    TestName = "LoadAsync Demo",
                    stringArr = new[] { "test1", "test2", "stage02" },
                    longArr = new[] { 1L, 2L, 3L }
                }
            };

            // Сохраняем объект через контракт провайдера
            var savedId = await redb.SaveAsync(testObj);
            logger.LogInformation("💾 Объект сохранен: ID={savedId}", savedId);

            // ✅ ДЕМОНСТРАЦИЯ КОНТРАКТОВ: Загружаем объект через базовый контракт (использует SecurityContext)
            logger.LogInformation("🔍 Загружаем объект через базовый контракт LoadAsync(long id)...");
            var loadedObj = await redb.LoadAsync<AnalyticsRecordProps>(savedId);
            
            logger.LogInformation("✅ Объект загружен через базовый API: id={id}, name='{name}', scheme_id={schemeId}", 
                loadedObj.id, loadedObj.name, loadedObj.scheme_id);
            logger.LogInformation("   Properties: Article='{Article}', Date={Date}, Stock={Stock}", 
                loadedObj.properties.Article, loadedObj.properties.Date, loadedObj.properties.Stock);
            logger.LogInformation("   🔐 Права проверены автоматически из SecurityContext");
            
            // Сохраняем ID для других этапов
            SetStageData("Stage02_CreatedObjectId", savedId);
        }
    }
}
