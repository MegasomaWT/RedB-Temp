using redb.Core.DBModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace redb.Core.Caching
{
    /// <summary>
    /// Интерфейс для статического кеша метаданных в RedbObject
    /// Реализует предложение пользователя по хранению кеша в статических полях
    /// </summary>
    public interface IStaticMetadataCache
    {
        /// <summary>
        /// Получить схему для .NET типа из статического кеша
        /// </summary>
        /// <typeparam name="TProps">Тип свойств объекта</typeparam>
        /// <returns>Схема или null если не найдена</returns>
        _RScheme? GetSchemeForType<TProps>() where TProps : class;
        
        /// <summary>
        /// Получить схему для .NET типа из статического кеша
        /// </summary>
        /// <param name="type">Тип объекта</param>
        /// <returns>Схема или null если не найдена</returns>
        _RScheme? GetSchemeForType(Type type);
        
        /// <summary>
        /// Установить схему для .NET типа в статический кеш
        /// </summary>
        /// <typeparam name="TProps">Тип свойств объекта</typeparam>
        /// <param name="scheme">Схема для кеширования</param>
        void SetSchemeForType<TProps>(_RScheme scheme) where TProps : class;
        
        /// <summary>
        /// Установить схему для .NET типа в статический кеш
        /// </summary>
        /// <param name="type">Тип объекта</param>
        /// <param name="scheme">Схема для кеширования</param>
        void SetSchemeForType(Type type, _RScheme scheme);
        
        /// <summary>
        /// Получить структуру по ID из статического кеша
        /// </summary>
        /// <param name="structureId">ID структуры</param>
        /// <returns>Структура или null если не найдена</returns>
        _RStructure? GetStructure(long structureId);
        
        /// <summary>
        /// Установить структуру в статический кеш
        /// </summary>
        /// <param name="structure">Структура для кеширования</param>
        void SetStructure(_RStructure structure);
        
        /// <summary>
        /// Получить тип по ID из статического кеша
        /// </summary>
        /// <param name="typeId">ID типа</param>
        /// <returns>Тип или null если не найден</returns>
        _RType? GetType(long typeId);
        
        /// <summary>
        /// Установить тип в статический кеш
        /// </summary>
        /// <param name="type">Тип для кеширования</param>
        void SetType(_RType type);
        
        /// <summary>
        /// Получить полные метаданные для типа из статического кеша
        /// </summary>
        /// <typeparam name="TProps">Тип свойств объекта</typeparam>
        /// <returns>Полные метаданные или null если не найдены</returns>
        CompleteSchemeMetadata? GetCompleteMetadataForType<TProps>() where TProps : class;
        
        /// <summary>
        /// Установить полные метаданные для типа в статический кеш
        /// </summary>
        /// <typeparam name="TProps">Тип свойств объекта</typeparam>
        /// <param name="metadata">Полные метаданные</param>
        void SetCompleteMetadataForType<TProps>(CompleteSchemeMetadata metadata) where TProps : class;
        
        /// <summary>
        /// Очистить весь статический кеш (осторожно - влияет на все экземпляры!)
        /// </summary>
        void ClearAll();
        
        /// <summary>
        /// Удалить схему для типа из статического кеша
        /// </summary>
        /// <typeparam name="TProps">Тип для удаления</typeparam>
        void RemoveSchemeForType<TProps>() where TProps : class;
        
        /// <summary>
        /// Получить статистику статического кеша
        /// </summary>
        /// <returns>Статистика кеша</returns>
        StaticCacheStatistics GetStatistics();
    }
    
    /// <summary>
    /// Статический кеш метаданных реализующий предложение пользователя
    /// Использует потокобезопасные ConcurrentDictionary в статических полях
    /// </summary>
    public class StaticMetadataCache : IStaticMetadataCache
    {
        // === СТАТИЧЕСКИЕ КЕШИ (предложение пользователя) ===
        
        /// <summary>
        /// Кеш схем по типу объекта (.NET Type -> схема)
        /// </summary>
        private static readonly ConcurrentDictionary<Type, _RScheme> _schemesByType 
            = new ConcurrentDictionary<Type, _RScheme>();
            
        /// <summary>
        /// Кеш структур по ID (ID структуры -> структура)
        /// </summary>
        private static readonly ConcurrentDictionary<long, _RStructure> _structuresById 
            = new ConcurrentDictionary<long, _RStructure>();
            
        /// <summary>
        /// Кеш типов по ID (ID типа -> тип)
        /// </summary>
        private static readonly ConcurrentDictionary<long, _RType> _typesById 
            = new ConcurrentDictionary<long, _RType>();
            
        /// <summary>
        /// Кеш полных метаданных по типу (.NET Type -> полные метаданные)
        /// </summary>
        private static readonly ConcurrentDictionary<Type, CompleteSchemeMetadata> _completeMetadataByType 
            = new ConcurrentDictionary<Type, CompleteSchemeMetadata>();
            
        // === СТАТИСТИКА ===
        
        private static long _totalGets = 0;
        private static long _totalSets = 0;
        private static long _cacheHits = 0;
        private static long _cacheMisses = 0;
        
        // === РЕАЛИЗАЦИЯ ИНТЕРФЕЙСА ===
        
        public _RScheme? GetSchemeForType<TProps>() where TProps : class
        {
            return GetSchemeForType(typeof(TProps));
        }
        
        public _RScheme? GetSchemeForType(Type type)
        {
            System.Threading.Interlocked.Increment(ref _totalGets);
            
            if (_schemesByType.TryGetValue(type, out var scheme))
            {
                System.Threading.Interlocked.Increment(ref _cacheHits);
                return scheme;
            }
            
            System.Threading.Interlocked.Increment(ref _cacheMisses);
            return null;
        }
        
        public void SetSchemeForType<TProps>(_RScheme scheme) where TProps : class
        {
            SetSchemeForType(typeof(TProps), scheme);
        }
        
        public void SetSchemeForType(Type type, _RScheme scheme)
        {
            System.Threading.Interlocked.Increment(ref _totalSets);
            _schemesByType.TryAdd(type, scheme);
        }
        
        public _RStructure? GetStructure(long structureId)
        {
            System.Threading.Interlocked.Increment(ref _totalGets);
            
            if (_structuresById.TryGetValue(structureId, out var structure))
            {
                System.Threading.Interlocked.Increment(ref _cacheHits);
                return structure;
            }
            
            System.Threading.Interlocked.Increment(ref _cacheMisses);
            return null;
        }
        
        public void SetStructure(_RStructure structure)
        {
            System.Threading.Interlocked.Increment(ref _totalSets);
            _structuresById.TryAdd(structure.Id, structure);
        }
        
        public _RType? GetType(long typeId)
        {
            System.Threading.Interlocked.Increment(ref _totalGets);
            
            if (_typesById.TryGetValue(typeId, out var type))
            {
                System.Threading.Interlocked.Increment(ref _cacheHits);
                return type;
            }
            
            System.Threading.Interlocked.Increment(ref _cacheMisses);
            return null;
        }
        
        public void SetType(_RType type)
        {
            System.Threading.Interlocked.Increment(ref _totalSets);
            _typesById.TryAdd(type.Id, type);
        }
        
        public CompleteSchemeMetadata? GetCompleteMetadataForType<TProps>() where TProps : class
        {
            var type = typeof(TProps);
            System.Threading.Interlocked.Increment(ref _totalGets);
            
            if (_completeMetadataByType.TryGetValue(type, out var metadata))
            {
                System.Threading.Interlocked.Increment(ref _cacheHits);
                metadata.MarkAsUsed(); // Обновляем статистику использования
                return metadata;
            }
            
            System.Threading.Interlocked.Increment(ref _cacheMisses);
            return null;
        }
        
        public void SetCompleteMetadataForType<TProps>(CompleteSchemeMetadata metadata) where TProps : class
        {
            var type = typeof(TProps);
            metadata.AssociatedType = type;
            System.Threading.Interlocked.Increment(ref _totalSets);
            _completeMetadataByType.TryAdd(type, metadata);
        }
        
        public void ClearAll()
        {
            _schemesByType.Clear();
            _structuresById.Clear();
            _typesById.Clear();
            _completeMetadataByType.Clear();
            
            // Сброс статистики
            System.Threading.Interlocked.Exchange(ref _totalGets, 0);
            System.Threading.Interlocked.Exchange(ref _totalSets, 0);
            System.Threading.Interlocked.Exchange(ref _cacheHits, 0);
            System.Threading.Interlocked.Exchange(ref _cacheMisses, 0);
        }
        
        public void RemoveSchemeForType<TProps>() where TProps : class
        {
            var type = typeof(TProps);
            _schemesByType.TryRemove(type, out _);
            _completeMetadataByType.TryRemove(type, out _);
        }
        
        public StaticCacheStatistics GetStatistics()
        {
            return new StaticCacheStatistics
            {
                TotalGets = System.Threading.Interlocked.Read(ref _totalGets),
                TotalSets = System.Threading.Interlocked.Read(ref _totalSets),
                CacheHits = System.Threading.Interlocked.Read(ref _cacheHits),
                CacheMisses = System.Threading.Interlocked.Read(ref _cacheMisses),
                
                CachedSchemesCount = _schemesByType.Count,
                CachedStructuresCount = _structuresById.Count,
                CachedTypesCount = _typesById.Count,
                CachedCompleteMetadataCount = _completeMetadataByType.Count,
                
                EstimatedMemoryUsageBytes = EstimateMemoryUsage()
            };
        }
        
        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===
        
        /// <summary>
        /// Приблизительная оценка использования памяти кешем
        /// </summary>
        private long EstimateMemoryUsage()
        {
            const int averageSchemeSize = 200; // байт на схему
            const int averageStructureSize = 300; // байт на структуру  
            const int averageTypeSize = 100; // байт на тип
            const int averageCompleteMetadataSize = 2000; // байт на полные метаданные
            
            return (_schemesByType.Count * averageSchemeSize) +
                   (_structuresById.Count * averageStructureSize) +
                   (_typesById.Count * averageTypeSize) +
                   (_completeMetadataByType.Count * averageCompleteMetadataSize);
        }
    }
    
    /// <summary>
    /// Статистика статического кеша
    /// </summary>
    public class StaticCacheStatistics
    {
        /// <summary>
        /// Общее количество операций чтения
        /// </summary>
        public long TotalGets { get; set; }
        
        /// <summary>
        /// Общее количество операций записи
        /// </summary>
        public long TotalSets { get; set; }
        
        /// <summary>
        /// Количество попаданий в кеш
        /// </summary>
        public long CacheHits { get; set; }
        
        /// <summary>
        /// Количество промахов кеша
        /// </summary>
        public long CacheMisses { get; set; }
        
        /// <summary>
        /// Процент попаданий в кеш
        /// </summary>
        public double HitRatio => (CacheHits + CacheMisses) > 0 ? 
            (double)CacheHits / (CacheHits + CacheMisses) : 0.0;
            
        /// <summary>
        /// Количество кешированных схем
        /// </summary>
        public int CachedSchemesCount { get; set; }
        
        /// <summary>
        /// Количество кешированных структур
        /// </summary>
        public int CachedStructuresCount { get; set; }
        
        /// <summary>
        /// Количество кешированных типов
        /// </summary>
        public int CachedTypesCount { get; set; }
        
        /// <summary>
        /// Количество кешированных полных метаданных
        /// </summary>
        public int CachedCompleteMetadataCount { get; set; }
        
        /// <summary>
        /// Общее количество элементов в кеше
        /// </summary>
        public int TotalCachedItems => CachedSchemesCount + CachedStructuresCount + 
                                      CachedTypesCount + CachedCompleteMetadataCount;
                                      
        /// <summary>
        /// Приблизительное использование памяти в байтах
        /// </summary>
        public long EstimatedMemoryUsageBytes { get; set; }
        
        /// <summary>
        /// Использование памяти в мегабайтах
        /// </summary>
        public double MemoryUsageMB => EstimatedMemoryUsageBytes / (1024.0 * 1024.0);
        
        /// <summary>
        /// Время создания статистики
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public override string ToString()
        {
            return $"StaticCache: {TotalCachedItems} items, {HitRatio:P2} hit ratio, {MemoryUsageMB:F2}MB";
        }
    }
}
