using redb.Core.DBModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace redb.Core.Caching
{
    /// <summary>
    /// Композитный кеш метаданных объединяющий схемы, структуры и типы
    /// Предоставляет единый интерфейс для всех операций кеширования метаданных
    /// </summary>
    public interface ICompositeMetadataCache
    {
        // === КОМПОНЕНТЫ КЕША ===
        
        /// <summary>
        /// Кеш схем объектов
        /// </summary>
        ISchemeMetadataCache Schemes { get; }
        
        /// <summary>
        /// Кеш структур полей
        /// </summary>
        IStructureMetadataCache Structures { get; }
        
        /// <summary>
        /// Кеш типов данных
        /// </summary>
        ITypeMetadataCache Types { get; }
        
        // === КОМПОЗИТНЫЕ ОПЕРАЦИИ ===
        
        /// <summary>
        /// Получить полные метаданные для .NET типа (схема + структуры + типы)
        /// Наиболее часто используемая операция - одним вызовом получаем все необходимое
        /// </summary>
        /// <typeparam name="TProps">Тип свойств объекта</typeparam>
        /// <returns>Полные метаданные или null если схема не найдена</returns>
        Task<CompleteSchemeMetadata?> GetCompleteMetadataAsync<TProps>() where TProps : class;
        
        /// <summary>
        /// Получить полные метаданные для .NET типа
        /// </summary>
        /// <param name="type">Тип объекта</param>
        /// <returns>Полные метаданные или null если схема не найдена</returns>
        Task<CompleteSchemeMetadata?> GetCompleteMetadataAsync(Type type);
        
        /// <summary>
        /// Получить полные метаданные по ID схемы
        /// </summary>
        /// <param name="schemeId">ID схемы</param>
        /// <returns>Полные метаданные или null если схема не найдена</returns>
        Task<CompleteSchemeMetadata?> GetCompleteMetadataAsync(long schemeId);
        
        /// <summary>
        /// Установить полные метаданные в кеш
        /// </summary>
        /// <param name="metadata">Полные метаданные для кеширования</param>
        void SetCompleteMetadata(CompleteSchemeMetadata metadata);
        
        /// <summary>
        /// Установить полные метаданные в кеш с привязкой к типу
        /// </summary>
        /// <typeparam name="TProps">Тип для привязки</typeparam>
        /// <param name="metadata">Полные метаданные для кеширования</param>
        void SetCompleteMetadataForType<TProps>(CompleteSchemeMetadata metadata) where TProps : class;
        
        // === МАССОВЫЕ ОПЕРАЦИИ ===
        
        /// <summary>
        /// Предварительно загрузить (прогреть) кеш для списка типов
        /// </summary>
        /// <param name="types">Типы для предварительной загрузки</param>
        /// <param name="loadFromDatabase">Функция загрузки из БД</param>
        Task WarmupCacheAsync(Type[] types, Func<Type, Task<CompleteSchemeMetadata?>> loadFromDatabase);
        
        /// <summary>
        /// Предварительно загрузить кеш для всех схем
        /// </summary>
        /// <param name="loadFromDatabase">Функция загрузки из БД</param>
        Task WarmupAllSchemesAsync(Func<Task<List<CompleteSchemeMetadata>>> loadFromDatabase);
        
        // === ИНВАЛИДАЦИЯ ===
        
        /// <summary>
        /// Инвалидировать все связанные кеши для схемы
        /// Удаляет схему, её структуры и очищает привязки к типам
        /// </summary>
        /// <param name="schemeId">ID схемы</param>
        void InvalidateSchemeCompletely(long schemeId);
        
        /// <summary>
        /// Инвалидировать кеш для .NET типа
        /// </summary>
        /// <typeparam name="TProps">Тип для инвалидации</typeparam>
        void InvalidateTypeCompletely<TProps>() where TProps : class;
        
        /// <summary>
        /// Инвалидировать кеш для .NET типа
        /// </summary>
        /// <param name="type">Тип для инвалидации</param>
        void InvalidateTypeCompletely(Type type);
        
        /// <summary>
        /// Полная очистка всех кешей
        /// </summary>
        void InvalidateAll();
        
        // === СТАТИСТИКА ===
        
        /// <summary>
        /// Получить объединенную статистику всех кешей
        /// </summary>
        /// <returns>Сводная статистика</returns>
        CompositeMetadataCacheStatistics GetStatistics();
        
        /// <summary>
        /// Получить детальную статистику каждого компонента
        /// </summary>
        /// <returns>Детальная статистика по компонентам</returns>
        DetailedCacheStatistics GetDetailedStatistics();
        
        // === ДИАГНОСТИКА ===
        
        /// <summary>
        /// Получить информацию о состоянии кеша для диагностики
        /// </summary>
        /// <returns>Диагностическая информация</returns>
        CacheDiagnosticInfo GetDiagnosticInfo();
        
        /// <summary>
        /// Экспортировать состояние кеша для анализа
        /// </summary>
        /// <returns>Экспортированное состояние кеша</returns>
        CacheExportData ExportCacheState();
    }
    
    /// <summary>
    /// Полные метаданные схемы включающие схему, структуры и типы
    /// </summary>
    public class CompleteSchemeMetadata
    {
        /// <summary>
        /// Схема объекта
        /// </summary>
        public _RScheme Scheme { get; set; } = null!;
        
        /// <summary>
        /// Все структуры схемы упорядоченные по Order
        /// </summary>
        public List<_RStructure> Structures { get; set; } = new();
        
        /// <summary>
        /// Карта имя структуры -> структура для быстрого поиска
        /// </summary>
        public Dictionary<string, _RStructure> StructuresByName { get; set; } = new();
        
        /// <summary>
        /// Карта ID структуры -> структура для быстрого поиска
        /// </summary>
        public Dictionary<long, _RStructure> StructuresById { get; set; } = new();
        
        /// <summary>
        /// Все типы используемые в структурах
        /// </summary>
        public Dictionary<long, _RType> Types { get; set; } = new();
        
        /// <summary>
        /// .NET тип связанный с этой схемой (если есть)
        /// </summary>
        public Type? AssociatedType { get; set; }
        
        /// <summary>
        /// Время создания метаданных
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Время последнего использования
        /// </summary>
        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Счетчик использований
        /// </summary>
        public long UsageCount { get; set; }
        
        /// <summary>
        /// Приблизительный размер в байтах
        /// </summary>
        public long EstimatedSizeBytes { get; set; }
        
        /// <summary>
        /// Валидны ли метаданные (для проверки консистентности)
        /// </summary>
        public bool IsValid => Scheme != null && Structures.Any();
        
        /// <summary>
        /// Обновить время последнего использования
        /// </summary>
        public void MarkAsUsed()
        {
            LastUsedAt = DateTime.UtcNow;
            UsageCount++;
        }
    }
    
    /// <summary>
    /// Сводная статистика композитного кеша
    /// </summary>
    public class CompositeMetadataCacheStatistics
    {
        /// <summary>
        /// Общее количество попаданий во все кеши
        /// </summary>
        public long TotalHits { get; set; }
        
        /// <summary>
        /// Общее количество промахов во всех кешах
        /// </summary>
        public long TotalMisses { get; set; }
        
        /// <summary>
        /// Общее количество запросов
        /// </summary>
        public long TotalRequests => TotalHits + TotalMisses;
        
        /// <summary>
        /// Общий процент попаданий в кеш
        /// </summary>
        public double OverallHitRatio => TotalRequests > 0 ? (double)TotalHits / TotalRequests : 0.0;
        
        /// <summary>
        /// Общий размер всех кешей в байтах
        /// </summary>
        public long TotalSizeBytes { get; set; }
        
        /// <summary>
        /// Общее количество кешированных элементов
        /// </summary>
        public int TotalCachedItems { get; set; }
        
        /// <summary>
        /// Время создания статистики
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Статистика по типам операций
        /// </summary>
        public Dictionary<string, long> OperationStats { get; set; } = new();
        
        /// <summary>
        /// Топ наиболее используемых схем
        /// </summary>
        public List<TopUsedScheme> TopUsedSchemes { get; set; } = new();
    }
    
    /// <summary>
    /// Детальная статистика по каждому компоненту кеша
    /// </summary>
    public class DetailedCacheStatistics
    {
        /// <summary>
        /// Статистика кеша схем
        /// </summary>
        public SchemeCacheStatistics SchemeStats { get; set; } = new();
        
        /// <summary>
        /// Статистика кеша структур
        /// </summary>
        public StructureCacheStatistics StructureStats { get; set; } = new();
        
        /// <summary>
        /// Статистика кеша типов
        /// </summary>
        public TypeCacheStatistics TypeStats { get; set; } = new();
        
        /// <summary>
        /// Сводная статистика
        /// </summary>
        public CompositeMetadataCacheStatistics CompositeStats { get; set; } = new();
    }
    
    /// <summary>
    /// Информация о наиболее используемой схеме
    /// </summary>
    public class TopUsedScheme
    {
        /// <summary>
        /// ID схемы
        /// </summary>
        public long SchemeId { get; set; }
        
        /// <summary>
        /// Имя схемы
        /// </summary>
        public string SchemeName { get; set; } = "";
        
        /// <summary>
        /// Связанный .NET тип (если есть)
        /// </summary>
        public string? AssociatedTypeName { get; set; }
        
        /// <summary>
        /// Количество использований
        /// </summary>
        public long UsageCount { get; set; }
        
        /// <summary>
        /// Время последнего использования
        /// </summary>
        public DateTime LastUsedAt { get; set; }
    }
    
    /// <summary>
    /// Диагностическая информация о состоянии кеша
    /// </summary>
    public class CacheDiagnosticInfo
    {
        /// <summary>
        /// Информация о здоровье кеша
        /// </summary>
        public CacheHealthStatus HealthStatus { get; set; }
        
        /// <summary>
        /// Потенциальные проблемы
        /// </summary>
        public List<string> Issues { get; set; } = new();
        
        /// <summary>
        /// Рекомендации по оптимизации
        /// </summary>
        public List<string> Recommendations { get; set; } = new();
        
        /// <summary>
        /// Информация о памяти
        /// </summary>
        public MemoryUsageInfo MemoryInfo { get; set; } = new();
        
        /// <summary>
        /// Информация о производительности
        /// </summary>
        public PerformanceInfo PerformanceInfo { get; set; } = new();
    }
    
    /// <summary>
    /// Статус здоровья кеша
    /// </summary>
    public enum CacheHealthStatus
    {
        Healthy,
        Warning,
        Critical,
        Unknown
    }
    
    /// <summary>
    /// Информация об использовании памяти кешем
    /// </summary>
    public class MemoryUsageInfo
    {
        /// <summary>
        /// Используемая память в байтах
        /// </summary>
        public long UsedBytes { get; set; }
        
        /// <summary>
        /// Максимальная память в байтах (если ограничена)
        /// </summary>
        public long? MaxBytes { get; set; }
        
        /// <summary>
        /// Процент использования памяти
        /// </summary>
        public double UsagePercentage { get; set; }
        
        /// <summary>
        /// Фрагментация памяти (если доступна информация)
        /// </summary>
        public double? FragmentationPercentage { get; set; }
    }
    
    /// <summary>
    /// Информация о производительности кеша
    /// </summary>
    public class PerformanceInfo
    {
        /// <summary>
        /// Среднее время доступа к кешу в миллисекундах
        /// </summary>
        public double AverageAccessTimeMs { get; set; }
        
        /// <summary>
        /// Среднее время загрузки из источника данных в миллисекундах
        /// </summary>
        public double AverageLoadTimeMs { get; set; }
        
        /// <summary>
        /// Количество операций в секунду
        /// </summary>
        public double OperationsPerSecond { get; set; }
        
        /// <summary>
        /// Пиковое время доступа в миллисекундах
        /// </summary>
        public double PeakAccessTimeMs { get; set; }
    }
    
    /// <summary>
    /// Экспортированное состояние кеша для анализа
    /// </summary>
    public class CacheExportData
    {
        /// <summary>
        /// Время экспорта
        /// </summary>
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Версия формата экспорта
        /// </summary>
        public string FormatVersion { get; set; } = "1.0";
        
        /// <summary>
        /// Экспортированные схемы
        /// </summary>
        public List<_RScheme> Schemes { get; set; } = new();
        
        /// <summary>
        /// Экспортированные структуры
        /// </summary>
        public List<_RStructure> Structures { get; set; } = new();
        
        /// <summary>
        /// Экспортированные типы
        /// </summary>
        public List<_RType> Types { get; set; } = new();
        
        /// <summary>
        /// Привязки типов к схемам
        /// </summary>
        public Dictionary<string, long> TypeToSchemeMapping { get; set; } = new();
        
        /// <summary>
        /// Статистика использования
        /// </summary>
        public DetailedCacheStatistics Statistics { get; set; } = new();
    }
}
