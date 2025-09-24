using System;
using redb.Core.Models.Configuration;

namespace redb.Core.Extensions
{
    /// <summary>
    /// Extension методы для работы с конфигурацией RedbService
    /// </summary>
    public static class RedbServiceConfigurationExtensions
    {
        /// <summary>
        /// Создать builder для конфигурации
        /// </summary>
        public static RedbServiceConfigurationBuilder CreateBuilder(this RedbServiceConfiguration configuration)
        {
            return new RedbServiceConfigurationBuilder(configuration);
        }

        /// <summary>
        /// Клонировать конфигурацию
        /// </summary>
        public static RedbServiceConfiguration Clone(this RedbServiceConfiguration source)
        {
            return new RedbServiceConfiguration
            {
                // Настройки удаления объектов
                IdResetStrategy = source.IdResetStrategy,
                MissingObjectStrategy = source.MissingObjectStrategy,

                // Настройки безопасности
                DefaultCheckPermissionsOnLoad = source.DefaultCheckPermissionsOnLoad,
                DefaultCheckPermissionsOnSave = source.DefaultCheckPermissionsOnSave,
                DefaultCheckPermissionsOnDelete = source.DefaultCheckPermissionsOnDelete,

                // Настройки схем
                DefaultStrictDeleteExtra = source.DefaultStrictDeleteExtra,
                AutoSyncSchemesOnSave = source.AutoSyncSchemesOnSave,

                // Настройки загрузки
                DefaultLoadDepth = source.DefaultLoadDepth,
                DefaultMaxTreeDepth = source.DefaultMaxTreeDepth,

                // Настройки производительности
                EnableMetadataCache = source.EnableMetadataCache,
                MetadataCacheLifetimeMinutes = source.MetadataCacheLifetimeMinutes,

                // Настройки валидации
                EnableSchemaValidation = source.EnableSchemaValidation,
                EnableDataValidation = source.EnableDataValidation,

                // Настройки аудита
                AutoSetModifyDate = source.AutoSetModifyDate,
                AutoRecomputeHash = source.AutoRecomputeHash,

                // Настройки контекста безопасности
                // DefaultSecurityPriority убран,
                SystemUserId = source.SystemUserId,

                // Настройки сериализации
                JsonOptions = new JsonSerializationOptions
                {
                    WriteIndented = source.JsonOptions.WriteIndented,
                    UseUnsafeRelaxedJsonEscaping = source.JsonOptions.UseUnsafeRelaxedJsonEscaping
                }
            };
        }

        /// <summary>
        /// Объединить конфигурации (target перезаписывается значениями из source)
        /// </summary>
        public static RedbServiceConfiguration MergeWith(this RedbServiceConfiguration target, RedbServiceConfiguration source)
        {
            var result = target.Clone();

            // Объединяем только не-default значения
            if (source.IdResetStrategy != ObjectIdResetStrategy.Manual)
                result.IdResetStrategy = source.IdResetStrategy;

            if (source.MissingObjectStrategy != MissingObjectStrategy.ThrowException)
                result.MissingObjectStrategy = source.MissingObjectStrategy;

            // Безопасность - всегда объединяем
            result.DefaultCheckPermissionsOnLoad = source.DefaultCheckPermissionsOnLoad;
            result.DefaultCheckPermissionsOnSave = source.DefaultCheckPermissionsOnSave;
            result.DefaultCheckPermissionsOnDelete = source.DefaultCheckPermissionsOnDelete;

            // Остальные настройки
            result.DefaultStrictDeleteExtra = source.DefaultStrictDeleteExtra;
            result.AutoSyncSchemesOnSave = source.AutoSyncSchemesOnSave;
            result.DefaultLoadDepth = source.DefaultLoadDepth;
            result.DefaultMaxTreeDepth = source.DefaultMaxTreeDepth;
            result.EnableMetadataCache = source.EnableMetadataCache;
            result.MetadataCacheLifetimeMinutes = source.MetadataCacheLifetimeMinutes;
            result.EnableSchemaValidation = source.EnableSchemaValidation;
            result.EnableDataValidation = source.EnableDataValidation;
            result.AutoSetModifyDate = source.AutoSetModifyDate;
            result.AutoRecomputeHash = source.AutoRecomputeHash;
            // result.DefaultSecurityPriority = source.DefaultSecurityPriority; // Убран
            result.SystemUserId = source.SystemUserId;

            // JSON настройки
            result.JsonOptions.WriteIndented = source.JsonOptions.WriteIndented;
            result.JsonOptions.UseUnsafeRelaxedJsonEscaping = source.JsonOptions.UseUnsafeRelaxedJsonEscaping;

            return result;
        }

