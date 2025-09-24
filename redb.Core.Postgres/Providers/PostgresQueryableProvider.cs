using redb.Core.Providers;
using redb.Core.Query;
using redb.Core.Query.QueryExpressions;
using redb.Core.Postgres.Query;
using redb.Core.Serialization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using redb.Core.Models.Contracts;
using redb.Core.Models.Configuration;
using redb.Core.Models.Security;

namespace redb.Core.Postgres.Providers
{
    /// <summary>
    /// PostgreSQL реализация провайдера LINQ-запросов
    /// </summary>
    public class PostgresQueryableProvider : IQueryableProvider
    {
        private readonly RedbContext _context;
        private readonly IRedbObjectSerializer _serializer;
        private readonly ISchemeSyncProvider _schemeSync;
        private readonly ILogger? _logger;
        private readonly IRedbSecurityContext _securityContext;
        private readonly RedbServiceConfiguration _configuration;

        public PostgresQueryableProvider(
            RedbContext context,
            IRedbObjectSerializer serializer,
            ISchemeSyncProvider schemeSync,
            IRedbSecurityContext securityContext,
            RedbServiceConfiguration? configuration = null,
            ILogger? logger = null)
        {
            _context = context;
            _serializer = serializer;
            _schemeSync = schemeSync;
            _securityContext = securityContext;
            _configuration = configuration ?? new RedbServiceConfiguration();
            _logger = logger;
        }

        // ===== МЕТОДЫ ИЗ КОНТРАКТА IQueryableProvider =====

        // ===== ДРЕВОВИДНЫЕ LINQ-ЗАПРОСЫ =====

