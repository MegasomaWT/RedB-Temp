using System;

namespace redb.Core.Models.Configuration
{
    /// <summary>
    /// Предопределенные конфигурации для различных сценариев использования
    /// </summary>
    public static class PredefinedConfigurations
    {
        /// <summary>
        /// Конфигурация по умолчанию (сбалансированная)
        /// </summary>
        public static RedbServiceConfiguration Default => new RedbServiceConfiguration();

        /// <summary>
        /// Конфигурация для разработки и тестирования
        /// Приоритет: удобство разработки, подробная диагностика
        /// </summary>
        public static RedbServiceConfiguration Development => new RedbServiceConfiguration
        {
            // Отключаем проверки прав для удобства
            DefaultCheckPermissionsOnLoad = false,
            DefaultCheckPermissionsOnSave = false,
            DefaultCheckPermissionsOnDelete = false,

            // Автоматическое восстановление после ошибок
            IdResetStrategy = ObjectIdResetStrategy.AutoCreateNewOnSave,
            MissingObjectStrategy = MissingObjectStrategy.AutoSwitchToInsert,

            // Включаем все валидации для раннего обнаружения проблем
            EnableSchemaValidation = true,
            EnableDataValidation = true,

            // Отключаем кеш для актуальности данных при разработке
            EnableMetadataCache = false,

            // Подробное логирование JSON
            JsonOptions = new JsonSerializationOptions
            {
                WriteIndented = true,
                UseUnsafeRelaxedJsonEscaping = true
            },

            // Стандартные глубины
            DefaultLoadDepth = 10,
            DefaultMaxTreeDepth = 50,

            // Автоматический аудит
            AutoSetModifyDate = true,
            AutoRecomputeHash = true,

            // Системный пользователь для тестов
            SystemUserId = 0,
            // DefaultSecurityPriority убран
        };

        /// <summary>
        /// Конфигурация для продакшена (высокая безопасность)
        /// Приоритет: безопасность, стабильность, контролируемость
        /// </summary>
        public static RedbServiceConfiguration Production => new RedbServiceConfiguration
        {
            // Строгая проверка прав везде
            DefaultCheckPermissionsOnLoad = true,
            DefaultCheckPermissionsOnSave = true,
            DefaultCheckPermissionsOnDelete = true,

            // Консервативная обработка ошибок
            IdResetStrategy = ObjectIdResetStrategy.Manual,
            MissingObjectStrategy = MissingObjectStrategy.ThrowException,

            // Полная валидация
            EnableSchemaValidation = true,
            EnableDataValidation = true,

            // Агрессивное кеширование для производительности
            EnableMetadataCache = true,
            MetadataCacheLifetimeMinutes = 60,

            // Компактный JSON
            JsonOptions = new JsonSerializationOptions
            {
                WriteIndented = false,
                UseUnsafeRelaxedJsonEscaping = true
            },

            // Ограниченные глубины для производительности
            DefaultLoadDepth = 5,
            DefaultMaxTreeDepth = 30,

            // Полный аудит
            AutoSetModifyDate = true,
            AutoRecomputeHash = true,

            // Строгие настройки схем
            DefaultStrictDeleteExtra = true,
            AutoSyncSchemesOnSave = true,

            SystemUserId = 0,
            // DefaultSecurityPriority убран
        };

