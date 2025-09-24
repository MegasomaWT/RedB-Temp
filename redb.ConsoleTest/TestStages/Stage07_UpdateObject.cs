using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.Models.Entities;
using System;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// ЭТАП 7: Обновление объекта
    /// </summary>
    public class Stage07_UpdateObject : BaseTestStage
    {
        public override int Order => 7;
        public override string Name => "Обновление объекта";
        public override string Description => "Изменяем поля объекта и демонстрируем UPDATE операции";

        public long UpdatedObjectId { get; private set; }

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("✏️ === ЭТАП 7: ОБНОВЛЕНИЕ ОБЪЕКТА ===");

                // Получаем объект из предыдущего этапа
                var createdObj = GetStageData<RedbObject<AnalyticsRecordProps>>("CreatedObject");
                if (createdObj == null)
                {
                    logger.LogError("❌ Не найден созданный объект из предыдущих этапов");
                    throw new InvalidOperationException("Не найден созданный объект из предыдущих этапов");
                }

                logger.LogInformation("Объект ДО изменений:");
                logger.LogInformation("   Name: '{oldName}' → TestName: '{oldTestName}' → Stock: {oldStock}",
                    createdObj.name, createdObj.properties.TestName, createdObj.properties.Stock);

                logger.LogInformation("Применяем изменения:");
                var oldName = createdObj.name;
                var oldTestName = createdObj.properties.TestName;
                var oldStock = createdObj.properties.Stock;

                createdObj.name = "Обновленная запись";
                createdObj.properties.TestName = "Console Test Update";
                createdObj.properties.Stock = 150;
                createdObj.date_modify = DateTime.Now;

                logger.LogInformation("   Name: '{oldName}' → '{newName}'", oldName, createdObj.name);
                logger.LogInformation("   TestName: '{oldTestName}' → '{newTestName}'", oldTestName, createdObj.properties.TestName);
                logger.LogInformation("   Stock: {oldStock} → {newStock}", oldStock, createdObj.properties.Stock);
                logger.LogInformation("   date_modify: обновлено до текущего времени");

                logger.LogInformation("Сохраняем изменения (UPDATE в _objects и _values)...");
                UpdatedObjectId = await redb.SaveAsync(createdObj);
                logger.LogInformation("✅ Объект обновлен, ID: {updatedId}", UpdatedObjectId);

            // Сохраняем обновленный объект для следующих этапов
            SetStageData("UpdatedObject", createdObj);
            SetStageData("UpdatedObjectId", UpdatedObjectId);
        }
    }
}
