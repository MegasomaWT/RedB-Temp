using System;
using System.Threading.Tasks;
using redb.Core.DBModels;
using redb.Core.Extensions;
using redb.Core.Models.Entities;
using redb.Core.Models.Configuration;

namespace redb.Core.Examples
{
    /// <summary>
    /// Примеры использования системы конфигурации RedbService
    /// </summary>
    public static class ConfigurationExamples
    {
        /// <summary>
        /// Пример 1: Использование предопределенных конфигураций
        /// </summary>
        public static void Example1_PredefinedConfigurations()
        {
            // Для разработки
            var devConfig = PredefinedConfigurations.Development;
            
            // Для продакшена
            var prodConfig = PredefinedConfigurations.Production;
            
            // Для массовых операций
            var bulkConfig = PredefinedConfigurations.BulkOperations;
            
            // По имени
            var debugConfig = PredefinedConfigurations.GetByName("debug");
        }

        /// <summary>
        /// Пример 2: Использование ConfigurationBuilder
        /// </summary>
        public static void Example2_ConfigurationBuilder()
        {
            // Создание конфигурации с нуля
            var config1 = new RedbServiceConfigurationBuilder()
                .WithStrictSecurity()
                .WithLoadDepth(defaultDepth: 5)
                .WithMetadataCache(enabled: true, lifetimeMinutes: 60)
                .Build();

            // Модификация существующей конфигурации
            var config2 = new RedbServiceConfigurationBuilder(PredefinedConfigurations.Development)
                .WithStrictSecurity()
                .WithoutCache()
                .Build();

            // Использование предустановленных профилей
            var config3 = new RedbServiceConfigurationBuilder()
                .ForProduction()
                .WithLoadDepth(defaultDepth: 3) // Переопределяем глубину
                .Build();
        }

        /// <summary>
        /// Пример 3: Валидация конфигурации
        /// </summary>
        public static void Example3_ConfigurationValidation()
        {
            var config = new RedbServiceConfiguration
            {
                DefaultLoadDepth = -1, // Ошибка!
                MetadataCacheLifetimeMinutes = 0 // Ошибка при включенном кеше!
            };

            // Валидация
            var validationResult = ConfigurationValidator.Validate(config);
            
            if (!validationResult.IsValid)
            {
                Console.WriteLine("Ошибки конфигурации:");
                foreach (var message in validationResult.GetAllMessages())
                {
                    Console.WriteLine($"  {message}");
                }
            }

            // Автоматическое исправление критических ошибок
            var fixedConfig = ConfigurationValidator.FixCriticalErrors(config);
        }

        /// <summary>
        /// Пример 4: Extension методы
        /// </summary>
        public static void Example4_ExtensionMethods()
        {
            var config = PredefinedConfigurations.Development;

            // Клонирование
            var clonedConfig = config.Clone();

            // Объединение конфигураций
            var mergedConfig = PredefinedConfigurations.Development
                .MergeWith(PredefinedConfigurations.HighPerformance);

            // Проверки
            bool isProductionSafe = config.IsProductionSafe();
            bool isPerformanceOptimized = config.IsPerformanceOptimized();
            string description = config.GetDescription();

            Console.WriteLine($"Configuration: {description}");
            Console.WriteLine($"Production safe: {isProductionSafe}");
            Console.WriteLine($"Performance optimized: {isPerformanceOptimized}");
        }

        /// <summary>
        /// Пример 5: Временные конфигурации
        /// </summary>
        public static async Task Example5_TemporaryConfigurations(IRedbService redbService)
        {
            // Временное отключение проверок прав для массовой операции
            using (redbService.ApplyTemporary(builder => builder.WithoutPermissionChecks()))
            {
                // Здесь выполняем операции без проверки прав
                // await redbService.SaveAsync(someObject);
            }
            // Конфигурация автоматически восстанавливается

            // Временная конфигурация для отладки
            using (redbService.ApplyTemporary(PredefinedConfigurations.Debug))
            {
                // Здесь работаем с подробным логированием
                // var obj = await redbService.LoadAsync<SomeType>(id);
            }
        }

        /// <summary>
        /// Пример 6: Решение проблемы с удаленными объектами
        /// </summary>
        public static async Task Example6_DeletedObjectsProblem(IRedbService redbService)
        {
            // Проблема: после удаления объект сохраняет ID в памяти
            var obj = new RedbObject<TestProps> { name = "Test Object" };
            var objectId = await redbService.SaveAsync(obj); // obj.id = 12345
            
            await redbService.DeleteAsync(obj); // Удаляем из БД, использует _securityContext
            
            // Без конфигурации это вызовет ошибку:
            // await redbService.SaveAsync(obj); // ❌ ОШИБКА!

            // Решение 1: Автосброс ID при удалении
            redbService.UpdateConfiguration(config => 
            {
                config.IdResetStrategy = ObjectIdResetStrategy.AutoResetOnDelete;
            });

            await redbService.DeleteAsync(obj); // obj.id автоматически = 0, использует _securityContext
            await redbService.SaveAsync(obj);   // ✅ Создается новый объект

            // Решение 2: Автосоздание при сохранении удаленного
            redbService.UpdateConfiguration(config => 
            {
                config.MissingObjectStrategy = MissingObjectStrategy.AutoSwitchToInsert;
            });

            await redbService.SaveAsync(obj); // ✅ Автоматически создается новый объект
        }

        /// <summary>
        /// Пример 7: Конфигурация для разных сред
        /// </summary>
        public static RedbServiceConfiguration Example7_EnvironmentSpecificConfig(string environment)
        {
            return environment.ToLowerInvariant() switch
            {
                "development" => new RedbServiceConfigurationBuilder()
                    .ForDevelopment()
                    .WithPrettyJson()
                    .Build(),

                "testing" => new RedbServiceConfigurationBuilder()
                    .ForIntegrationTesting()
                    .WithoutCache() // Для изоляции тестов
                    .Build(),

                "staging" => new RedbServiceConfigurationBuilder()
                    .ForProduction()
                    .WithLoadDepth(defaultDepth: 8) // Больше для тестирования
                    .Build(),

                "production" => new RedbServiceConfigurationBuilder()
                    .ForProduction()
                    .WithLoadDepth(defaultDepth: 3) // Меньше для производительности
                    .WithMetadataCache(enabled: true, lifetimeMinutes: 120)
                    .Build(),

                _ => PredefinedConfigurations.Default
            };
        }

        /// <summary>
        /// Пример 8: Динамическая настройка в зависимости от нагрузки
        /// </summary>
        public static void Example8_DynamicConfiguration(IRedbService redbService, int currentLoad)
        {
            if (currentLoad > 1000) // Высокая нагрузка
            {
                redbService.UpdateConfiguration(config =>
                {
                    config.DefaultLoadDepth = 1;
                    config.EnableDataValidation = false;
                    config.DefaultCheckPermissionsOnLoad = false;
                });
            }
            else if (currentLoad < 100) // Низкая нагрузка
            {
                redbService.UpdateConfiguration(config =>
                {
                    config.DefaultLoadDepth = 10;
                    config.EnableDataValidation = true;
                    config.DefaultCheckPermissionsOnLoad = true;
                });
            }
        }

        /// <summary>
        /// Пример тестового класса свойств
        /// </summary>
        private class TestProps
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }
    }
}
