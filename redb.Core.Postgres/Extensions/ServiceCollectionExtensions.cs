using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using redb.Core;
using redb.Core.Configuration;
using System;
using redb.Core.Models.Configuration;

namespace redb.Core.Postgres.Extensions
{
    /// <summary>
    /// Extension методы для регистрации RedbService в DI контейнере
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Регистрирует RedbService с конфигурацией по умолчанию
        /// </summary>
        public static IServiceCollection AddRedbService(this IServiceCollection services)
        {
            services.AddScoped<RedbServiceConfiguration>();
            services.AddScoped<IRedbService, RedbService>();
            return services;
        }

        /// <summary>
        /// Регистрирует RedbService с конфигурацией из IConfiguration
        /// </summary>
        public static IServiceCollection AddRedbService(
            this IServiceCollection services, 
            IConfiguration configuration, 
            string sectionName = "RedbService")
        {
            // Регистрируем конфигурацию из appsettings.json
            var config = configuration.GetSection(sectionName).Get<RedbServiceConfiguration>() 
                        ?? new RedbServiceConfiguration();
            
            services.AddSingleton(config);
            services.AddScoped<IRedbService, RedbService>();

            return services;
        }

        /// <summary>
        /// Регистрирует RedbService с программной конфигурацией через Action
        /// </summary>
        public static IServiceCollection AddRedbService(
            this IServiceCollection services,
            Action<RedbServiceConfiguration> configureOptions)
        {
            var config = new RedbServiceConfiguration();
            configureOptions(config);
            
            services.AddSingleton(config);
            services.AddScoped<IRedbService, RedbService>();

            return services;
        }

        /// <summary>
        /// Регистрирует RedbService с программной конфигурацией через Builder
        /// </summary>
        public static IServiceCollection AddRedbService(
            this IServiceCollection services,
            Action<RedbServiceConfigurationBuilder> configureBuilder)
        {
            var builder = new RedbServiceConfigurationBuilder();
            configureBuilder(builder);
            var config = builder.Build();

            services.AddSingleton(config);
            services.AddScoped<IRedbService, RedbService>();

            return services;
        }

        /// <summary>
        /// Регистрирует RedbService с предопределенным профилем
        /// </summary>
        public static IServiceCollection AddRedbService(
            this IServiceCollection services,
            string profileName)
        {
            var config = profileName.ToLowerInvariant() switch
            {
                "development" => PredefinedConfigurations.Development,
                "production" => PredefinedConfigurations.Production,
                "highperformance" => PredefinedConfigurations.HighPerformance,
                "bulkoperations" => PredefinedConfigurations.BulkOperations,
                "debug" => PredefinedConfigurations.Debug,
                "integrationtesting" => PredefinedConfigurations.IntegrationTesting,
                "datamigration" => PredefinedConfigurations.DataMigration,
                _ => throw new ArgumentException($"Unknown profile: {profileName}")
            };

            services.AddSingleton(config);
            services.AddScoped<IRedbService, RedbService>();

            return services;
        }

        /// <summary>
        /// Регистрирует RedbService с комбинированной конфигурацией (профиль + дополнительная настройка)
        /// </summary>
        public static IServiceCollection AddRedbService(
            this IServiceCollection services,
            string profileName,
            Action<RedbServiceConfigurationBuilder> additionalConfiguration)
        {
            var baseConfig = profileName.ToLowerInvariant() switch
            {
                "development" => PredefinedConfigurations.Development,
                "production" => PredefinedConfigurations.Production,
                "highperformance" => PredefinedConfigurations.HighPerformance,
                "bulkoperations" => PredefinedConfigurations.BulkOperations,
                "debug" => PredefinedConfigurations.Debug,
                "integrationtesting" => PredefinedConfigurations.IntegrationTesting,
                "datamigration" => PredefinedConfigurations.DataMigration,
                _ => throw new ArgumentException($"Unknown profile: {profileName}")
            };

            var builder = new RedbServiceConfigurationBuilder(baseConfig);
            additionalConfiguration(builder);
            var finalConfig = builder.Build();

            services.AddSingleton(finalConfig);
            services.AddScoped<IRedbService, RedbService>();

            return services;
        }

        /// <summary>
        /// Регистрирует RedbService с валидацией конфигурации
        /// </summary>
        public static IServiceCollection AddValidatedRedbService(
            this IServiceCollection services,
            IConfiguration configuration,
            bool throwOnValidationError = false,
            string sectionName = "RedbService")
        {
            var config = configuration.GetSection(sectionName).Get<RedbServiceConfiguration>() 
                        ?? new RedbServiceConfiguration();
            
            // Валидируем конфигурацию
            var validator = new RedbServiceConfigurationValidator();
            var validationResult = validator.Validate(null, config);
            
            if (!validationResult.Succeeded && throwOnValidationError)
            {
                throw new InvalidOperationException($"Configuration validation failed: {validationResult.FailureMessage}");
            }

            services.AddSingleton(config);
            services.AddScoped<IRedbService, RedbService>();

            return services;
        }

        /// <summary>
        /// Регистрирует RedbService с мониторингом изменений конфигурации (hot-reload)
        /// </summary>
        public static IServiceCollection AddRedbServiceWithHotReload(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "RedbService")
        {
            // Для hot-reload используем IOptionsMonitor
            services.Configure<RedbServiceConfiguration>(configuration.GetSection(sectionName));
            
            // Регистрируем мониторинг изменений (если доступен)
            // Мониторинг конфигурации отложен
            // services.AddRedbServiceConfigurationMonitoring(configuration);

            // Регистрируем RedbService с поддержкой hot-reload
            services.AddScoped<IRedbService>(provider =>
            {
                try
                {
                    var configMonitor = provider.GetService<IOptionsMonitor<RedbServiceConfiguration>>();
                    if (configMonitor != null)
                    {
                        // TODO: Реализовать поддержку hot-reload в RedbService
                        var config = configMonitor.CurrentValue;
                        return new RedbService(provider);
                    }
                }
                catch { }
                
                // Fallback к обычной конфигурации
                return new RedbService(provider);
            });

            return services;
        }
    }
}
