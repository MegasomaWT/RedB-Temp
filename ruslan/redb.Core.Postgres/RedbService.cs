using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using redb.Core.DBModels;
using redb.Core.Providers;
using redb.Core.Query;
using redb.Core.Serialization;
using redb.Core.Postgres.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using redb.Core.Models.Enums;
using redb.Core.Models.Permissions;
using redb.Core.Models.Contracts;
using redb.Core.Models.Security;
using redb.Core.Models.Entities;
using redb.Core.Models.Configuration;

namespace redb.Core.Postgres
{
    /// <summary>
    /// PostgreSQL реализация IRedbService - композиция всех провайдеров
    /// </summary>
    public class RedbService : IRedbService
    {
        private readonly RedbContext _context;
        private readonly ISchemeSyncProvider _schemeSync;
        private readonly IObjectStorageProvider _objectStorage;
        private readonly ITreeProvider _treeProvider;
        private readonly IPermissionProvider _permissionProvider;
        private readonly IQueryableProvider _queryProvider;
        private readonly IValidationProvider _validationProvider;
        private readonly IRedbSecurityContext _securityContext;
        private readonly IUserProvider _userProvider;
        private readonly IRoleProvider _roleProvider;
        private RedbServiceConfiguration _configuration;

        public Core.RedbContext RedbContext => _context;
        public IRedbSecurityContext SecurityContext => _securityContext;
        public IUserProvider UserProvider => _userProvider;
        public IRoleProvider RoleProvider => _roleProvider;
        public RedbServiceConfiguration Configuration => _configuration;

        public RedbService(IServiceProvider serviceProvider)
        {
            _context = serviceProvider.GetService<RedbContext>() ?? 
                throw new InvalidOperationException(
                    "RedbContext не зарегистрирован в DI контейнере. " +
                    "Добавьте services.AddDbContext<RedbContext>() в конфигурацию.");
            var serializer = serviceProvider.GetService<IRedbObjectSerializer>() ?? new SystemTextJsonRedbSerializer();
            var logger = serviceProvider.GetService<ILogger<RedbService>>() as ILogger;

            // Инициализация конфигурации
            _configuration = serviceProvider.GetService<RedbServiceConfiguration>() ?? new RedbServiceConfiguration();

            // Инициализация контекста безопасности
            _securityContext = serviceProvider.GetService<IRedbSecurityContext>() ?? 
                              AmbientSecurityContext.GetOrCreateDefault();

            // Создание провайдеров
            _schemeSync = new PostgresSchemeSyncProvider(_context, _configuration);
            _permissionProvider = new PostgresPermissionProvider(_context, _securityContext);
            _userProvider = new PostgresUserProvider(_context, _securityContext);
            _roleProvider = new PostgresRoleProvider(_context, _securityContext);
            _objectStorage = new PostgresObjectStorageProvider(_context, serializer, _permissionProvider, _securityContext, _schemeSync, _configuration);
            _treeProvider = new PostgresTreeProvider(_context, _objectStorage, _permissionProvider, serializer, _securityContext, _configuration, _schemeSync);
            _queryProvider = new PostgresQueryableProvider(_context, serializer, _schemeSync, _securityContext, _configuration, logger);
            _validationProvider = new PostgresValidationProvider(_context);
        }

        // === МЕТАДАННЫЕ БАЗЫ ДАННЫХ ===
        public string dbVersion => _context.Database.SqlQueryRaw<string>("SELECT version() \"Value\"").First();
        public string dbType => _context.Database.IsNpgsql() ? "Postgresql" : "undefined";
        public string dbMigration => _context.Database.GetMigrations().Last();
        public int? dbSize => _context.Database.SqlQueryRaw<int>("select pg_database_size(current_database()) \"Value\"").First();

        // === БАЗОВЫЕ EF CORE ОПЕРАЦИИ (legacy) ===
        public IQueryable<T> GetAll<T>() where T : class => _context.Set<T>();
        public Task<T?> GetById<T>(long id) where T : class => _context.FindAsync<T>(id).AsTask();
        public Task<int> DeleteById<T>(long id) where T : class => _context.Set<T>().Where(e => EF.Property<long>(e, "Id") == id).ExecuteDeleteAsync();

        // === ДЕЛЕГИРОВАНИЕ К ПРОВАЙДЕРАМ ===

