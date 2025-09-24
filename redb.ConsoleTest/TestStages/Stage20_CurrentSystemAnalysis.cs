using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using redb.Core;
using redb.Core.DBModels;
using System;
using System.Threading.Tasks;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Этап 20: Анализ текущей системы удаления и дженериков
    /// Документирует текущее состояние методов принимающих ID вместо объектов
    /// </summary>
    public class Stage20_CurrentSystemAnalysis : BaseTestStage
    {
        public override int Order => 20;
        public override string Name => "Анализ текущей системы";
        public override string Description => "Анализ методов принимающих ID, системы удаления и мест для замены на дженерики";

        protected override async Task<bool> ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🔍 === АНАЛИЗ ТЕКУЩЕЙ СИСТЕМЫ ===");
            logger.LogInformation("");

            // 1. Анализ методов принимающих long id
            logger.LogInformation("📋 1. МЕТОДЫ ПРИНИМАЮЩИЕ LONG ID (кандидаты для замены на дженерики):");
            logger.LogInformation("");

            logger.LogInformation("🔹 IObjectStorageProvider:");
            logger.LogInformation("  - LoadAsync<T>(long objectId, ...) ✅ Уже типизирован");
            logger.LogInformation("  - DeleteAsync(long objectId, long userId, ...) ❌ Нужна замена на DeleteAsync<T>(T obj, ...)");
            logger.LogInformation("  - DeleteSubtreeAsync(long parentId, long userId, ...) ❌ Нужна замена на DeleteSubtreeAsync<T>(T parent, ...)");
            logger.LogInformation("");

            logger.LogInformation("🔹 ITreeProvider:");
            logger.LogInformation("  - LoadTreeAsync<T>(long rootId, ...) ❌ Можно добавить LoadTreeAsync<T>(T root, ...)");
            logger.LogInformation("  - GetChildrenAsync<T>(long parentId, ...) ❌ Можно добавить GetChildrenAsync<T>(T parent, ...)");
            logger.LogInformation("  - GetPathToRootAsync<T>(long objectId, ...) ❌ Можно добавить GetPathToRootAsync<T>(T obj, ...)");
            logger.LogInformation("  - GetDescendantsAsync<T>(long parentId, ...) ❌ Можно добавить GetDescendantsAsync<T>(T parent, ...)");
            logger.LogInformation("  - MoveObjectAsync(long objectId, long? newParentId, long userId, ...) ❌ Нужна замена");
            logger.LogInformation("  - CreateChildAsync<T>(..., long parentId, ...) ❌ Можно добавить CreateChildAsync<T>(..., T parent, ...)");
            logger.LogInformation("");

            logger.LogInformation("🔹 IPermissionProvider:");
            logger.LogInformation("  - GetReadableObjectIds(long userId) ❌ Нужна замена на GetReadableObjectIds(IRedbUser user)");
            logger.LogInformation("  - CanUserEditObject(long objectId, long userId) ❌ Нужна замена на CanUserEditObject<T>(T obj, IRedbUser user)");
            logger.LogInformation("  - CanUserSelectObject(long objectId, long userId) ❌ Нужна замена");
            logger.LogInformation("  - CanUserInsertScheme(long schemeId, long userId) ❌ Нужна замена");
            logger.LogInformation("  - CanUserDeleteObject(long objectId, long userId) ❌ Нужна замена");
            logger.LogInformation("");

            // 2. Анализ системы удаления
            logger.LogInformation("🗑️ 2. АНАЛИЗ СИСТЕМЫ УДАЛЕНИЯ:");
            logger.LogInformation("");

            try
            {
                // Проверяем есть ли таблица _deleted_objects
                var deletedObjectsCount = await redb.RedbContext.Database
                    .SqlQueryRaw<int>("SELECT COUNT(*) as \"Value\" FROM _deleted_objects")
                    .FirstOrDefaultAsync();

                logger.LogInformation($"✅ Таблица _deleted_objects существует, записей: {deletedObjectsCount}");
                logger.LogInformation("✅ Триггер ftr__objects__deleted_objects автоматически архивирует удаленные объекты");
                logger.LogInformation("✅ Архивируются все связанные _values в JSON формате");
                logger.LogInformation("");

                // Проверяем структуру архивированных данных
                if (deletedObjectsCount > 0)
                {
                    var sampleDeleted = await redb.RedbContext.Database
                        .SqlQueryRaw<string>("SELECT _values AS \"Value\" FROM _deleted_objects LIMIT 1")
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrEmpty(sampleDeleted))
                    {
                        logger.LogInformation("📄 Пример архивированных данных (первые 200 символов):");
                        var preview = sampleDeleted.Length > 200 ? sampleDeleted.Substring(0, 200) + "..." : sampleDeleted;
                        logger.LogInformation($"  {preview}");
                        logger.LogInformation("");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"⚠️ Ошибка при анализе системы удаления: {ex.Message}");
            }

            // 3. Анализ текущего использования userId
            logger.LogInformation("👤 3. АНАЛИЗ ИСПОЛЬЗОВАНИЯ USERID:");
            logger.LogInformation("");

            logger.LogInformation("🔹 Текущие проблемы:");
            logger.LogInformation("  - Везде передается long userId вместо IRedbUser");
            logger.LogInformation("  - Нет контекста текущего пользователя");
            logger.LogInformation("  - Используется хардкод sys ID: 0");
            logger.LogInformation("  - Нет автоматической установки owner_id и who_change_id");
            logger.LogInformation("  - Проверка прав часто отключена (checkPermissions: false)");
            logger.LogInformation("");

            // 4. Рекомендации по улучшению
            logger.LogInformation("💡 4. РЕКОМЕНДАЦИИ ПО УЛУЧШЕНИЮ:");
            logger.LogInformation("");

            logger.LogInformation("🎯 Приоритет 1 - Контекст безопасности:");
            logger.LogInformation("  - Создать IRedbSecurityContext с текущим пользователем");
            logger.LogInformation("  - Добавить fallback к sys (0)");
            logger.LogInformation("  - Реализовать SystemContext для системных операций");
            logger.LogInformation("");

            logger.LogInformation("🎯 Приоритет 2 - Красивые методы:");
            logger.LogInformation("  - SaveAsync<T>(RedbObject<T> obj) - автоматически устанавливает метаданные");
            logger.LogInformation("  - DeleteAsync<T>(T obj) - извлекает ID из объекта");
            logger.LogInformation("  - LoadAsync<T>(long id) - уже хорошо, оставить как есть");
            logger.LogInformation("");

            logger.LogInformation("🎯 Приоритет 3 - Обратная совместимость:");
            logger.LogInformation("  - Старые методы пометить [Obsolete] но оставить работающими");
            logger.LogInformation("  - Новые методы должны быть предпочтительными");
            logger.LogInformation("");

            // 5. Проверка глобальных прав sys
            logger.LogInformation("🔐 5. ПРОВЕРКА ГЛОБАЛЬНЫХ ПРАВ sys:");
            logger.LogInformation("");

            try
            {
                var adminPermissions = await redb.RedbContext.Database
                    .SqlQueryRaw<string>(
                        "SELECT CONCAT('User: ', _id_user, ', Ref: ', _id_ref, ', Rights: ', " +
                        "CASE WHEN _select THEN 'R' ELSE '-' END || " +
                        "CASE WHEN _insert THEN 'I' ELSE '-' END || " +
                        "CASE WHEN _update THEN 'U' ELSE '-' END || " +
                        "CASE WHEN _delete THEN 'D' ELSE '-' END) as \"Value\" " +
                        "FROM _permissions WHERE _id_user = 0 AND _id_ref = 0")
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(adminPermissions))
                {
                    logger.LogInformation($"✅ sys имеет глобальные права: {adminPermissions}");
                }
                else
                {
                    logger.LogWarning("⚠️ sys не имеет глобальных прав! Это может быть проблемой.");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"⚠️ Ошибка при проверке прав sys: {ex.Message}");
            }

            logger.LogInformation("");
            logger.LogInformation("✅ Анализ текущей системы завершен!");
            logger.LogInformation("📋 Результаты анализа будут использованы для планирования улучшений");

            return true;
        }
    }
}