        public async Task<ITreeQueryable<TProps>> TreeQueryAsync<TProps>() where TProps : class, new()
        {
            var scheme = await _schemeSync.GetSchemeByTypeAsync<TProps>();
            if (scheme == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            var effectiveUser = _securityContext.GetEffectiveUser();
            return TreeQueryPrivate<TProps>(scheme.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnQuery);
        }

        public async Task<ITreeQueryable<TProps>> TreeQueryAsync<TProps>(IRedbUser user) where TProps : class, new()
        {
            var scheme = await _schemeSync.GetSchemeByTypeAsync<TProps>();
            if (scheme == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            return TreeQueryPrivate<TProps>(scheme.Id, user.Id, _configuration.DefaultCheckPermissionsOnQuery);
        }

        public ITreeQueryable<TProps> TreeQuery<TProps>() where TProps : class, new()
        {
            var scheme = _schemeSync.GetSchemeByTypeAsync<TProps>().Result;
            if (scheme == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            var effectiveUser = _securityContext.GetEffectiveUser();
            return TreeQueryPrivate<TProps>(scheme.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnQuery);
        }

        public ITreeQueryable<TProps> TreeQuery<TProps>(IRedbUser user) where TProps : class, new()
        {
            var scheme = _schemeSync.GetSchemeByTypeAsync<TProps>().Result;
            if (scheme == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            return TreeQueryPrivate<TProps>(scheme.Id, user.Id, _configuration.DefaultCheckPermissionsOnQuery);
        }

        // ===== ДРЕВОВИДНЫЕ LINQ С ОГРАНИЧЕНИЕМ ПОДДЕРЕВА =====

        public async Task<ITreeQueryable<TProps>> TreeQueryAsync<TProps>(long rootObjectId, int? maxDepth = null) where TProps : class, new()
        {
            var scheme = await _schemeSync.GetSchemeByTypeAsync<TProps>();
            if (scheme == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            var effectiveUser = _securityContext.GetEffectiveUser();
            return TreeQueryPrivate<TProps>(scheme.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnQuery, rootObjectId, maxDepth);
        }

        /// <summary>
        /// 🚀 ЗАКАЗЧИК: TreeQuery с nullable rootObject - удобнее для клиентского кода
        /// </summary>
        public async Task<ITreeQueryable<TProps>> TreeQueryAsync<TProps>(IRedbObject? rootObject, int? maxDepth = null) where TProps : class, new()
        {
            // Если rootObject = null, возвращаем пустой queryable (удобнее чем исключение)
            if (rootObject == null)
            {
                var scheme = await _schemeSync.GetSchemeByTypeAsync<TProps>();
                if (scheme == null)
                    throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

                var effectiveUser = _securityContext.GetEffectiveUser();
                return CreateEmptyTreeQuery<TProps>(scheme.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnQuery);
            }

            var schemeResolved = await _schemeSync.GetSchemeByTypeAsync<TProps>();
            if (schemeResolved == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            var effectiveUserResolved = _securityContext.GetEffectiveUser();
            return TreeQueryPrivate<TProps>(schemeResolved.Id, effectiveUserResolved.Id, _configuration.DefaultCheckPermissionsOnQuery, rootObject.Id, maxDepth);
        }

        /// <summary>
        /// 🚀 ЗАКАЗЧИК: TreeQuery с списком rootObjects - поиск среди потомков ЛЮБОГО из объектов
        /// </summary>
        public async Task<ITreeQueryable<TProps>> TreeQueryAsync<TProps>(IEnumerable<IRedbObject> rootObjects, int? maxDepth = null) where TProps : class, new()
        {
            var rootList = rootObjects?.ToList() ?? new List<IRedbObject>();
            
            // Если список пустой, возвращаем пустой queryable (удобнее чем исключение)
            if (!rootList.Any())
            {
                var scheme = await _schemeSync.GetSchemeByTypeAsync<TProps>();
                if (scheme == null)
                    throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

                var effectiveUser = _securityContext.GetEffectiveUser();
                return CreateEmptyTreeQuery<TProps>(scheme.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnQuery);
            }

            // Если один объект, используем обычный TreeQuery  
            if (rootList.Count == 1)
            {
                return await TreeQueryAsync<TProps>(rootList.First(), maxDepth);
            }

            // Если несколько объектов, строим $or фильтр с $descendantsOf для каждого
            var schemeResolved = await _schemeSync.GetSchemeByTypeAsync<TProps>();
            if (schemeResolved == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            var effectiveUserResolved = _securityContext.GetEffectiveUser();
            return CreateMultiRootTreeQuery<TProps>(schemeResolved.Id, effectiveUserResolved.Id, _configuration.DefaultCheckPermissionsOnQuery, rootList, maxDepth);
        }

        public async Task<ITreeQueryable<TProps>> TreeQueryAsync<TProps>(long rootObjectId, IRedbUser user, int? maxDepth = null) where TProps : class, new()
        {
            var scheme = await _schemeSync.GetSchemeByTypeAsync<TProps>();
            if (scheme == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            return TreeQueryPrivate<TProps>(scheme.Id, user.Id, _configuration.DefaultCheckPermissionsOnQuery, rootObjectId, maxDepth);
        }

        /// <summary>
        /// 🚀 ЗАКАЗЧИК: TreeQuery с nullable rootObject и пользователем
        /// </summary>
        public async Task<ITreeQueryable<TProps>> TreeQueryAsync<TProps>(IRedbObject? rootObject, IRedbUser user, int? maxDepth = null) where TProps : class, new()
        {
            // Если rootObject = null, возвращаем пустой queryable
            if (rootObject == null)
            {
                var scheme = await _schemeSync.GetSchemeByTypeAsync<TProps>();
                if (scheme == null)
                    throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

                return CreateEmptyTreeQuery<TProps>(scheme.Id, user.Id, _configuration.DefaultCheckPermissionsOnQuery);
            }

            var schemeResolved = await _schemeSync.GetSchemeByTypeAsync<TProps>();
            if (schemeResolved == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            return TreeQueryPrivate<TProps>(schemeResolved.Id, user.Id, _configuration.DefaultCheckPermissionsOnQuery, rootObject.Id, maxDepth);
        }

        /// <summary>
        /// 🚀 ЗАКАЗЧИК: TreeQuery с списком rootObjects и пользователем
        /// </summary>
        public async Task<ITreeQueryable<TProps>> TreeQueryAsync<TProps>(IEnumerable<IRedbObject> rootObjects, IRedbUser user, int? maxDepth = null) where TProps : class, new()
        {
            var rootList = rootObjects?.ToList() ?? new List<IRedbObject>();
            
            // Если список пустой, возвращаем пустой queryable
            if (!rootList.Any())
            {
                var scheme = await _schemeSync.GetSchemeByTypeAsync<TProps>();
                if (scheme == null)
                    throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

                return CreateEmptyTreeQuery<TProps>(scheme.Id, user.Id, _configuration.DefaultCheckPermissionsOnQuery);
            }

            // Если один объект, используем обычный TreeQuery  
            if (rootList.Count == 1)
            {
                return await TreeQueryAsync<TProps>(rootList.First(), user, maxDepth);
            }

            // Если несколько объектов, строим составной запрос
            var schemeResolved = await _schemeSync.GetSchemeByTypeAsync<TProps>();
            if (schemeResolved == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            return CreateMultiRootTreeQuery<TProps>(schemeResolved.Id, user.Id, _configuration.DefaultCheckPermissionsOnQuery, rootList, maxDepth);
        }

        // ===== СИНХРОННЫЕ МЕТОДЫ ДЛЯ ДРЕВОВИДНЫХ LINQ С ОГРАНИЧЕНИЕМ ПОДДЕРЕВА =====

        public ITreeQueryable<TProps> TreeQuery<TProps>(long rootObjectId, int? maxDepth = null) where TProps : class, new()
        {
            var scheme = _schemeSync.GetSchemeByTypeAsync<TProps>().Result;
            if (scheme == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            var effectiveUser = _securityContext.GetEffectiveUser();
            return TreeQueryPrivate<TProps>(scheme.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnQuery, rootObjectId, maxDepth);
        }

                /// <summary>
        /// 🚀 ЗАКАЗЧИК: Синхронный TreeQuery с nullable rootObject
        /// </summary>
        public ITreeQueryable<TProps> TreeQuery<TProps>(IRedbObject? rootObject, int? maxDepth = null) where TProps : class, new()
        {
            // Если rootObject = null, возвращаем пустой queryable
            if (rootObject == null)
            {
                var scheme = _schemeSync.GetSchemeByTypeAsync<TProps>().Result;
                if (scheme == null)
                    throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

                var effectiveUser = _securityContext.GetEffectiveUser();
                return CreateEmptyTreeQuery<TProps>(scheme.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnQuery);
            }

            var schemeResolved = _schemeSync.GetSchemeByTypeAsync<TProps>().Result;
            if (schemeResolved == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            var effectiveUserResolved = _securityContext.GetEffectiveUser();
            return TreeQueryPrivate<TProps>(schemeResolved.Id, effectiveUserResolved.Id, _configuration.DefaultCheckPermissionsOnQuery, rootObject.Id, maxDepth);
        }

        /// <summary>
        /// 🚀 ЗАКАЗЧИК: Синхронный TreeQuery с списком rootObjects
        /// </summary>
        public ITreeQueryable<TProps> TreeQuery<TProps>(IEnumerable<IRedbObject> rootObjects, int? maxDepth = null) where TProps : class, new()
        {
            var rootList = rootObjects?.ToList() ?? new List<IRedbObject>();
            
            // Если список пустой, возвращаем пустой queryable
            if (!rootList.Any())
            {
                var scheme = _schemeSync.GetSchemeByTypeAsync<TProps>().Result;
                if (scheme == null)
                    throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

                var effectiveUser = _securityContext.GetEffectiveUser();
                return CreateEmptyTreeQuery<TProps>(scheme.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnQuery);
            }

            // Если один объект, используем обычный TreeQuery  
            if (rootList.Count == 1)
            {
                return TreeQuery<TProps>(rootList.First(), maxDepth);
            }

            // Если несколько объектов, строим составной запрос
            var schemeResolved = _schemeSync.GetSchemeByTypeAsync<TProps>().Result;
            if (schemeResolved == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            var effectiveUserResolved = _securityContext.GetEffectiveUser();
            return CreateMultiRootTreeQuery<TProps>(schemeResolved.Id, effectiveUserResolved.Id, _configuration.DefaultCheckPermissionsOnQuery, rootList, maxDepth);
        }

        public ITreeQueryable<TProps> TreeQuery<TProps>(long rootObjectId, IRedbUser user, int? maxDepth = null) where TProps : class, new()
        {
            var scheme = _schemeSync.GetSchemeByTypeAsync<TProps>().Result;
            if (scheme == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            return TreeQueryPrivate<TProps>(scheme.Id, user.Id, _configuration.DefaultCheckPermissionsOnQuery, rootObjectId, maxDepth);
        }

        /// <summary>
        /// 🚀 ЗАКАЗЧИК: Синхронный TreeQuery с nullable rootObject и пользователем
        /// </summary>
        public ITreeQueryable<TProps> TreeQuery<TProps>(IRedbObject? rootObject, IRedbUser user, int? maxDepth = null) where TProps : class, new()
        {
            // Если rootObject = null, возвращаем пустой queryable
            if (rootObject == null)
            {
                var scheme = _schemeSync.GetSchemeByTypeAsync<TProps>().Result;
                if (scheme == null)
                    throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

                return CreateEmptyTreeQuery<TProps>(scheme.Id, user.Id, _configuration.DefaultCheckPermissionsOnQuery);
            }

            var schemeResolved = _schemeSync.GetSchemeByTypeAsync<TProps>().Result;
            if (schemeResolved == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            return TreeQueryPrivate<TProps>(schemeResolved.Id, user.Id, _configuration.DefaultCheckPermissionsOnQuery, rootObject.Id, maxDepth);
        }

        /// <summary>
        /// 🚀 ЗАКАЗЧИК: Синхронный TreeQuery с списком rootObjects и пользователем
        /// </summary>
        public ITreeQueryable<TProps> TreeQuery<TProps>(IEnumerable<IRedbObject> rootObjects, IRedbUser user, int? maxDepth = null) where TProps : class, new()
        {
            var rootList = rootObjects?.ToList() ?? new List<IRedbObject>();
            
            // Если список пустой, возвращаем пустой queryable
            if (!rootList.Any())
            {
                var scheme = _schemeSync.GetSchemeByTypeAsync<TProps>().Result;
                if (scheme == null)
                    throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

                return CreateEmptyTreeQuery<TProps>(scheme.Id, user.Id, _configuration.DefaultCheckPermissionsOnQuery);
            }

            // Если один объект, используем обычный TreeQuery  
            if (rootList.Count == 1)
            {
                return TreeQuery<TProps>(rootList.First(), user, maxDepth);
            }

            // Если несколько объектов, строим составной запрос
            var schemeResolved = _schemeSync.GetSchemeByTypeAsync<TProps>().Result;
            if (schemeResolved == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            return CreateMultiRootTreeQuery<TProps>(schemeResolved.Id, user.Id, _configuration.DefaultCheckPermissionsOnQuery, rootList, maxDepth);
        }

        public async Task<IRedbQueryable<TProps>> QueryAsync<TProps>() where TProps : class, new()
        {
            var scheme = await _schemeSync.GetSchemeByTypeAsync<TProps>();
            if (scheme == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            var effectiveUser = _securityContext.GetEffectiveUser();
            return QueryPrivate<TProps>(scheme.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnQuery);
        }

        public async Task<IRedbQueryable<TProps>> QueryAsync<TProps>(IRedbUser user) where TProps : class, new()
        {
            var scheme = await _schemeSync.GetSchemeByTypeAsync<TProps>();
            if (scheme == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            return QueryPrivate<TProps>(scheme.Id, user.Id, _configuration.DefaultCheckPermissionsOnQuery);
        }

        public IRedbQueryable<TProps> Query<TProps>() where TProps : class, new()
        {
            var scheme = _schemeSync.GetSchemeByTypeAsync<TProps>().Result;
            if (scheme == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            var effectiveUser = _securityContext.GetEffectiveUser();
            return QueryPrivate<TProps>(scheme.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnQuery);
        }

        public IRedbQueryable<TProps> Query<TProps>(IRedbUser user) where TProps : class, new()
        {
            var scheme = _schemeSync.GetSchemeByTypeAsync<TProps>().Result;
            if (scheme == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");

            return QueryPrivate<TProps>(scheme.Id, user.Id, _configuration.DefaultCheckPermissionsOnQuery);
        }

        // ===== ПРИВАТНЫЕ МЕТОДЫ (низкоуровневый доступ) =====

        private IRedbQueryable<TProps> QueryPrivate<TProps>(long schemeId, long? userId = null, bool checkPermissions = false) where TProps : class, new()
        {
            var queryProvider = new PostgresQueryProvider(_context, _serializer, _logger);
            return queryProvider.CreateQuery<TProps>(schemeId, userId, checkPermissions);
        }

        private async Task<IRedbQueryable<TProps>> QueryAsyncPrivate<TProps>(long? userId = null, bool checkPermissions = false) where TProps : class, new()
        {
            var scheme = await _schemeSync.EnsureSchemeFromTypeAsync<TProps>();
            return QueryPrivate<TProps>(scheme.Id, userId, checkPermissions);
        }

        // ===== ПРИВАТНЫЕ МЕТОДЫ ДЛЯ ДРЕВОВИДНЫХ LINQ =====

        private ITreeQueryable<TProps> TreeQueryPrivate<TProps>(long schemeId, long? userId = null, bool checkPermissions = false, long? rootObjectId = null, int? maxDepth = null) where TProps : class, new()
        {
            var treeQueryProvider = new PostgresTreeQueryProvider(_context, _serializer, _logger);
            return treeQueryProvider.CreateTreeQuery<TProps>(schemeId, userId, checkPermissions, rootObjectId, maxDepth);
        }

        /// <summary>
        /// 🚀 ЗАКАЗЧИК: Создать пустой TreeQueryable (удобно когда rootObject = null)
        /// Возвращает queryable который при выполнении даст пустой список
        /// </summary>
        private ITreeQueryable<TProps> CreateEmptyTreeQuery<TProps>(long schemeId, long? userId = null, bool checkPermissions = false) where TProps : class, new()
        {
            var treeQueryProvider = new PostgresTreeQueryProvider(_context, _serializer, _logger);
            
            // ✅ ИСПРАВЛЕНИЕ: Создаем пустой TreeQueryable правильно, без приведения типов
            var emptyTreeQuery = treeQueryProvider.CreateTreeQuery<TProps>(schemeId, userId, checkPermissions);
            
            // Добавляем фильтр который никогда не выполняется: WHERE 1=0
            // ✅ ИСПРАВЛЕНИЕ: Where() возвращает IRedbQueryable, приводим к ITreeQueryable
            var emptyQueryWithFilter = emptyTreeQuery.Where(x => false);
            return (ITreeQueryable<TProps>)emptyQueryWithFilter;
        }

        /// <summary>
        /// 🚀 ЗАКАЗЧИК: Создать TreeQueryable для поиска среди потомков ЛЮБОГО из списка rootObjects
        /// Использует $or фильтр с $descendantsOf для каждого объекта
        /// </summary>
        private ITreeQueryable<TProps> CreateMultiRootTreeQuery<TProps>(long schemeId, long? userId, bool checkPermissions, List<IRedbObject> rootObjects, int? maxDepth) where TProps : class, new()
        {
            var treeQueryProvider = new PostgresTreeQueryProvider(_context, _serializer, _logger);
            
            // ✅ ПОЛНАЯ РЕАЛИЗАЦИЯ: Используем ParentIds для множественных корней
            var parentIds = rootObjects.Select(obj => obj.Id).ToArray();
            
            // Создаем специальный контекст с множественными родителями
            var multiRootContext = new TreeQueryContext<TProps>(schemeId, userId, checkPermissions, null, maxDepth)
            {
                ParentIds = parentIds  // ✅ ИСПОЛЬЗУЕМ НОВОЕ ПОЛЕ ParentIds[]
            };
            
            // Создаем TreeQueryable с контекстом множественных родителей
            var filterParser = new PostgresFilterExpressionParser();
            var orderingParser = new PostgresOrderingExpressionParser();
            
            return new PostgresTreeQueryable<TProps>(treeQueryProvider, multiRootContext, filterParser, orderingParser);
        }

    }
}