        // ISchemeSyncProvider
        public Task<IRedbScheme> EnsureSchemeFromTypeAsync<TProps>(string? schemeName = null, string? alias = null) where TProps : class
            => _schemeSync.EnsureSchemeFromTypeAsync<TProps>(schemeName, alias);
        
        public Task<List<IRedbStructure>> SyncStructuresFromTypeAsync<TProps>(IRedbScheme scheme, bool strictDeleteExtra = true) where TProps : class
            => _schemeSync.SyncStructuresFromTypeAsync<TProps>(scheme, strictDeleteExtra);
        
        public Task<IRedbScheme> SyncSchemeAsync<TProps>() where TProps : class
            => _schemeSync.SyncSchemeAsync<TProps>();

        // === НОВЫЕ МЕТОДЫ ИЗ ISchemeSyncProvider ===
        public Task<IRedbScheme?> GetSchemeByTypeAsync<TProps>() where TProps : class
            => _schemeSync.GetSchemeByTypeAsync<TProps>();

        public Task<IRedbScheme?> GetSchemeByTypeAsync(Type type)
            => _schemeSync.GetSchemeByTypeAsync(type);

        public Task<IRedbScheme> LoadSchemeByTypeAsync<TProps>() where TProps : class
            => _schemeSync.LoadSchemeByTypeAsync<TProps>();

        public Task<IRedbScheme> LoadSchemeByTypeAsync(Type type)
            => _schemeSync.LoadSchemeByTypeAsync(type);

        public Task<List<IRedbStructure>> GetStructuresByTypeAsync<TProps>() where TProps : class
            => _schemeSync.GetStructuresByTypeAsync<TProps>();

        public Task<List<IRedbStructure>> GetStructuresByTypeAsync(Type type)
            => _schemeSync.GetStructuresByTypeAsync(type);

        public Task<bool> SchemeExistsForTypeAsync<TProps>() where TProps : class
            => _schemeSync.SchemeExistsForTypeAsync<TProps>();

        public Task<bool> SchemeExistsForTypeAsync(Type type)
            => _schemeSync.SchemeExistsForTypeAsync(type);

        public Task<bool> SchemeExistsByNameAsync(string schemeName)
            => _schemeSync.SchemeExistsByNameAsync(schemeName);

        public string GetSchemeNameForType<TProps>() where TProps : class
            => _schemeSync.GetSchemeNameForType<TProps>();

        public string GetSchemeNameForType(Type type)
            => _schemeSync.GetSchemeNameForType(type);

        public string? GetSchemeAliasForType<TProps>() where TProps : class
            => _schemeSync.GetSchemeAliasForType<TProps>();

        public string? GetSchemeAliasForType(Type type)
            => _schemeSync.GetSchemeAliasForType(type);

        // ===== НЕДОСТАЮЩИЕ МЕТОДЫ ИЗ ISchemeSyncProvider =====
        
        public Task<IRedbScheme?> GetSchemeByIdAsync(long schemeId)
            => _schemeSync.GetSchemeByIdAsync(schemeId);
        
        public Task<IRedbScheme?> GetSchemeByNameAsync(string schemeName)
            => _schemeSync.GetSchemeByNameAsync(schemeName);
        
        public Task<List<IRedbScheme>> GetSchemesAsync()
            => _schemeSync.GetSchemesAsync();
        
        public Task<List<IRedbStructure>> GetStructuresAsync(IRedbScheme scheme)
            => _schemeSync.GetStructuresAsync(scheme);

        // ЗАКОММЕНТИРОВАНО: Метод с userId и checkPermissions НЕТ в контракте IObjectStorageProvider
        /*
        public Task<RedbObject<TProps>> LoadAsync<TProps>(long objectId, int depth = 10, long? userId = null, bool checkPermissions = false) where TProps : class, new()
            => _objectStorage.LoadAsync<TProps>(objectId, depth, userId, checkPermissions);
        */

        public Task<RedbObject<TProps>> LoadAsync<TProps>(IRedbObject obj, int depth = 10) where TProps : class, new()
            => _objectStorage.LoadAsync<TProps>(obj, depth);

        public Task<RedbObject<TProps>> LoadAsync<TProps>(IRedbObject obj, IRedbUser user, int depth = 10) where TProps : class, new()
            => _objectStorage.LoadAsync<TProps>(obj, user, depth);

        // ===== УДАЛЕНИЕ ПОДДЕРЕВЬЕВ (из контракта ITreeProvider) =====
        
