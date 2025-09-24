using redb.Core.Models.Entities;
using redb.Core.Models.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace redb.Core.Providers
{
    /// <summary>
    /// Провайдер для работы с сохранением/загрузкой объектов в EAV
    /// Проверка прав управляется централизованно через конфигурацию
    /// </summary>
    public interface IObjectStorageProvider
    {
        // ===== БАЗОВЫЕ МЕТОДЫ (используют _securityContext и конфигурацию) =====
        
        /// <summary>
        /// Загрузить объект из EAV по ID (использует _securityContext и config.DefaultCheckPermissionsOnLoad)
        /// </summary>
        Task<RedbObject<TProps>> LoadAsync<TProps>(long objectId, int depth = 10) where TProps : class, new();
        
        /// <summary>
        /// Загрузить объект из EAV (использует _securityContext и config.DefaultCheckPermissionsOnLoad)
        /// </summary>
        Task<RedbObject<TProps>> LoadAsync<TProps>(IRedbObject obj, int depth = 10) where TProps : class, new();

        /// <summary>
        /// Загрузить объект из EAV с явно указанным пользователем по ID (использует config.DefaultCheckPermissionsOnLoad)
        /// </summary>
        Task<RedbObject<TProps>> LoadAsync<TProps>(long objectId, IRedbUser user, int depth = 10) where TProps : class, new();

        /// <summary>
        /// Загрузить объект из EAV с явно указанным пользователем (использует config.DefaultCheckPermissionsOnLoad)
        /// </summary>
        Task<RedbObject<TProps>> LoadAsync<TProps>(IRedbObject obj, IRedbUser user, int depth = 10) where TProps : class, new();

        /// <summary>
        /// Сохранить объект в EAV (использует _securityContext и config.DefaultCheckPermissionsOnSave)
        /// </summary>
        Task<long> SaveAsync<TProps>(IRedbObject<TProps> obj) where TProps : class, new();
        
        /// <summary>
        /// Удалить объект (использует _securityContext и config.DefaultCheckPermissionsOnDelete)
        /// </summary>
        Task<bool> DeleteAsync<TProps>(IRedbObject<TProps> obj) where TProps : class, new();
        
        // ===== ПЕРЕГРУЗКИ С ЯВНЫМ ПОЛЬЗОВАТЕЛЕМ (используют конфигурацию) =====
        
       
        /// <summary>
        /// Сохранить объект в EAV с явно указанным пользователем (использует config.DefaultCheckPermissionsOnSave)
        /// </summary>
        Task<long> SaveAsync<TProps>(IRedbObject<TProps> obj, IRedbUser user) where TProps : class, new();
        
        /// <summary>
        /// Удалить объект с явно указанным пользователем (использует config.DefaultCheckPermissionsOnDelete)
        /// </summary>
        Task<bool> DeleteAsync<TProps>(IRedbObject<TProps> obj, IRedbUser user) where TProps : class, new();

        // ===== МАССОВЫЕ ОПЕРАЦИИ (БЕЗ ПРОВЕРКИ ПРАВ) =====
        
        /// <summary>
        /// 🚀 МАССОВАЯ ВСТАВКА: Создать множество новых объектов за одну операцию (НЕ проверяет права)
        /// - Создает схемы если их нет (аналогично SaveAsync)
        /// - Генерирует ID для объектов с id == 0 через GetNextKey
        /// - Полностью обрабатывает рекурсивные вложенные объекты, массивы, Class поля
        /// - Использует BulkInsert для максимальной производительности
        /// - Если id != 0 полагается на ошибки БД для дубликатов (не проверяет заранее)
        /// </summary>
        Task<List<long>> AddNewObjectsAsync<TProps>(List<IRedbObject<TProps>> objects) where TProps : class, new();
        
        /// <summary>
        /// 🚀 МАССОВАЯ ВСТАВКА с явным пользователем: Создать множество новых объектов (НЕ проверяет права)
        /// - Устанавливает OwnerId и WhoChangeId для всех объектов от указанного пользователя
        /// - Остальная логика идентична AddNewObjectsAsync без пользователя
        /// </summary>
        Task<List<long>> AddNewObjectsAsync<TProps>(List<IRedbObject<TProps>> objects, IRedbUser user) where TProps : class, new();
    }
}
