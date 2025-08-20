using System;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using redb.Core.Models.Configuration;

namespace redb.Core.Configuration
{
    /// <summary>
    /// Extension методы для интеграции RedbServiceConfiguration с IConfiguration
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Получить конфигурацию RedbService из IConfiguration
        /// </summary>
        /// <param name="configuration">Конфигурация приложения</param>
        /// <param name="sectionName">Имя секции в конфигурации (по умолчанию "RedbService")</param>
        /// <returns>Настроенная конфигурация RedbService</returns>
        public static RedbServiceConfiguration GetRedbServiceConfiguration(
            this IConfiguration configuration, 
            string sectionName = "RedbService")
        {
            var section = configuration.GetSection(sectionName);
            
            if (!section.Exists())
            {
                // Если секция не существует, возвращаем конфигурацию по умолчанию
                return new RedbServiceConfiguration();
            }

            // Поддержка профилей
            var profileName = section["Profile"];
            var baseConfig = GetBaseConfigurationFromProfile(profileName);
            
            // Применение основных настроек из конфигурации
            ApplyConfigurationSettings(section, baseConfig);
            
            // Применение переопределений
            var overridesSection = section.GetSection("Overrides");
            if (overridesSection.Exists())
            {
                ApplyConfigurationSettings(overridesSection, baseConfig);
            }
            
            return baseConfig;
        }

        /// <summary>
        /// Получить конфигурацию RedbService с валидацией
        /// </summary>
        /// <param name="configuration">Конфигурация приложения</param>
        /// <param name="sectionName">Имя секции в конфигурации</param>
        /// <param name="throwOnValidationError">Выбрасывать исключение при ошибках валидации</param>
        /// <returns>Валидированная конфигурация RedbService</returns>
        public static RedbServiceConfiguration GetValidatedRedbServiceConfiguration(
            this IConfiguration configuration,
            string sectionName = "RedbService",
            bool throwOnValidationError = true)
        {
            var config = configuration.GetRedbServiceConfiguration(sectionName);
            
            var validationResult = ConfigurationValidator.Validate(config);
            
            if (!validationResult.IsValid)
            {
                if (throwOnValidationError)
                {
                    var errors = string.Join(Environment.NewLine, validationResult.GetAllMessages());
                    throw new InvalidOperationException($"Invalid RedbService configuration:{Environment.NewLine}{errors}");
                }
                
                // Автоматическое исправление критических ошибок
                config = ConfigurationValidator.FixCriticalErrors(config);
            }
            
            return config;
        }

        /// <summary>
        /// Проверить, существует ли секция конфигурации RedbService
        /// </summary>
        /// <param name="configuration">Конфигурация приложения</param>
        /// <param name="sectionName">Имя секции в конфигурации</param>
        /// <returns>true, если секция существует</returns>
        public static bool HasRedbServiceConfiguration(
            this IConfiguration configuration,
            string sectionName = "RedbService")
        {
            return configuration.GetSection(sectionName).Exists();
        }

        /// <summary>
        /// Получить описание конфигурации из IConfiguration
        /// </summary>
        /// <param name="configuration">Конфигурация приложения</param>
        /// <param name="sectionName">Имя секции в конфигурации</param>
        /// <returns>Описание конфигурации</returns>
        public static string GetRedbServiceConfigurationDescription(
            this IConfiguration configuration,
            string sectionName = "RedbService")
        {
            var config = configuration.GetRedbServiceConfiguration(sectionName);
            return config.GetDescription();
        }

        /// <summary>
        /// Создать builder на основе конфигурации из IConfiguration
        /// </summary>
        /// <param name="configuration">Конфигурация приложения</param>
        /// <param name="sectionName">Имя секции в конфигурации</param>
        /// <returns>Builder для дальнейшей настройки</returns>
        public static RedbServiceConfigurationBuilder CreateRedbServiceBuilder(
            this IConfiguration configuration,
            string sectionName = "RedbService")
        {
            var baseConfig = configuration.GetRedbServiceConfiguration(sectionName);
            return new RedbServiceConfigurationBuilder(baseConfig);
        }

        // === ПРИВАТНЫЕ МЕТОДЫ ===

        /// <summary>
        /// Получить базовую конфигурацию из профиля
        /// </summary>
        private static RedbServiceConfiguration GetBaseConfigurationFromProfile(string? profileName)
        {
            if (string.IsNullOrEmpty(profileName))
            {
                return new RedbServiceConfiguration();
            }

            try
            {
                return PredefinedConfigurations.GetByName(profileName);
            }
            catch (ArgumentException)
            {
                // Если профиль не найден, используем конфигурацию по умолчанию
                return new RedbServiceConfiguration();
            }
        }

