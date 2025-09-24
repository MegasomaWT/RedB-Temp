using redb.Core.DBModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace redb.Core.Caching
{
    /// <summary>
    /// Специализированный кеш для структур полей
    /// Быстрый доступ к метаданным структур по схеме, ID и типу
    /// </summary>
    public interface IStructureMetadataCache
    {
        // === ПОЛУЧЕНИЕ СТРУКТУР ===
        
        /// <summary>
        /// Получить все структуры для схемы (наиболее частый случай)
        /// </summary>
        /// <param name="schemeId">ID схемы</param>
        /// <returns>Список структур схемы</returns>
        Task<List<_RStructure>> GetStructuresBySchemeIdAsync(long schemeId);
        
        /// <summary>
        /// Получить структуры для .NET типа
        /// </summary>
        /// <typeparam name="TProps">Тип свойств объекта</typeparam>
        /// <returns>Список структур для типа</returns>
        Task<List<_RStructure>> GetStructuresByTypeAsync<TProps>() where TProps : class;
        
        /// <summary>
        /// Получить структуры для .NET типа
        /// </summary>
        /// <param name="type">Тип объекта</param>
        /// <returns>Список структур для типа</returns>
        Task<List<_RStructure>> GetStructuresByTypeAsync(Type type);
        
        /// <summary>
        /// Получить структуру по ID
        /// </summary>
        /// <param name="structureId">ID структуры</param>
        /// <returns>Структура или null если не найдена</returns>
        Task<_RStructure?> GetStructureByIdAsync(long structureId);
        
        /// <summary>
        /// Получить структуру по имени в рамках схемы
        /// </summary>
        /// <param name="schemeId">ID схемы</param>
        /// <param name="structureName">Имя структуры</param>
        /// <returns>Структура или null если не найдена</returns>
        Task<_RStructure?> GetStructureByNameAsync(long schemeId, string structureName);
        
        /// <summary>
        /// Получить карту имя -> структура для схемы (оптимизация для частых поисков)
        /// </summary>
        /// <param name="schemeId">ID схемы</param>
        /// <returns>Словарь имя структуры -> структура</returns>
        Task<Dictionary<string, _RStructure>> GetStructuresMapBySchemeIdAsync(long schemeId);
        
        // === УСТАНОВКА/ОБНОВЛЕНИЕ СТРУКТУР ===
        
        /// <summary>
        /// Добавить или обновить структуру в кеше
        /// </summary>
        /// <param name="structure">Структура для кеширования</param>
        void SetStructure(_RStructure structure);
        
        /// <summary>
        /// Добавить или обновить все структуры схемы в кеше
        /// </summary>
        /// <param name="schemeId">ID схемы</param>
        /// <param name="structures">Список структур</param>
        void SetStructuresForScheme(long schemeId, List<_RStructure> structures);
        
        /// <summary>
        /// Добавить или обновить структуры для типа в кеше
        /// </summary>
        /// <typeparam name="TProps">Тип для привязки</typeparam>
        /// <param name="structures">Список структур</param>
        void SetStructuresForType<TProps>(List<_RStructure> structures) where TProps : class;
        
        /// <summary>
        /// Добавить или обновить структуры для типа в кеше
        /// </summary>
        /// <param name="type">Тип для привязки</param>
        /// <param name="structures">Список структур</param>
        void SetStructuresForType(Type type, List<_RStructure> structures);
        
        // === ИНВАЛИДАЦИЯ ===
        
        /// <summary>
        /// Удалить структуру из кеша по ID
        /// </summary>
        /// <param name="structureId">ID структуры</param>
        void InvalidateStructure(long structureId);
        
        /// <summary>
        /// Удалить все структуры схемы из кеша
        /// </summary>
        /// <param name="schemeId">ID схемы</param>
        void InvalidateStructuresForScheme(long schemeId);
        
        /// <summary>
        /// Удалить структуры типа из кеша
        /// </summary>
        /// <typeparam name="TProps">Тип для удаления</typeparam>
        void InvalidateStructuresForType<TProps>() where TProps : class;
        
        /// <summary>
        /// Удалить структуры типа из кеша
        /// </summary>
        /// <param name="type">Тип для удаления</param>
        void InvalidateStructuresForType(Type type);
        
        /// <summary>
        /// Очистить весь кеш структур
        /// </summary>
        void InvalidateAll();
        
        // === СТАТИСТИКА ===
        
        /// <summary>
        /// Получить статистику кеша структур
        /// </summary>
        /// <returns>Статистика использования кеша</returns>
        StructureCacheStatistics GetStatistics();
        
        /// <summary>
        /// Получить все кешированные структуры (для диагностики)
        /// </summary>
        /// <returns>Словарь ID -> структура</returns>
        Dictionary<long, _RStructure> GetAllCachedStructures();
        
        /// <summary>
        /// Получить карту схем к их структурам (для диагностики)
        /// </summary>
        /// <returns>Словарь ID схемы -> список структур</returns>
        Dictionary<long, List<_RStructure>> GetSchemeToStructuresMap();
    }
    
    /// <summary>
    /// Статистика кеша структур
    /// </summary>
    public class StructureCacheStatistics
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
        /// Количество кешированных структур
        /// </summary>
        public int CachedStructuresCount { get; set; }
        
        /// <summary>
        /// Количество кешированных схем с их структурами
        /// </summary>
        public int CachedSchemesCount { get; set; }
        
        /// <summary>
        /// Количество кешированных привязок тип -> структуры
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
        
        /// <summary>
        /// Статистика по схемам - сколько раз запрашивались структуры каждой схемы
        /// </summary>
        public Dictionary<long, long> RequestsByScheme { get; set; } = new();
    }
}
