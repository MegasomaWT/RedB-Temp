using redb.Core.DBModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace redb.Core.Caching
{
    /// <summary>
    /// Специализированный кеш для схем объектов
    /// Быстрый доступ к метаданным схем по типу, ID и имени
    /// </summary>
    public interface ISchemeMetadataCache
    {
        // === ПОЛУЧЕНИЕ СХЕМ ===
        
        /// <summary>
        /// Получить схему по .NET типу (наиболее частый случай)
        /// </summary>
        /// <typeparam name="TProps">Тип свойств объекта</typeparam>
        /// <returns>Схема или null если не найдена</returns>
        Task<_RScheme?> GetSchemeByTypeAsync<TProps>() where TProps : class;
        
        /// <summary>
        /// Получить схему по .NET типу
        /// </summary>
        /// <param name="type">Тип объекта</param>
        /// <returns>Схема или null если не найдена</returns>
        Task<_RScheme?> GetSchemeByTypeAsync(Type type);
        
        /// <summary>
        /// Получить схему по ID
        /// </summary>
        /// <param name="schemeId">ID схемы</param>
        /// <returns>Схема или null если не найдена</returns>
        Task<_RScheme?> GetSchemeByIdAsync(long schemeId);
        
        /// <summary>
        /// Получить схему по имени
        /// </summary>
        /// <param name="schemeName">Имя схемы</param>
        /// <returns>Схема или null если не найдена</returns>
        Task<_RScheme?> GetSchemeByNameAsync(string schemeName);
        
        // === УСТАНОВКА/ОБНОВЛЕНИЕ СХЕМ ===
        
        /// <summary>
        /// Добавить или обновить схему в кеше
        /// </summary>
        /// <param name="scheme">Схема для кеширования</param>
        void SetScheme(_RScheme scheme);
        
        /// <summary>
        /// Добавить или обновить схему в кеше с привязкой к типу
        /// </summary>
        /// <typeparam name="TProps">Тип для привязки</typeparam>
        /// <param name="scheme">Схема для кеширования</param>
        void SetSchemeForType<TProps>(_RScheme scheme) where TProps : class;
        
        /// <summary>
        /// Добавить или обновить схему в кеше с привязкой к типу
        /// </summary>
        /// <param name="type">Тип для привязки</param>
        /// <param name="scheme">Схема для кеширования</param>
        void SetSchemeForType(Type type, _RScheme scheme);
        
        // === ИНВАЛИДАЦИЯ ===
        
        /// <summary>
        /// Удалить схему из кеша по ID
        /// </summary>
        /// <param name="schemeId">ID схемы</param>
        void InvalidateScheme(long schemeId);
        
        /// <summary>
        /// Удалить схему из кеша по имени
        /// </summary>
        /// <param name="schemeName">Имя схемы</param>
        void InvalidateScheme(string schemeName);
        
        /// <summary>
        /// Удалить схему из кеша по типу
        /// </summary>
        /// <typeparam name="TProps">Тип для удаления</typeparam>
        void InvalidateSchemeForType<TProps>() where TProps : class;
        
        /// <summary>
        /// Удалить схему из кеша по типу
        /// </summary>
        /// <param name="type">Тип для удаления</param>
        void InvalidateSchemeForType(Type type);
        
        /// <summary>
        /// Очистить весь кеш схем
        /// </summary>
        void InvalidateAll();
        
        // === СТАТИСТИКА ===
        
        /// <summary>
        /// Получить статистику кеша схем
        /// </summary>
        /// <returns>Статистика использования кеша</returns>
        SchemeCacheStatistics GetStatistics();
        
        /// <summary>
        /// Получить все кешированные схемы (для диагностики)
        /// </summary>
        /// <returns>Словарь ID -> схема</returns>
        Dictionary<long, _RScheme> GetAllCachedSchemes();
    }
    
    /// <summary>
    /// Статистика кеша схем
    /// </summary>
    public class SchemeCacheStatistics
    {
        /// <summary>
        /// Количество попаданий в кеш
        /// </summary>
        public long Hits { get; set; }
        
        /// <summary>
        /// Количество промахов кеша
        /// </summary>
        public long Misses { get; set; }
        
        /// <summary>
        /// Общее количество запросов
        /// </summary>
        public long TotalRequests => Hits + Misses;
        
        /// <summary>
        /// Процент попаданий в кеш (0.0 - 1.0)
        /// </summary>
        public double HitRatio => TotalRequests > 0 ? (double)Hits / TotalRequests : 0.0;
        
        /// <summary>
        /// Количество кешированных схем
        /// </summary>
        public int CachedSchemesCount { get; set; }
        
        /// <summary>
        /// Количество кешированных привязок тип -> схема
        /// </summary>
        public int TypeMappingsCount { get; set; }
        
        /// <summary>
        /// Приблизительный размер кеша в байтах
        /// </summary>
        public long EstimatedSizeBytes { get; set; }
        
        /// <summary>
        /// Время последнего обращения к кешу
        /// </summary>
        public DateTime LastAccessTime { get; set; }
        
        /// <summary>
        /// Время создания кеша
        /// </summary>
        public DateTime CreatedTime { get; set; }
        
        /// <summary>
        /// Детальная статистика по типам запросов
        /// </summary>
        public Dictionary<string, long> RequestsByType { get; set; } = new();
    }
}
