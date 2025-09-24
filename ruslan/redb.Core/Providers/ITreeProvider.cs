using redb.Core.Models.Contracts;
using redb.Core.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace redb.Core.Providers
{
    /// <summary>
    /// Провайдер для работы с древовидными структурами
    /// Проверка прав управляется централизованно через конфигурацию (по аналогии с IObjectStorageProvider)
    /// </summary>
    public interface ITreeProvider
    {
        // ===== БАЗОВЫЕ МЕТОДЫ (используют _securityContext и конфигурацию) =====
        
        /// <summary>
        /// Загрузить дерево/поддерево (использует _securityContext и config.DefaultCheckPermissionsOnLoad)
        /// </summary>
        Task<TreeRedbObject<TProps>> LoadTreeAsync<TProps>(IRedbObject rootObj, int? maxDepth = null) where TProps : class, new();
        
        /// <summary>
        /// Получить прямых детей объекта (использует _securityContext и config.DefaultCheckPermissionsOnLoad)
        /// </summary>
        Task<IEnumerable<TreeRedbObject<TProps>>> GetChildrenAsync<TProps>(IRedbObject parentObj) where TProps : class, new();
        
        /// <summary>
        /// Получить путь от объекта к корню (использует _securityContext и config.DefaultCheckPermissionsOnLoad)
        /// </summary>
        Task<IEnumerable<TreeRedbObject<TProps>>> GetPathToRootAsync<TProps>(IRedbObject obj) where TProps : class, new();
        
        /// <summary>
        /// Получить всех потомков объекта (использует _securityContext и config.DefaultCheckPermissionsOnLoad)
        /// </summary>
        Task<IEnumerable<TreeRedbObject<TProps>>> GetDescendantsAsync<TProps>(IRedbObject parentObj, int? maxDepth = null) where TProps : class, new();
        
        /// <summary>
        /// Переместить объект в дереве (использует _securityContext и config.DefaultCheckPermissionsOnSave)
        /// </summary>
        Task MoveObjectAsync(IRedbObject obj, IRedbObject? newParentObj);
        
        /// <summary>
        /// Создать дочерний объект (использует _securityContext и config.DefaultCheckPermissionsOnSave)
        /// </summary>
        Task<long> CreateChildAsync<TProps>(TreeRedbObject<TProps> obj, IRedbObject parentObj) where TProps : class, new();
        
        /// <summary>
        /// Удалить поддерево объектов рекурсивно (использует _securityContext и config.DefaultCheckPermissionsOnDelete)
        /// </summary>
        Task<int> DeleteSubtreeAsync(IRedbObject parentObj);

        // ===== ПЕРЕГРУЗКИ С ЯВНЫМ ПОЛЬЗОВАТЕЛЕМ (используют конфигурацию) =====
        
        /// <summary>
        /// Загрузить дерево/поддерево с явно указанным пользователем (использует config.DefaultCheckPermissionsOnLoad)
        /// </summary>
        Task<TreeRedbObject<TProps>> LoadTreeAsync<TProps>(IRedbObject rootObj, IRedbUser user, int? maxDepth = null) where TProps : class, new();
        
        /// <summary>
        /// Получить прямых детей объекта с явно указанным пользователем (использует config.DefaultCheckPermissionsOnLoad)
        /// </summary>
        Task<IEnumerable<TreeRedbObject<TProps>>> GetChildrenAsync<TProps>(IRedbObject parentObj, IRedbUser user) where TProps : class, new();
        
        /// <summary>
        /// Получить путь от объекта к корню с явно указанным пользователем (использует config.DefaultCheckPermissionsOnLoad)
        /// </summary>
        Task<IEnumerable<TreeRedbObject<TProps>>> GetPathToRootAsync<TProps>(IRedbObject obj, IRedbUser user) where TProps : class, new();
        
        /// <summary>
        /// Получить всех потомков объекта с явно указанным пользователем (использует config.DefaultCheckPermissionsOnLoad)
        /// </summary>
        Task<IEnumerable<TreeRedbObject<TProps>>> GetDescendantsAsync<TProps>(IRedbObject parentObj, IRedbUser user, int? maxDepth = null) where TProps : class, new();
        
        /// <summary>
        /// Переместить объект в дереве с явно указанным пользователем (использует config.DefaultCheckPermissionsOnSave)
        /// </summary>
        Task MoveObjectAsync(IRedbObject obj, IRedbObject? newParentObj, IRedbUser user);
        
        /// <summary>
        /// Создать дочерний объект с явно указанным пользователем (использует config.DefaultCheckPermissionsOnSave)
        /// </summary>
        Task<long> CreateChildAsync<TProps>(TreeRedbObject<TProps> obj, IRedbObject parentObj, IRedbUser user) where TProps : class, new();
        
        /// <summary>
        /// Удалить поддерево объектов рекурсивно с явно указанным пользователем (использует config.DefaultCheckPermissionsOnDelete)
        /// </summary>
        Task<int> DeleteSubtreeAsync(IRedbObject parentObj, IRedbUser user);

        // ===== ПОЛИМОРФНЫЕ МЕТОДЫ (для смешанных деревьев) =====
        
        /// <summary>
        /// Загрузить полиморфное дерево/поддерево - поддерживает объекты разных схем в одном дереве
        /// Использует _securityContext и config.DefaultCheckPermissionsOnLoad
        /// </summary>
        Task<ITreeRedbObject> LoadPolymorphicTreeAsync(IRedbObject rootObj, int? maxDepth = null);
        
        /// <summary>
        /// Получить всех прямых детей объекта независимо от их схем
        /// Использует _securityContext и config.DefaultCheckPermissionsOnLoad
        /// </summary>
        Task<IEnumerable<ITreeRedbObject>> GetPolymorphicChildrenAsync(IRedbObject parentObj);
        
        /// <summary>
        /// Получить полиморфный путь от объекта к корню - объекты могут быть разных схем
        /// Использует _securityContext и config.DefaultCheckPermissionsOnLoad
        /// </summary>
        Task<IEnumerable<ITreeRedbObject>> GetPolymorphicPathToRootAsync(IRedbObject obj);
        
        /// <summary>
        /// Получить всех полиморфных потомков объекта независимо от их схем
        /// Использует _securityContext и config.DefaultCheckPermissionsOnLoad
        /// </summary>
        Task<IEnumerable<ITreeRedbObject>> GetPolymorphicDescendantsAsync(IRedbObject parentObj, int? maxDepth = null);
        


        // ===== ПОЛИМОРФНЫЕ МЕТОДЫ С ЯВНЫМ ПОЛЬЗОВАТЕЛЕМ =====
        
        /// <summary>
        /// Загрузить полиморфное дерево/поддерево с явно указанным пользователем
        /// Использует config.DefaultCheckPermissionsOnLoad
        /// </summary>
        Task<ITreeRedbObject> LoadPolymorphicTreeAsync(IRedbObject rootObj, IRedbUser user, int? maxDepth = null);
        
        /// <summary>
        /// Получить всех прямых детей объекта независимо от их схем с явно указанным пользователем
        /// Использует config.DefaultCheckPermissionsOnLoad
        /// </summary>
        Task<IEnumerable<ITreeRedbObject>> GetPolymorphicChildrenAsync(IRedbObject parentObj, IRedbUser user);
        
        /// <summary>
        /// Получить полиморфный путь от объекта к корню с явно указанным пользователем
        /// Использует config.DefaultCheckPermissionsOnLoad
        /// </summary>
        Task<IEnumerable<ITreeRedbObject>> GetPolymorphicPathToRootAsync(IRedbObject obj, IRedbUser user);
        
        /// <summary>
        /// Получить всех полиморфных потомков объекта с явно указанным пользователем
        /// Использует config.DefaultCheckPermissionsOnLoad
        /// </summary>
        Task<IEnumerable<ITreeRedbObject>> GetPolymorphicDescendantsAsync(IRedbObject parentObj, IRedbUser user, int? maxDepth = null);
        

    }
}
