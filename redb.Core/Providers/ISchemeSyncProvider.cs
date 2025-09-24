using System;
using System.Threading.Tasks;
using redb.Core.Models.Contracts;
using System.Collections.Generic;

namespace redb.Core.Providers
{
    /// <summary>
    /// Провайдер для работы с схемами и структурами (Code-First)
    /// </summary>
    public interface ISchemeSyncProvider
    {
        // ===== МЕТОДЫ С КОНТРАКТАМИ =====

        /// <summary>
        /// Создать/получить схему по имени из типа свойств
        /// Если schemeName = null, используется имя класса TProps
        /// </summary>
        Task<IRedbScheme> EnsureSchemeFromTypeAsync<TProps>() where TProps : class;

        /// <summary>
        /// Синхронизировать структуры схемы по типу свойств (по умолчанию удаляет лишние поля)
        /// </summary>
        Task<List<IRedbStructure>> SyncStructuresFromTypeAsync<TProps>(IRedbScheme scheme, bool strictDeleteExtra = true) where TProps : class;

        /// <summary>
        /// Упрощенный метод синхронизации схемы с автоопределением имени и алиаса
        /// Имя схемы и алиас определяются из атрибута RedbSchemeAttribute
        /// </summary>
        Task<IRedbScheme> SyncSchemeAsync<TProps>() where TProps : class;
        
        // ===== ПОИСК СХЕМ =====
        
        /// <summary>
        /// Получить схему по ID
        /// </summary>
        Task<IRedbScheme?> GetSchemeByIdAsync(long schemeId);
        
        /// <summary>
        /// Получить схему по имени
        /// </summary>
        Task<IRedbScheme?> GetSchemeByNameAsync(string schemeName);
        
        /// <summary>
        /// Получить схему по типу C# класса
        /// Использует имя класса для поиска схемы
        /// </summary>
        Task<IRedbScheme?> GetSchemeByTypeAsync<TProps>() where TProps : class;
        
        /// <summary>
        /// Получить схему по типу C# класса
        /// Использует имя класса для поиска схемы
        /// </summary>
        Task<IRedbScheme?> GetSchemeByTypeAsync(Type type);
        
        /// <summary>
        /// Загрузить схему по типу C# класса (с исключением если не найдена)
        /// </summary>
        Task<IRedbScheme> LoadSchemeByTypeAsync<TProps>() where TProps : class;
        
        /// <summary>
        /// Загрузить схему по типу C# класса (с исключением если не найдена)
        /// </summary>
        Task<IRedbScheme> LoadSchemeByTypeAsync(Type type);
        
        /// <summary>
        /// Получить все схемы
        /// </summary>
        Task<List<IRedbScheme>> GetSchemesAsync();
        
        /// <summary>
        /// Получить структуры схемы
        /// </summary>
        Task<List<IRedbStructure>> GetStructuresAsync(IRedbScheme scheme);
        
        /// <summary>
        /// Получить структуры схемы по типу C# класса
        /// </summary>
        Task<List<IRedbStructure>> GetStructuresByTypeAsync<TProps>() where TProps : class;
        
        /// <summary>
        /// Получить структуры схемы по типу C# класса
        /// </summary>
        Task<List<IRedbStructure>> GetStructuresByTypeAsync(Type type);
        
        // ===== ПРОВЕРКА СУЩЕСТВОВАНИЯ СХЕМ =====
        
        /// <summary>
        /// Проверить, существует ли схема для типа C# класса
        /// </summary>
        Task<bool> SchemeExistsForTypeAsync<TProps>() where TProps : class;
        
        /// <summary>
        /// Проверить, существует ли схема для типа C# класса
        /// </summary>
        Task<bool> SchemeExistsForTypeAsync(Type type);
        
        /// <summary>
        /// Проверить, существует ли схема по имени
        /// </summary>
        Task<bool> SchemeExistsByNameAsync(string schemeName);
        
        // ===== УТИЛИТАРНЫЕ МЕТОДЫ =====
        
        /// <summary>
        /// Получить имя схемы для типа C# класса
        /// Учитывает атрибуты и пространства имен
        /// </summary>
        string GetSchemeNameForType<TProps>() where TProps : class;
        
        /// <summary>
        /// Получить имя схемы для типа C# класса
        /// Учитывает атрибуты и пространства имен
        /// </summary>
        string GetSchemeNameForType(Type type);
        
        /// <summary>
        /// Получить алиас схемы для типа C# класса
        /// Извлекает из атрибута RedbSchemeAttribute
        /// </summary>
        string? GetSchemeAliasForType<TProps>() where TProps : class;
        
        /// <summary>
        /// Получить алиас схемы для типа C# класса
        /// Извлекает из атрибута RedbSchemeAttribute
        /// </summary>
        string? GetSchemeAliasForType(Type type);
    }
}
