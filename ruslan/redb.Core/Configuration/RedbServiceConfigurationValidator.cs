using System;
using System.Linq;
using Microsoft.Extensions.Options;
using redb.Core.Models.Configuration;

namespace redb.Core.Configuration
{
    /// <summary>
    /// Валидатор конфигурации RedbService для интеграции с Options pattern
    /// </summary>
    public class RedbServiceConfigurationValidator : IValidateOptions<RedbServiceConfiguration>
    {
        /// <summary>
        /// Валидировать конфигурацию
        /// </summary>
        /// <param name="name">Имя конфигурации (обычно null для default)</param>
        /// <param name="options">Конфигурация для валидации</param>
        /// <returns>Результат валидации</returns>
        public ValidateOptionsResult Validate(string? name, RedbServiceConfiguration options)
        {
            if (options == null)
            {
                return ValidateOptionsResult.Fail("RedbServiceConfiguration cannot be null");
            }

            var validationResult = ConfigurationValidator.Validate(options);

            if (validationResult.IsValid)
            {
                // Если есть только предупреждения, считаем конфигурацию валидной
                return ValidateOptionsResult.Success;
            }

            // Собираем все ошибки
            var errorMessages = validationResult.Errors
                .Select(e => $"{e.PropertyName}: {e.Message}")
                .ToList();

            // Добавляем предупреждения как информационные сообщения
            var warningMessages = validationResult.Warnings
                .Select(w => $"WARNING - {w.PropertyName}: {w.Message}")
                .ToList();

            var allMessages = errorMessages.Concat(warningMessages);

            return ValidateOptionsResult.Fail(allMessages);
        }
    }

    /// <summary>
    /// Расширенный валидатор с поддержкой автоисправления
    /// </summary>
    public class RedbServiceConfigurationValidatorWithAutoFix : IValidateOptions<RedbServiceConfiguration>
    {
        private readonly bool _autoFixCriticalErrors;

        /// <summary>
        /// Создать валидатор с возможностью автоисправления
        /// </summary>
        /// <param name="autoFixCriticalErrors">Автоматически исправлять критические ошибки</param>
        public RedbServiceConfigurationValidatorWithAutoFix(bool autoFixCriticalErrors = true)
        {
            _autoFixCriticalErrors = autoFixCriticalErrors;
        }

        public ValidateOptionsResult Validate(string? name, RedbServiceConfiguration options)
        {
            if (options == null)
            {
                return ValidateOptionsResult.Fail("RedbServiceConfiguration cannot be null");
            }

            var validationResult = ConfigurationValidator.Validate(options);

            // Если есть критические ошибки и включено автоисправление
            if (validationResult.HasCriticalErrors && _autoFixCriticalErrors)
            {
                // Исправляем критические ошибки in-place
                var fixedConfig = ConfigurationValidator.FixCriticalErrors(options);
                CopyFixedValues(fixedConfig, options);

                // Повторно валидируем исправленную конфигурацию
                validationResult = ConfigurationValidator.Validate(options);
            }

            if (validationResult.IsValid)
            {
                return ValidateOptionsResult.Success;
            }

            // Если остались ошибки после автоисправления
            var errorMessages = validationResult.Errors
                .Where(e => e.Severity != ConfigurationValidationSeverity.Warning)
                .Select(e => $"{e.PropertyName}: {e.Message} (Current: {e.CurrentValue})")
                .ToList();

            if (errorMessages.Any())
            {
                return ValidateOptionsResult.Fail(errorMessages);
            }

            // Только предупреждения - считаем валидным
            return ValidateOptionsResult.Success;
        }

        /// <summary>
        /// Копировать исправленные значения
        /// </summary>
        private static void CopyFixedValues(RedbServiceConfiguration source, RedbServiceConfiguration target)
        {
            target.DefaultLoadDepth = source.DefaultLoadDepth;
            target.DefaultMaxTreeDepth = source.DefaultMaxTreeDepth;
            target.SystemUserId = source.SystemUserId;
            target.MetadataCacheLifetimeMinutes = source.MetadataCacheLifetimeMinutes;
        }
    }

