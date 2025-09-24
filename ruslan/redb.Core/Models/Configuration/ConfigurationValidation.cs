using System;
using System.Collections.Generic;
using System.Linq;

namespace redb.Core.Models.Configuration
{
    /// <summary>
    /// Результат валидации конфигурации
    /// </summary>
    public class ConfigurationValidationResult
    {
        public bool IsValid { get; set; }
        public List<ConfigurationValidationError> Errors { get; set; } = new();
        public List<ConfigurationValidationWarning> Warnings { get; set; } = new();

        /// <summary>
        /// Есть ли критические ошибки
        /// </summary>
        public bool HasCriticalErrors => Errors.Any(e => e.Severity == ConfigurationValidationSeverity.Critical);

        /// <summary>
        /// Есть ли предупреждения
        /// </summary>
        public bool HasWarnings => Warnings.Any();

        /// <summary>
        /// Получить все сообщения
        /// </summary>
        public IEnumerable<string> GetAllMessages()
        {
            foreach (var error in Errors)
                yield return $"ERROR: {error.Message}";
            
            foreach (var warning in Warnings)
                yield return $"WARNING: {warning.Message}";
        }
    }

    /// <summary>
    /// Ошибка валидации конфигурации
    /// </summary>
    public class ConfigurationValidationError
    {
        public string PropertyName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ConfigurationValidationSeverity Severity { get; set; } = ConfigurationValidationSeverity.Error;
        public object? CurrentValue { get; set; }
        public string? SuggestedFix { get; set; }
    }

    /// <summary>
    /// Предупреждение валидации конфигурации
    /// </summary>
    public class ConfigurationValidationWarning
    {
        public string PropertyName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Recommendation { get; set; }
    }

    /// <summary>
    /// Уровень серьезности ошибки валидации конфигурации
    /// </summary>
    public enum ConfigurationValidationSeverity
    {
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Валидатор конфигурации RedbService
    /// </summary>
    public static class ConfigurationValidator
    {
        /// <summary>
        /// Валидировать конфигурацию
        /// </summary>
        public static ConfigurationValidationResult Validate(RedbServiceConfiguration configuration)
        {
            var result = new ConfigurationValidationResult { IsValid = true };

            // Валидация глубины загрузки
            ValidateLoadDepth(configuration, result);

            // Валидация кеширования
            ValidateCaching(configuration, result);

            // Валидация безопасности
            ValidateSecurity(configuration, result);

            // Валидация стратегий
            ValidateStrategies(configuration, result);

            // Валидация JSON настроек
            ValidateJsonOptions(configuration, result);

            // Проверка совместимости настроек
            ValidateCompatibility(configuration, result);

            result.IsValid = !result.HasCriticalErrors;
            return result;
        }

        private static void ValidateLoadDepth(RedbServiceConfiguration config, ConfigurationValidationResult result)
        {
            if (config.DefaultLoadDepth < 1)
            {
                result.Errors.Add(new ConfigurationValidationError
                {
                    PropertyName = nameof(config.DefaultLoadDepth),
                    Message = "DefaultLoadDepth должен быть больше 0",
                    Severity = ConfigurationValidationSeverity.Critical,
                    CurrentValue = config.DefaultLoadDepth,
                    SuggestedFix = "Установите значение >= 1"
                });
            }

            if (config.DefaultLoadDepth > 50)
            {
                result.Warnings.Add(new ConfigurationValidationWarning
                {
                    PropertyName = nameof(config.DefaultLoadDepth),
                    Message = "Большая глубина загрузки может снизить производительность",
                    Recommendation = "Рассмотрите использование значения <= 10 для лучшей производительности"
                });
            }

            if (config.DefaultMaxTreeDepth < 1)
            {
                result.Errors.Add(new ConfigurationValidationError
                {
                    PropertyName = nameof(config.DefaultMaxTreeDepth),
                    Message = "DefaultMaxTreeDepth должен быть больше 0",
                    Severity = ConfigurationValidationSeverity.Critical,
                    CurrentValue = config.DefaultMaxTreeDepth,
                    SuggestedFix = "Установите значение >= 1"
                });
            }

            if (config.DefaultMaxTreeDepth > 100)
            {
                result.Warnings.Add(new ConfigurationValidationWarning
                {
                    PropertyName = nameof(config.DefaultMaxTreeDepth),
                    Message = "Очень большая глубина дерева может вызвать проблемы с производительностью",
                    Recommendation = "Рассмотрите использование значения <= 50"
                });
            }
        }

        private static void ValidateCaching(RedbServiceConfiguration config, ConfigurationValidationResult result)
        {
            if (config.EnableMetadataCache && config.MetadataCacheLifetimeMinutes < 1)
            {
                result.Errors.Add(new ConfigurationValidationError
                {
                    PropertyName = nameof(config.MetadataCacheLifetimeMinutes),
                    Message = "Время жизни кеша должно быть больше 0 минут при включенном кешировании",
                    Severity = ConfigurationValidationSeverity.Error,
                    CurrentValue = config.MetadataCacheLifetimeMinutes,
                    SuggestedFix = "Установите значение >= 1 или отключите кеширование"
                });
            }

            if (config.MetadataCacheLifetimeMinutes > 1440) // 24 часа
            {
                result.Warnings.Add(new ConfigurationValidationWarning
                {
                    PropertyName = nameof(config.MetadataCacheLifetimeMinutes),
                    Message = "Очень долгое время жизни кеша может привести к устареванию данных",
                    Recommendation = "Рассмотрите использование значения <= 120 минут"
                });
            }
        }