        /// <summary>
        /// Проверить, является ли конфигурация безопасной для продакшена
        /// </summary>
        public static bool IsProductionSafe(this RedbServiceConfiguration configuration)
        {
            return configuration.DefaultCheckPermissionsOnLoad &&
                   configuration.DefaultCheckPermissionsOnSave &&
                   configuration.DefaultCheckPermissionsOnDelete &&
                   configuration.EnableSchemaValidation &&
                   configuration.EnableDataValidation &&
                   configuration.MissingObjectStrategy == MissingObjectStrategy.ThrowException;
        }

        /// <summary>
        /// Проверить, является ли конфигурация оптимизированной для производительности
        /// </summary>
        public static bool IsPerformanceOptimized(this RedbServiceConfiguration configuration)
        {
            return !configuration.DefaultCheckPermissionsOnLoad &&
                   !configuration.DefaultCheckPermissionsOnSave &&
                   !configuration.EnableSchemaValidation &&
                   !configuration.EnableDataValidation &&
                   configuration.DefaultLoadDepth <= 3 &&
                   configuration.EnableMetadataCache;
        }

        /// <summary>
        /// Получить описание конфигурации
        /// </summary>
        public static string GetDescription(this RedbServiceConfiguration configuration)
        {
            var features = new System.Collections.Generic.List<string>();

            // Безопасность
            if (configuration.DefaultCheckPermissionsOnLoad || 
                configuration.DefaultCheckPermissionsOnSave || 
                configuration.DefaultCheckPermissionsOnDelete)
            {
                features.Add("Security enabled");
            }

            // Стратегии
            if (configuration.IdResetStrategy != ObjectIdResetStrategy.Manual)
                features.Add($"ID Reset: {configuration.IdResetStrategy}");

            if (configuration.MissingObjectStrategy != MissingObjectStrategy.ThrowException)
                features.Add($"Missing Objects: {configuration.MissingObjectStrategy}");

            // Производительность
            if (configuration.EnableMetadataCache)
                features.Add($"Cache: {configuration.MetadataCacheLifetimeMinutes}min");

            if (configuration.DefaultLoadDepth != 10)
                features.Add($"Load Depth: {configuration.DefaultLoadDepth}");

            // Валидация
            if (!configuration.EnableSchemaValidation || !configuration.EnableDataValidation)
                features.Add("Validation disabled");

            return string.Join(", ", features);
        }

