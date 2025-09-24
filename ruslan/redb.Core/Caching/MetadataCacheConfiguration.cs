//using System;
//using System.Collections.Generic;

//namespace redb.Core.Caching
//{
//    /// <summary>
//    /// Конфигурация системы кеширования метаданных
//    /// </summary>
//    public class MetadataCacheConfiguration
//    {
//        /// <summary>
//        /// Включено ли кеширование метаданных
//        /// </summary>
//        public bool EnableMetadataCache { get; set; } = true;
        
//        /// <summary>
//        /// Тип реализации кеша метаданных
//        /// </summary>
//        public MetadataCacheType CacheType { get; set; } = MetadataCacheType.InMemory;
        
//        /// <summary>
//        /// Конфигурация кеширования схем
//        /// </summary>
//        public SchemeCacheConfiguration Schemes { get; set; } = new();
        
//        /// <summary>
//        /// Конфигурация кеширования структур
//        /// </summary>
//        public StructureCacheConfiguration Structures { get; set; } = new();
        
//        /// <summary>
//        /// Конфигурация кеширования типов
//        /// </summary>
//        public TypeCacheConfiguration Types { get; set; } = new();
        
//        /// <summary>
//        /// Конфигурация композитного кеша
//        /// </summary>
//        public CompositeCacheConfiguration Composite { get; set; } = new();
        
//        /// <summary>
//        /// Конфигурация предварительной загрузки (warm-up)
//        /// </summary>
//        public CacheWarmupConfiguration Warmup { get; set; } = new();
        
//        /// <summary>
//        /// Конфигурация мониторинга и диагностики
//        /// </summary>
//        public CacheMonitoringConfiguration Monitoring { get; set; } = new();
        
//        /// <summary>
//        /// Дополнительные настройки производительности
//        /// </summary>
//        public CachePerformanceConfiguration Performance { get; set; } = new();
        
//        /// <summary>
//        /// Создать конфигурацию по умолчанию для development окружения
//        /// </summary>
//        public static MetadataCacheConfiguration Development()
//        {
//            return new MetadataCacheConfiguration
//            {
//                EnableMetadataCache = true,
//                CacheType = MetadataCacheType.InMemory,
//                Schemes = new SchemeCacheConfiguration { MaxItems = 1000, LifetimeMinutes = 60 },
//                Structures = new StructureCacheConfiguration { MaxItems = 10000, LifetimeMinutes = 30 },
//                Types = new TypeCacheConfiguration { MaxItems = 100, LifetimeMinutes = 120 },
//                Monitoring = new CacheMonitoringConfiguration { EnableDetailedStats = true },
//                Performance = new CachePerformanceConfiguration { ConcurrencyLevel = 4 }
//            };
//        }
        
//        /// <summary>
//        /// Создать конфигурацию по умолчанию для production окружения
//        /// </summary>
//        public static MetadataCacheConfiguration Production()
//        {
//            return new MetadataCacheConfiguration
//            {
//                EnableMetadataCache = true,
//                CacheType = MetadataCacheType.InMemory,
//                Schemes = new SchemeCacheConfiguration { MaxItems = 5000, LifetimeMinutes = 240 },
//                Structures = new StructureCacheConfiguration { MaxItems = 50000, LifetimeMinutes = 120 },
//                Types = new TypeCacheConfiguration { MaxItems = 200, LifetimeMinutes = 1440 }, // 24 часа
//                Warmup = new CacheWarmupConfiguration { EnableWarmup = true, WarmupTimeoutSeconds = 30 },
//                Monitoring = new CacheMonitoringConfiguration { EnablePerformanceCounters = true },
//                Performance = new CachePerformanceConfiguration { ConcurrencyLevel = Environment.ProcessorCount }
//            };
//        }
//    }
    
//    /// <summary>
//    /// Типы реализации кеша метаданных
//    /// </summary>
//    public enum MetadataCacheType
//    {
//        /// <summary>
//        /// Кеш в памяти приложения (по умолчанию)
//        /// </summary>
//        InMemory,
        
//        /// <summary>
//        /// Статический кеш в RedbObject (предложение пользователя)
//        /// </summary>
//        StaticInRedbObject,
        
//        /// <summary>
//        /// Гибридный кеш (in-memory + distributed)
//        /// </summary>
//        Hybrid,
        
//        /// <summary>
//        /// Distributed кеш (Redis и т.д.) - для будущих версий
//        /// </summary>
//        Distributed,
        
//        /// <summary>
//        /// Кеш отключен (прямое обращение к БД)
//        /// </summary>
//        None
//    }
    
