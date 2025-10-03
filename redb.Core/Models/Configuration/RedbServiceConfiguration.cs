using System;
using System.Text.Json.Serialization;
using redb.Core.Configuration;
using redb.Core.Caching;

namespace redb.Core.Models.Configuration
{
    /// <summary>
    /// Конфигурация поведения RedbService
    /// </summary>
    public class RedbServiceConfiguration
    {
        // === НАСТРОЙКИ УДАЛЕНИЯ ОБЪЕКТОВ ===

        /// <summary>
        /// Стратегия обработки ID после удаления объекта
        /// </summary>
        [JsonConverter(typeof(ObjectIdResetStrategyJsonConverter))]
        public ObjectIdResetStrategy IdResetStrategy { get; set; } = ObjectIdResetStrategy.Manual;

        /// <summary>
        /// Стратегия обработки несуществующих объектов при UPDATE
        /// </summary>
        [JsonConverter(typeof(MissingObjectStrategyJsonConverter))]
        public MissingObjectStrategy MissingObjectStrategy { get; set; } = MissingObjectStrategy.AutoSwitchToInsert;

        // === НАСТРОЙКИ БЕЗОПАСНОСТИ ПО УМОЛЧАНИЮ ===

        /// <summary>
        /// Проверять права доступа по умолчанию при загрузке объектов
        /// </summary>
        public bool DefaultCheckPermissionsOnLoad { get; set; } = false;

        /// <summary>
        /// Проверять права доступа по умолчанию при сохранении объектов
        /// </summary>
        public bool DefaultCheckPermissionsOnSave { get; set; } = false;

        /// <summary>
        /// Проверять права доступа по умолчанию при удалении объектов
        /// </summary>
        public bool DefaultCheckPermissionsOnDelete { get; set; } = true;

        /// <summary>
        /// Проверять права доступа по умолчанию при выполнении запросов
        /// </summary>
        public bool DefaultCheckPermissionsOnQuery { get; set; } = false;

        // === НАСТРОЙКИ СХЕМ ===

        /// <summary>
        /// Строго удалять лишние поля при синхронизации схем по умолчанию
        /// </summary>
        public bool DefaultStrictDeleteExtra { get; set; } = true;

        /// <summary>
        /// Автоматически синхронизировать схемы при сохранении объектов
        /// </summary>
        public bool AutoSyncSchemesOnSave { get; set; } = true;

        // === НАСТРОЙКИ ЗАГРУЗКИ ОБЪЕКТОВ ===

        /// <summary>
        /// Глубина загрузки вложенных объектов по умолчанию
        /// </summary>
        public int DefaultLoadDepth { get; set; } = 10;

        /// <summary>
        /// Максимальная глубина для древовидных структур
        /// </summary>
        public int DefaultMaxTreeDepth { get; set; } = 50;

        // === НАСТРОЙКИ ПРОИЗВОДИТЕЛЬНОСТИ ===

        /// <summary>
        /// Включить кеширование метаданных схем (УСТАРЕВШИЙ - используйте MetadataCache)
        /// </summary>
        //[Obsolete("Используйте MetadataCache.EnableMetadataCache вместо этого свойства")]
        public bool EnableMetadataCache { get; set; } = true;

        /// <summary>
        /// Время жизни кеша метаданных (в минутах) (УСТАРЕВШИЙ - используйте MetadataCache)
        /// </summary>
        //[Obsolete("Используйте MetadataCache.Schemes.LifetimeMinutes вместо этого свойства")]
        public int MetadataCacheLifetimeMinutes { get; set; } = 30;

        // === НАСТРОЙКИ КЕШИРОВАНИЯ МЕТАДАННЫХ (НОВЫЕ) ===

        /// <summary>
        /// Расширенная конфигурация кеширования метаданных
        /// </summary>
        //public MetadataCacheConfiguration MetadataCache { get; set; } = new();

        // === НАСТРОЙКИ ВАЛИДАЦИИ ===

        /// <summary>
        /// Включить валидацию схем перед синхронизацией
        /// </summary>
        public bool EnableSchemaValidation { get; set; } = true;

        /// <summary>
        /// Включить валидацию данных при сохранении
        /// </summary>
        public bool EnableDataValidation { get; set; } = true;

        // === НАСТРОЙКИ АУДИТА ===

        /// <summary>
        /// Автоматически устанавливать дату изменения при сохранении
        /// </summary>
        public bool AutoSetModifyDate { get; set; } = true;

        /// <summary>
        /// Автоматически пересчитывать хеш при сохранении
        /// </summary>
        public bool AutoRecomputeHash { get; set; } = true;

        // === НАСТРОЙКИ КОНТЕКСТА БЕЗОПАСНОСТИ ===

        // Приоритет контекста безопасности убран - используется простая логика GetEffectiveUser()

        /// <summary>
        /// ID системного пользователя для операций без проверки прав
        /// </summary>
        public long SystemUserId { get; set; } = 0;

        // === НАСТРОЙКИ EAV СОХРАНЕНИЯ ===

        /// <summary>
        /// Стратегия сохранения EAV свойств
        /// </summary>
        [JsonConverter(typeof(EavSaveStrategyJsonConverter))]
        public EavSaveStrategy EavSaveStrategy { get; set; } = EavSaveStrategy.DeleteInsert;





        // === НАСТРОЙКИ СЕРИАЛИЗАЦИИ ===

        /// <summary>
        /// Настройки JSON сериализации для массивов
        /// </summary>
        public JsonSerializationOptions JsonOptions { get; set; } = new JsonSerializationOptions();

