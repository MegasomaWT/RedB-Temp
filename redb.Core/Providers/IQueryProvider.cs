using redb.Core.Query;
using System.Collections.Generic;
using System.Threading.Tasks;
using redb.Core.Models.Contracts;

namespace redb.Core.Providers
{
    /// <summary>
    /// Провайдер для создания LINQ-запросов (высокоуровневый API)
    /// </summary>
    public interface IQueryableProvider
    {
        /// <summary>
        /// Создать типобезопасный запрос по типу (автоматически определит схему по имени класса)
        /// </summary>
        Task<IRedbQueryable<TProps>> QueryAsync<TProps>() where TProps : class, new();
        
        /// <summary>
        /// Создать типобезопасный запрос по типу с указанным пользователем
        /// </summary>
        Task<IRedbQueryable<TProps>> QueryAsync<TProps>(IRedbUser user) where TProps : class, new();
        
        /// <summary>
        /// Создать типобезопасный запрос по типу (синхронно)
        /// </summary>
        IRedbQueryable<TProps> Query<TProps>() where TProps : class, new();
        
        /// <summary>
        /// Создать типобезопасный запрос по типу с указанным пользователем (синхронно)
        /// </summary>
        IRedbQueryable<TProps> Query<TProps>(IRedbUser user) where TProps : class, new();
        
        // ===== ДРЕВОВИДНЫЕ LINQ-ЗАПРОСЫ =====
        
        /// <summary>
        /// Создать типобезопасный древовидный запрос по типу (автоматически определит схему по имени класса)
        /// Поддерживает иерархические операторы: WhereHasAncestor, WhereHasDescendant, WhereLevel, WhereRoots, WhereLeaves
        /// </summary>
        Task<ITreeQueryable<TProps>> TreeQueryAsync<TProps>() where TProps : class, new();
        
        /// <summary>
        /// Создать типобезопасный древовидный запрос по типу с указанным пользователем
        /// Поддерживает иерархические операторы: WhereHasAncestor, WhereHasDescendant, WhereLevel, WhereRoots, WhereLeaves
        /// </summary>
        Task<ITreeQueryable<TProps>> TreeQueryAsync<TProps>(IRedbUser user) where TProps : class, new();
        
        /// <summary>
        /// Создать типобезопасный древовидный запрос по типу (синхронно)
        /// Поддерживает иерархические операторы: WhereHasAncestor, WhereHasDescendant, WhereLevel, WhereRoots, WhereLeaves
        /// </summary>
        ITreeQueryable<TProps> TreeQuery<TProps>() where TProps : class, new();
        
        /// <summary>
        /// Создать типобезопасный древовидный запрос по типу с указанным пользователем (синхронно)
        /// Поддерживает иерархические операторы: WhereHasAncestor, WhereHasDescendant, WhereLevel, WhereRoots, WhereLeaves
        /// </summary>
        ITreeQueryable<TProps> TreeQuery<TProps>(IRedbUser user) where TProps : class, new();
        
        // ===== ДРЕВОВИДНЫЕ LINQ С ОГРАНИЧЕНИЕМ ПОДДЕРЕВА =====
        
        /// <summary>
        /// Создать древовидный запрос ограниченный поддеревом конкретного объекта (по ID)
        /// Поиск будет выполняться только среди потомков указанного rootObjectId
        /// </summary>
        Task<ITreeQueryable<TProps>> TreeQueryAsync<TProps>(long rootObjectId, int? maxDepth = null) where TProps : class, new();
        
        /// <summary>
        /// 🚀 ЗАКАЗЧИК: Создать древовидный запрос ограниченный поддеревом конкретного объекта
        /// Поиск будет выполняться только среди потомков указанного rootObject
        /// Если rootObject = null, возвращает пустой queryable (удобнее для клиентского кода)
        /// </summary>
        Task<ITreeQueryable<TProps>> TreeQueryAsync<TProps>(IRedbObject? rootObject, int? maxDepth = null) where TProps : class, new();
        
        /// <summary>
        /// 🚀 ЗАКАЗЧИК: Создать древовидный запрос ограниченный поддеревьями списка объектов  
        /// Поиск будет выполняться среди потомков ЛЮБОГО из указанных rootObjects
        /// Если список пустой, возвращает пустой queryable (удобнее для клиентского кода)
        /// </summary>
        Task<ITreeQueryable<TProps>> TreeQueryAsync<TProps>(IEnumerable<IRedbObject> rootObjects, int? maxDepth = null) where TProps : class, new();
        
