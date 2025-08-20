using redb.Core.Query;
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
        /// Создать типобезопасный запрос для схемы с указанным пользователем
        /// </summary>
        IRedbQueryable<TProps> Query<TProps>(IRedbScheme scheme, IRedbUser user) where TProps : class, new();

        /// <summary>
        /// Создать типобезопасный запрос для схемы с текущим пользователем из контекста
        /// </summary>
        IRedbQueryable<TProps> Query<TProps>(IRedbScheme scheme) where TProps : class, new();

        /// <summary>
        /// Создать типобезопасный запрос для схемы с указанным пользователем (асинхронно)
        /// </summary>
        Task<IRedbQueryable<TProps>> QueryAsync<TProps>(IRedbScheme scheme, IRedbUser user) where TProps : class, new();

        /// <summary>
        /// Создать типобезопасный запрос для схемы с текущим пользователем из контекста (асинхронно)
        /// </summary>
        Task<IRedbQueryable<TProps>> QueryAsync<TProps>(IRedbScheme scheme) where TProps : class, new();
        
        /// <summary>
        /// Создать типобезопасный запрос по имени схемы (автоматически найдет схему)
        /// </summary>
        Task<IRedbQueryable<TProps>> QueryAsync<TProps>(string schemeName) where TProps : class, new();
        
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
        
        /// <summary>
        /// Создать типобезопасный запрос для дочерних объектов (автоматически определит схему по типу)
        /// </summary>
        Task<IRedbQueryable<TProps>> QueryChildrenAsync<TProps>(IRedbObject parentObj) where TProps : class, new();
        
        /// <summary>
        /// Создать типобезопасный запрос для дочерних объектов с указанным пользователем
        /// </summary>
        Task<IRedbQueryable<TProps>> QueryChildrenAsync<TProps>(IRedbObject parentObj, IRedbUser user) where TProps : class, new();
        
        /// <summary>
        /// Синхронная версия запроса дочерних объектов
        /// </summary>
        IRedbQueryable<TProps> QueryChildren<TProps>(IRedbObject parentObj) where TProps : class, new();
    }
}
