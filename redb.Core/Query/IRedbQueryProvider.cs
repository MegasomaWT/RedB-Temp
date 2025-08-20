using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace redb.Core.Query;

/// <summary>
/// Провайдер для выполнения LINQ-запросов
/// </summary>
public interface IRedbQueryProvider
{
    /// <summary>
    /// Создать новый запрос для указанной схемы
    /// </summary>
    IRedbQueryable<TProps> CreateQuery<TProps>(long schemeId, long? userId = null, bool checkPermissions = false) 
        where TProps : class, new();
    
    /// <summary>
    /// Выполнить запрос асинхронно
    /// </summary>
    Task<object> ExecuteAsync(Expression expression, Type elementType);
}