        public Task<int> DeleteSubtreeAsync(IRedbObject parentObj)
            => _treeProvider.DeleteSubtreeAsync(parentObj);

        public Task<int> DeleteSubtreeAsync(IRedbObject parentObj, IRedbUser user)
            => _treeProvider.DeleteSubtreeAsync(parentObj, user);

        public Task<int> DeleteSubtreeAsync(RedbObject parentObj)
            => _treeProvider.DeleteSubtreeAsync(parentObj);

        public Task<int> DeleteSubtreeAsync(RedbObject parentObj, IRedbUser user)
            => _treeProvider.DeleteSubtreeAsync(parentObj, user);


        // ===== БАЗОВЫЕ МЕТОДЫ (из контракта IPermissionProvider) =====
        
        public IQueryable<long> GetReadableObjectIds()
            => _permissionProvider.GetReadableObjectIds();
        
        public Task<bool> CanUserEditObject(IRedbObject obj)
            => _permissionProvider.CanUserEditObject(obj);
        
        public Task<bool> CanUserSelectObject(IRedbObject obj)
            => _permissionProvider.CanUserSelectObject(obj);

        public Task<bool> CanUserInsertScheme(IRedbScheme scheme)
            => _permissionProvider.CanUserInsertScheme(scheme);

        public Task<bool> CanUserInsertScheme(IRedbScheme scheme, IRedbUser user)
            => _permissionProvider.CanUserInsertScheme(scheme, user);

        public Task<bool> CanUserDeleteObject(IRedbObject obj)
            => _permissionProvider.CanUserDeleteObject(obj);

        // ===== ПЕРЕГРУЗКИ С ЯВНЫМ ПОЛЬЗОВАТЕЛЕМ (из контракта IPermissionProvider) =====
        
        public IQueryable<long> GetReadableObjectIds(IRedbUser user)
            => _permissionProvider.GetReadableObjectIds(user);
        
        public Task<bool> CanUserEditObject(IRedbObject obj, IRedbUser user)
            => _permissionProvider.CanUserEditObject(obj, user);
        
        public Task<bool> CanUserSelectObject(IRedbObject obj, IRedbUser user)
            => _permissionProvider.CanUserSelectObject(obj, user);
        
        public Task<bool> CanUserDeleteObject(IRedbObject obj, IRedbUser user)
            => _permissionProvider.CanUserDeleteObject(obj, user);

        // ===== КРАСИВЫЕ МЕТОДЫ С ОБЪЕКТАМИ (из контракта IPermissionProvider) =====
        
        public Task<bool> CanUserEditObject(RedbObject obj)
            => _permissionProvider.CanUserEditObject(obj);
        
        public Task<bool> CanUserSelectObject(RedbObject obj)
            => _permissionProvider.CanUserSelectObject(obj);
        
        public Task<bool> CanUserDeleteObject(RedbObject obj)
            => _permissionProvider.CanUserDeleteObject(obj);
        
        public Task<bool> CanUserEditObject(RedbObject obj, IRedbUser user)
            => _permissionProvider.CanUserEditObject(obj, user);
        
        public Task<bool> CanUserSelectObject(RedbObject obj, IRedbUser user)
            => _permissionProvider.CanUserSelectObject(obj, user);
        
        public Task<bool> CanUserDeleteObject(RedbObject obj, IRedbUser user)
            => _permissionProvider.CanUserDeleteObject(obj, user);

        public Task<bool> CanUserInsertScheme(RedbObject obj, IRedbUser user)
            => _permissionProvider.CanUserInsertScheme(obj, user);


        // ===== МЕТОДЫ ИЗ КОНТРАКТА IQueryableProvider =====

        public IRedbQueryable<TProps> Query<TProps>(IRedbScheme scheme, IRedbUser user) where TProps : class, new()
            => _queryProvider.Query<TProps>(scheme, user);

        public IRedbQueryable<TProps> Query<TProps>(IRedbScheme scheme) where TProps : class, new()
            => _queryProvider.Query<TProps>(scheme);

        public Task<IRedbQueryable<TProps>> QueryAsync<TProps>(IRedbScheme scheme, IRedbUser user) where TProps : class, new()
            => _queryProvider.QueryAsync<TProps>(scheme, user);

        public Task<IRedbQueryable<TProps>> QueryAsync<TProps>(IRedbScheme scheme) where TProps : class, new()
            => _queryProvider.QueryAsync<TProps>(scheme);
        
