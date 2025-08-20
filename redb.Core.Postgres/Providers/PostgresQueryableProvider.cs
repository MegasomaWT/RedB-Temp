using redb.Core.Providers;
using redb.Core.Query;
using redb.Core.Postgres.Query;
using redb.Core.Serialization;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using redb.Core.Models.Contracts;
using redb.Core.Models.Configuration;
using redb.Core.Models.Security;
using System;

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
        
        public IRedbQueryable<TProps> Query<TProps>(IRedbScheme scheme, IRedbUser user) where TProps : class, new()
        {
            return QueryPrivate<TProps>(scheme.Id, user.Id, _configuration.DefaultCheckPermissionsOnQuery);
        }

        public IRedbQueryable<TProps> Query<TProps>(IRedbScheme scheme) where TProps : class, new()
        {
            var effectiveUser = _securityContext.GetEffectiveUser();
            return QueryPrivate<TProps>(scheme.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnQuery);
        }

        public async Task<IRedbQueryable<TProps>> QueryAsync<TProps>(IRedbScheme scheme, IRedbUser user) where TProps : class, new()
        {
            return QueryPrivate<TProps>(scheme.Id, user.Id, _configuration.DefaultCheckPermissionsOnQuery);
        }

        public async Task<IRedbQueryable<TProps>> QueryAsync<TProps>(IRedbScheme scheme) where TProps : class, new()
        {
            var effectiveUser = _securityContext.GetEffectiveUser();
            return QueryPrivate<TProps>(scheme.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnQuery);
        }
        
        public async Task<IRedbQueryable<TProps>> QueryAsync<TProps>(string schemeName) where TProps : class, new()
        {
            var scheme = await _schemeSync.GetSchemeByNameAsync(schemeName);
            if (scheme == null)
                throw new InvalidOperationException($"Схема '{schemeName}' не найдена");
            
            var effectiveUser = _securityContext.GetEffectiveUser();
            return QueryPrivate<TProps>(scheme.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnQuery);
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

        private async Task<IRedbQueryable<TProps>> QueryAsyncPrivate<TProps>(string? schemeName = null, long? userId = null, bool checkPermissions = false) where TProps : class, new()
        {
            var scheme = await _schemeSync.EnsureSchemeFromTypeAsync<TProps>(schemeName);
            return QueryPrivate<TProps>(scheme.Id, userId, checkPermissions);
        }

        private IRedbQueryable<TProps> QueryChildrenPrivate<TProps>(long schemeId, long parentId, long? userId = null, bool checkPermissions = false) where TProps : class, new()
        {
            var queryProvider = new PostgresQueryProvider(_context, _serializer, _logger);
            return queryProvider.CreateChildrenQuery<TProps>(schemeId, parentId, userId, checkPermissions);
        }

        // ===== МЕТОДЫ ДЛЯ РАБОТЫ С ДОЧЕРНИМИ ОБЪЕКТАМИ =====
        
        public async Task<IRedbQueryable<TProps>> QueryChildrenAsync<TProps>(IRedbObject parentObj) where TProps : class, new()
        {
            var scheme = await _schemeSync.GetSchemeByTypeAsync<TProps>();
            if (scheme == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");
            
            var effectiveUser = _securityContext.GetEffectiveUser();
            return QueryChildrenPrivate<TProps>(scheme.Id, parentObj.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnQuery);
        }
        
        public async Task<IRedbQueryable<TProps>> QueryChildrenAsync<TProps>(IRedbObject parentObj, IRedbUser user) where TProps : class, new()
        {
            var scheme = await _schemeSync.GetSchemeByTypeAsync<TProps>();
            if (scheme == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");
            
            return QueryChildrenPrivate<TProps>(scheme.Id, parentObj.Id, user.Id, _configuration.DefaultCheckPermissionsOnQuery);
        }
        
        public IRedbQueryable<TProps> QueryChildren<TProps>(IRedbObject parentObj) where TProps : class, new()
        {
            var scheme = _schemeSync.GetSchemeByTypeAsync<TProps>().Result;
            if (scheme == null)
                throw new InvalidOperationException($"Схема для типа '{typeof(TProps).Name}' не найдена");
            
            var effectiveUser = _securityContext.GetEffectiveUser();
            return QueryChildrenPrivate<TProps>(scheme.Id, parentObj.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnQuery);
        }

    }
}
