using redb.Core.Models.Entities;
using redb.Core.Models.Contracts;
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
    }
}