        public Task<IRedbQueryable<TProps>> QueryAsync<TProps>(string schemeName) where TProps : class, new()
            => _queryProvider.QueryAsync<TProps>(schemeName);
        
        public Task<IRedbQueryable<TProps>> QueryAsync<TProps>() where TProps : class, new()
            => _queryProvider.QueryAsync<TProps>();
        
        public Task<IRedbQueryable<TProps>> QueryAsync<TProps>(IRedbUser user) where TProps : class, new()
            => _queryProvider.QueryAsync<TProps>(user);
        
        public IRedbQueryable<TProps> Query<TProps>() where TProps : class, new()
            => _queryProvider.Query<TProps>();
        
        public IRedbQueryable<TProps> Query<TProps>(IRedbUser user) where TProps : class, new()
            => _queryProvider.Query<TProps>(user);

        public Task<List<SupportedType>> GetSupportedTypesAsync()
            => _validationProvider.GetSupportedTypesAsync();

        public Task<ValidationIssue?> ValidateTypeAsync(Type csharpType, string propertyName)
            => _validationProvider.ValidateTypeAsync(csharpType, propertyName);

        public Task<SchemaValidationResult> ValidateSchemaAsync<TProps>(string schemeName, bool strictDeleteExtra = true) where TProps : class
            => _validationProvider.ValidateSchemaAsync<TProps>(schemeName, strictDeleteExtra);

        public ValidationIssue? ValidatePropertyConstraints(Type propertyType, string propertyName, bool isRequired, bool isArray)
            => _validationProvider.ValidatePropertyConstraints(propertyType, propertyName, isRequired, isArray);

        // ===== НЕДОСТАЮЩИЕ МЕТОДЫ ИЗ IValidationProvider =====
        
        public Task<SchemaValidationResult> ValidateSchemaAsync<TProps>(IRedbScheme scheme, bool strictDeleteExtra = true) where TProps : class
            => _validationProvider.ValidateSchemaAsync<TProps>(scheme, strictDeleteExtra);
        
        public Task<SchemaChangeReport> AnalyzeSchemaChangesAsync<TProps>(IRedbScheme scheme) where TProps : class
            => _validationProvider.AnalyzeSchemaChangesAsync<TProps>(scheme);

        // === МЕТОДЫ УПРАВЛЕНИЯ КОНТЕКСТОМ БЕЗОПАСНОСТИ ===
        public void SetCurrentUser(IRedbUser user)
        {
            _securityContext.SetCurrentUser(user);
        }

        public IDisposable CreateSystemContext()
        {
            return _securityContext.CreateSystemContext();
        }

        public long GetEffectiveUserId()
        {
            return _securityContext.GetEffectiveUserId();
        }


        // ===== 🔧 ДЕЛЕГИРОВАНИЕ CRUD МЕТОДОВ ДЛЯ РАЗРЕШЕНИЙ =====

        public Task<IRedbPermission> CreatePermissionAsync(PermissionRequest request, IRedbUser? currentUser = null)
            => _permissionProvider.CreatePermissionAsync(request, currentUser);

        public Task<IRedbPermission> UpdatePermissionAsync(IRedbPermission permission, PermissionRequest request, IRedbUser? currentUser = null)
            => _permissionProvider.UpdatePermissionAsync(permission, request, currentUser);

        public Task<bool> DeletePermissionAsync(IRedbPermission permission, IRedbUser? currentUser = null)
            => _permissionProvider.DeletePermissionAsync(permission, currentUser);

        // ===== 🔍 ДЕЛЕГИРОВАНИЕ ПОИСКА РАЗРЕШЕНИЙ =====

        public Task<List<IRedbPermission>> GetPermissionsByUserAsync(IRedbUser user)
            => _permissionProvider.GetPermissionsByUserAsync(user);

        public Task<List<IRedbPermission>> GetPermissionsByRoleAsync(IRedbRole role)
            => _permissionProvider.GetPermissionsByRoleAsync(role);

        public Task<List<IRedbPermission>> GetPermissionsByObjectAsync(IRedbObject obj)
            => _permissionProvider.GetPermissionsByObjectAsync(obj);

        public Task<IRedbPermission?> GetPermissionByIdAsync(long permissionId)
            => _permissionProvider.GetPermissionByIdAsync(permissionId);

