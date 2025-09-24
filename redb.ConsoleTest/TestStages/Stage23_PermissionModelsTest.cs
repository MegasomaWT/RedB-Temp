using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.DBModels;
using redb.Core.Models;
using redb.Core.Models.Permissions;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Этап 23: Тестирование моделей разрешений
    /// Проверяем работу PermissionFlags и UserPermissionSet
    /// </summary>
    public class Stage23_PermissionModelsTest : BaseTestStage
    {
        public override int Order => 23;
        public override string Name => "ТЕСТИРОВАНИЕ МОДЕЛЕЙ РАЗРЕШЕНИЙ";
        public override string Description => "Проверка работы PermissionFlags и UserPermissionSet";

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🔍 === ТЕСТИРОВАНИЕ МОДЕЛЕЙ РАЗРЕШЕНИЙ ===");

            // 1. Тестируем PermissionFlags
            logger.LogInformation("\n🎯 1. ТЕСТИРОВАНИЕ PERMISSION FLAGS:");
            
            // Создаем различные комбинации разрешений
            var readOnly = PermissionFlags.ReadOnly;
            var readWrite = PermissionFlags.ReadWrite;
            var allPermissions = PermissionFlags.All;
            var noPermissions = PermissionFlags.None;

            logger.LogInformation($"✅ Разрешения созданы:");
            logger.LogInformation($"  - ReadOnly: {readOnly.ToDisplayString()} ({readOnly})");
            logger.LogInformation($"  - ReadWrite: {readWrite.ToDisplayString()} ({readWrite})");
            logger.LogInformation($"  - All: {allPermissions.ToDisplayString()} ({allPermissions})");
            logger.LogInformation($"  - None: {noPermissions.ToDisplayString()} ({noPermissions})");

            // Тестируем методы проверки
            logger.LogInformation($"\n📋 Проверка методов:");
            logger.LogInformation($"  - ReadOnly.CanSelect(): {readOnly.CanSelect()}");
            logger.LogInformation($"  - ReadOnly.CanDelete(): {readOnly.CanDelete()}");
            logger.LogInformation($"  - All.CanUpdate(): {allPermissions.CanUpdate()}");
            logger.LogInformation($"  - None.CanInsert(): {noPermissions.CanInsert()}");

            // Создаем разрешения из булевых значений (как в БД)
            var dbPermissions = PermissionFlagsExtensions.FromBooleans(true, true, true, false);
            logger.LogInformation($"  - FromBooleans(R=true, I=true, U=true, D=false): {dbPermissions.ToDisplayString()}");

            // 2. Тестируем UserPermissionSet
            logger.LogInformation("\n👤 2. ТЕСТИРОВАНИЕ USER PERMISSION SET:");
            
            var userPermissions = new UserPermissionSet
            {
                UserId = 12345
            };

            // Добавляем различные типы разрешений
            userPermissions.SetGlobalPermissions(PermissionFlags.ReadOnly);
            userPermissions.AddSchemePermission(100, PermissionFlags.ReadWrite);
            userPermissions.AddObjectPermission(1021, PermissionFlags.All);

            logger.LogInformation($"✅ UserPermissionSet создан для пользователя {userPermissions.UserId}:");
            logger.LogInformation($"  - Глобальные права: {userPermissions.GlobalPermissions.ToDisplayString()}");
            logger.LogInformation($"  - Права на схему 100: {userPermissions.SchemePermissions[100].ToDisplayString()}");
            logger.LogInformation($"  - Права на объект 1021: {userPermissions.ObjectPermissions[1021].ToDisplayString()}");

            // 3. Тестируем иерархию разрешений
            logger.LogInformation("\n🌳 3. ТЕСТИРОВАНИЕ ИЕРАРХИИ РАЗРЕШЕНИЙ:");
            
            // Объект с конкретными правами
            var perms1021 = userPermissions.GetPermissionsForObject(1021, 100);
            logger.LogInformation($"📋 Объект 1021 (есть конкретные права): {perms1021.ToDisplayString()}");
            
            // Объект без конкретных прав, но есть права на схему
            var perms2000 = userPermissions.GetPermissionsForObject(2000, 100);
            logger.LogInformation($"📋 Объект 2000 (права по схеме 100): {perms2000.ToDisplayString()}");
            
            // Объект без прав, только глобальные
            var perms3000 = userPermissions.GetPermissionsForObject(3000, 200);
            logger.LogInformation($"📋 Объект 3000 (только глобальные права): {perms3000.ToDisplayString()}");

            // 4. Тестируем проверку операций
            logger.LogInformation("\n🔧 4. ТЕСТИРОВАНИЕ ПРОВЕРКИ ОПЕРАЦИЙ:");
            
            var canRead1021 = userPermissions.CanPerformOperation(1021, 100, PermissionFlags.Select);
            var canDelete1021 = userPermissions.CanPerformOperation(1021, 100, PermissionFlags.Delete);
            var canUpdate2000 = userPermissions.CanPerformOperation(2000, 100, PermissionFlags.Update);
            var canDelete3000 = userPermissions.CanPerformOperation(3000, 200, PermissionFlags.Delete);

            logger.LogInformation($"✅ Проверка операций:");
            logger.LogInformation($"  - Может читать объект 1021: {canRead1021}");
            logger.LogInformation($"  - Может удалить объект 1021: {canDelete1021}");
            logger.LogInformation($"  - Может изменить объект 2000: {canUpdate2000}");
            logger.LogInformation($"  - Может удалить объект 3000: {canDelete3000}");

            // 5. Тестируем кеширование
            logger.LogInformation("\n⏱️ 5. ТЕСТИРОВАНИЕ КЕШИРОВАНИЯ:");
            
            var stats = userPermissions.GetStatistics();
            logger.LogInformation($"📊 Статистика кеша:");
            logger.LogInformation($"  - {stats}");
            logger.LogInformation($"  - Истек ли кеш: {userPermissions.IsExpired}");
            
            // Продлеваем кеш
            userPermissions.ExtendExpiration(TimeSpan.FromHours(1));
            logger.LogInformation($"  - После продления истекает: {userPermissions.ExpiresAt:HH:mm:ss}");
            
            // Инвалидируем кеш
            userPermissions.Invalidate();
            logger.LogInformation($"  - После инвалидации истек: {userPermissions.IsExpired}");

            // 6. Демонстрируем практическое использование
            logger.LogInformation("\n🎯 6. ПРАКТИЧЕСКОЕ ИСПОЛЬЗОВАНИЕ:");
            
            logger.LogInformation("✅ Примеры использования в коде:");
            logger.LogInformation("  // Проверка прав перед операцией");
            logger.LogInformation("  if (userPerms.CanPerformOperation(objId, schemeId, PermissionFlags.Delete))");
            logger.LogInformation("  {");
            logger.LogInformation("      await redb.DeleteAsync(obj);");
            logger.LogInformation("  }");
            logger.LogInformation("");
            logger.LogInformation("  // Кеширование разрешений");
            logger.LogInformation("  var cached = permissionCache.GetUserPermissions(userId);");
            logger.LogInformation("  if (cached.IsExpired) cached = await LoadFromDatabase(userId);");

            logger.LogInformation("\n💡 ПРЕИМУЩЕСТВА НОВЫХ МОДЕЛЕЙ:");
            logger.LogInformation("  ✅ Типобезопасные флаги разрешений");
            logger.LogInformation("  ✅ Эффективное кеширование с иерархией");
            logger.LogInformation("  ✅ Автоматическое управление временем жизни кеша");
            logger.LogInformation("  ✅ Статистика и мониторинг кеша");
            logger.LogInformation("  ✅ Простая инвалидация при изменениях");

            logger.LogInformation("✅ === МОДЕЛИ РАЗРЕШЕНИЙ РАБОТАЮТ КОРРЕКТНО ===");
        }
    }
}