        /// <summary>
        /// Конфигурация для массовых операций
        /// Приоритет: максимальная скорость, минимум проверок
        /// </summary>
        public static RedbServiceConfiguration BulkOperations => new RedbServiceConfiguration
        {
            // Отключаем все проверки прав
            DefaultCheckPermissionsOnLoad = false,
            DefaultCheckPermissionsOnSave = false,
            DefaultCheckPermissionsOnDelete = false,

            // Автоматическое восстановление для продолжения операций
            IdResetStrategy = ObjectIdResetStrategy.AutoCreateNewOnSave,
            MissingObjectStrategy = MissingObjectStrategy.AutoSwitchToInsert,

            // Отключаем валидацию для скорости
            EnableSchemaValidation = false,
            EnableDataValidation = false,

            // Отключаем кеш (может быть неактуальным при массовых изменениях)
            EnableMetadataCache = false,

            // Минимальный JSON
            JsonOptions = new JsonSerializationOptions
            {
                WriteIndented = false,
                UseUnsafeRelaxedJsonEscaping = true
            },

            // Минимальные глубины
            DefaultLoadDepth = 1,
            DefaultMaxTreeDepth = 1,

            // Отключаем автоматический аудит для скорости
            AutoSetModifyDate = false,
            AutoRecomputeHash = false,

            // Автосинхронизация схем может замедлить массовые операции
            AutoSyncSchemesOnSave = false,
            DefaultStrictDeleteExtra = false,

            SystemUserId = 0,
            // DefaultSecurityPriority убран
        };

        /// <summary>
        /// Конфигурация для высокой производительности
        /// Приоритет: скорость, кеширование, оптимизация
        /// </summary>
        public static RedbServiceConfiguration HighPerformance => new RedbServiceConfiguration
        {
            // Минимальные проверки прав
            DefaultCheckPermissionsOnLoad = false,
            DefaultCheckPermissionsOnSave = false,
            DefaultCheckPermissionsOnDelete = true, // Оставляем для безопасности

            // Умеренное восстановление
            IdResetStrategy = ObjectIdResetStrategy.AutoResetOnDelete,
            MissingObjectStrategy = MissingObjectStrategy.AutoSwitchToInsert,

            // Отключаем валидацию данных, оставляем схемы
            EnableSchemaValidation = true,
            EnableDataValidation = false,

            // Агрессивное кеширование
            EnableMetadataCache = true,
            MetadataCacheLifetimeMinutes = 120,

            // Компактный JSON
            JsonOptions = new JsonSerializationOptions
            {
                WriteIndented = false,
                UseUnsafeRelaxedJsonEscaping = true
            },

            // Оптимальные глубины
            DefaultLoadDepth = 3,
            DefaultMaxTreeDepth = 10,

            // Минимальный аудит
            AutoSetModifyDate = true,
            AutoRecomputeHash = false, // Отключаем для скорости

            // Оптимизированная синхронизация
            AutoSyncSchemesOnSave = true,
            DefaultStrictDeleteExtra = false, // Не удаляем лишние поля для скорости

            SystemUserId = 0,
            // DefaultSecurityPriority убран
        };

        /// <summary>
        /// Конфигурация для отладки
        /// Приоритет: максимум информации, подробная диагностика
        /// </summary>
        public static RedbServiceConfiguration Debug => new RedbServiceConfiguration
        {
            // Отключаем проверки для удобства отладки
            DefaultCheckPermissionsOnLoad = false,
            DefaultCheckPermissionsOnSave = false,
            DefaultCheckPermissionsOnDelete = false,

            // Автовосстановление для продолжения отладки
            IdResetStrategy = ObjectIdResetStrategy.AutoCreateNewOnSave,
            MissingObjectStrategy = MissingObjectStrategy.AutoSwitchToInsert,

            // Максимальная валидация
            EnableSchemaValidation = true,
            EnableDataValidation = true,

            // Отключаем кеш для актуальности
            EnableMetadataCache = false,

            // Подробный JSON для анализа
            JsonOptions = new JsonSerializationOptions
            {
                WriteIndented = true,
                UseUnsafeRelaxedJsonEscaping = true
            },

            // Большие глубины для полной картины
            DefaultLoadDepth = 20,
            DefaultMaxTreeDepth = 100,

            // Полный аудит
            AutoSetModifyDate = true,
            AutoRecomputeHash = true,

            // Подробная синхронизация
            AutoSyncSchemesOnSave = true,
            DefaultStrictDeleteExtra = true,

            SystemUserId = 0,
            // DefaultSecurityPriority убран
        };