        public Task<bool> CanUserEditObject(long objectId, long userId)
            => _permissionProvider.CanUserEditObject(objectId, userId);



        public Task<bool> CanUserSelectObject(long objectId, long userId)
            => _permissionProvider.CanUserSelectObject(objectId, userId);

        public Task<bool> CanUserInsertScheme(long schemeId, long userId)
            => _permissionProvider.CanUserInsertScheme(schemeId, userId);
        public Task<bool> CanUserDeleteObject(long objectId, long userId)
            => _permissionProvider.CanUserDeleteObject(objectId, userId);


        // ===== 🎯 ДЕЛЕГИРОВАНИЕ УПРАВЛЕНИЯ РАЗРЕШЕНИЯМИ =====

        public Task<bool> GrantPermissionAsync(IRedbUser user, IRedbObject obj, PermissionAction actions, IRedbUser? currentUser = null)
            => _permissionProvider.GrantPermissionAsync(user, obj, actions, currentUser);

        public Task<bool> GrantPermissionAsync(IRedbRole role, IRedbObject obj, PermissionAction actions, IRedbUser? currentUser = null)
            => _permissionProvider.GrantPermissionAsync(role, obj, actions, currentUser);

        public Task<bool> RevokePermissionAsync(IRedbUser user, IRedbObject obj, IRedbUser? currentUser = null)
            => _permissionProvider.RevokePermissionAsync(user, obj, currentUser);

        public Task<bool> RevokePermissionAsync(IRedbRole role, IRedbObject obj, IRedbUser? currentUser = null)
            => _permissionProvider.RevokePermissionAsync(role, obj, currentUser);

        public Task<int> RevokeAllUserPermissionsAsync(IRedbUser user, IRedbUser? currentUser = null)
            => _permissionProvider.RevokeAllUserPermissionsAsync(user, currentUser);

        public Task<int> RevokeAllRolePermissionsAsync(IRedbRole role, IRedbUser? currentUser = null)
            => _permissionProvider.RevokeAllRolePermissionsAsync(role, currentUser);

        // ===== 🌟 ДЕЛЕГИРОВАНИЕ ЭФФЕКТИВНЫХ ПРАВ =====

        public Task<EffectivePermissionResult> GetEffectivePermissionsAsync(IRedbUser user, IRedbObject obj)
            => _permissionProvider.GetEffectivePermissionsAsync(user, obj);

        public Task<Dictionary<IRedbObject, EffectivePermissionResult>> GetEffectivePermissionsBatchAsync(IRedbUser user, IRedbObject[] objects)
            => _permissionProvider.GetEffectivePermissionsBatchAsync(user, objects);

        public Task<List<EffectivePermissionResult>> GetAllEffectivePermissionsAsync(IRedbUser user)
            => _permissionProvider.GetAllEffectivePermissionsAsync(user);

        // ===== 📊 ДЕЛЕГИРОВАНИЕ СТАТИСТИКИ =====

        public Task<int> GetPermissionCountAsync()
            => _permissionProvider.GetPermissionCountAsync();

        public Task<int> GetUserPermissionCountAsync(IRedbUser user)
            => _permissionProvider.GetUserPermissionCountAsync(user);

        public Task<int> GetRolePermissionCountAsync(IRedbRole role)
            => _permissionProvider.GetRolePermissionCountAsync(role);

        // ===== 🔧 УПРАВЛЕНИЕ КОНФИГУРАЦИЕЙ =====
        
        /// <summary>
        /// Обновить конфигурацию всех провайдеров
        /// </summary>
        private void UpdateProvidersConfiguration()
        {
            // Провайдеры используют _configuration через ссылку, обновляется автоматически
            // Дополнительно можно обновить специфические настройки если нужно
        }

        public void UpdateConfiguration(Action<RedbServiceConfiguration> configure)
        {
            configure(_configuration);
            // Обновляем провайдеры с новой конфигурацией
            UpdateProvidersConfiguration();
        }

        public void UpdateConfiguration(Action<RedbServiceConfigurationBuilder> configureBuilder)
        {
            var builder = new RedbServiceConfigurationBuilder(_configuration);
            configureBuilder(builder);
            _configuration = builder.Build();
            // Обновляем провайдеры с новой конфигурацией
            UpdateProvidersConfiguration();
        }

        // ===== ДЕЛЕГИРОВАНИЕ К ПРОВАЙДЕРАМ (методы из контракта IObjectStorageProvider) =====