        /// <summary>
        /// Создать древовидный запрос ограниченный поддеревом с указанным пользователем (по ID)
        /// Поиск будет выполняться только среди потомков указанного rootObjectId
        /// </summary>
        Task<ITreeQueryable<TProps>> TreeQueryAsync<TProps>(long rootObjectId, IRedbUser user, int? maxDepth = null) where TProps : class, new();
        
        // ===== ДРЕВОВИДНЫЕ LINQ С ПОЛЬЗОВАТЕЛЯМИ И РАСШИРЕННЫМИ ВОЗМОЖНОСТЯМИ =====
        
        /// <summary>
        /// 🚀 ЗАКАЗЧИК: Создать древовидный запрос ограниченный поддеревом с указанным пользователем
        /// Если rootObject = null, возвращает пустой queryable (удобнее для клиентского кода) 
        /// </summary>
        Task<ITreeQueryable<TProps>> TreeQueryAsync<TProps>(IRedbObject? rootObject, IRedbUser user, int? maxDepth = null) where TProps : class, new();
        
        /// <summary>
        /// 🚀 ЗАКАЗЧИК: Создать древовидный запрос ограниченный поддеревьями списка объектов с указанным пользователем
        /// Если список пустой, возвращает пустой queryable (удобнее для клиентского кода)
        /// </summary>
        Task<ITreeQueryable<TProps>> TreeQueryAsync<TProps>(IEnumerable<IRedbObject> rootObjects, IRedbUser user, int? maxDepth = null) where TProps : class, new();
        
        // ===== СИНХРОННЫЕ ВЕРСИИ =====
        
        /// <summary>
        /// Создать древовидный запрос ограниченный поддеревом (синхронно, по ID)
        /// Поиск будет выполняться только среди потомков указанного rootObjectId
        /// </summary>
        ITreeQueryable<TProps> TreeQuery<TProps>(long rootObjectId, int? maxDepth = null) where TProps : class, new();
        
        /// <summary>
        /// 🚀 ЗАКАЗЧИК: Создать древовидный запрос ограниченный поддеревом (синхронно)
        /// Если rootObject = null, возвращает пустой queryable (удобнее для клиентского кода)
        /// </summary>
        ITreeQueryable<TProps> TreeQuery<TProps>(IRedbObject? rootObject, int? maxDepth = null) where TProps : class, new();
        
        /// <summary>
        /// 🚀 ЗАКАЗЧИК: Создать древовидный запрос ограниченный поддеревьями списка объектов (синхронно)
        /// Если список пустой, возвращает пустой queryable (удобнее для клиентского кода)
        /// </summary>
        ITreeQueryable<TProps> TreeQuery<TProps>(IEnumerable<IRedbObject> rootObjects, int? maxDepth = null) where TProps : class, new();
        
        /// <summary>
        /// Создать древовидный запрос ограниченный поддеревом с указанным пользователем (синхронно, по ID)
        /// Поиск будет выполняться только среди потомков указанного rootObjectId
        /// </summary>
        ITreeQueryable<TProps> TreeQuery<TProps>(long rootObjectId, IRedbUser user, int? maxDepth = null) where TProps : class, new();
        
        /// <summary>
        /// 🚀 ЗАКАЗЧИК: Создать древовидный запрос ограниченный поддеревом с указанным пользователем (синхронно)
        /// Если rootObject = null, возвращает пустой queryable (удобнее для клиентского кода)
        /// </summary>
        ITreeQueryable<TProps> TreeQuery<TProps>(IRedbObject? rootObject, IRedbUser user, int? maxDepth = null) where TProps : class, new();
        
        /// <summary>
        /// 🚀 ЗАКАЗЧИК: Создать древовидный запрос ограниченный поддеревьями списка объектов с указанным пользователем (синхронно)
        /// Если список пустой, возвращает пустой queryable (удобнее для клиентского кода)
        /// </summary>
        ITreeQueryable<TProps> TreeQuery<TProps>(IEnumerable<IRedbObject> rootObjects, IRedbUser user, int? maxDepth = null) where TProps : class, new();
    }
}
