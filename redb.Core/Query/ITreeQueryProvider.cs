using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using redb.Core.Models.Contracts;

namespace redb.Core.Query;

/// <summary>
/// Расширенный провайдер для выполнения древовидных LINQ-запросов
/// Наследует базовый функционал IRedbQueryProvider и добавляет поддержку иерархических ограничений
/// </summary>
public interface ITreeQueryProvider : IRedbQueryProvider
{
    /// <summary>
    /// Создать древовидный запрос с поддержкой иерархических ограничений
    /// Использует SQL функцию search_tree_objects_with_facets() вместо search_objects_with_facets()
    /// </summary>
    /// <param name="schemeId">ID схемы объектов</param>
    /// <param name="userId">ID пользователя для проверки прав (null = текущий пользователь)</param>
    /// <param name="checkPermissions">Проверять права доступа к объектам</param>
    /// <param name="rootObjectId">Ограничение поиска поддеревом указанного корня (null = весь лес)</param>
    /// <param name="maxDepth">Максимальная глубина поиска в дереве (null = без ограничений)</param>
    /// <returns>Типизированный древовидный запрос</returns>
    ITreeQueryable<TProps> CreateTreeQuery<TProps>(
        long schemeId, 
        long? userId = null, 
        bool checkPermissions = false,
        long? rootObjectId = null,
        int? maxDepth = null
    ) where TProps : class, new();
}
