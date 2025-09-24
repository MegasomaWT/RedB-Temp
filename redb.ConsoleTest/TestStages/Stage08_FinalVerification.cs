using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.Postgres;
using redb.Core.Providers;
using System;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// ЭТАП 8: Финальная проверка
    /// </summary>
    public class Stage08_FinalVerification : BaseTestStage
    {
        public override int Order => 8;
        public override string Name => "Финальная проверка";
        public override string Description => "Загружаем финальное состояние объекта и проверяем результаты обновления";

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🎯 === ЭТАП 8: ФИНАЛЬНАЯ ПРОВЕРКА ===");

                // Получаем ID обновленного объекта
                var updatedObjectId = GetStageData<long>("UpdatedObjectId");
                if (updatedObjectId == 0)
                {
                    logger.LogError("❌ Не найден ID обновленного объекта из предыдущих этапов");
                    throw new InvalidOperationException("Не найден ID обновленного объекта из предыдущих этапов");
                }

                logger.LogInformation("Загружаем финальное состояние объекта {updatedId}...", updatedObjectId);
                var updatedObj = await ((IObjectStorageProvider)redb).LoadAsync<AnalyticsRecordProps>(updatedObjectId);
                logger.LogInformation("✅ Финальный объект: name='{name}', TestName='{testName}', Stock={stock}",
                    updatedObj.name, updatedObj.properties.TestName, updatedObj.properties.Stock);

                logger.LogInformation("Что изменилось в результате обновления:");
                logger.LogInformation("   В _objects: обновлены поля _name, _date_modify, _hash");
                logger.LogInformation("   В _values: обновлены значения Stock и TestName");
                logger.LogInformation("   MD5 хеш пересчитан автоматически на основе новых properties");

                // Проверяем корректность обновления
                if (updatedObj.name != "Обновленная запись")
                {
                    logger.LogError("❌ Некорректное значение name: ожидалось 'Обновленная запись', получено '{actual}'", updatedObj.name);
                    throw new InvalidOperationException($"Некорректное значение name: ожидалось 'Обновленная запись', получено '{updatedObj.name}'");
                }

                if (updatedObj.properties.TestName != "Console Test Update")
                {
                    logger.LogError("❌ Некорректное значение TestName: ожидалось 'Console Test Update', получено '{actual}'", updatedObj.properties.TestName);
                    throw new InvalidOperationException($"Некорректное значение TestName: ожидалось 'Console Test Update', получено '{updatedObj.properties.TestName}'");
                }

                if (updatedObj.properties.Stock != 150)
                {
                    logger.LogError("❌ Некорректное значение Stock: ожидалось 150, получено {actual}", updatedObj.properties.Stock);
                    throw new InvalidOperationException($"Некорректное значение Stock: ожидалось 150, получено {updatedObj.properties.Stock}");
                }

            // Сохраняем финальный объект для следующих этапов
            SetStageData("FinalObject", updatedObj);
        }
    }
}