        // === МЕТОДЫ ===

        /// <summary>
        /// Создать копию конфигурации
        /// </summary>
        public RedbServiceConfiguration Clone()
        {
            return new RedbServiceConfiguration
            {
                IdResetStrategy = IdResetStrategy,
                MissingObjectStrategy = MissingObjectStrategy,
                DefaultCheckPermissionsOnLoad = DefaultCheckPermissionsOnLoad,
                DefaultCheckPermissionsOnSave = DefaultCheckPermissionsOnSave,
                DefaultCheckPermissionsOnDelete = DefaultCheckPermissionsOnDelete,
                DefaultStrictDeleteExtra = DefaultStrictDeleteExtra,
                AutoSyncSchemesOnSave = AutoSyncSchemesOnSave,
                DefaultLoadDepth = DefaultLoadDepth,
                DefaultMaxTreeDepth = DefaultMaxTreeDepth,
                EnableMetadataCache = EnableMetadataCache,
                MetadataCacheLifetimeMinutes = MetadataCacheLifetimeMinutes,
                //MetadataCache = MetadataCache, // Новые настройки кеширования
                EnableSchemaValidation = EnableSchemaValidation,
                EnableDataValidation = EnableDataValidation,
                AutoSetModifyDate = AutoSetModifyDate,
                AutoRecomputeHash = AutoRecomputeHash,
                // DefaultSecurityPriority убран,
                SystemUserId = SystemUserId,
                JsonOptions = new JsonSerializationOptions
                {
                    WriteIndented = JsonOptions.WriteIndented,
                    UseUnsafeRelaxedJsonEscaping = JsonOptions.UseUnsafeRelaxedJsonEscaping
                }
            };
        }

        /// <summary>
        /// Получить описание конфигурации
        /// </summary>
        public string GetDescription()
        {
            return $"RedbService Configuration: " +
                   $"LoadDepth={DefaultLoadDepth}, " +
                   $"TreeDepth={DefaultMaxTreeDepth}, " +
                   $"Cache={EnableMetadataCache}, " +
                   // $"Security={DefaultSecurityPriority}, " +
                   $"IdReset={IdResetStrategy}, " +
                   $"MissingObj={MissingObjectStrategy}";
        }

        /// <summary>
        /// Проверить, безопасна ли конфигурация для продакшена
        /// </summary>
        public bool IsProductionSafe()
        {
            return DefaultCheckPermissionsOnLoad &&
                   DefaultCheckPermissionsOnSave &&
                   DefaultCheckPermissionsOnDelete &&
                   EnableSchemaValidation &&
                   EnableDataValidation &&
                   DefaultLoadDepth <= 10 &&
                   !JsonOptions.WriteIndented;
        }

        /// <summary>
        /// Проверить, оптимизирована ли конфигурация для производительности
        /// </summary>
        public bool IsPerformanceOptimized()
        {
            return !DefaultCheckPermissionsOnLoad &&
                   !DefaultCheckPermissionsOnSave &&
                   !DefaultCheckPermissionsOnDelete &&
                   (EnableMetadataCache/* || MetadataCache.EnableMetadataCache*/) && // Поддержка новых и старых настроек
                   DefaultLoadDepth <= 5 &&
                   DefaultMaxTreeDepth <= 10 &&
                   !JsonOptions.WriteIndented;
                   //&& MetadataCache.Warmup.EnableWarmup; // Дополнительная проверка для производительности
        }
    }

    /// <summary>
    /// Стратегия обработки ID объекта после удаления
    /// </summary>
    public enum ObjectIdResetStrategy
    {
        /// <summary>
        /// Ручной сброс ID (текущее поведение)
        /// </summary>
        Manual,

        /// <summary>
        /// Автоматический сброс ID при удалении через DeleteAsync(RedbObject)
        /// </summary>
        AutoResetOnDelete,

        /// <summary>
        /// Автоматическое создание нового объекта при попытке сохранить удаленный
        /// </summary>
        AutoCreateNewOnSave
    }

    /// <summary>
    /// Стратегия обработки несуществующих объектов при UPDATE
    /// </summary>
    public enum MissingObjectStrategy
    {
        /// <summary>
        /// Выбросить исключение (текущее поведение)
        /// </summary>
        ThrowException,

        /// <summary>
        /// Автоматически переключиться на INSERT
        /// </summary>
        AutoSwitchToInsert,

        /// <summary>
        /// Вернуть null/false без ошибки
        /// </summary>
        ReturnNull
    }

    /// <summary>
    /// Стратегия сохранения EAV свойств
    /// </summary>
    public enum EavSaveStrategy
    {
        /// <summary>
        /// Простая стратегия - всегда DELETE + INSERT всех свойств
        /// Надежная, но неэффективная для больших объектов
        /// </summary>
        DeleteInsert,
        
        /// <summary>
        /// Эффективная стратегия - сравнение с БД и обновление только измененных свойств
        /// Рекомендуется по умолчанию
        /// </summary>
        ChangeTracking
    }

    /// <summary>
    /// Настройки JSON сериализации
    /// </summary>
    public class JsonSerializationOptions
    {
        /// <summary>
        /// Форматировать JSON с отступами
        /// </summary>
        public bool WriteIndented { get; set; } = false;

        /// <summary>
        /// Использовать безопасное кодирование JavaScript
        /// </summary>
        public bool UseUnsafeRelaxedJsonEscaping { get; set; } = true;
    }
}