        // IObjectStorageProvider - базовые методы
        public Task<RedbObject<TProps>> LoadAsync<TProps>(long objectId, int depth = 10) where TProps : class, new()
            => _objectStorage.LoadAsync<TProps>(objectId, depth);

        public Task<long> SaveAsync<TProps>(IRedbObject<TProps> obj) where TProps : class, new()
            => _objectStorage.SaveAsync(obj);

        public Task<bool> DeleteAsync<TProps>(IRedbObject<TProps> obj) where TProps : class, new()
            => _objectStorage.DeleteAsync<TProps>(obj);

        // IObjectStorageProvider - методы с явным пользователем
        public Task<RedbObject<TProps>> LoadAsync<TProps>(long objectId, IRedbUser user, int depth = 10) where TProps : class, new()
            => _objectStorage.LoadAsync<TProps>(objectId, user, depth);

        public Task<long> SaveAsync<TProps>(IRedbObject<TProps> obj, IRedbUser user) where TProps : class, new()
            => _objectStorage.SaveAsync(obj, user);

        public Task<bool> DeleteAsync<TProps>(IRedbObject<TProps> obj, IRedbUser user) where TProps : class, new()
            => _objectStorage.DeleteAsync<TProps>(obj, user);



        // ===== РЕАЛИЗАЦИЯ ITreeProvider =====

        /// <summary>
        /// Загрузить дерево/поддерево (использует _securityContext и конфигурацию)
        /// </summary>
        public Task<TreeRedbObject<TProps>> LoadTreeAsync<TProps>(IRedbObject rootObj, int? maxDepth = null) where TProps : class, new()
            => _treeProvider.LoadTreeAsync<TProps>(rootObj, maxDepth);

        /// <summary>
        /// Получить прямых детей объекта (использует _securityContext и конфигурацию)
        /// </summary>
        public Task<IEnumerable<TreeRedbObject<TProps>>> GetChildrenAsync<TProps>(IRedbObject parentObj) where TProps : class, new()
            => _treeProvider.GetChildrenAsync<TProps>(parentObj);

        /// <summary>
        /// Получить путь от объекта к корню (использует _securityContext и конфигурацию)
        /// </summary>
        public Task<IEnumerable<TreeRedbObject<TProps>>> GetPathToRootAsync<TProps>(IRedbObject obj) where TProps : class, new()
            => _treeProvider.GetPathToRootAsync<TProps>(obj);

        /// <summary>
        /// Получить всех потомков объекта (использует _securityContext и конфигурацию)
        /// </summary>
        public Task<IEnumerable<TreeRedbObject<TProps>>> GetDescendantsAsync<TProps>(IRedbObject parentObj, int? maxDepth = null) where TProps : class, new()
            => _treeProvider.GetDescendantsAsync<TProps>(parentObj, maxDepth);

        /// <summary>
        /// Переместить объект в дереве (использует _securityContext и конфигурацию)
        /// </summary>
        public Task MoveObjectAsync(IRedbObject obj, IRedbObject? newParentObj)
            => _treeProvider.MoveObjectAsync(obj, newParentObj);

        /// <summary>
        /// Создать дочерний объект (использует _securityContext и конфигурацию)
        /// </summary>
        public Task<long> CreateChildAsync<TProps>(TreeRedbObject<TProps> obj, IRedbObject parentObj) where TProps : class, new()
            => _treeProvider.CreateChildAsync<TProps>(obj, parentObj);



        // ===== ПЕРЕГРУЗКИ С ЯВНЫМ ПОЛЬЗОВАТЕЛЕМ =====

        /// <summary>
        /// Загрузить дерево/поддерево с явно указанным пользователем
        /// </summary>
        public Task<TreeRedbObject<TProps>> LoadTreeAsync<TProps>(IRedbObject rootObj, IRedbUser user, int? maxDepth = null) where TProps : class, new()
            => _treeProvider.LoadTreeAsync<TProps>(rootObj, user, maxDepth);

        /// <summary>
        /// Получить прямых детей объекта с явно указанным пользователем
        /// </summary>
        public Task<IEnumerable<TreeRedbObject<TProps>>> GetChildrenAsync<TProps>(IRedbObject parentObj, IRedbUser user) where TProps : class, new()
            => _treeProvider.GetChildrenAsync<TProps>(parentObj, user);