        /// <summary>
        /// Применить временную конфигурацию с автоматическим восстановлением
        /// </summary>
        public static IDisposable ApplyTemporary(this IRedbService service, RedbServiceConfiguration temporaryConfig)
        {
            var originalConfig = service.Configuration.Clone();
            service.UpdateConfiguration(config => 
            {
                // Копируем все свойства из temporaryConfig
                config.IdResetStrategy = temporaryConfig.IdResetStrategy;
                config.MissingObjectStrategy = temporaryConfig.MissingObjectStrategy;
                config.DefaultCheckPermissionsOnLoad = temporaryConfig.DefaultCheckPermissionsOnLoad;
                config.DefaultCheckPermissionsOnSave = temporaryConfig.DefaultCheckPermissionsOnSave;
                config.DefaultCheckPermissionsOnDelete = temporaryConfig.DefaultCheckPermissionsOnDelete;
                config.DefaultLoadDepth = temporaryConfig.DefaultLoadDepth;
                config.DefaultMaxTreeDepth = temporaryConfig.DefaultMaxTreeDepth;
                config.EnableMetadataCache = temporaryConfig.EnableMetadataCache;
                config.MetadataCacheLifetimeMinutes = temporaryConfig.MetadataCacheLifetimeMinutes;
                config.EnableSchemaValidation = temporaryConfig.EnableSchemaValidation;
                config.EnableDataValidation = temporaryConfig.EnableDataValidation;
                config.AutoSetModifyDate = temporaryConfig.AutoSetModifyDate;
                config.AutoRecomputeHash = temporaryConfig.AutoRecomputeHash;
                // config.DefaultSecurityPriority = temporaryConfig.DefaultSecurityPriority; // Убран
                config.SystemUserId = temporaryConfig.SystemUserId;
                config.JsonOptions = temporaryConfig.JsonOptions;
            });
            
            return new TemporaryConfigurationScope(service, originalConfig);
        }

        /// <summary>
        /// Применить временную конфигурацию через builder
        /// </summary>
        public static IDisposable ApplyTemporary(this IRedbService service, Func<RedbServiceConfigurationBuilder, RedbServiceConfigurationBuilder> configure)
        {
            var builder = new RedbServiceConfigurationBuilder(service.Configuration);
            var temporaryConfig = configure(builder).Build();
            
            return service.ApplyTemporary(temporaryConfig);
        }
    }

    /// <summary>
    /// Scope для временной конфигурации
    /// </summary>
    internal class TemporaryConfigurationScope : IDisposable
    {
        private readonly IRedbService _service;
        private readonly RedbServiceConfiguration _originalConfiguration;
        private bool _disposed = false;

        public TemporaryConfigurationScope(IRedbService service, RedbServiceConfiguration originalConfiguration)
        {
            _service = service;
            _originalConfiguration = originalConfiguration;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _service.UpdateConfiguration(config => 
                {
                    // Восстанавливаем все свойства из originalConfiguration
                    config.IdResetStrategy = _originalConfiguration.IdResetStrategy;
                    config.MissingObjectStrategy = _originalConfiguration.MissingObjectStrategy;
                    config.DefaultCheckPermissionsOnLoad = _originalConfiguration.DefaultCheckPermissionsOnLoad;
                    config.DefaultCheckPermissionsOnSave = _originalConfiguration.DefaultCheckPermissionsOnSave;
                    config.DefaultCheckPermissionsOnDelete = _originalConfiguration.DefaultCheckPermissionsOnDelete;
                    config.DefaultLoadDepth = _originalConfiguration.DefaultLoadDepth;
                    config.DefaultMaxTreeDepth = _originalConfiguration.DefaultMaxTreeDepth;
                    config.EnableMetadataCache = _originalConfiguration.EnableMetadataCache;
                    config.MetadataCacheLifetimeMinutes = _originalConfiguration.MetadataCacheLifetimeMinutes;
                    config.EnableSchemaValidation = _originalConfiguration.EnableSchemaValidation;
                    config.EnableDataValidation = _originalConfiguration.EnableDataValidation;
                    config.AutoSetModifyDate = _originalConfiguration.AutoSetModifyDate;
                    config.AutoRecomputeHash = _originalConfiguration.AutoRecomputeHash;
                    // config.DefaultSecurityPriority = _originalConfiguration.DefaultSecurityPriority; // Убран
                    config.SystemUserId = _originalConfiguration.SystemUserId;
                    config.JsonOptions = _originalConfiguration.JsonOptions;
                });
                _disposed = true;
            }
        }
    }
}
