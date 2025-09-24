using System;
using System.Threading.Tasks;
using redb.Core.Caching;

namespace redb.Core.Models.Contracts
{
    /// <summary>
    /// Расширение ISchemeSyncProvider с методами управления кешем метаданных
    /// Добавляет возможности горячего переключения, инвалидации и предзагрузки кеша
    /// </summary>
    public interface ISchemeCacheProvider
    {
        // ===== УПРАВЛЕНИЕ СОСТОЯНИЕМ КЕША =====
        
        /// <summary>
        /// Включить/выключить кеширование метаданных на лету (hot toggle)
        /// При выключении автоматически очищается весь кеш
        /// </summary>
        /// <param name="enabled">true - включить кеш, false - выключить</param>
        void SetCacheEnabled(bool enabled);
        
        /// <summary>
        /// Проверить, включено ли кеширование
        /// </summary>
        bool IsCacheEnabled { get; }
        
        // ===== ИНВАЛИДАЦИЯ КЕША =====
        
        /// <summary>
        /// Полная очистка всех кешей метаданных
        /// Сбрасывает статистику до нуля
        /// </summary>
        void InvalidateCache();
        
        /// <summary>
        /// Очистить кеш метаданных для конкретного типа C#
        /// </summary>
        /// <typeparam name="TProps">Тип свойств объекта</typeparam>
        void InvalidateSchemeCache<TProps>() where TProps : class;
        
        /// <summary>
        /// Очистить кеш метаданных для схемы по ID
        /// </summary>
        /// <param name="schemeId">ID схемы в базе данных</param>
        void InvalidateSchemeCache(long schemeId);
        
        /// <summary>
        /// Очистить кеш метаданных для схемы по имени
        /// </summary>
        /// <param name="schemeName">Имя схемы</param>
        void InvalidateSchemeCache(string schemeName);
        
        // ===== СТАТИСТИКА И МОНИТОРИНГ =====
        
        /// <summary>
        /// Получить детальную статистику работы кеша
        /// </summary>
        CacheStatistics GetCacheStatistics();
        
        /// <summary>
        /// Сбросить статистику кеша (обнулить счетчики)
        /// Сам кеш остается нетронутым
        /// </summary>
        void ResetCacheStatistics();
        
        // ===== ПРЕДЗАГРУЗКА КЕША (WARMUP) =====
        
        /// <summary>
        /// Предварительно загрузить метаданные для типа C#
        /// Полезно для оптимизации производительности при старте приложения
        /// </summary>
        /// <typeparam name="TProps">Тип свойств объекта</typeparam>
        Task WarmupCacheAsync<TProps>() where TProps : class;
        
        /// <summary>
        /// Предварительно загрузить метаданные для массива типов C#
        /// </summary>
        /// <param name="types">Массив типов для предзагрузки</param>
        Task WarmupCacheAsync(Type[] types);
        
        /// <summary>
        /// Предварительно загрузить метаданные для всех известных схем
        /// Использовать осторожно - может быть ресурсозатратно
        /// </summary>
        Task WarmupAllSchemesAsync();
        
        // ===== ДИАГНОСТИКА =====
        
        /// <summary>
        /// Получить диагностическую информацию о состоянии кеша
        /// Включает рекомендации по оптимизации
        /// </summary>
        CacheDiagnosticInfo GetCacheDiagnosticInfo();
        
        /// <summary>
        /// Оценить текущее потребление памяти кешем в байтах
        /// Приблизительное значение для мониторинга
        /// </summary>
        long EstimateMemoryUsage();
    }
}
