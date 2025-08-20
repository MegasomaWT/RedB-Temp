using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using redb.Core.Models.Contracts;
using redb.Core.Models.Configuration;

namespace redb.Core.Caching
{
    /// <summary>
    /// Глобальный статический кеш метаданных для всего приложения
    /// Обеспечивает единый доступ к кешированным схемам из любого места приложения
    /// </summary>
    public static class GlobalMetadataCache
    {
        // ===== СТАТИЧЕСКИЕ КЕШИ =====
        private static readonly ConcurrentDictionary<string, IRedbScheme> _schemeByName = new();
        private static readonly ConcurrentDictionary<long, IRedbScheme> _schemeById = new();
        private static readonly ConcurrentDictionary<string, long> _typeCache = new();
        
        // ===== СТАТИСТИКА =====
        private static long _schemeHits = 0;
        private static long _schemeMisses = 0;
        private static long _typeHits = 0;
        private static long _typeMisses = 0;
        private static DateTime _lastResetTime = DateTime.Now;
        
        // ===== НАСТРОЙКИ =====
        private static volatile bool _cacheEnabled = true;
        private static volatile int _cacheLifetimeMinutes = 30;
        private static readonly object _lock = new();
        
        /// <summary>
        /// Инициализировать кеш с настройками из конфигурации
        /// </summary>
        public static void Initialize(RedbServiceConfiguration configuration)
        {
            lock (_lock)
            {
                _cacheEnabled = configuration.EnableMetadataCache;
                _cacheLifetimeMinutes = 30; // Используем значение по умолчанию
                
                if (!_cacheEnabled)
                {
                    Clear();
                }
            }
        }
        
        /// <summary>
        /// Включено ли кеширование
        /// </summary>
        public static bool IsEnabled => _cacheEnabled;
        
        // ===== МЕТОДЫ ДОСТУПА К СХЕМАМ =====
        
        /// <summary>
        /// Получить схему по имени из кеша
        /// </summary>
        public static IRedbScheme? GetScheme(string schemeName)
        {
            if (!_cacheEnabled)
                return null;
                
            if (_schemeByName.TryGetValue(schemeName, out var scheme))
            {
                Interlocked.Increment(ref _schemeHits);
                return scheme;
            }
            
            Interlocked.Increment(ref _schemeMisses);
            return null;
        }
        
        /// <summary>
        /// Получить схему по ID из кеша
        /// </summary>
        public static IRedbScheme? GetScheme(long schemeId)
        {
            if (!_cacheEnabled)
                return null;
                
            if (_schemeById.TryGetValue(schemeId, out var scheme))
            {
                Interlocked.Increment(ref _schemeHits);
                return scheme;
            }
            
            Interlocked.Increment(ref _schemeMisses);
            return null;
        }
        
        /// <summary>
        /// Кешировать схему
        /// </summary>
        public static void CacheScheme(IRedbScheme scheme)
        {
            if (!_cacheEnabled || scheme == null)
                return;
                
            _schemeByName.TryAdd(scheme.Name, scheme);
            _schemeById.TryAdd(scheme.Id, scheme);
        }
        
        /// <summary>
        /// Получить ID типа из кеша типов
        /// </summary>
        public static long? GetTypeId(string typeName)
        {
            if (!_cacheEnabled)
                return null;
                
            if (_typeCache.TryGetValue(typeName, out var typeId))
            {
                Interlocked.Increment(ref _typeHits);
                return typeId;
            }
            
            Interlocked.Increment(ref _typeMisses);
            return null;
        }
        
        /// <summary>
        /// Кешировать ID типа
        /// </summary>
        public static void CacheType(string typeName, long typeId)
        {
            if (!_cacheEnabled)
                return;
                
            _typeCache.TryAdd(typeName, typeId);
        }
        
        // ===== УПРАВЛЕНИЕ КЕШЕМ =====
        
        /// <summary>
        /// Включить/выключить кеширование (hot toggle)
        /// </summary>
        public static void SetEnabled(bool enabled)
        {
            lock (_lock)
            {
                _cacheEnabled = enabled;
                
                if (!enabled)
                {
                    Clear();
                }
            }
        }
        
        /// <summary>
        /// Очистить весь кеш
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _schemeByName.Clear();
                _schemeById.Clear();
                _typeCache.Clear();
                
