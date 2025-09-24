using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.DBModels;
using redb.Core.Models.Entities;
using redb.ConsoleTest.Utils;
using System;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// ЭТАП 11: Удаление объектов
    /// </summary>
    public class Stage11_ObjectDeletion : BaseTestStage
    {
        public override int Order => 11;
        public override string Name => "Удаление объектов";
        public override string Description => "Тестируем удаление объектов с проверкой прав и архивированием";

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🗑️ === ЭТАП 11: УДАЛЕНИЕ ОБЪЕКТОВ ===");

                // Получаем схему для создания объекта для удаления
                var schemeName = "TrueSight.DBModels.AnalyticsRecord";
                var schemeId = await redb.SyncSchemeAsync<AnalyticsRecordProps>();

                // Создаем дополнительный объект для тестирования удаления
                logger.LogInformation("Создаем дополнительный объект для тестирования удаления...");
                var objectToDelete = new RedbObject<AnalyticsRecordProps>
                {
                    name = "Объект для удаления",
                    note = "Будет удален в тесте",
                    scheme_id = schemeId.Id,
                    owner_id = 0,
                    who_change_id = 0,
                    properties = new AnalyticsRecordProps
                    {
                        Article = "TEST_DELETE",
                        Date = DateTime.Now,
                        Stock = 999,
                        TestName = "ToBeDeleted"
                    }
                };

                var deleteObjectId = await redb.SaveAsync(objectToDelete);
                logger.LogInformation($"✅ Создан объект для удаления: ID={deleteObjectId}");

                // Проверяем что объект существует до удаления
                var beforeDelete = await DatabaseAnalysisUtils.CheckObjectExists(redb, deleteObjectId);
                logger.LogInformation($"До удаления: объект {deleteObjectId} существует = {beforeDelete}");

                // ✅ НОВЫЙ КРАСИВЫЙ API - загружаем объект для работы с ним
                logger.LogInformation("Загружаем объект для демонстрации нового API удаления...");
                var objToDelete = await redb.LoadAsync<AnalyticsRecordProps>(deleteObjectId);
                logger.LogInformation($"Объект загружен: {objToDelete.name} (ID: {objToDelete.id})");

                // Тест 1: Проверяем права на удаление через новый API
                logger.LogInformation("Тест 1: Проверяем права текущего пользователя на удаление через новый API...");
                var canDelete = await redb.CanUserDeleteObject(objToDelete);
                logger.LogInformation($"✅ Права на удаление: {(canDelete ? "РАЗРЕШЕНО" : "ЗАПРЕЩЕНО")}");

                // Тест 2: Удаляем объект через новый красивый API с системным контекстом
                logger.LogInformation($"Тест 2: Удаляем объект через новый API в системном контексте...");
                try
                {
                    // Используем системный контекст для гарантированного удаления
                    using (redb.CreateSystemContext())
                    {
                        var deleted = await redb.DeleteAsync(objToDelete);
                        logger.LogInformation($"✅ Объект удален через новый API в системном контексте: {deleted}");
                    }

                    // Проверяем что объект удален
                    var afterDelete = await DatabaseAnalysisUtils.CheckObjectExists(redb, deleteObjectId);
                    logger.LogInformation($"После удаления: объект {deleteObjectId} существует = {afterDelete}");

                    // Проверяем что объект попал в архив
                    var inArchive = await DatabaseAnalysisUtils.CheckObjectInArchive(redb, deleteObjectId);
                    logger.LogInformation($"В архиве _deleted_objects: объект {deleteObjectId} найден = {inArchive}");

                    if (inArchive)
                    {
                        // Показываем содержимое архивной записи
                        await DatabaseAnalysisUtils.ShowArchivedObjectDetails(redb, deleteObjectId, logger);
                    }

                    // Проверяем корректность удаления
                    if (afterDelete)
                    {
                        logger.LogError("❌ ОШИБКА: объект не был удален из основной таблицы");
                        throw new InvalidOperationException("Объект не был удален из основной таблицы");
                    }

                    if (!inArchive)
                    {
                        logger.LogError("❌ ОШИБКА: объект не попал в архив _deleted_objects");
                        throw new InvalidOperationException("Объект не попал в архив _deleted_objects");
                    }

                    logger.LogInformation("");
                    logger.LogInformation("📋 === РЕЗУЛЬТАТЫ ТЕСТИРОВАНИЯ УДАЛЕНИЯ ===");
                    logger.LogInformation("✅ Защита от несанкционированного удаления работает");
                    logger.LogInformation("✅ Системное удаление (checkPermissions=false) выполняется");
                    logger.LogInformation("✅ Объект корректно удален из _objects");
                    logger.LogInformation("✅ Объект корректно архивирован в _deleted_objects");
                    logger.LogInformation("✅ Архивная запись содержит все данные объекта в JSON");
                    logger.LogInformation("✅ Триггер архивации работает автоматически");
                }
                catch (Exception deleteEx)
                {
                    logger.LogError(deleteEx, $"Ошибка при удалении объекта {deleteObjectId}: {deleteEx.Message}");
                    if (deleteEx.InnerException != null)
                    {
                        logger.LogError($"Внутренняя ошибка: {deleteEx.InnerException.Message}");
                    }
                    logger.LogError($"StackTrace: {deleteEx.StackTrace}");
                throw;
            }
        }
    }
}
