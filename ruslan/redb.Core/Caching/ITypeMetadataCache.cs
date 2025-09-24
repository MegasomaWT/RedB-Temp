using redb.Core.DBModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace redb.Core.Caching
{
    /// <summary>
    /// Специализированный кеш для типов данных
    /// Быстрый доступ к метаданным типов по ID и имени
    /// Типы изменяются очень редко, поэтому этот кеш может жить долго
    /// </summary>
    public interface ITypeMetadataCache
    {
        // === ПОЛУЧЕНИЕ ТИПОВ ===
        
        /// <summary>
        /// Получить все типы данных (часто используется при инициализации)
        /// </summary>
        /// <returns>Список всех типов</returns>
        Task<List<_RType>> GetAllTypesAsync();
        
        /// <summary>
        /// Получить тип по ID
        /// </summary>
        /// <param name="typeId">ID типа</param>
        /// <returns>Тип или null если не найден</returns>
        Task<_RType?> GetTypeByIdAsync(long typeId);
        
        /// <summary>
        /// Получить тип по имени (String, Long, DateTime, etc.)
        /// </summary>
        /// <param name="typeName">Имя типа</param>
        /// <returns>Тип или null если не найден</returns>
        Task<_RType?> GetTypeByNameAsync(string typeName);
        
        /// <summary>
        /// Получить карту всех типов ID -> тип (оптимизация для массовых операций)
        /// </summary>
        /// <returns>Словарь ID -> тип</returns>
        Task<Dictionary<long, _RType>> GetAllTypesMapAsync();
        
        /// <summary>
        /// Получить карту всех типов имя -> тип (оптимизация для поиска по имени)
        /// </summary>
        /// <returns>Словарь имя -> тип</returns>
        Task<Dictionary<string, _RType>> GetTypesByNameMapAsync();
        
        /// <summary>
        /// Получить ID типа по имени (быстрый доступ к часто используемой информации)
        /// </summary>
        /// <param name="typeName">Имя типа</param>
        /// <returns>ID типа или null если не найден</returns>
        Task<long?> GetTypeIdByNameAsync(string typeName);
        
        // === УСТАНОВКА/ОБНОВЛЕНИЕ ТИПОВ ===
        
        /// <summary>
        /// Добавить или обновить тип в кеше
        /// </summary>
        /// <param name="type">Тип для кеширования</param>
        void SetType(_RType type);
        
        /// <summary>
        /// Добавить или обновить все типы в кеше (обычно при инициализации)
        /// </summary>
        /// <param name="types">Список всех типов</param>
        void SetAllTypes(List<_RType> types);
        
        // === ИНВАЛИДАЦИЯ ===
        
        /// <summary>
        /// Удалить тип из кеша по ID
        /// </summary>
        /// <param name="typeId">ID типа</param>
        void InvalidateType(long typeId);
        
        /// <summary>
        /// Удалить тип из кеша по имени
        /// </summary>
        /// <param name="typeName">Имя типа</param>
        void InvalidateType(string typeName);
        
        /// <summary>
        /// Очистить весь кеш типов (редкая операция)
        /// </summary>
        void InvalidateAll();
        
        // === СТАТИСТИКА ===
        
        /// <summary>
        /// Получить статистику кеша типов
        /// </summary>
        /// <returns>Статистика использования кеша</returns>
        TypeCacheStatistics GetStatistics();
        
        /// <summary>
        /// Получить все кешированные типы (для диагностики)
        /// </summary>
        /// <returns>Словарь ID -> тип</returns>
        Dictionary<long, _RType> GetAllCachedTypes();
        
        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===
        
        /// <summary>
        /// Проверить, поддерживает ли тип массивы
        /// </summary>
        /// <param name="typeId">ID типа</param>
        /// <returns>true если тип поддерживает массивы</returns>
        Task<bool> TypeSupportsArraysAsync(long typeId);
        
        /// <summary>
        /// Проверить, является ли тип nullable
        /// </summary>
        /// <param name="typeId">ID типа</param>
        /// <returns>true если тип может быть null</returns>
        Task<bool> TypeSupportsNullAsync(long typeId);
        
        /// <summary>
        /// Получить .NET тип для REDB типа
        /// </summary>
        /// <param name="typeId">ID REDB типа</param>
        /// <returns>Соответствующий .NET тип или null</returns>
        Task<Type?> GetNetTypeAsync(long typeId);
    }
    
    /// <summary>
    /// Статистика кеша типов
    /// </summary>
    public class TypeCacheStatistics
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
        /// Количество кешированных типов
        /// </summary>
        public int CachedTypesCount { get; set; }
        
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
        /// Время последнего полного обновления кеша типов
        /// </summary>
        public DateTime LastFullRefreshTime { get; set; }
        
        /// <summary>
        /// Детальная статистика по типам запросов
        /// </summary>
        public Dictionary<string, long> RequestsByType { get; set; } = new();
        
        /// <summary>
        /// Статистика запросов по именам типов
        /// </summary>
        public Dictionary<string, long> RequestsByTypeName { get; set; } = new();
        
        /// <summary>
        /// Информация о типах и их использовании
        /// </summary>
        public Dictionary<string, TypeUsageInfo> TypeUsageStats { get; set; } = new();
    }
    
    /// <summary>
    /// Информация об использовании конкретного типа
    /// </summary>
    public class TypeUsageInfo
    {
        /// <summary>
        /// ID типа
        /// </summary>
        public long TypeId { get; set; }
        
        /// <summary>
        /// Имя типа
        /// </summary>
        public string TypeName { get; set; } = "";
        
        /// <summary>
        /// Количество обращений к типу
        /// </summary>
        public long RequestCount { get; set; }
        
        /// <summary>
        /// Время последнего обращения
        /// </summary>
        public DateTime LastRequestTime { get; set; }
        
        /// <summary>
        /// Поддерживает ли тип массивы
        /// </summary>
        public bool SupportsArrays { get; set; }
        
        /// <summary>
        /// Поддерживает ли тип null значения
        /// </summary>
        public bool SupportsNull { get; set; }
    }
}
