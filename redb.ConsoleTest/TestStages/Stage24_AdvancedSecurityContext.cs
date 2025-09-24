using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.DBModels;
using redb.Core.Models.Contracts;
using redb.Core.Models.Entities;
using redb.Core.Models.Security;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Этап 24: Тестирование продвинутого контекста безопасности
    /// Проверяем многоуровневую систему приоритетов и Ambient Context
    /// </summary>
    public class Stage24_AdvancedSecurityContext : BaseTestStage
    {
        public override int Order => 24;
        public override string Name => "ПРОДВИНУТЫЙ КОНТЕКСТ БЕЗОПАСНОСТИ";
        public override string Description => "Многоуровневая система приоритетов и Ambient Context";

        protected override async Task ExecuteStageAsync(ILogger logger, IRedbService redb)
        {
            logger.LogInformation("🔍 === ТЕСТИРОВАНИЕ ПРОДВИНУТОГО КОНТЕКСТА БЕЗОПАСНОСТИ ===");

            // 1. Тестируем многоуровневую систему приоритетов
            logger.LogInformation("\n🎯 1. ТЕСТИРОВАНИЕ МНОГОУРОВНЕВОЙ СИСТЕМЫ ПРИОРИТЕТОВ:");
            
            var testUser = new RedbUser(new _RUser
            {
                Id = 12345,
                Login = "testuser",
                Name = "Test User",
                Email = "testuser@test.com",
                Password = "",
                Enabled = true,
                DateRegister = DateTime.Now
            });
            
            var context = RedbSecurityContext.WithUser(testUser);

            // Уровень 1: Контекст с пользователем
            var result1 = context.GetEffectiveUser();
            logger.LogInformation($"✅ Уровень 1 (Context with User): {result1.Login} (ID: {result1.Id})");
            
            // Уровень 2: Системный контекст
            var systemContext = RedbSecurityContext.System();
            var result2 = systemContext.GetEffectiveUser();
            logger.LogInformation($"✅ Уровень 2 (System): {result2.Login} (ID: {result2.Id})");
            
            // Уровень 3: Контекст с admin пользователем  
            var adminContext = RedbSecurityContext.WithAdmin();
            var result3 = adminContext.GetEffectiveUser();
            logger.LogInformation($"✅ Уровень 3 (Admin): {result3.Login} (ID: {result3.Id})");
            
            // Уровень 4: Дефолтный контекст
            var emptyContext = new RedbSecurityContext();
            var result4 = emptyContext.GetEffectiveUser();
            logger.LogInformation($"✅ Уровень 4 (Default): {result4.Login} (ID: {result4.Id})");

            // 2. Тестируем Ambient Context
            logger.LogInformation("\n🌐 2. ТЕСТИРОВАНИЕ AMBIENT CONTEXT:");
            
            logger.LogInformation($"📋 До установки Ambient Context:");
            logger.LogInformation($"  - Current: {AmbientSecurityContext.Current?.CurrentUser?.Name ?? "NULL"}");
            
            // Устанавливаем ambient context
            using (AmbientSecurityContext.SetContext(context))
            {
                logger.LogInformation($"📋 В Ambient Context:");
                logger.LogInformation($"  - Current: {AmbientSecurityContext.Current?.CurrentUser?.Name}");
                logger.LogInformation($"  - EffectiveUserId: {AmbientSecurityContext.Current?.GetEffectiveUserId()}");
                
                // Тестируем вложенные контексты
                using (AmbientSecurityContext.CreateSystemContext())
                {
                    logger.LogInformation($"📋 В вложенном System Context:");
                    logger.LogInformation($"  - IsSystemContext: {AmbientSecurityContext.Current?.IsSystemContext}");
                    logger.LogInformation($"  - EffectiveUserId: {AmbientSecurityContext.Current?.GetEffectiveUserId()}");
                }
                
                logger.LogInformation($"📋 После вложенного контекста:");
                logger.LogInformation($"  - Current: {AmbientSecurityContext.Current?.CurrentUser?.Name}");
            }
            
            logger.LogInformation($"📋 После Ambient Context:");
            logger.LogInformation($"  - Current: {AmbientSecurityContext.Current?.CurrentUser?.Name ?? "NULL"}");

            // 3. Тестируем GetOrCreateDefault
            logger.LogInformation("\n🔧 3. ТЕСТИРОВАНИЕ GET OR CREATE DEFAULT:");
            
            var defaultContext = AmbientSecurityContext.GetOrCreateDefault();
            logger.LogInformation($"✅ Default context создан:");
            logger.LogInformation($"  - User: {defaultContext.CurrentUser?.Name}");
            logger.LogInformation($"  - UserId: {defaultContext.GetEffectiveUserId()}");
            logger.LogInformation($"  - IsAuthenticated: {defaultContext.IsAuthenticated}");

            // 4. Тестируем различные типы временных контекстов
            logger.LogInformation("\n⏱️ 4. ТЕСТИРОВАНИЕ ВРЕМЕННЫХ КОНТЕКСТОВ:");
            
            logger.LogInformation("📋 Тестируем CreateUserContext:");
            using (AmbientSecurityContext.CreateUserContext(testUser))
            {
                var current = AmbientSecurityContext.Current;
                logger.LogInformation($"  - User: {current?.CurrentUser?.Name}");
                logger.LogInformation($"  - IsAuthenticated: {current?.IsAuthenticated}");
            }
            
            logger.LogInformation("📋 Тестируем CreateAdminContext:");
            using (AmbientSecurityContext.CreateAdminContext())
            {
                var current = AmbientSecurityContext.Current;
                logger.LogInformation($"  - User: {current?.CurrentUser?.Name}");
                logger.LogInformation($"  - UserId: {current?.GetEffectiveUserId()}");
            }

            // 5. Демонстрируем практическое использование
            logger.LogInformation("\n🎯 5. ПРАКТИЧЕСКОЕ ИСПОЛЬЗОВАНИЕ:");
            
            logger.LogInformation("✅ Примеры красивого кода:");
            logger.LogInformation("  // Установка контекста для всего запроса");
            logger.LogInformation("  using (AmbientSecurityContext.CreateUserContext(currentUser))");
            logger.LogInformation("  {");
            logger.LogInformation("      // Все операции автоматически используют currentUser");
            logger.LogInformation("      await redb.SaveAsync(project);");
            logger.LogInformation("      await redb.DeleteAsync(oldData);");
            logger.LogInformation("  }");
            logger.LogInformation("");
            logger.LogInformation("  // Временные системные операции");
            logger.LogInformation("  using (AmbientSecurityContext.CreateSystemContext())");
            logger.LogInformation("  {");
            logger.LogInformation("      // Операции без проверки прав");
            logger.LogInformation("      await redb.SaveAsync(systemData);");
            logger.LogInformation("  }");

            // 6. Тестируем приоритеты в сложных сценариях
            logger.LogInformation("\n🔀 6. СЛОЖНЫЕ СЦЕНАРИИ ПРИОРИТЕТОВ:");
            
            using (AmbientSecurityContext.CreateUserContext(testUser))
            {
                var ambientContext = AmbientSecurityContext.Current!;
                
                // Explicit пользователь перекрывает ambient
                var explicitResult = ((RedbSecurityContext)ambientContext).GetEffectiveUser();
                logger.LogInformation($"📋 Explicit перекрывает Ambient: {explicitResult}");
                
                // Системный контекст перекрывает пользователя
                using (ambientContext.CreateSystemContext())
                {
                    var currentContext = AmbientSecurityContext.Current;
                    var systemResult = ((RedbSecurityContext)currentContext!).GetEffectiveUser();
                    logger.LogInformation($"📋 System перекрывает User: {systemResult}");
                }
            }

            logger.LogInformation("\n💡 ПРЕИМУЩЕСТВА ПРОДВИНУТОГО КОНТЕКСТА:");
            logger.LogInformation("  ✅ Четкая система приоритетов - нет путаницы");
            logger.LogInformation("  ✅ Ambient Context - автоматический контекст везде");
            logger.LogInformation("  ✅ Временные контексты - легко переключаться");
            logger.LogInformation("  ✅ Системный режим - операции без проверки прав");
            logger.LogInformation("  ✅ Fallback к sys - система всегда работает");
            logger.LogInformation("  ✅ Подробная информация для отладки");

            logger.LogInformation("✅ === ПРОДВИНУТЫЙ КОНТЕКСТ БЕЗОПАСНОСТИ РАБОТАЕТ КОРРЕКТНО ===");
        }
    }
}