                // Сбрасываем статистику
                Interlocked.Exchange(ref _schemeHits, 0);
                Interlocked.Exchange(ref _schemeMisses, 0);
                Interlocked.Exchange(ref _typeHits, 0);
                Interlocked.Exchange(ref _typeMisses, 0);
                _lastResetTime = DateTime.Now;
            }
        }
        
        /// <summary>
        /// Инвалидировать схему по имени
        /// </summary>
        public static void InvalidateScheme(string schemeName)
        {
            if (_schemeByName.TryGetValue(schemeName, out var scheme))
            {
                _schemeByName.TryRemove(schemeName, out _);
                _schemeById.TryRemove(scheme.Id, out _);
            }
        }
        
        /// <summary>
        /// Инвалидировать схему по ID
        /// </summary>
        public static void InvalidateScheme(long schemeId)
        {
            if (_schemeById.TryGetValue(schemeId, out var scheme))
            {
                _schemeByName.TryRemove(scheme.Name, out _);
                _schemeById.TryRemove(schemeId, out _);
            }
        }
        
        /// <summary>
        /// Инвалидировать схему по типу C#
        /// </summary>
        public static void InvalidateScheme<T>() where T : class
        {
            InvalidateScheme(typeof(T).Name);
        }
        
        // ===== СТАТИСТИКА =====
        
        /// <summary>
        /// Получить статистику кеша
        /// </summary>
        public static CacheStatistics GetStatistics()
        {
            return new CacheStatistics
            {
                SchemeHits = (int)_schemeHits,
                SchemeMisses = (int)_schemeMisses,
                TypeHits = (int)_typeHits,
                TypeMisses = (int)_typeMisses,
                StructureHits = 0, // Структуры теперь инкапсулированы в схемах
                StructureMisses = 0
            };
        }
        
        /// <summary>
        /// Сбросить статистику
        /// </summary>
        public static void ResetStatistics()
        {
            lock (_lock)
            {
                Interlocked.Exchange(ref _schemeHits, 0);
                Interlocked.Exchange(ref _schemeMisses, 0);
                Interlocked.Exchange(ref _typeHits, 0);
                Interlocked.Exchange(ref _typeMisses, 0);
                _lastResetTime = DateTime.Now;
            }
        }
        
        /// <summary>
        /// Оценить использование памяти
        /// </summary>
        private static long EstimateMemoryUsage()
        {
            // Приблизительная оценка:
            // - Схема с структурами: ~2KB на схему (включая все структуры)
            // - Тип: ~100 байт
            
            var schemeMemory = _schemeByName.Count * 2048;
            var typeMemory = _typeCache.Count * 100;
            
            return schemeMemory + typeMemory;
        }
        
        /// <summary>
        /// Получить упрощенную диагностическую информацию (заглушка для совместимости)
        /// </summary>
        public static string GetDiagnosticInfo()
        {
            var stats = GetStatistics();
            return $"Схемы в кеше: {_schemeByName.Count}, " +
                   $"Типы в кеше: {_typeCache.Count}, " +
                   $"Hit Rate схем: {stats.SchemeHitRatio:P1}, " +
                   $"Hit Rate типов: {stats.TypeHitRatio:P1}, " +
                   $"Общий Hit Rate: {stats.OverallHitRatio:P1}, " +
                   $"Память: ~{EstimateMemoryUsage() / 1024}КБ";
        }
        
        /// <summary>
        /// Предзагрузка кеша для массива типов
        /// </summary>
        public static async Task WarmupAsync<T>(Func<Type, Task<IRedbScheme?>> schemeLoader) where T : class
        {
            var scheme = await schemeLoader(typeof(T));
            if (scheme != null)
            {
                CacheScheme(scheme);
                // Статистика warmup ведется через обращения к кешу
            }
        }
        
        /// <summary>
        /// Предзагрузка кеша для массива типов
        /// </summary>
        public static async Task WarmupAsync(Type[] types, Func<Type, Task<IRedbScheme?>> schemeLoader)
        {
            foreach (var type in types)
            {
                var scheme = await schemeLoader(type);
                if (scheme != null)
                {
                    CacheScheme(scheme);
                }
            }
            // Статистика warmup ведется через обращения к кешу
        }
    }
}