        /// <summary>
        /// Получить путь от объекта к корню с явно указанным пользователем
        /// </summary>
        public Task<IEnumerable<TreeRedbObject<TProps>>> GetPathToRootAsync<TProps>(IRedbObject obj, IRedbUser user) where TProps : class, new()
            => _treeProvider.GetPathToRootAsync<TProps>(obj, user);

        /// <summary>
        /// Получить всех потомков объекта с явно указанным пользователем
        /// </summary>
        public Task<IEnumerable<TreeRedbObject<TProps>>> GetDescendantsAsync<TProps>(IRedbObject parentObj, IRedbUser user, int? maxDepth = null) where TProps : class, new()
            => _treeProvider.GetDescendantsAsync<TProps>(parentObj, user, maxDepth);

        /// <summary>
        /// Переместить объект в дереве с явно указанным пользователем
        /// </summary>
        public Task MoveObjectAsync(IRedbObject obj, IRedbObject? newParentObj, IRedbUser user)
            => _treeProvider.MoveObjectAsync(obj, newParentObj, user);

        /// <summary>
        /// Создать дочерний объект с явно указанным пользователем
        /// </summary>
        public Task<long> CreateChildAsync<TProps>(TreeRedbObject<TProps> obj, IRedbObject parentObj, IRedbUser user) where TProps : class, new()
            => _treeProvider.CreateChildAsync<TProps>(obj, parentObj, user);

        // ===== ПОЛИМОРФНЫЕ МЕТОДЫ (для смешанных деревьев) =====
        
        /// <summary>
        /// Загрузить полиморфное дерево/поддерево - поддерживает объекты разных схем в одном дереве
        /// </summary>
        public Task<ITreeRedbObject> LoadPolymorphicTreeAsync(IRedbObject rootObj, int? maxDepth = null)
            => _treeProvider.LoadPolymorphicTreeAsync(rootObj, maxDepth);
            
        /// <summary>
        /// Получить всех прямых детей объекта независимо от их схем
        /// </summary>
        public Task<IEnumerable<ITreeRedbObject>> GetPolymorphicChildrenAsync(IRedbObject parentObj)
            => _treeProvider.GetPolymorphicChildrenAsync(parentObj);
            
        /// <summary>
        /// Получить полиморфный путь от объекта к корню - объекты могут быть разных схем
        /// </summary>
        public Task<IEnumerable<ITreeRedbObject>> GetPolymorphicPathToRootAsync(IRedbObject obj)
            => _treeProvider.GetPolymorphicPathToRootAsync(obj);
            
        /// <summary>
        /// Получить всех полиморфных потомков объекта независимо от их схем
        /// </summary>
        public Task<IEnumerable<ITreeRedbObject>> GetPolymorphicDescendantsAsync(IRedbObject parentObj, int? maxDepth = null)
            => _treeProvider.GetPolymorphicDescendantsAsync(parentObj, maxDepth);
            


        // ===== ПОЛИМОРФНЫЕ МЕТОДЫ С ЯВНЫМ ПОЛЬЗОВАТЕЛЕМ =====
        
        /// <summary>
        /// Загрузить полиморфное дерево/поддерево с явно указанным пользователем
        /// </summary>
        public Task<ITreeRedbObject> LoadPolymorphicTreeAsync(IRedbObject rootObj, IRedbUser user, int? maxDepth = null)
            => _treeProvider.LoadPolymorphicTreeAsync(rootObj, user, maxDepth);
            
        /// <summary>
        /// Получить всех прямых детей объекта независимо от их схем с явно указанным пользователем
        /// </summary>
        public Task<IEnumerable<ITreeRedbObject>> GetPolymorphicChildrenAsync(IRedbObject parentObj, IRedbUser user)
            => _treeProvider.GetPolymorphicChildrenAsync(parentObj, user);
            
        /// <summary>
        /// Получить полиморфный путь от объекта к корню с явно указанным пользователем
        /// </summary>
        public Task<IEnumerable<ITreeRedbObject>> GetPolymorphicPathToRootAsync(IRedbObject obj, IRedbUser user)
            => _treeProvider.GetPolymorphicPathToRootAsync(obj, user);
            
        /// <summary>
        /// Получить всех полиморфных потомков объекта с явно указанным пользователем
        /// </summary>
        public Task<IEnumerable<ITreeRedbObject>> GetPolymorphicDescendantsAsync(IRedbObject parentObj, IRedbUser user, int? maxDepth = null)
            => _treeProvider.GetPolymorphicDescendantsAsync(parentObj, user, maxDepth);

