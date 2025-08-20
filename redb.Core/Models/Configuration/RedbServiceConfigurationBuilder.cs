using System;

namespace redb.Core.Models.Configuration
{
    /// <summary>
    /// Builder для удобной настройки конфигурации RedbService
    /// </summary>
    public class RedbServiceConfigurationBuilder
    {
        private RedbServiceConfiguration _configuration;

        public RedbServiceConfigurationBuilder()
        {
            _configuration = new RedbServiceConfiguration();
        }

        public RedbServiceConfigurationBuilder(RedbServiceConfiguration baseConfiguration)
        {
            _configuration = baseConfiguration ?? new RedbServiceConfiguration();
        }

        // === НАСТРОЙКИ УДАЛЕНИЯ ОБЪЕКТОВ ===

        /// <summary>
        /// Настроить стратегию обработки ID после удаления объекта
        /// </summary>
        public RedbServiceConfigurationBuilder WithIdResetStrategy(ObjectIdResetStrategy strategy)
        {
            _configuration.IdResetStrategy = strategy;
            return this;
        }

        /// <summary>
        /// Настроить стратегию обработки несуществующих объектов при UPDATE
        /// </summary>
        public RedbServiceConfigurationBuilder WithMissingObjectStrategy(MissingObjectStrategy strategy)
        {
            _configuration.MissingObjectStrategy = strategy;
            return this;
        }

        // === НАСТРОЙКИ БЕЗОПАСНОСТИ ===

        /// <summary>
        /// Настроить проверку прав доступа по умолчанию
        /// </summary>
        public RedbServiceConfigurationBuilder WithDefaultPermissions(
            bool checkOnLoad = false, 
            bool checkOnSave = false, 
            bool checkOnDelete = true)
        {
            _configuration.DefaultCheckPermissionsOnLoad = checkOnLoad;
            _configuration.DefaultCheckPermissionsOnSave = checkOnSave;
            _configuration.DefaultCheckPermissionsOnDelete = checkOnDelete;
            return this;
        }

        /// <summary>
        /// Включить строгую безопасность (проверка прав везде)
        /// </summary>
        public RedbServiceConfigurationBuilder WithStrictSecurity()
        {
            return WithDefaultPermissions(checkOnLoad: true, checkOnSave: true, checkOnDelete: true);
        }

        /// <summary>
        /// Отключить проверку прав (для разработки/тестирования)
        /// </summary>
        public RedbServiceConfigurationBuilder WithoutPermissionChecks()
        {
            return WithDefaultPermissions(checkOnLoad: false, checkOnSave: false, checkOnDelete: false);
        }

        /// <summary>
        /// Настроить ID системного пользователя
        /// </summary>
        public RedbServiceConfigurationBuilder WithSystemUser(long systemUserId)
        {
            _configuration.SystemUserId = systemUserId;
            return this;
        }

        /// <summary>
        /// Настроить приоритет контекста безопасности
        /// </summary>
        // public RedbServiceConfigurationBuilder WithSecurityPriority(SecurityContextPriority priority)
        // {
        //     _configuration.DefaultSecurityPriority = priority;
        //     return this;
        // }

        // === НАСТРОЙКИ СХЕМ ===

        /// <summary>
        /// Настроить поведение синхронизации схем
        /// </summary>
        public RedbServiceConfigurationBuilder WithSchemaSync(
            bool strictDeleteExtra = true, 
            bool autoSyncOnSave = true)
        {
            _configuration.DefaultStrictDeleteExtra = strictDeleteExtra;
            _configuration.AutoSyncSchemesOnSave = autoSyncOnSave;
            return this;
        }

        // === НАСТРОЙКИ ЗАГРУЗКИ ===

        /// <summary>
        /// Настроить глубину загрузки объектов
        /// </summary>
        public RedbServiceConfigurationBuilder WithLoadDepth(int defaultDepth = 10, int maxTreeDepth = 50)
        {
            _configuration.DefaultLoadDepth = defaultDepth;
            _configuration.DefaultMaxTreeDepth = maxTreeDepth;
            return this;
        }

        // === НАСТРОЙКИ ПРОИЗВОДИТЕЛЬНОСТИ ===

        /// <summary>
        /// Настроить кеширование метаданных
        /// </summary>
        public RedbServiceConfigurationBuilder WithMetadataCache(
            bool enabled = true, 
            int lifetimeMinutes = 30)
        {
            _configuration.EnableMetadataCache = enabled;
            _configuration.MetadataCacheLifetimeMinutes = lifetimeMinutes;
            return this;
        }

        /// <summary>
        /// Отключить кеширование (для отладки)
        /// </summary>
        public RedbServiceConfigurationBuilder WithoutCache()
        {
            return WithMetadataCache(enabled: false);
        }

