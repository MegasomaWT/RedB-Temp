using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using redb.Core.Models.Configuration;

namespace redb.Core.Configuration
{
    /// <summary>
    /// Extension методы для регистрации RedbService в DI контейнере
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Добавить конфигурацию RedbService из IConfiguration
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        /// <param name="configuration">Конфигурация приложения</param>
        /// <param name="sectionName">Имя секции в конфигурации</param>
        /// <returns>Коллекция сервисов для цепочки вызовов</returns>
        public static IServiceCollection AddRedbServiceConfiguration(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "RedbService")
        {
            // Регистрируем конфигурацию через Options pattern
            services.Configure<RedbServiceConfiguration>(
                configuration.GetSection(sectionName));

            // Добавляем валидацию конфигурации
            services.AddSingleton<IValidateOptions<RedbServiceConfiguration>, RedbServiceConfigurationValidator>();

            // Регистрируем прямой доступ к конфигурации
            services.AddSingleton<RedbServiceConfiguration>(provider =>
            {
                var options = provider.GetRequiredService<IOptionsMonitor<RedbServiceConfiguration>>();
                return options.CurrentValue;
            });

            return services;
        }

        /// <summary>
        /// Добавить конфигурацию RedbService с валидацией
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        /// <param name="configuration">Конфигурация приложения</param>
        /// <param name="sectionName">Имя секции в конфигурации</param>
        /// <param name="throwOnValidationError">Выбрасывать исключение при ошибках валидации</param>
        /// <returns>Коллекция сервисов для цепочки вызовов</returns>
        public static IServiceCollection AddValidatedRedbServiceConfiguration(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "RedbService",
            bool throwOnValidationError = true)
        {
            // Получаем и валидируем конфигурацию при регистрации
            var config = configuration.GetValidatedRedbServiceConfiguration(sectionName, throwOnValidationError);

            // Регистрируем валидированную конфигурацию
            services.AddSingleton(config);

            // Также регистрируем через Options pattern для совместимости
            services.Configure<RedbServiceConfiguration>(options =>
            {
                // Копируем все свойства из валидированной конфигурации
                CopyConfigurationProperties(config, options);
            });

            return services;
        }

        /// <summary>
        /// Добавить конфигурацию RedbService через builder
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        /// <param name="configureBuilder">Делегат для настройки builder'а</param>
        /// <returns>Коллекция сервисов для цепочки вызовов</returns>
        public static IServiceCollection AddRedbServiceConfiguration(
            this IServiceCollection services,
            Action<RedbServiceConfigurationBuilder> configureBuilder)
        {
            var builder = new RedbServiceConfigurationBuilder();
            configureBuilder(builder);
            var config = builder.Build();

            services.AddSingleton(config);

            return services;
        }

        /// <summary>
        /// Добавить конфигурацию RedbService с комбинированием IConfiguration и builder
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        /// <param name="configuration">Конфигурация приложения</param>
        /// <param name="configureBuilder">Делегат для дополнительной настройки</param>
        /// <param name="sectionName">Имя секции в конфигурации</param>
        /// <returns>Коллекция сервисов для цепочки вызовов</returns>
        public static IServiceCollection AddRedbServiceConfiguration(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<RedbServiceConfigurationBuilder> configureBuilder,
            string sectionName = "RedbService")
        {
            // Создаем builder на основе конфигурации
            var builder = configuration.CreateRedbServiceBuilder(sectionName);
            
            // Применяем дополнительные настройки
            configureBuilder(builder);
            
            var config = builder.Build();
            services.AddSingleton(config);

            return services;
        }

        /// <summary>
        /// Добавить предопределенную конфигурацию RedbService
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        /// <param name="predefinedConfig">Предопределенная конфигурация</param>
        /// <returns>Коллекция сервисов для цепочки вызовов</returns>
        public static IServiceCollection AddRedbServiceConfiguration(
            this IServiceCollection services,
            RedbServiceConfiguration predefinedConfig)
        {
            services.AddSingleton(predefinedConfig);
            return services;
        }

        /// <summary>
        /// Добавить конфигурацию RedbService по имени профиля
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        /// <param name="profileName">Имя профиля конфигурации</param>
        /// <param name="configureBuilder">Опциональная дополнительная настройка</param>
        /// <returns>Коллекция сервисов для цепочки вызовов</returns>
        public static IServiceCollection AddRedbServiceConfiguration(
            this IServiceCollection services,
            string profileName,
            Action<RedbServiceConfigurationBuilder>? configureBuilder = null)
        {
            var config = PredefinedConfigurations.GetByName(profileName);
            
            if (configureBuilder != null)
            {
                var builder = new RedbServiceConfigurationBuilder(config);
                configureBuilder(builder);
                config = builder.Build();
            }

            services.AddSingleton(config);
            return services;
        }