        // ===== НОВЫЕ МЕТОДЫ ДЛЯ РАБОТЫ С ДОЧЕРНИМИ ОБЪЕКТАМИ =====
        
        public Task<IRedbQueryable<TProps>> QueryChildrenAsync<TProps>(IRedbObject? parentObj) where TProps : class, new()
            => _queryProvider.QueryChildrenAsync<TProps>(parentObj);

        public Task<IRedbQueryable<TProps>> QueryChildrenAsync<TProps>(IRedbObject? parentObj, IRedbUser user) where TProps : class, new()
            => _queryProvider.QueryChildrenAsync<TProps>(parentObj, user);

        public IRedbQueryable<TProps> QueryChildren<TProps>(IRedbObject? parentObj) where TProps : class, new()
            => _queryProvider.QueryChildren<TProps>(parentObj);

        // ===== МЕТОДЫ ДЛЯ РАБОТЫ С ПОТОМКАМИ (РЕКУРСИВНО) =====
        
        public Task<IRedbQueryable<TProps>> QueryDescendantsAsync<TProps>(IRedbObject? parentObj, int? maxDepth = null) where TProps : class, new()
            => _queryProvider.QueryDescendantsAsync<TProps>(parentObj, maxDepth);

        public Task<IRedbQueryable<TProps>> QueryDescendantsAsync<TProps>(IRedbObject? parentObj, IRedbUser user, int? maxDepth = null) where TProps : class, new()
            => _queryProvider.QueryDescendantsAsync<TProps>(parentObj, user, maxDepth);

        public IRedbQueryable<TProps> QueryDescendants<TProps>(IRedbObject? parentObj, int? maxDepth = null) where TProps : class, new()
            => _queryProvider.QueryDescendants<TProps>(parentObj, maxDepth);

        // ===== BATCH МЕТОДЫ ДЛЯ РАБОТЫ С НЕСКОЛЬКИМИ РОДИТЕЛЬСКИМИ ОБЪЕКТАМИ =====

        /// <summary>
        /// Создать типобезопасный запрос для дочерних объектов нескольких родителей
        /// </summary>
        public Task<IRedbQueryable<TProps>> QueryChildrenAsync<TProps>(IEnumerable<IRedbObject> parentObjs) where TProps : class, new()
            => _queryProvider.QueryChildrenAsync<TProps>(parentObjs);

        /// <summary>
        /// Создать типобезопасный запрос для дочерних объектов нескольких родителей с указанным пользователем
        /// </summary>
        public Task<IRedbQueryable<TProps>> QueryChildrenAsync<TProps>(IEnumerable<IRedbObject> parentObjs, IRedbUser user) where TProps : class, new()
            => _queryProvider.QueryChildrenAsync<TProps>(parentObjs, user);

        /// <summary>
        /// Синхронная версия запроса дочерних объектов нескольких родителей
        /// </summary>
        public IRedbQueryable<TProps> QueryChildren<TProps>(IEnumerable<IRedbObject> parentObjs) where TProps : class, new()
            => _queryProvider.QueryChildren<TProps>(parentObjs);

        /// <summary>
        /// Создать типобезопасный запрос для всех потомков нескольких родителей
        /// </summary>
        public Task<IRedbQueryable<TProps>> QueryDescendantsAsync<TProps>(IEnumerable<IRedbObject> parentObjs, int? maxDepth = null) where TProps : class, new()
            => _queryProvider.QueryDescendantsAsync<TProps>(parentObjs, maxDepth);

        /// <summary>
        /// Создать типобезопасный запрос для всех потомков нескольких родителей с указанным пользователем
        /// </summary>
        public Task<IRedbQueryable<TProps>> QueryDescendantsAsync<TProps>(IEnumerable<IRedbObject> parentObjs, IRedbUser user, int? maxDepth = null) where TProps : class, new()
            => _queryProvider.QueryDescendantsAsync<TProps>(parentObjs, user, maxDepth);

        /// <summary>
        /// Синхронная версия запроса потомков нескольких родителей
        /// </summary>
        public IRedbQueryable<TProps> QueryDescendants<TProps>(IEnumerable<IRedbObject> parentObjs, int? maxDepth = null) where TProps : class, new()
            => _queryProvider.QueryDescendants<TProps>(parentObjs, maxDepth);

    }
}