        // === НАСТРОЙКИ ВАЛИДАЦИИ ===

        /// <summary>
        /// Настроить валидацию
        /// </summary>
        public RedbServiceConfigurationBuilder WithValidation(
            bool schemaValidation = true, 
            bool dataValidation = true)
        {
            _configuration.EnableSchemaValidation = schemaValidation;
            _configuration.EnableDataValidation = dataValidation;
            return this;
        }

        /// <summary>
        /// Отключить валидацию (для производительности)
        /// </summary>
        public RedbServiceConfigurationBuilder WithoutValidation()
        {
            return WithValidation(schemaValidation: false, dataValidation: false);
        }

        // === НАСТРОЙКИ АУДИТА ===

        /// <summary>
        /// Настроить автоматический аудит
        /// </summary>
        public RedbServiceConfigurationBuilder WithAudit(
            bool autoSetModifyDate = true, 
            bool autoRecomputeHash = true)
        {
            _configuration.AutoSetModifyDate = autoSetModifyDate;
            _configuration.AutoRecomputeHash = autoRecomputeHash;
            return this;
        }

        // === НАСТРОЙКИ JSON ===

        /// <summary>
        /// Настроить JSON сериализацию
        /// </summary>
        public RedbServiceConfigurationBuilder WithJsonOptions(Action<JsonSerializationOptions> configure)
        {
            configure(_configuration.JsonOptions);
            return this;
        }

        /// <summary>
        /// Включить красивое форматирование JSON
        /// </summary>
        public RedbServiceConfigurationBuilder WithPrettyJson()
        {
            _configuration.JsonOptions.WriteIndented = true;
            return this;
        }

        // === ПРЕДУСТАНОВЛЕННЫЕ КОНФИГУРАЦИИ ===

        /// <summary>
        /// Конфигурация для разработки/тестирования
        /// </summary>
        public RedbServiceConfigurationBuilder ForDevelopment()
        {
            return WithoutPermissionChecks()
                .WithIdResetStrategy(ObjectIdResetStrategy.AutoCreateNewOnSave)
                .WithMissingObjectStrategy(MissingObjectStrategy.AutoSwitchToInsert)
                .WithValidation(schemaValidation: true, dataValidation: true)
                .WithPrettyJson()
                .WithMetadataCache(enabled: false); // Отключаем кеш для отладки
        }

        /// <summary>
        /// Конфигурация для продакшена (высокая безопасность)
        /// </summary>
        public RedbServiceConfigurationBuilder ForProduction()
        {
            return WithStrictSecurity()
                .WithIdResetStrategy(ObjectIdResetStrategy.Manual)
                .WithMissingObjectStrategy(MissingObjectStrategy.ThrowException)
                .WithValidation(schemaValidation: true, dataValidation: true)
                .WithLoadDepth(defaultDepth: 5, maxTreeDepth: 30) // Меньше для производительности
                .WithMetadataCache(enabled: true, lifetimeMinutes: 60);
        }

        /// <summary>
        /// Конфигурация для массовых операций
        /// </summary>
        public RedbServiceConfigurationBuilder ForBulkOperations()
        {
            return WithoutPermissionChecks()
                .WithIdResetStrategy(ObjectIdResetStrategy.AutoCreateNewOnSave)
                .WithMissingObjectStrategy(MissingObjectStrategy.AutoSwitchToInsert)
                .WithoutValidation()
                .WithLoadDepth(defaultDepth: 1, maxTreeDepth: 1)
                .WithoutCache();
        }

        /// <summary>
        /// Конфигурация для высокой производительности
        /// </summary>
        public RedbServiceConfigurationBuilder ForHighPerformance()
        {
            return WithoutPermissionChecks()
                .WithoutValidation()
                .WithLoadDepth(defaultDepth: 3, maxTreeDepth: 10)
                .WithMetadataCache(enabled: true, lifetimeMinutes: 120)
                .WithAudit(autoSetModifyDate: false, autoRecomputeHash: false);
        }

        /// <summary>
        /// Построить конфигурацию
        /// </summary>
        public RedbServiceConfiguration Build()
        {
            return _configuration;
        }

        /// <summary>
        /// Применить дополнительную настройку
        /// </summary>
        public RedbServiceConfigurationBuilder Configure(Action<RedbServiceConfiguration> configure)
        {
            configure(_configuration);
            return this;
        }

        /// <summary>
        /// Настройка для интеграционного тестирования
        /// </summary>
        public RedbServiceConfigurationBuilder ForIntegrationTesting()
        {
            _configuration = PredefinedConfigurations.IntegrationTesting.Clone();
            return this;
        }
    }
}
