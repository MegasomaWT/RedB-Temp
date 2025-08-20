using System;
using System.Threading.Tasks;

namespace redb.Core.Models.Contracts
{
    /// <summary>
    /// Типизированный интерфейс для объектов REDB с конкретным типом свойств
    /// Расширяет базовый IRedbObject добавляя типобезопасный доступ к свойствам
    /// Обеспечивает IntelliSense и compile-time проверку типов
    /// </summary>
    /// <typeparam name="TProps">Тип класса свойств объекта</typeparam>
    public interface IRedbObject<TProps> : IRedbObject where TProps : class
    {
        /// <summary>
        /// Типизированный доступ к свойствам объекта
        /// Заменяет необходимость работы с raw JSON или приведением типов
        /// </summary>
        TProps properties { get; set; }
        
        /// <summary>
        /// Получить схему для типа TProps (с использованием кеша)
        /// Удобный метод для получения метаданных конкретного типа
        /// </summary>
        Task<IRedbScheme> GetSchemeForTypeAsync();
        
        /// <summary>
        /// Получить структуры схемы для типа TProps (с использованием кеша)
        /// Возвращает инкапсулированные структуры из схемы
        /// </summary>
        Task<IReadOnlyCollection<IRedbStructure>> GetStructuresForTypeAsync();
        
        /// <summary>
        /// Получить структуру по имени поля для типа TProps
        /// Использует быстрый поиск по имени в инкапсулированной схеме
        /// </summary>
        Task<IRedbStructure?> GetStructureByNameAsync(string fieldName);
        
        /// <summary>
        /// Пересчитать хеш на основе текущих свойств типа TProps
        /// Типизированная версия RecomputeHash() с учетом конкретного типа
        /// </summary>
        void RecomputeHashForType();
        
        /// <summary>
        /// Получить новый хеш на основе текущих свойств без изменения объекта
        /// Типизированная версия GetComputedHash()
        /// </summary>
        Guid ComputeHashForType();
        
        /// <summary>
        /// Проверить, соответствует ли текущий хеш свойствам типа TProps
        /// Типизированная проверка целостности данных
        /// </summary>
        bool IsHashValidForType();
        
        /// <summary>
        /// Создать копию объекта с теми же метаданными но новыми свойствами
        /// Полезно для создания похожих объектов
        /// </summary>
        IRedbObject<TProps> CloneWithProperties(TProps newProperties);
        
        /// <summary>
        /// Очистить кеш метаданных для типа TProps
        /// Удобный метод для инвалидации кеша на уровне объекта
        /// </summary>
        void InvalidateCacheForType();
        
        /// <summary>
        /// Предзагрузить кеш метаданных для типа TProps
        /// Полезно для оптимизации производительности
        /// </summary>
        Task WarmupCacheForTypeAsync();
    }
}