        private static void ValidateSecurity(RedbServiceConfiguration config, ConfigurationValidationResult result)
        {
            if (config.SystemUserId < 0)
            {
                result.Errors.Add(new ConfigurationValidationError
                {
                    PropertyName = nameof(config.SystemUserId),
                    Message = "SystemUserId не может быть отрицательным",
                    Severity = ConfigurationValidationSeverity.Error,
                    CurrentValue = config.SystemUserId,
                    SuggestedFix = "Установите значение >= 0"
                });
            }

            // Предупреждение о небезопасной конфигурации
            if (!config.DefaultCheckPermissionsOnLoad && 
                !config.DefaultCheckPermissionsOnSave && 
                !config.DefaultCheckPermissionsOnDelete)
            {
                result.Warnings.Add(new ConfigurationValidationWarning
                {
                    PropertyName = "Security",
                    Message = "Все проверки прав доступа отключены - это может быть небезопасно",
                    Recommendation = "Включите проверки прав для продакшена"
                });
            }
        }

        private static void ValidateStrategies(RedbServiceConfiguration config, ConfigurationValidationResult result)
        {
            // Проверка совместимости стратегий
            if (config.IdResetStrategy == ObjectIdResetStrategy.AutoCreateNewOnSave &&
                config.MissingObjectStrategy == MissingObjectStrategy.ThrowException)
            {
                result.Warnings.Add(new ConfigurationValidationWarning
                {
                    PropertyName = "Strategies",
                    Message = "Конфликт стратегий: AutoCreateNewOnSave + ThrowException может привести к неожиданному поведению",
                    Recommendation = "Используйте MissingObjectStrategy.AutoSwitchToInsert с AutoCreateNewOnSave"
                });
            }
        }

        private static void ValidateJsonOptions(RedbServiceConfiguration config, ConfigurationValidationResult result)
        {
            if (config.JsonOptions.WriteIndented)
            {
                result.Warnings.Add(new ConfigurationValidationWarning
                {
                    PropertyName = nameof(config.JsonOptions.WriteIndented),
                    Message = "Форматированный JSON увеличивает размер данных",
                    Recommendation = "Отключите WriteIndented для продакшена"
                });
            }
        }

        private static void ValidateCompatibility(RedbServiceConfiguration config, ConfigurationValidationResult result)
        {
            // Проверка конфигурации для высокой производительности
            if (!config.EnableSchemaValidation && !config.EnableDataValidation && config.EnableMetadataCache)
            {
                // Это нормально для высокопроизводительных сценариев
                result.Warnings.Add(new ConfigurationValidationWarning
                {
                    PropertyName = "Performance",
                    Message = "Конфигурация оптимизирована для производительности (валидация отключена)",
                    Recommendation = "Убедитесь, что данные валидны на уровне приложения"
                });
            }

            // Проверка конфигурации для разработки
            if (config.IdResetStrategy == ObjectIdResetStrategy.AutoCreateNewOnSave &&
                config.MissingObjectStrategy == MissingObjectStrategy.AutoSwitchToInsert &&
                !config.DefaultCheckPermissionsOnLoad)
            {
                result.Warnings.Add(new ConfigurationValidationWarning
                {
                    PropertyName = "Development",
                    Message = "Конфигурация выглядит как настройка для разработки",
                    Recommendation = "Не используйте эту конфигурацию в продакшене"
                });
            }
        }

        /// <summary>
        /// Быстрая проверка критических ошибок
        /// </summary>
        public static bool HasCriticalErrors(RedbServiceConfiguration configuration)
        {
            return configuration.DefaultLoadDepth < 1 ||
                   configuration.DefaultMaxTreeDepth < 1 ||
                   configuration.SystemUserId < 0 ||
                   configuration.EnableMetadataCache && configuration.MetadataCacheLifetimeMinutes < 1;
        }

        /// <summary>
        /// Исправить критические ошибки автоматически
        /// </summary>
        public static RedbServiceConfiguration FixCriticalErrors(RedbServiceConfiguration configuration)
        {
            var fixedConfig = configuration.Clone();

            if (fixedConfig.DefaultLoadDepth < 1)
                fixedConfig.DefaultLoadDepth = 10;

            if (fixedConfig.DefaultMaxTreeDepth < 1)
                fixedConfig.DefaultMaxTreeDepth = 50;

            if (fixedConfig.SystemUserId < 0)
                fixedConfig.SystemUserId = 0;

            if (fixedConfig.EnableMetadataCache && fixedConfig.MetadataCacheLifetimeMinutes < 1)
                fixedConfig.MetadataCacheLifetimeMinutes = 30;

            return fixedConfig;
        }
    }
}