//    /// <summary>
//    /// Базовая конфигурация компонента кеша
//    /// </summary>
//    public abstract class BaseCacheConfiguration
//    {
//        /// <summary>
//        /// Максимальное количество элементов в кеше
//        /// </summary>
//        public int MaxItems { get; set; } = 1000;
        
//        /// <summary>
//        /// Время жизни элементов в кеше в минутах
//        /// </summary>
//        public int LifetimeMinutes { get; set; } = 60;
        
//        /// <summary>
//        /// Включить статистику для этого компонента
//        /// </summary>
//        public bool EnableStatistics { get; set; } = true;
        
//        /// <summary>
//        /// Стратегия удаления при превышении лимита
//        /// </summary>
//        public CacheEvictionStrategy EvictionStrategy { get; set; } = CacheEvictionStrategy.LRU;
        
//        /// <summary>
//        /// Интервал очистки устаревших элементов в минутах
//        /// </summary>
//        public int CleanupIntervalMinutes { get; set; } = 15;
//    }
    
//    /// <summary>
//    /// Конфигурация кеша схем
//    /// </summary>
//    public class SchemeCacheConfiguration : BaseCacheConfiguration
//    {
//        public SchemeCacheConfiguration()
//        {
//            MaxItems = 1000;
//            LifetimeMinutes = 240; // 4 часа - схемы изменяются редко
//        }
        
//        /// <summary>
//        /// Включить кеширование привязок .NET тип -> схема
//        /// </summary>
//        public bool EnableTypeToSchemeMapping { get; set; } = true;
        
//        /// <summary>
//        /// Предварительно загружать часто используемые схемы
//        /// </summary>
//        public bool PreloadPopularSchemes { get; set; } = true;
        
//        /// <summary>
//        /// Список имен схем для предварительной загрузки
//        /// </summary>
//        public List<string> PreloadSchemes { get; set; } = new();
//    }
    
//    /// <summary>
//    /// Конфигурация кеша структур
//    /// </summary>
//    public class StructureCacheConfiguration : BaseCacheConfiguration
//    {
//        public StructureCacheConfiguration()
//        {
//            MaxItems = 10000;
//            LifetimeMinutes = 120; // 2 часа
//        }
        
//        /// <summary>
//        /// Включить кеширование карт имя структуры -> структура по схемам
//        /// </summary>
//        public bool EnableNameMapping { get; set; } = true;
        
//        /// <summary>
//        /// Включить кеширование полных наборов структур по схемам
//        /// </summary>
//        public bool EnableSchemeStructureSets { get; set; } = true;
        
//        /// <summary>
//        /// Максимальное количество структур в одной схеме для кеширования набором
//        /// </summary>
//        public int MaxStructuresPerScheme { get; set; } = 200;
//    }
    
//    /// <summary>
//    /// Конфигурация кеша типов
//    /// </summary>
//    public class TypeCacheConfiguration : BaseCacheConfiguration
//    {
//        public TypeCacheConfiguration()
//        {
//            MaxItems = 100;
//            LifetimeMinutes = 1440; // 24 часа - типы практически не изменяются
//        }
        
//        /// <summary>
//        /// Загружать все типы сразу при инициализации
//        /// </summary>
//        public bool LoadAllTypesOnStartup { get; set; } = true;
        
//        /// <summary>
//        /// Включить кеширование карт имя -> тип
//        /// </summary>
//        public bool EnableNameToTypeMapping { get; set; } = true;
        
//        /// <summary>
//        /// Включить кеширование дополнительной информации о типах
//        /// </summary>
//        public bool EnableTypeMetadata { get; set; } = true;
//    }
    
//    /// <summary>
//    /// Конфигурация композитного кеша
//    /// </summary>
//    public class CompositeCacheConfiguration
//    {
//        /// <summary>
//        /// Максимальное количество полных метаданных схем
//        /// </summary>
//        public int MaxCompleteMetadata { get; set; } = 500;
        
//        /// <summary>
//        /// Время жизни композитных метаданных в минутах
//        /// </summary>
//        public int CompleteMetadataLifetimeMinutes { get; set; } = 60;
        
//        /// <summary>
//        /// Включить автоматическую сборку композитных метаданных
//        /// </summary>
//        public bool EnableAutoComposition { get; set; } = true;
        
//        /// <summary>
//        /// Включить кеширование привязок .NET тип -> полные метаданные
//        /// </summary>
//        public bool EnableTypeToMetadataMapping { get; set; } = true;
//    }
    
//    /// <summary>
//    /// Конфигурация предварительной загрузки кеша
//    /// </summary>
//    public class CacheWarmupConfiguration
//    {
//        /// <summary>
//        /// Включить предварительную загрузку кеша
//        /// </summary>
//        public bool EnableWarmup { get; set; } = false;
        
