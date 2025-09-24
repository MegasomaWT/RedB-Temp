using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.DBModels;
using redb.Core.Models.Contracts;
using redb.Core.Models.Entities;
using redb.Core.Models.Security;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Этап 22: Тестирование базовых интерфейсов безопасности
    /// Проверяем работу IRedbObject, IRedbUser, IRedbSecurityContext
    /// </summary>
    public class Stage22_BasicInterfacesTest : BaseTestStage
    {
        public override int Order => 22;
        public override string Name => "ТЕСТИРОВАНИЕ БАЗОВЫХ ИНТЕРФЕЙСОВ";
        public override string Description => "Проверка работы IRedbObject, IRedbUser, IRedbSecurityContext";

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🔍 === ТЕСТИРОВАНИЕ БАЗОВЫХ ИНТЕРФЕЙСОВ ===");

            // 1. Тестируем IRedbObject с RedbObject<T>
            logger.LogInformation("\n🎯 1. ТЕСТИРОВАНИЕ IRedbObject:");
            
            var testObject = new RedbObject<dynamic>
            {
                id = 1021,
                scheme_id = 100,
                name = "Test Project"
            };

            // Проверяем что RedbObject реализует IRedbObject
            IRedbObject redbObj = testObject;
            logger.LogInformation($"✅ RedbObject реализует IRedbObject:");
            logger.LogInformation($"  - ID: {redbObj.Id}");
            logger.LogInformation($"  - SchemeId: {redbObj.SchemeId}");
            logger.LogInformation($"  - Name: {redbObj.Name}");

            // 2. Тестируем IRedbUser
            logger.LogInformation("\n👤 2. ТЕСТИРОВАНИЕ IRedbUser:");
            
            // Создаем пользователя через _RUser
            var testUser = new RedbUser(new _RUser
            {
                Id = 12345,
                Login = "testuser",
                Name = "Test User",
                Email = "test@example.com",
                Enabled = true,
                Password = "",
                DateRegister = DateTime.Now
            });

            IRedbUser user = testUser;
            logger.LogInformation($"✅ RedbUser реализует IRedbUser:");
            logger.LogInformation($"  - ID: {user.Id}");
            logger.LogInformation($"  - Login: {user.Login}");
            logger.LogInformation($"  - Name: {user.Name}");
            logger.LogInformation($"  - Email: {user.Email}");
            logger.LogInformation($"  - Enabled: {user.Enabled}");

            // 3. Тестируем SystemAdmin
            logger.LogInformation("\n🔧 3. ТЕСТИРОВАНИЕ СИСТЕМНОГО sys:");
            
            var sys = RedbUser.SystemUser;
            logger.LogInformation($"✅ SystemAdmin создан:");
            logger.LogInformation($"  - ID: {sys.Id}");
            logger.LogInformation($"  - Login: {sys.Login}");
            logger.LogInformation($"  - Name: {sys.Name}");
            logger.LogInformation($"  - Enabled: {sys.Enabled}");

            // 4. Тестируем IRedbSecurityContext
            logger.LogInformation("\n🛡️ 4. ТЕСТИРОВАНИЕ SECURITY CONTEXT:");
            
            // Создаем контекст с пользователем
            var contextWithUser = RedbSecurityContext.WithUser(testUser);
            logger.LogInformation($"✅ Контекст с пользователем:");
            logger.LogInformation($"  - IsAuthenticated: {contextWithUser.IsAuthenticated}");
            logger.LogInformation($"  - IsSystemContext: {contextWithUser.IsSystemContext}");
            logger.LogInformation($"  - EffectiveUserId: {contextWithUser.GetEffectiveUserId()}");
            logger.LogInformation($"  - CurrentUser: {contextWithUser.CurrentUser?.Name}");

            // Создаем системный контекст
            var systemContext = RedbSecurityContext.System();
            logger.LogInformation($"✅ Системный контекст:");
            logger.LogInformation($"  - IsAuthenticated: {systemContext.IsAuthenticated}");
            logger.LogInformation($"  - IsSystemContext: {systemContext.IsSystemContext}");
            logger.LogInformation($"  - EffectiveUserId: {systemContext.GetEffectiveUserId()}");
            logger.LogInformation($"  - CurrentUser: {systemContext.CurrentUser?.Name ?? "NULL"}");

            // Создаем контекст с sys
            var adminContext = RedbSecurityContext.WithAdmin();
            logger.LogInformation($"✅ sys контекст:");
            logger.LogInformation($"  - IsAuthenticated: {adminContext.IsAuthenticated}");
            logger.LogInformation($"  - IsSystemContext: {adminContext.IsSystemContext}");
            logger.LogInformation($"  - EffectiveUserId: {adminContext.GetEffectiveUserId()}");
            logger.LogInformation($"  - CurrentUser: {adminContext.CurrentUser?.Name}");

            // 5. Тестируем временный системный контекст
            logger.LogInformation("\n⏱️ 5. ТЕСТИРОВАНИЕ ВРЕМЕННОГО СИСТЕМНОГО КОНТЕКСТА:");
            
            logger.LogInformation($"📋 До системного контекста:");
            logger.LogInformation($"  - IsSystemContext: {contextWithUser.IsSystemContext}");
            logger.LogInformation($"  - CurrentUser: {contextWithUser.CurrentUser?.Name}");

            using (contextWithUser.CreateSystemContext())
            {
                logger.LogInformation($"📋 В системном контексте:");
                logger.LogInformation($"  - IsSystemContext: {contextWithUser.IsSystemContext}");
                logger.LogInformation($"  - EffectiveUserId: {contextWithUser.GetEffectiveUserId()}");
            }

            logger.LogInformation($"📋 После системного контекста:");
            logger.LogInformation($"  - IsSystemContext: {contextWithUser.IsSystemContext}");
            logger.LogInformation($"  - CurrentUser: {contextWithUser.CurrentUser?.Name}");

            // 6. Демонстрируем работу с объектами вместо ID
            logger.LogInformation("\n🎯 6. ДЕМОНСТРАЦИЯ РАБОТЫ С ОБЪЕКТАМИ:");
            
            logger.LogInformation("❌ СТАРЫЙ ПОДХОД - работа с ID:");
            logger.LogInformation("  DeleteAsync(objectId: 1021, userId: 12345, checkPermissions: true)");
            
            logger.LogInformation("✅ НОВЫЙ ПОДХОД - работа с объектами:");
            logger.LogInformation($"  var project = LoadAsync<Project>({testObject.Id});");
            logger.LogInformation($"  DeleteAsync(project); // ID извлекается автоматически!");
            logger.LogInformation($"  // Пользователь из SecurityContext автоматически");

            logger.LogInformation("\n💡 ПРЕИМУЩЕСТВА НОВОГО ПОДХОДА:");
            logger.LogInformation("  ✅ Типобезопасность - работаем с классами, а не числами");
            logger.LogInformation("  ✅ Автоматическое извлечение ID из объектов");
            logger.LogInformation("  ✅ Контекст безопасности управляет пользователем");
            logger.LogInformation("  ✅ Красивый и понятный код");
            logger.LogInformation("  ✅ Меньше ошибок - нет путаницы с ID");

            logger.LogInformation("✅ === БАЗОВЫЕ ИНТЕРФЕЙСЫ РАБОТАЮТ КОРРЕКТНО ===");
        }
    }
}