        /// <summary>
        /// Конфигурация для интеграционных тестов
        /// Приоритет: предсказуемость, изоляция, воспроизводимость
        /// </summary>
        public static RedbServiceConfiguration IntegrationTesting => new RedbServiceConfiguration
        {
            // Включаем проверки прав для тестирования безопасности
            DefaultCheckPermissionsOnLoad = true,
            DefaultCheckPermissionsOnSave = true,
            DefaultCheckPermissionsOnDelete = true,

            // Строгая обработка ошибок для выявления проблем
            IdResetStrategy = ObjectIdResetStrategy.Manual,
            MissingObjectStrategy = MissingObjectStrategy.ThrowException,

            // Полная валидация
            EnableSchemaValidation = true,
            EnableDataValidation = true,

            // Отключаем кеш для изоляции тестов
            EnableMetadataCache = false,

            // Читаемый JSON для анализа результатов
            JsonOptions = new JsonSerializationOptions
            {
                WriteIndented = true,
                UseUnsafeRelaxedJsonEscaping = true
            },

            // Стандартные глубины
            DefaultLoadDepth = 10,
            DefaultMaxTreeDepth = 50,

            // Полный аудит для проверки
            AutoSetModifyDate = true,
            AutoRecomputeHash = true,

            // Строгая синхронизация
            AutoSyncSchemesOnSave = true,
            DefaultStrictDeleteExtra = true,

            SystemUserId = 0,
            // DefaultSecurityPriority убран
        };

        /// <summary>
        /// Конфигурация для миграции данных
        /// Приоритет: надежность, восстановление, гибкость схем
        /// </summary>
        public static RedbServiceConfiguration DataMigration => new RedbServiceConfiguration
        {
            // Отключаем проверки прав для системных операций
            DefaultCheckPermissionsOnLoad = false,
            DefaultCheckPermissionsOnSave = false,
            DefaultCheckPermissionsOnDelete = false,

            // Максимальная толерантность к ошибкам
            IdResetStrategy = ObjectIdResetStrategy.AutoCreateNewOnSave,
            MissingObjectStrategy = MissingObjectStrategy.AutoSwitchToInsert,

            // Валидация схем важна, данных - нет (могут быть некорректные данные)
            EnableSchemaValidation = true,
            EnableDataValidation = false,

            // Отключаем кеш (схемы могут часто меняться)
            EnableMetadataCache = false,

            // Компактный JSON для экономии места
            JsonOptions = new JsonSerializationOptions
            {
                WriteIndented = false,
                UseUnsafeRelaxedJsonEscaping = true
            },

            // Средние глубины
            DefaultLoadDepth = 5,
            DefaultMaxTreeDepth = 25,

            // Полный аудит для отслеживания миграции
            AutoSetModifyDate = true,
            AutoRecomputeHash = true,

            // Гибкая синхронизация схем
            AutoSyncSchemesOnSave = true,
            DefaultStrictDeleteExtra = false, // Не удаляем поля при миграции

            SystemUserId = 0,
            // DefaultSecurityPriority убран
        };

        /// <summary>
        /// Получить конфигурацию по имени
        /// </summary>
        public static RedbServiceConfiguration GetByName(string name)
        {
            return name.ToLowerInvariant() switch
            {
                "default" => Default,
                "development" => Development,
                "production" => Production,
                "bulk" or "bulkoperations" => BulkOperations,
                "performance" or "highperformance" => HighPerformance,
                "debug" => Debug,
                "test" or "integrationtesting" => IntegrationTesting,
                "migration" or "datamigration" => DataMigration,
                _ => throw new ArgumentException($"Unknown configuration name: {name}")
            };
        }

        /// <summary>
        /// Получить все доступные имена конфигураций
        /// </summary>
        public static string[] GetAvailableNames()
        {
            return new[]
            {
                "Default",
                "Development", 
                "Production",
                "BulkOperations",
                "HighPerformance",
                "Debug",
                "IntegrationTesting",
                "DataMigration"
            };
        }
    }
}