    /// <summary>
    /// Валидатор для конкретных сценариев использования
    /// </summary>
    public class ScenarioBasedConfigurationValidator : IValidateOptions<RedbServiceConfiguration>
    {
        private readonly ConfigurationScenario _expectedScenario;

        /// <summary>
        /// Создать валидатор для конкретного сценария
        /// </summary>
        /// <param name="expectedScenario">Ожидаемый сценарий использования</param>
        public ScenarioBasedConfigurationValidator(ConfigurationScenario expectedScenario)
        {
            _expectedScenario = expectedScenario;
        }

        public ValidateOptionsResult Validate(string? name, RedbServiceConfiguration options)
        {
            if (options == null)
            {
                return ValidateOptionsResult.Fail("RedbServiceConfiguration cannot be null");
            }

            // Базовая валидация
            var baseValidation = ConfigurationValidator.Validate(options);
            if (baseValidation.HasCriticalErrors)
            {
                var criticalErrors = baseValidation.Errors
                    .Where(e => e.Severity == ConfigurationValidationSeverity.Critical)
                    .Select(e => e.Message);
                return ValidateOptionsResult.Fail(criticalErrors);
            }

            // Валидация для конкретного сценария
            var scenarioErrors = ValidateForScenario(options, _expectedScenario);
            if (scenarioErrors.Any())
            {
                return ValidateOptionsResult.Fail(scenarioErrors);
            }

            return ValidateOptionsResult.Success;
        }

        /// <summary>
        /// Валидация для конкретного сценария
        /// </summary>
        private static string[] ValidateForScenario(RedbServiceConfiguration config, ConfigurationScenario scenario)
        {
            var errors = new System.Collections.Generic.List<string>();

            switch (scenario)
            {
                case ConfigurationScenario.Production:
                    if (!config.IsProductionSafe())
                    {
                        errors.Add("Configuration is not safe for production environment");
                    }
                    if (config.JsonOptions.WriteIndented)
                    {
                        errors.Add("WriteIndented should be false in production for performance");
                    }
                    break;

                case ConfigurationScenario.Development:
                    if (config.DefaultCheckPermissionsOnLoad || 
                        config.DefaultCheckPermissionsOnSave || 
                        config.DefaultCheckPermissionsOnDelete)
                    {
                        errors.Add("Permission checks should typically be disabled in development");
                    }
                    break;

                case ConfigurationScenario.HighPerformance:
                    if (!config.IsPerformanceOptimized())
                    {
                        errors.Add("Configuration is not optimized for high performance");
                    }
                    if (config.DefaultLoadDepth > 5)
                    {
                        errors.Add("DefaultLoadDepth should be <= 5 for high performance scenarios");
                    }
                    break;

                case ConfigurationScenario.BulkOperations:
                    if (config.EnableDataValidation)
                    {
                        errors.Add("Data validation should be disabled for bulk operations");
                    }
                    if (config.DefaultLoadDepth > 1)
                    {
                        errors.Add("DefaultLoadDepth should be 1 for bulk operations");
                    }
                    break;
            }

            return errors.ToArray();
        }
    }

    /// <summary>
    /// Сценарии использования конфигурации
    /// </summary>
    public enum ConfigurationScenario
    {
        /// <summary>
        /// Продакшн среда
        /// </summary>
        Production,

        /// <summary>
        /// Среда разработки
        /// </summary>
        Development,

        /// <summary>
        /// Высокая производительность
        /// </summary>
        HighPerformance,

        /// <summary>
        /// Массовые операции
        /// </summary>
        BulkOperations,

        /// <summary>
        /// Интеграционное тестирование
        /// </summary>
        IntegrationTesting,

        /// <summary>
        /// Отладка
        /// </summary>
        Debug
    }
}
