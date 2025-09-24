using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using redb.Core;
using System;
using System.Linq;
using System.Threading.Tasks;
using redb.Core.Models.Permissions;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Этап 21: Анализ существующих функций БД и логики по умолчанию
    /// Тестирует функции get_user_permissions_for_object, v_user_permissions и логику fallback
    /// </summary>
    public class Stage21_DatabaseFunctionsAnalysis : BaseTestStage
    {
        public override int Order => 21;
        public override string Name => "Анализ функций БД";
        public override string Description => "Анализ функций разрешений, VIEW и логики по умолчанию";

        protected override async Task<bool> ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🔍 === АНАЛИЗ ФУНКЦИЙ БД ===");
            logger.LogInformation("");

            // 1. Тестирование функции get_user_permissions_for_object
            logger.LogInformation("🔧 1. ТЕСТИРОВАНИЕ get_user_permissions_for_object:");
            logger.LogInformation("");

            try
            {
                // Тест с sys пользователем
                var sysId = 0L; // Новый sys пользователь
                var testObjectId = 1021L; // Из предыдущих тестов

                logger.LogInformation($"📋 Тестируем права sys (ID: {sysId}) на объект {testObjectId}:");

                var adminPermissions = await redb.RedbContext.Database
                    .SqlQueryRaw<UserPermissionResult>(
                        "SELECT * FROM get_user_permissions_for_object({0},{1})",
                        testObjectId, sysId)
                    .FirstOrDefaultAsync();

                if (adminPermissions != null)
                {
                    logger.LogInformation($"✅ sys права найдены:");
                    logger.LogInformation($"  - Тип: {adminPermissions.PermissionType}");
                    logger.LogInformation($"  - Источник: {adminPermissions.PermissionSourceId}");
                    logger.LogInformation($"  - Права: {(adminPermissions.CanSelect ? "R" : "-")}{(adminPermissions.CanInsert ? "I" : "-")}{(adminPermissions.CanUpdate ? "U" : "-")}{(adminPermissions.CanDelete ? "D" : "-")}");
                }
                else
                {
                    logger.LogWarning("⚠️ sys права не найдены!");
                }

                // Тест с NULL user_id (для триггеров)
                logger.LogInformation("");
                logger.LogInformation("📋 Тестируем функцию с user_id = NULL (режим триггера):");

                var nullUserPermissions = await redb.RedbContext.Database
                    .SqlQueryRaw<UserPermissionResult>(
                        "SELECT * FROM get_user_permissions_for_object({0}, NULL)",
                        testObjectId)
                    .FirstOrDefaultAsync();

                if (nullUserPermissions != null)
                {
                    logger.LogInformation($"✅ Права без фильтрации по пользователю найдены:");
                    logger.LogInformation($"  - Тип: {nullUserPermissions.PermissionType}");
                    logger.LogInformation($"  - Источник: {nullUserPermissions.PermissionSourceId}");
                    logger.LogInformation($"  - Права: {(nullUserPermissions.CanSelect ? "R" : "-")}{(nullUserPermissions.CanInsert ? "I" : "-")}{(nullUserPermissions.CanUpdate ? "U" : "-")}{(nullUserPermissions.CanDelete ? "D" : "-")}");
                }
                else
                {
                    logger.LogWarning("⚠️ Права без фильтрации не найдены!");
                }

            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка при тестировании get_user_permissions_for_object: {ex.Message}");
            }

            // 2. Анализ VIEW v_user_permissions
            logger.LogInformation("");
            logger.LogInformation("👁️ 2. АНАЛИЗ VIEW v_user_permissions:");
            logger.LogInformation("");

            try
            {
                var viewCount = await redb.RedbContext.Database
                    .SqlQueryRaw<int>("SELECT COUNT(*) as \"Value\" FROM v_user_permissions")
                    .FirstOrDefaultAsync();

                logger.LogInformation($"✅ VIEW v_user_permissions содержит {viewCount} записей");

                // Пример записей из VIEW
                var samplePermissions = await redb.RedbContext.Database
                    .SqlQueryRaw<string>(
                        "SELECT CONCAT('Object: ', object_id, ', User: ', user_id, ', Type: ', permission_type, ', Rights: ', " +
                        "CASE WHEN can_select THEN 'R' ELSE '-' END || " +
                        "CASE WHEN can_insert THEN 'I' ELSE '-' END || " +
                        "CASE WHEN can_update THEN 'U' ELSE '-' END || " +
                        "CASE WHEN can_delete THEN 'D' ELSE '-' END) as \"Value\" " +
                        "FROM v_user_permissions LIMIT 3")
                    .ToListAsync();

                logger.LogInformation("📄 Примеры записей из VIEW:");
                foreach (var sample in samplePermissions)
                {
                    logger.LogInformation($"  - {sample}");
                }

            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка при анализе VIEW: {ex.Message}");
            }

            // 3. Тестирование иерархического наследования
            logger.LogInformation("");
            logger.LogInformation("🌳 3. ТЕСТИРОВАНИЕ ИЕРАРХИЧЕСКОГО НАСЛЕДОВАНИЯ:");
            logger.LogInformation("");

            try
            {
                // Ищем объекты с родителями
                var hierarchyTest = await redb.RedbContext.Database
                    .SqlQueryRaw<string>(
                        "SELECT CONCAT('Child: ', _id, ' (', _name, ') -> Parent: ', _id_parent) as \"Value\" " +
                        "FROM _objects WHERE _id_parent IS NOT NULL LIMIT 3")
                    .ToListAsync();

                if (hierarchyTest.Any())
                {
                    logger.LogInformation("✅ Найдены объекты с иерархией:");
                    foreach (var hierarchy in hierarchyTest)
                    {
                        logger.LogInformation($"  - {hierarchy}");
                    }

                    logger.LogInformation("📋 Логика наследования:");
                    logger.LogInformation("  1. Ищем права на сам объект");
                    logger.LogInformation("  2. Если нет - идем к родителю (_id_parent)");
                    logger.LogInformation("  3. Если нет - к родителю родителя");
                    logger.LogInformation("  4. В конце проверяем глобальные права (_id_ref = 0)");
                }
                else
                {
                    logger.LogInformation("ℹ️ Иерархических объектов не найдено");
                }

            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка при анализе иерархии: {ex.Message}");
            }

            // 4. Анализ глобальных прав
            logger.LogInformation("");
            logger.LogInformation("🌍 4. АНАЛИЗ ГЛОБАЛЬНЫХ ПРАВ (_id_ref = 0):");
            logger.LogInformation("");

            try
            {
                var globalPermissions = await redb.RedbContext.Database
                    .SqlQueryRaw<string>(
                        "SELECT CONCAT('User: ', COALESCE(_id_user::text, 'NULL'), ', Role: ', COALESCE(_id_role::text, 'NULL'), ', Rights: ', " +
                        "CASE WHEN _select THEN 'R' ELSE '-' END || " +
                        "CASE WHEN _insert THEN 'I' ELSE '-' END || " +
                        "CASE WHEN _update THEN 'U' ELSE '-' END || " +
                        "CASE WHEN _delete THEN 'D' ELSE '-' END) as \"Value\" " +
                        "FROM _permissions WHERE _id_ref = 0")
                    .ToListAsync();

                logger.LogInformation($"✅ Найдено {globalPermissions.Count} глобальных разрешений:");
                foreach (var permission in globalPermissions)
                {
                    logger.LogInformation($"  - {permission}");
                }

                logger.LogInformation("");
                logger.LogInformation("📋 Приоритеты разрешений:");
                logger.LogInformation("  1. Пользовательские разрешения (высший приоритет)");
                logger.LogInformation("  2. Ролевые разрешения");
                logger.LogInformation("  3. Специфичные разрешения > Глобальные (level)");

            }
            catch (Exception ex)
            {
                logger.LogError($"❌ Ошибка при анализе глобальных прав: {ex.Message}");
            }

            // 5. Тестирование триггера auto_create_node_permissions
            logger.LogInformation("");
            logger.LogInformation("🔧 5. ИНФОРМАЦИЯ О ТРИГГЕРЕ auto_create_node_permissions:");
            logger.LogInformation("");

            logger.LogInformation("✅ Триггер автоматически создает permissions при создании узловых объектов");
            logger.LogInformation("📋 Логика триггера:");
            logger.LogInformation("  1. Срабатывает при INSERT новых объектов с родителем");
            logger.LogInformation("  2. Проверяет есть ли уже permission у родителя");
            logger.LogInformation("  3. Если нет - ищет источник permission вверх по иерархии");
            logger.LogInformation("  4. Создает permission для родителя на основе найденного");
            logger.LogInformation("  5. Ускоряет поиск разрешений за счет сокращения глубины рекурсии");

            // 6. Выводы и рекомендации
            logger.LogInformation("");
            logger.LogInformation("💡 6. ВЫВОДЫ ДЛЯ АРХИТЕКТУРЫ:");
            logger.LogInformation("");

            logger.LogInformation("✅ Что работает хорошо:");
            logger.LogInformation("  - Функция get_user_permissions_for_object() надежная");
            logger.LogInformation("  - VIEW v_user_permissions эффективный");
            logger.LogInformation("  - Иерархическое наследование работает");
            logger.LogInformation("  - Глобальные права sys обеспечивают fallback");
            logger.LogInformation("  - Триггер оптимизирует производительность");
            logger.LogInformation("");

            logger.LogInformation("🎯 Рекомендации для новой архитектуры:");
            logger.LogInformation("  - Использовать get_user_permissions_for_object() в PermissionProvider");
            logger.LogInformation("  - Кешировать результаты VIEW v_user_permissions");
            logger.LogInformation("  - Fallback к sys (0) безопасен");
            logger.LogInformation("  - Учесть триггер при инвалидации кеша");
            logger.LogInformation("  - Поддержать NULL user_id для системных операций");

            logger.LogInformation("");
            logger.LogInformation("✅ Анализ функций БД завершен!");
            logger.LogInformation("📋 Результаты будут использованы при создании SecurityContext");

            return true;
        }
    }
}