        /// <summary>
        /// Применить настройки из секции конфигурации
        /// </summary>
        private static void ApplyConfigurationSettings(IConfigurationSection section, RedbServiceConfiguration config)
        {
            // Настройки удаления объектов
            if (section["IdResetStrategy"] != null)
            {
                if (Enum.TryParse<ObjectIdResetStrategy>(section["IdResetStrategy"], true, out var idResetStrategy))
                {
                    config.IdResetStrategy = idResetStrategy;
                }
            }

            if (section["MissingObjectStrategy"] != null)
            {
                if (Enum.TryParse<MissingObjectStrategy>(section["MissingObjectStrategy"], true, out var missingObjectStrategy))
                {
                    config.MissingObjectStrategy = missingObjectStrategy;
                }
            }

            // Настройки безопасности
            if (section["DefaultCheckPermissionsOnLoad"] != null)
            {
                config.DefaultCheckPermissionsOnLoad = section.GetValue<bool>("DefaultCheckPermissionsOnLoad");
            }

            if (section["DefaultCheckPermissionsOnSave"] != null)
            {
                config.DefaultCheckPermissionsOnSave = section.GetValue<bool>("DefaultCheckPermissionsOnSave");
            }

            if (section["DefaultCheckPermissionsOnDelete"] != null)
            {
                config.DefaultCheckPermissionsOnDelete = section.GetValue<bool>("DefaultCheckPermissionsOnDelete");
            }

            // Настройки схем
            if (section["DefaultStrictDeleteExtra"] != null)
            {
                config.DefaultStrictDeleteExtra = section.GetValue<bool>("DefaultStrictDeleteExtra");
            }

            if (section["AutoSyncSchemesOnSave"] != null)
            {
                config.AutoSyncSchemesOnSave = section.GetValue<bool>("AutoSyncSchemesOnSave");
            }

            // Настройки загрузки
            if (section["DefaultLoadDepth"] != null)
            {
                config.DefaultLoadDepth = section.GetValue<int>("DefaultLoadDepth");
            }

            if (section["DefaultMaxTreeDepth"] != null)
            {
                config.DefaultMaxTreeDepth = section.GetValue<int>("DefaultMaxTreeDepth");
            }

            // Настройки производительности
            if (section["EnableMetadataCache"] != null)
            {
                config.EnableMetadataCache = section.GetValue<bool>("EnableMetadataCache");
            }

            if (section["MetadataCacheLifetimeMinutes"] != null)
            {
                config.MetadataCacheLifetimeMinutes = section.GetValue<int>("MetadataCacheLifetimeMinutes");
            }

            // Настройки валидации
            if (section["EnableSchemaValidation"] != null)
            {
                config.EnableSchemaValidation = section.GetValue<bool>("EnableSchemaValidation");
            }

            if (section["EnableDataValidation"] != null)
            {
                config.EnableDataValidation = section.GetValue<bool>("EnableDataValidation");
            }

            // Настройки аудита
            if (section["AutoSetModifyDate"] != null)
            {
                config.AutoSetModifyDate = section.GetValue<bool>("AutoSetModifyDate");
            }

            if (section["AutoRecomputeHash"] != null)
            {
                config.AutoRecomputeHash = section.GetValue<bool>("AutoRecomputeHash");
            }

            // Настройки контекста безопасности
            if (section["DefaultSecurityPriority"] != null)
            {
                // DefaultSecurityPriority убран - используется простая логика GetEffectiveUser()
            }

            if (section["SystemUserId"] != null)
            {
                config.SystemUserId = section.GetValue<long>("SystemUserId");
            }

            // Настройки JSON
            var jsonSection = section.GetSection("JsonOptions");
            if (jsonSection.Exists())
            {
                if (jsonSection["WriteIndented"] != null)
                {
                    config.JsonOptions.WriteIndented = jsonSection.GetValue<bool>("WriteIndented");
                }

                if (jsonSection["UseUnsafeRelaxedJsonEscaping"] != null)
                {
                    config.JsonOptions.UseUnsafeRelaxedJsonEscaping = jsonSection.GetValue<bool>("UseUnsafeRelaxedJsonEscaping");
                }
            }
        }
    }
}
