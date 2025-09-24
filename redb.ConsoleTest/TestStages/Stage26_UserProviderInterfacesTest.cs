using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using redb.Core;
using redb.Core.DBModels;
using redb.Core.Models.Contracts;
using redb.Core.Models.Entities;
using redb.Core.Providers;

namespace redb.ConsoleTest.TestStages
{
    /// <summary>
    /// Тест интерфейсов провайдеров пользователей, ролей и разрешений
    /// Проверяет что все интерфейсы корректно определены и компилируются
    /// </summary>
    public class Stage26_UserProviderInterfacesTest : ITestStage
    {
        public string Name => "Тест интерфейсов провайдеров (IUserProvider, IRoleProvider, IPermissionProvider)";
        public string Description => "Проверка корректности определения интерфейсов провайдеров для пользователей, ролей и разрешений";
        public int Order => 26;

        public async Task ExecuteAsync(ILogger logger, IRedbService service)
        {
            logger.LogInformation("🔌 Тестирование интерфейсов провайдеров...");
            
            try
            {
                // === ТЕСТ 1: Проверка IUserProvider ===
                logger.LogInformation("📋 Тест 1: Проверка методов IUserProvider");
                await TestIUserProviderInterface();
                logger.LogInformation("✅ IUserProvider - все методы определены корректно");
                
                // === ТЕСТ 2: Проверка IRoleProvider ===
                logger.LogInformation("📋 Тест 2: Проверка методов IRoleProvider");
                await TestIRoleProviderInterface();
                logger.LogInformation("✅ IRoleProvider - все методы определены корректно");
                
                // === ТЕСТ 3: Проверка расширенного IPermissionProvider ===
                logger.LogInformation("📋 Тест 3: Проверка расширенного IPermissionProvider");
                await TestIPermissionProviderInterface();
                logger.LogInformation("✅ IPermissionProvider - все методы определены корректно");
                
                // === ТЕСТ 4: Проверка совместимости типов ===
                logger.LogInformation("📋 Тест 4: Проверка совместимости типов");
                TestTypeCompatibility();
                logger.LogInformation("✅ Все типы совместимы");
                
                logger.LogInformation("🎉 Все интерфейсы провайдеров успешно протестированы!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Ошибка при тестировании интерфейсов: {Message}", ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Тест интерфейса IUserProvider
        /// </summary>
        private async Task TestIUserProviderInterface()
        {
            // Проверяем что интерфейс существует и имеет все необходимые методы
            var interfaceType = typeof(IUserProvider);
            
            // CRUD операции
            CheckMethod(interfaceType, "CreateUserAsync");
            CheckMethod(interfaceType, "UpdateUserAsync");
            CheckMethod(interfaceType, "DeleteUserAsync");
            
            // Поиск и получение
            CheckMethod(interfaceType, "GetUserByIdAsync");
            CheckMethod(interfaceType, "GetUserByLoginAsync");
            CheckMethod(interfaceType, "LoadUserAsync"); // Должно быть 2 перегрузки
            CheckMethod(interfaceType, "GetUsersAsync");
            
            // Аутентификация
            CheckMethod(interfaceType, "ValidateUserAsync");
            CheckMethod(interfaceType, "ChangePasswordAsync");
            CheckMethod(interfaceType, "SetPasswordAsync");
            
            // Управление статусом
            CheckMethod(interfaceType, "EnableUserAsync");
            CheckMethod(interfaceType, "DisableUserAsync");
            
            // Валидация
            CheckMethod(interfaceType, "ValidateUserDataAsync");
            CheckMethod(interfaceType, "IsLoginAvailableAsync");
            
            // Статистика
            CheckMethod(interfaceType, "GetUserCountAsync");
            CheckMethod(interfaceType, "GetActiveUserCountAsync");
            
            Console.WriteLine($"   ✓ IUserProvider содержит {interfaceType.GetMethods().Length} методов");
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Тест интерфейса IRoleProvider
        /// </summary>
        private async Task TestIRoleProviderInterface()
        {
            var interfaceType = typeof(IRoleProvider);
            
            // CRUD ролей
            CheckMethod(interfaceType, "CreateRoleAsync");
            CheckMethod(interfaceType, "UpdateRoleAsync");
            CheckMethod(interfaceType, "DeleteRoleAsync");
            
            // Поиск ролей
            CheckMethod(interfaceType, "GetRoleByIdAsync");
            CheckMethod(interfaceType, "GetRoleByNameAsync");
            CheckMethod(interfaceType, "LoadRoleAsync"); // Должно быть 2 перегрузки
            CheckMethod(interfaceType, "GetRolesAsync");
            
            // Управление пользователь-роль
            CheckMethod(interfaceType, "AssignUserToRoleAsync");
            CheckMethod(interfaceType, "RemoveUserFromRoleAsync");
            CheckMethod(interfaceType, "SetUserRolesAsync");
            CheckMethod(interfaceType, "GetUserRolesAsync");
            CheckMethod(interfaceType, "GetRoleUsersAsync");
            CheckMethod(interfaceType, "UserHasRoleAsync");
            
            // Валидация
            CheckMethod(interfaceType, "IsRoleNameAvailableAsync");
            
            // Статистика
            CheckMethod(interfaceType, "GetRoleCountAsync");
            CheckMethod(interfaceType, "GetRoleUserCountAsync");
            CheckMethod(interfaceType, "GetRoleStatisticsAsync");
            
            Console.WriteLine($"   ✓ IRoleProvider содержит {interfaceType.GetMethods().Length} методов");
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Тест расширенного интерфейса IPermissionProvider
        /// </summary>
        private async Task TestIPermissionProviderInterface()
        {
            var interfaceType = typeof(IPermissionProvider);
            
            // Существующие методы проверки прав
            CheckMethod(interfaceType, "GetReadableObjectIds");
            CheckMethod(interfaceType, "CanUserEditObject");
            CheckMethod(interfaceType, "CanUserSelectObject");
            CheckMethod(interfaceType, "CanUserInsertScheme");
            CheckMethod(interfaceType, "CanUserDeleteObject");
            
            // Новые CRUD методы
            CheckMethod(interfaceType, "CreatePermissionAsync");
            CheckMethod(interfaceType, "UpdatePermissionAsync");
            CheckMethod(interfaceType, "DeletePermissionAsync");
            
            // Поиск разрешений
            CheckMethod(interfaceType, "GetPermissionsByUserAsync");
            CheckMethod(interfaceType, "GetPermissionsByRoleAsync");
            CheckMethod(interfaceType, "GetPermissionsByObjectAsync");
            CheckMethod(interfaceType, "GetPermissionByIdAsync");
            
            // Управление разрешениями
            CheckMethod(interfaceType, "GrantPermissionAsync");
            CheckMethod(interfaceType, "RevokePermissionAsync");
            CheckMethod(interfaceType, "RevokeAllUserPermissionsAsync");
            CheckMethod(interfaceType, "RevokeAllRolePermissionsAsync");
            
            // Эффективные права
            CheckMethod(interfaceType, "GetEffectivePermissionsAsync");
            CheckMethod(interfaceType, "GetEffectivePermissionsBatchAsync");
            CheckMethod(interfaceType, "GetAllEffectivePermissionsAsync");
            
            // Статистика
            CheckMethod(interfaceType, "GetPermissionCountAsync");
            CheckMethod(interfaceType, "GetUserPermissionCountAsync");
            CheckMethod(interfaceType, "GetRolePermissionCountAsync");
            
            Console.WriteLine($"   ✓ IPermissionProvider содержит {interfaceType.GetMethods().Length} методов");
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Проверка совместимости типов
        /// </summary>
        private void TestTypeCompatibility()
        {
            // Проверяем что все необходимые типы существуют
            CheckType<IRedbUser>("IRedbUser");
            CheckType<RedbUser>("RedbUser");
            CheckType<_RUser>("_RUser");
            CheckType<_RRole>("_RRole");
            CheckType<_RPermission>("_RPermission");
            CheckType<_RUsersRole>("_RUsersRole");
            
            // Проверяем что RedbUser реализует IRedbUser
            if (!typeof(IRedbUser).IsAssignableFrom(typeof(RedbUser)))
            {
                throw new InvalidOperationException("RedbUser должен реализовывать IRedbUser");
            }
            
            Console.WriteLine("   ✓ Все основные типы существуют и совместимы");
        }
        
        /// <summary>
        /// Проверить наличие метода в интерфейсе
        /// </summary>
        private void CheckMethod(Type interfaceType, string methodName)
        {
            var methods = interfaceType.GetMethods();
            bool found = false;
            
            foreach (var method in methods)
            {
                if (method.Name == methodName)
                {
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                throw new InvalidOperationException($"Метод {methodName} не найден в интерфейсе {interfaceType.Name}");
            }
        }
        
        /// <summary>
        /// Проверить наличие типа
        /// </summary>
        private void CheckType<T>(string typeName)
        {
            var type = typeof(T);
            if (type == null)
            {
                throw new InvalidOperationException($"Тип {typeName} не найден");
            }
            Console.WriteLine($"   ✓ Тип {typeName} найден: {type.FullName}");
        }
    }
}