        /// <summary>
        /// Добавить мониторинг изменений конфигурации RedbService
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        /// <param name="configuration">Конфигурация приложения</param>
        /// <param name="sectionName">Имя секции в конфигурации</param>
        /// <returns>Коллекция сервисов для цепочки вызовов</returns>
        public static IServiceCollection AddRedbServiceConfigurationMonitoring(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "RedbService")
        {
            // Регистрируем мониторинг изменений через IOptionsMonitor
            services.Configure<RedbServiceConfiguration>(
                configuration.GetSection(sectionName));

            // Добавляем валидацию при изменениях
            services.AddSingleton<IValidateOptions<RedbServiceConfiguration>, RedbServiceConfigurationValidator>();

            // Регистрируем сервис для отслеживания изменений
            services.AddSingleton<IRedbServiceConfigurationMonitor, RedbServiceConfigurationMonitor>();

            return services;
        }

        // === ПРИВАТНЫЕ МЕТОДЫ ===

        /// <summary>
        /// Копировать свойства конфигурации
        /// </summary>
        private static void CopyConfigurationProperties(RedbServiceConfiguration source, RedbServiceConfiguration target)
        {
            target.IdResetStrategy = source.IdResetStrategy;
            target.MissingObjectStrategy = source.MissingObjectStrategy;
            target.DefaultCheckPermissionsOnLoad = source.DefaultCheckPermissionsOnLoad;
            target.DefaultCheckPermissionsOnSave = source.DefaultCheckPermissionsOnSave;
            target.DefaultCheckPermissionsOnDelete = source.DefaultCheckPermissionsOnDelete;
            target.DefaultStrictDeleteExtra = source.DefaultStrictDeleteExtra;
            target.AutoSyncSchemesOnSave = source.AutoSyncSchemesOnSave;
            target.DefaultLoadDepth = source.DefaultLoadDepth;
            target.DefaultMaxTreeDepth = source.DefaultMaxTreeDepth;
            target.EnableMetadataCache = source.EnableMetadataCache;
            target.MetadataCacheLifetimeMinutes = source.MetadataCacheLifetimeMinutes;
            target.EnableSchemaValidation = source.EnableSchemaValidation;
            target.EnableDataValidation = source.EnableDataValidation;
            target.AutoSetModifyDate = source.AutoSetModifyDate;
            target.AutoRecomputeHash = source.AutoRecomputeHash;
            // target.DefaultSecurityPriority = source.DefaultSecurityPriority; // Убран
            target.SystemUserId = source.SystemUserId;
            target.JsonOptions.WriteIndented = source.JsonOptions.WriteIndented;
            target.JsonOptions.UseUnsafeRelaxedJsonEscaping = source.JsonOptions.UseUnsafeRelaxedJsonEscaping;
        }
    }

    /// <summary>
    /// Интерфейс для мониторинга изменений конфигурации
    /// </summary>
    public interface IRedbServiceConfigurationMonitor
    {
        /// <summary>
        /// Текущая конфигурация
        /// </summary>
        RedbServiceConfiguration CurrentConfiguration { get; }

        /// <summary>
        /// Событие изменения конфигурации
        /// </summary>
        event Action<RedbServiceConfiguration> ConfigurationChanged;
    }

    /// <summary>
    /// Реализация мониторинга изменений конфигурации
    /// </summary>
    internal class RedbServiceConfigurationMonitor : IRedbServiceConfigurationMonitor, IDisposable
    {
        private readonly IOptionsMonitor<RedbServiceConfiguration> _optionsMonitor;
        private readonly IDisposable? _changeSubscription;

        public RedbServiceConfigurationMonitor(IOptionsMonitor<RedbServiceConfiguration> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor;
            _changeSubscription = _optionsMonitor.OnChange(OnConfigurationChanged);
        }

        public RedbServiceConfiguration CurrentConfiguration => _optionsMonitor.CurrentValue;

        public event Action<RedbServiceConfiguration>? ConfigurationChanged;

        private void OnConfigurationChanged(RedbServiceConfiguration configuration)
        {
            // Валидируем новую конфигурацию
            var validationResult = ConfigurationValidator.Validate(configuration);
            if (validationResult.HasCriticalErrors)
            {
                // Логируем критические ошибки, но не блокируем изменение
                // В реальном приложении здесь должно быть логирование
                Console.WriteLine($"Critical configuration errors detected: {string.Join(", ", validationResult.GetAllMessages())}");
            }

            ConfigurationChanged?.Invoke(configuration);
        }

        public void Dispose()
        {
            _changeSubscription?.Dispose();
        }
    }
}