//        /// <summary>
//        /// Время ожидания предварительной загрузки в секундах
//        /// </summary>
//        public int WarmupTimeoutSeconds { get; set; } = 30;
        
//        /// <summary>
//        /// Загружать все типы при старте
//        /// </summary>
//        public bool WarmupAllTypes { get; set; } = true;
        
//        /// <summary>
//        /// Загружать популярные схемы при старте
//        /// </summary>
//        public bool WarmupPopularSchemes { get; set; } = false;
        
//        /// <summary>
//        /// Список .NET типов для предварительной загрузки
//        /// </summary>
//        public List<string> WarmupTypes { get; set; } = new();
        
//        /// <summary>
//        /// Запускать предварительную загрузку в фоне
//        /// </summary>
//        public bool WarmupInBackground { get; set; } = true;
//    }
    
//    /// <summary>
//    /// Конфигурация мониторинга и диагностики кеша
//    /// </summary>
//    public class CacheMonitoringConfiguration
//    {
//        /// <summary>
//        /// Включить детальную статистику
//        /// </summary>
//        public bool EnableDetailedStats { get; set; } = false;
        
//        /// <summary>
//        /// Включить счетчики производительности
//        /// </summary>
//        public bool EnablePerformanceCounters { get; set; } = false;
        
//        /// <summary>
//        /// Включить логирование операций кеша
//        /// </summary>
//        public bool EnableCacheLogging { get; set; } = false;
        
//        /// <summary>
//        /// Уровень логирования кеша
//        /// </summary>
//        public CacheLogLevel LogLevel { get; set; } = CacheLogLevel.Warning;
        
//        /// <summary>
//        /// Интервал сбора метрик в секундах
//        /// </summary>
//        public int MetricsCollectionIntervalSeconds { get; set; } = 60;
        
//        /// <summary>
//        /// Включить экспорт метрик
//        /// </summary>
//        public bool EnableMetricsExport { get; set; } = false;
//    }
    
//    /// <summary>
//    /// Конфигурация производительности кеша
//    /// </summary>
//    public class CachePerformanceConfiguration
//    {
//        /// <summary>
//        /// Уровень параллелизма для ConcurrentDictionary
//        /// </summary>
//        public int ConcurrencyLevel { get; set; } = Environment.ProcessorCount;
        
//        /// <summary>
//        /// Начальная емкость коллекций
//        /// </summary>
//        public int InitialCapacity { get; set; } = 1000;
        
//        /// <summary>
//        /// Включить компрессию данных в кеше
//        /// </summary>
//        public bool EnableCompression { get; set; } = false;
        
//        /// <summary>
//        /// Использовать слабые ссылки для редко используемых данных
//        /// </summary>
//        public bool UseWeakReferences { get; set; } = false;
        
//        /// <summary>
//        /// Максимальное время ожидания блокировки в миллисекундах
//        /// </summary>
//        public int MaxLockTimeoutMs { get; set; } = 1000;
        
//        /// <summary>
//        /// Включить пулы объектов для часто создаваемых типов
//        /// </summary>
//        public bool EnableObjectPooling { get; set; } = false;
//    }
    
//    /// <summary>
//    /// Стратегии удаления элементов из кеша
//    /// </summary>
//    public enum CacheEvictionStrategy
//    {
//        /// <summary>
//        /// Least Recently Used - удалять давно не использовавшиеся
//        /// </summary>
//        LRU,
        
//        /// <summary>
//        /// Least Frequently Used - удалять редко используемые
//        /// </summary>
//        LFU,
        
//        /// <summary>
//        /// First In First Out - удалять старые по времени создания
//        /// </summary>
//        FIFO,
        
//        /// <summary>
//        /// Random - случайное удаление
//        /// </summary>
//        Random,
        
//        /// <summary>
//        /// Time To Live - удалять по истечении времени жизни
//        /// </summary>
//        TTL
//    }
    
//    /// <summary>
//    /// Уровни логирования кеша
//    /// </summary>
//    public enum CacheLogLevel
//    {
//        /// <summary>
//        /// Логировать все операции
//        /// </summary>
//        Debug,
        
//        /// <summary>
//        /// Логировать важные операции
//        /// </summary>
//        Information,
        
//        /// <summary>
//        /// Логировать предупреждения
//        /// </summary>
//        Warning,
        
//        /// <summary>
//        /// Логировать только ошибки
//        /// </summary>
//        Error,
        
//        /// <summary>
//        /// Отключить логирование
//        /// </summary>
//        None
//    }
//}
