using redb.Core.Providers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using redb.Core.Serialization;
using redb.Core.Models.Contracts;
using redb.Core.Models.Entities;
using redb.Core.Models.Configuration;
using redb.Core.Models.Security;
using redb.Core.Models;
using redb.Core.Utils;

namespace redb.Core.Postgres.Providers
{
    /// <summary>
    /// PostgreSQL реализация провайдера древовидных структур
    /// Проверка прав управляется централизованно через конфигурацию (по аналогии с PostgresObjectStorageProvider)
    /// </summary>
    public class PostgresTreeProvider : ITreeProvider
    {
        private readonly RedbContext _context;
        private readonly IObjectStorageProvider _objectStorage;
        private readonly IPermissionProvider _permissionProvider;
        private readonly IRedbObjectSerializer _serializer;
        private readonly IRedbSecurityContext _securityContext;
        private readonly RedbServiceConfiguration _configuration;
        private readonly ISchemeSyncProvider _schemeSyncProvider;

        public PostgresTreeProvider(
            RedbContext context,
            IObjectStorageProvider objectStorage,
            IPermissionProvider permissionProvider,
            IRedbObjectSerializer serializer,
            IRedbSecurityContext securityContext,
            RedbServiceConfiguration configuration,
            ISchemeSyncProvider schemeSyncProvider)
        {
            _context = context;
            _objectStorage = objectStorage;
            _permissionProvider = permissionProvider;
            _serializer = serializer;
            _securityContext = securityContext;
            _configuration = configuration ?? new RedbServiceConfiguration();
            _schemeSyncProvider = schemeSyncProvider;
        }

        /// <summary>
        /// Инициализировать AutomaticTypeRegistry для поддержки полиморфных операций
        /// Должен вызываться при запуске приложения
        /// </summary>
        public async Task InitializeTypeRegistryAsync()
        {
            if (!AutomaticTypeRegistry.IsInitialized)
            {
                await AutomaticTypeRegistry.InitializeAsync(_schemeSyncProvider);

            }
        }

        // ===== БАЗОВЫЕ МЕТОДЫ (используют _securityContext и конфигурацию) =====
        
        public async Task<TreeRedbObject<TProps>> LoadTreeAsync<TProps>(IRedbObject rootObj, int? maxDepth = null) where TProps : class, new()
        {
            var effectiveUser = _securityContext.GetEffectiveUser();
            var actualMaxDepth = maxDepth ?? _configuration.DefaultMaxTreeDepth;
            return await LoadTreeWithUserAsync<TProps>(rootObj.Id, actualMaxDepth, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnLoad);
        }
        
        public async Task<IEnumerable<TreeRedbObject<TProps>>> GetChildrenAsync<TProps>(IRedbObject parentObj) where TProps : class, new()
        {
            var effectiveUser = _securityContext.GetEffectiveUser();
            return await GetChildrenWithUserAsync<TProps>(parentObj.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnLoad);
        }
        
        public async Task<IEnumerable<TreeRedbObject<TProps>>> GetPathToRootAsync<TProps>(IRedbObject obj) where TProps : class, new()
        {
            var effectiveUser = _securityContext.GetEffectiveUser();
            return await GetPathToRootWithUserAsync<TProps>(obj.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnLoad);
        }
        
        public async Task<IEnumerable<TreeRedbObject<TProps>>> GetDescendantsAsync<TProps>(IRedbObject parentObj, int? maxDepth = null) where TProps : class, new()
        {
            var effectiveUser = _securityContext.GetEffectiveUser();
            var actualMaxDepth = maxDepth ?? _configuration.DefaultMaxTreeDepth;
            return await GetDescendantsWithUserAsync<TProps>(parentObj.Id, actualMaxDepth, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnLoad);
        }
        
        public async Task MoveObjectAsync(IRedbObject obj, IRedbObject? newParentObj)
        {
            var effectiveUser = _securityContext.GetEffectiveUser();
            await MoveObjectWithUserAsync(obj.Id, newParentObj?.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnSave);
        }
        
        public async Task<long> CreateChildAsync<TProps>(TreeRedbObject<TProps> obj, IRedbObject parentObj) where TProps : class, new()
        {
            var effectiveUser = _securityContext.GetEffectiveUser();
            return await CreateChildWithUserAsync<TProps>(obj, parentObj.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnSave);
        }

        public async Task<int> DeleteSubtreeAsync(IRedbObject parentObj)
        {
            var effectiveUser = _securityContext.GetEffectiveUser();
            return await DeleteSubtreeWithUserAsync(parentObj.Id, effectiveUser);
        }

        // ===== ПЕРЕГРУЗКИ С ЯВНЫМ ПОЛЬЗОВАТЕЛЕМ (используют конфигурацию) =====
        
        public async Task<TreeRedbObject<TProps>> LoadTreeAsync<TProps>(IRedbObject rootObj, IRedbUser user, int? maxDepth = null) where TProps : class, new()
        {
            var actualMaxDepth = maxDepth ?? _configuration.DefaultMaxTreeDepth;
            return await LoadTreeWithUserAsync<TProps>(rootObj.Id, actualMaxDepth, user.Id, _configuration.DefaultCheckPermissionsOnLoad);
        }
        
        public async Task<IEnumerable<TreeRedbObject<TProps>>> GetChildrenAsync<TProps>(IRedbObject parentObj, IRedbUser user) where TProps : class, new()
        {
            return await GetChildrenWithUserAsync<TProps>(parentObj.Id, user.Id, _configuration.DefaultCheckPermissionsOnLoad);
        }
        
        public async Task<IEnumerable<TreeRedbObject<TProps>>> GetPathToRootAsync<TProps>(IRedbObject obj, IRedbUser user) where TProps : class, new()
        {
            return await GetPathToRootWithUserAsync<TProps>(obj.Id, user.Id, _configuration.DefaultCheckPermissionsOnLoad);
        }
        
        public async Task<IEnumerable<TreeRedbObject<TProps>>> GetDescendantsAsync<TProps>(IRedbObject parentObj, IRedbUser user, int? maxDepth = null) where TProps : class, new()
        {
            var actualMaxDepth = maxDepth ?? _configuration.DefaultMaxTreeDepth;
            return await GetDescendantsWithUserAsync<TProps>(parentObj.Id, actualMaxDepth, user.Id, _configuration.DefaultCheckPermissionsOnLoad);
        }
        
        public async Task MoveObjectAsync(IRedbObject obj, IRedbObject? newParentObj, IRedbUser user)
        {
            await MoveObjectWithUserAsync(obj.Id, newParentObj?.Id, user.Id, _configuration.DefaultCheckPermissionsOnSave);
        }
        
        public async Task<long> CreateChildAsync<TProps>(TreeRedbObject<TProps> obj, IRedbObject parentObj, IRedbUser user) where TProps : class, new()
        {
            return await CreateChildWithUserAsync<TProps>(obj, parentObj.Id, user.Id, _configuration.DefaultCheckPermissionsOnSave);
        }

        public async Task<int> DeleteSubtreeAsync(IRedbObject parentObj, IRedbUser user)
        {
            return await DeleteSubtreeWithUserAsync(parentObj.Id, user);
        }

        // ===== ПОЛИМОРФНЫЕ МЕТОДЫ (для смешанных деревьев) =====
        
        public async Task<ITreeRedbObject> LoadPolymorphicTreeAsync(IRedbObject rootObj, int? maxDepth = null)
        {
            var effectiveUser = _securityContext.GetEffectiveUser();
            var actualMaxDepth = maxDepth ?? _configuration.DefaultMaxTreeDepth;
            return await LoadPolymorphicTreeWithUserAsync(rootObj.Id, actualMaxDepth, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnLoad);
        }
        
        public async Task<IEnumerable<ITreeRedbObject>> GetPolymorphicChildrenAsync(IRedbObject parentObj)
        {
            var effectiveUser = _securityContext.GetEffectiveUser();
            return await GetPolymorphicChildrenWithUserAsync(parentObj.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnLoad);
        }
        
        public async Task<IEnumerable<ITreeRedbObject>> GetPolymorphicPathToRootAsync(IRedbObject obj)
        {
            var effectiveUser = _securityContext.GetEffectiveUser();
            return await GetPolymorphicPathToRootWithUserAsync(obj.Id, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnLoad);
        }
        
        public async Task<IEnumerable<ITreeRedbObject>> GetPolymorphicDescendantsAsync(IRedbObject parentObj, int? maxDepth = null)
        {
            var effectiveUser = _securityContext.GetEffectiveUser();
            var actualMaxDepth = maxDepth ?? _configuration.DefaultMaxTreeDepth;
            return await GetPolymorphicDescendantsWithUserAsync(parentObj.Id, actualMaxDepth, effectiveUser.Id, _configuration.DefaultCheckPermissionsOnLoad);
        }
        


        // ===== ПОЛИМОРФНЫЕ МЕТОДЫ С ЯВНЫМ ПОЛЬЗОВАТЕЛЕМ =====
        
        public async Task<ITreeRedbObject> LoadPolymorphicTreeAsync(IRedbObject rootObj, IRedbUser user, int? maxDepth = null)
        {
            var actualMaxDepth = maxDepth ?? _configuration.DefaultMaxTreeDepth;
            return await LoadPolymorphicTreeWithUserAsync(rootObj.Id, actualMaxDepth, user.Id, _configuration.DefaultCheckPermissionsOnLoad);
        }
        
        public async Task<IEnumerable<ITreeRedbObject>> GetPolymorphicChildrenAsync(IRedbObject parentObj, IRedbUser user)
        {
            return await GetPolymorphicChildrenWithUserAsync(parentObj.Id, user.Id, _configuration.DefaultCheckPermissionsOnLoad);
        }
        
        public async Task<IEnumerable<ITreeRedbObject>> GetPolymorphicPathToRootAsync(IRedbObject obj, IRedbUser user)
        {
            return await GetPolymorphicPathToRootWithUserAsync(obj.Id, user.Id, _configuration.DefaultCheckPermissionsOnLoad);
        }
        
        public async Task<IEnumerable<ITreeRedbObject>> GetPolymorphicDescendantsAsync(IRedbObject parentObj, IRedbUser user, int? maxDepth = null)
        {
            var actualMaxDepth = maxDepth ?? _configuration.DefaultMaxTreeDepth;
            return await GetPolymorphicDescendantsWithUserAsync(parentObj.Id, actualMaxDepth, user.Id, _configuration.DefaultCheckPermissionsOnLoad);
        }
        


        // ===== ПРИВАТНЫЕ МЕТОДЫ (для внутреннего использования) =====
        
        private async Task<TreeRedbObject<TProps>> LoadTreeWithUserAsync<TProps>(long rootId, int maxDepth = 10, long? userId = null, bool checkPermissions = false) where TProps : class, new()
        {
            // Загружаем базовый объект (только корневой, без связанных объектов - они загружаются отдельно)
            var baseObject = await _objectStorage.LoadAsync<TProps>(rootId, 1);
            
            // Создаем TreeRedbObject и копируем все поля
            var treeObject = new TreeRedbObject<TProps>
            {
                id = baseObject.id,
                parent_id = baseObject.parent_id,
                scheme_id = baseObject.scheme_id,
                owner_id = baseObject.owner_id,
                who_change_id = baseObject.who_change_id,
                date_create = baseObject.date_create,
                date_modify = baseObject.date_modify,
                date_begin = baseObject.date_begin,
                date_complete = baseObject.date_complete,
                key = baseObject.key,
                code_int = baseObject.code_int,
                code_string = baseObject.code_string,
                code_guid = baseObject.code_guid,
                name = baseObject.name,
                note = baseObject.note,
                @bool = baseObject.@bool,
                hash = baseObject.hash,
                properties = baseObject.properties
            };
            
            await LoadChildrenRecursively(treeObject, maxDepth - 1, userId, checkPermissions);
            
            return treeObject;
        }

        private async Task<IEnumerable<TreeRedbObject<TProps>>> GetChildrenWithUserAsync<TProps>(long parentId, long? userId = null, bool checkPermissions = false) where TProps : class, new()
        {
            // ✅ Получаем scheme_id для TProps чтобы фильтровать только объекты нужной схемы
            var scheme = await _schemeSyncProvider.GetSchemeByTypeAsync<TProps>();
            if (scheme == null)
            {
                throw new InvalidOperationException($"Схема для типа {typeof(TProps).Name} не найдена. Используйте SyncSchemeAsync<{typeof(TProps).Name}>() для создания схемы.");
            }
            
            // 🛡️ SQL с параметрами (защита от SQL injection) + фильтр по схеме
            var sql = @"
                SELECT json_data 
                FROM (
                    SELECT get_object_json(o._id, 1) as json_data
                    FROM _objects o
                    WHERE o._id_parent = {0}
                      AND o._id_scheme = {1}
                    ORDER BY o._name, o._id
                ) subquery
                WHERE json_data IS NOT NULL";
                
            var jsonResults = await _context.Database.SqlQueryRaw<string>(sql, parentId, scheme.Id).ToListAsync();
            
            var children = new List<TreeRedbObject<TProps>>();
            
            foreach (var json in jsonResults)
            {
                if (string.IsNullOrEmpty(json)) continue;
                
                try
                {
                    var redbObj = _serializer.Deserialize<TProps>(json);
                    if (redbObj == null) continue;
                    
                    // Проверяем права доступа, если включена проверка
                    if (checkPermissions && userId.HasValue)
                    {
                        var canSelect = await _permissionProvider.CanUserSelectObject(redbObj);
                        if (!canSelect) continue;
                    }
                    
                    var treeObj = ConvertToTreeObject<TProps>(redbObj);
                    children.Add(treeObj);
                }
                catch (Exception ex)
                {
                    // Логируем ошибку десериализации, но продолжаем

                }
            }
            
            return children;
        }

        private async Task<IEnumerable<TreeRedbObject<TProps>>> GetPathToRootWithUserAsync<TProps>(long objectId, long? userId = null, bool checkPermissions = false) where TProps : class, new()
        {
            var path = new List<TreeRedbObject<TProps>>();
            var visited = new HashSet<long>(); // 🛡️ ЗАЩИТА ОТ ЦИКЛОВ
            long? currentId = objectId;
            
            while (currentId.HasValue)
            {
                // 🚨 КРИТИЧЕСКАЯ ПРОВЕРКА: Обнаружение циклической ссылки
                if (visited.Contains(currentId.Value))
                {
                    // Логируем проблему, но не бросаем исключение - возвращаем путь который смогли построить

                    break;
                }
                
                visited.Add(currentId.Value);
                
                try
                {
                    // Используем эффективного пользователя для загрузки объектов
                    var effectiveUser = _securityContext.GetEffectiveUser();
                    var obj = await _objectStorage.LoadAsync<TProps>(currentId.Value, effectiveUser, 1);
                    var treeObj = ConvertToTreeObject<TProps>(obj);
                    path.Insert(0, treeObj); // Вставляем в начало для правильного порядка
                    currentId = obj.parent_id;
                }
                catch (UnauthorizedAccessException)
                {
                    // Если нет прав на объект в пути, прерываем построение пути
                    break;
                }
                catch (Exception)
                {
                    // Если объект не найден или другая ошибка, прерываем
                    break;
                }
            }
            
            // Устанавливаем связи Parent для корректной навигации
            for (int i = 0; i < path.Count - 1; i++)
            {
                path[i + 1].Parent = path[i];
            }
            
            return path;
        }

        private async Task<IEnumerable<TreeRedbObject<TProps>>> GetDescendantsWithUserAsync<TProps>(long parentId, int maxDepth = 50, long? userId = null, bool checkPermissions = false) where TProps : class, new()
        {
            var descendants = new List<TreeRedbObject<TProps>>();
            await CollectDescendants<TProps>(parentId, descendants, maxDepth, 0, userId, checkPermissions);
            return descendants;
        }

        private async Task MoveObjectWithUserAsync(long objectId, long? newParentId, long userId, bool checkPermissions = true)
        {
            // Проверяем права на редактирование объекта
            if (checkPermissions)
            {
                // Загружаем объект и проверяем права (как в PostgresObjectStorageProvider)
                var objectForPermissions = await _objectStorage.LoadAsync<object>(objectId, 1);
                var effectiveUser = _securityContext.GetEffectiveUser();
                var canEdit = await _permissionProvider.CanUserEditObject(objectForPermissions, effectiveUser);
                if (!canEdit)
                {
                    throw new UnauthorizedAccessException($"Пользователь {userId} не имеет прав на редактирование объекта {objectId}");
                }
            }
            
            // Проверяем, что новый родитель существует (если указан)
            if (newParentId.HasValue)
            {
                var parentExists = await _context.Objects.AnyAsync(o => o.Id == newParentId.Value);
                if (!parentExists)
                {
                    throw new ArgumentException($"Родительский объект {newParentId} не существует");
                }
                
                // Проверяем, что не создается циклическая ссылка
                await ValidateNoCyclicReference(objectId, newParentId.Value);
            }
            
            // Обновляем parent_id
            var obj = await _context.Objects.FirstOrDefaultAsync(o => o.Id == objectId);
            if (obj == null)
            {
                throw new ArgumentException($"Объект {objectId} не найден");
            }
            
            obj.IdParent = newParentId;
            obj.DateModify = DateTime.Now;
            obj.IdWhoChange = userId;
            
            await _context.SaveChangesAsync();
        }

        private async Task<long> CreateChildWithUserAsync<TProps>(TreeRedbObject<TProps> obj, long parentId, long? userId = null, bool checkPermissions = false) where TProps : class, new()
        {
            // 🎯 ГЕНЕРАЦИЯ ID: Если нет ID (= 0), берем из генератора
            if (obj.id == 0)
            {
                obj.id = _context.GetNextKey();
            }
            
            // Устанавливаем parent_id
            obj.parent_id = parentId;
            
            // Используем существующий метод сохранения с эффективным пользователем
            var effectiveUser = _securityContext.GetEffectiveUser();
            return await _objectStorage.SaveAsync<TProps>(obj, effectiveUser);
        }

        private async Task<int> DeleteSubtreeWithUserAsync(long parentId, IRedbUser user)
        {
            // Используем конфигурацию для проверки прав
            var checkPermissions = _configuration.DefaultCheckPermissionsOnDelete;
            
            if (checkPermissions)
            {
                // Загружаем объект и проверяем права (как в PostgresObjectStorageProvider)
                var obj = await _objectStorage.LoadAsync<object>(parentId, 1);
                var canDelete = await _permissionProvider.CanUserDeleteObject(obj, user);
                if (!canDelete)
                {
                    throw new UnauthorizedAccessException($"Пользователь {user.Id} не имеет прав на удаление поддерева объекта {parentId}");
                }
            }

            // Получаем всех потомков
            var descendants = await GetDescendantsWithUserAsync<object>(parentId, 100, user.Id, false);
            var objectIds = descendants.Select(d => d.id).ToList();
            objectIds.Add(parentId); // Добавляем сам родительский объект

            int deletedCount = 0;
            foreach (var objectId in objectIds)
            {
                var objToDelete = await _context.Objects.FindAsync(objectId);
                if (objToDelete != null)
                {
                    _context.Objects.Remove(objToDelete);
                    deletedCount++;
                }
            }

            await _context.SaveChangesAsync();
            return deletedCount;
        }

        private async Task LoadChildrenRecursively<TProps>(TreeRedbObject<TProps> parent, int remainingDepth, long? userId, bool checkPermissions) where TProps : class, new()
        {
            if (remainingDepth <= 0) return;
            
            var children = await GetChildrenWithUserAsync<TProps>(parent.id, userId, checkPermissions);
            
            foreach (var child in children)
            {
                child.Parent = parent;
                parent.Children.Add(child);
                
                // Рекурсивно загружаем детей для каждого ребенка
                await LoadChildrenRecursively(child, remainingDepth - 1, userId, checkPermissions);
            }
        }

        /// <summary>
        /// Рекурсивно собирает всех потомков
        /// </summary>
        private async Task CollectDescendants<TProps>(long parentId, List<TreeRedbObject<TProps>> descendants, int maxDepth, int currentDepth, long? userId, bool checkPermissions) where TProps : class, new()
        {
            if (currentDepth >= maxDepth) return;
            
            var children = await GetChildrenWithUserAsync<TProps>(parentId, userId, checkPermissions);
            
            foreach (var child in children)
            {
                descendants.Add(child);
                
                // Рекурсивно собираем потомков каждого ребенка
                await CollectDescendants<TProps>(child.id, descendants, maxDepth, currentDepth + 1, userId, checkPermissions);
            }
        }

        /// <summary>
        /// Проверяет, не создается ли циклическая ссылка при перемещении объекта
        /// </summary>
        private async Task ValidateNoCyclicReference(long objectId, long newParentId)
        {
            var visited = new HashSet<long>();
            long? currentId = newParentId;
            
            while (currentId.HasValue)
            {
                if (currentId.Value == objectId)
                {
                    throw new InvalidOperationException($"Нельзя переместить объект {objectId}: это создаст циклическую ссылку");
                }
                
                if (visited.Contains(currentId.Value))
                {
                    // Обнаружен цикл в существующей структуре - это не должно произойти, но лучше проверить
                    throw new InvalidOperationException("Обнаружена циклическая ссылка в существующей структуре данных");
                }
                
                visited.Add(currentId.Value);
                
                var parent = await _context.Objects.Where(o => o.Id == currentId.Value).Select(o => o.IdParent).FirstOrDefaultAsync();
                currentId = parent;
            }
        }

        /// <summary>
        /// Преобразует RedbObject в TreeRedbObject
        /// </summary>
        private TreeRedbObject<TProps> ConvertToTreeObject<TProps>(RedbObject<TProps> source) where TProps : class, new()
        {
            return new TreeRedbObject<TProps>
            {
                id = source.id,
                parent_id = source.parent_id,
                scheme_id = source.scheme_id,
                owner_id = source.owner_id,
                who_change_id = source.who_change_id,
                date_create = source.date_create,
                date_modify = source.date_modify,
                date_begin = source.date_begin,
                date_complete = source.date_complete,
                key = source.key,
                code_int = source.code_int,
                code_string = source.code_string,
                code_guid = source.code_guid,
                name = source.name,
                note = source.note,
                @bool = source.@bool,
                hash = source.hash,
                properties = source.properties
            };
        }

        /// <summary>
        /// Преобразует RedbObject в базовый TreeRedbObject (без типизированных свойств)
        /// </summary>
        private TreeRedbObject ConvertToPolymorphicTreeObject(RedbObject<object> source)
        {
            return new TreeRedbObject
            {
                id = source.id,
                parent_id = source.parent_id,
                scheme_id = source.scheme_id,
                owner_id = source.owner_id,
                who_change_id = source.who_change_id,
                date_create = source.date_create,
                date_modify = source.date_modify,
                date_begin = source.date_begin,
                date_complete = source.date_complete,
                key = source.key,
                code_int = source.code_int,
                code_string = source.code_string,
                code_guid = source.code_guid,
                name = source.name,
                note = source.note,
                @bool = source.@bool,
                hash = source.hash
            };
        }

        // ===== ПРИВАТНЫЕ ПОЛИМОРФНЫЕ МЕТОДЫ =====
        
        /// <summary>
        /// Динамически загрузить объект с определением типа по scheme_id через AutomaticTypeRegistry
        /// Использует оптимизированный SQL запрос (scheme_id + json за один раз)
        /// </summary>
        private async Task<IRedbObject> LoadDynamicObjectAsync(long objectId, IRedbUser? user = null)
        {
            // Проверяем права доступа если нужно
            if (_configuration.DefaultCheckPermissionsOnLoad && user != null)
            {
                var canRead = await _permissionProvider.CanUserSelectObject(objectId, user.Id);
                if (!canRead)
                {
                    throw new UnauthorizedAccessException($"Пользователь {user.Id} не имеет прав на чтение объекта {objectId}");
                }
            }

            // Оптимизированный SQL - scheme_id + json за один запрос
            var result = await _context.Database.SqlQueryRaw<SchemeWithJson>(
                @"SELECT _id_scheme as SchemeId, get_object_json(_id, 1)::text as JsonData 
                  FROM _objects WHERE _id = {0}", objectId).FirstOrDefaultAsync();

            if (result == null || string.IsNullOrEmpty(result.JsonData))
            {
                throw new InvalidOperationException($"Объект с ID {objectId} не найден");
            }

            // Автоматическое определение типа из реестра
            var propsType = AutomaticTypeRegistry.GetTypeBySchemeId(result.SchemeId) ?? typeof(object);

            // Динамически десериализуем в правильный тип
            return _serializer.DeserializeDynamic(result.JsonData, propsType);
        }
        
        private async Task<ITreeRedbObject> LoadPolymorphicTreeWithUserAsync(long rootId, int maxDepth = 10, long? userId = null, bool checkPermissions = false)
        {
            var user = userId.HasValue ? await GetUserByIdAsync(userId.Value) : null;
            
            // 🚀 Загружаем с полными типизированными свойствами!
            var baseObject = await LoadDynamicObjectAsync(rootId, user);
            
            // Создаем полноценный TreeRedbObject с сохранением properties
            var treeObject = ConvertToPolymorphicTreeObjectWithProps(baseObject);
            
            await LoadPolymorphicChildrenRecursively(treeObject, maxDepth - 1, userId, checkPermissions);
            
            return treeObject;
        }

        private async Task<IEnumerable<ITreeRedbObject>> GetPolymorphicChildrenWithUserAsync(long parentId, long? userId = null, bool checkPermissions = false)
        {
            // 🚀 Оптимизированный SQL - scheme_id + json + object_id за один запрос
            var sql = @"
                SELECT o._id as ObjectId, o._id_scheme as SchemeId, get_object_json(o._id, 1)::text as JsonData 
                FROM _objects o
                WHERE o._id_parent = {0}
                ORDER BY o._name, o._id";
                
            var results = await _context.Database.SqlQueryRaw<ChildObjectInfo>(sql, parentId).ToListAsync();
            
            var children = new List<ITreeRedbObject>();
            var user = userId.HasValue ? await GetUserByIdAsync(userId.Value) : null;
            
            foreach (var result in results)
            {
                if (string.IsNullOrEmpty(result.JsonData)) continue;
                
                try
                {
                    // Проверяем права доступа перед десериализацией
                    if (checkPermissions && userId.HasValue)
                    {
                        var canSelect = await _permissionProvider.CanUserSelectObject(result.ObjectId, userId.Value);
                        if (!canSelect) continue;
                    }
                    
                    // Автоматическое определение типа из реестра
                    var propsType = AutomaticTypeRegistry.GetTypeBySchemeId(result.SchemeId) ?? typeof(object);
                    
                    // Динамически десериализуем в правильный тип
                    var typedObject = _serializer.DeserializeDynamic(result.JsonData, propsType);
                    
                    // Конвертируем в TreeRedbObject с сохранением properties
                    var treeObj = ConvertToPolymorphicTreeObjectWithProps(typedObject);
                    children.Add(treeObj);
                }
                catch (Exception ex)
                {
                    // Логируем ошибку десериализации, но продолжаем

                }
            }
            
            return children;
        }

        private async Task<IEnumerable<ITreeRedbObject>> GetPolymorphicPathToRootWithUserAsync(long objectId, long? userId = null, bool checkPermissions = false)
        {
            var path = new List<ITreeRedbObject>();
            var visited = new HashSet<long>(); // 🛡️ ЗАЩИТА ОТ ЦИКЛОВ
            long? currentId = objectId;
            
            while (currentId.HasValue)
            {
                // 🚨 КРИТИЧЕСКАЯ ПРОВЕРКА: Обнаружение циклической ссылки
                if (visited.Contains(currentId.Value))
                {

                    break;
                }
                
                visited.Add(currentId.Value);
                
                try
                {
                    // 🚀 Используем динамическую загрузку с типизацией
                    var user = userId.HasValue ? await GetUserByIdAsync(userId.Value) : null;
                    var typedObject = await LoadDynamicObjectAsync(currentId.Value, user);
                    
                    // Дополнительная проверка прав если нужно (уже проверены в LoadDynamicObjectAsync)
                    if (checkPermissions && userId.HasValue)
                    {
                        var canSelect = await _permissionProvider.CanUserSelectObject(currentId.Value, userId.Value);
                        if (!canSelect) break;
                    }
                    
                    var treeObj = ConvertToPolymorphicTreeObjectWithProps(typedObject);
                    path.Insert(0, treeObj); // Вставляем в начало для правильного порядка
                    currentId = typedObject.ParentId;
                }
                catch (UnauthorizedAccessException)
                {
                    // Если нет прав на объект в пути, прерываем построение пути
                    break;
                }
                catch (Exception)
                {
                    // Если объект не найден или другая ошибка, прерываем
                    break;
                }
            }
            
            // Устанавливаем связи Parent для корректной навигации
            for (int i = 0; i < path.Count - 1; i++)
            {
                path[i + 1].Parent = path[i];
            }
            
            return path;
        }

        private async Task<IEnumerable<ITreeRedbObject>> GetPolymorphicDescendantsWithUserAsync(long parentId, int maxDepth = 50, long? userId = null, bool checkPermissions = false)
        {
            var descendants = new List<ITreeRedbObject>();
            await CollectPolymorphicDescendants(parentId, descendants, maxDepth, 0, userId, checkPermissions);
            return descendants;
        }



        private async Task LoadPolymorphicChildrenRecursively(ITreeRedbObject parent, int remainingDepth, long? userId, bool checkPermissions)
        {
            if (remainingDepth <= 0) return;
            
            var children = await GetPolymorphicChildrenWithUserAsync(parent.Id, userId, checkPermissions);
            
            foreach (var child in children)
            {
                child.Parent = parent;
                parent.Children.Add(child);
                
                // Рекурсивно загружаем детей для каждого ребенка
                await LoadPolymorphicChildrenRecursively(child, remainingDepth - 1, userId, checkPermissions);
            }
        }

        private async Task CollectPolymorphicDescendants(long parentId, List<ITreeRedbObject> descendants, int maxDepth, int currentDepth, long? userId, bool checkPermissions)
        {
            if (currentDepth >= maxDepth) return;
            
            var children = await GetPolymorphicChildrenWithUserAsync(parentId, userId, checkPermissions);
            
            foreach (var child in children)
            {
                descendants.Add(child);
                
                // Рекурсивно собираем потомков каждого ребенка
                await CollectPolymorphicDescendants(child.Id, descendants, maxDepth, currentDepth + 1, userId, checkPermissions);
            }
        }

        /// <summary>
        /// Получить пользователя по ID (заглушка, можно заменить на реальную реализацию)
        /// </summary>
        private async Task<IRedbUser?> GetUserByIdAsync(long userId)
        {
            // TODO: Интегрировать с IUserProvider когда он будет доступен
            return new DummyUser { Id = userId };
        }

        /// <summary>
        /// Конвертировать типизированный IRedbObject в ITreeRedbObject с сохранением properties
        /// </summary>
        private ITreeRedbObject ConvertToPolymorphicTreeObjectWithProps(IRedbObject source)
        {
            // Приводим к RedbObject для доступа к snake_case свойствам
            var redbObj = source as RedbObject;
            if (redbObj == null)
            {
                throw new InvalidOperationException($"Объект должен быть типа RedbObject, получен: {source.GetType()}");
            }

            // Создаем новый TreeRedbObject, наследующий от source
            var treeObject = new TreeRedbObjectDynamic(source)
            {
                id = redbObj.id,
                parent_id = redbObj.parent_id,
                scheme_id = redbObj.scheme_id,
                owner_id = redbObj.owner_id,
                who_change_id = redbObj.who_change_id,
                date_create = redbObj.date_create,
                date_modify = redbObj.date_modify,
                date_begin = redbObj.date_begin,
                date_complete = redbObj.date_complete,
                key = redbObj.key,
                code_int = redbObj.code_int,
                code_string = redbObj.code_string,
                code_guid = redbObj.code_guid,
                name = redbObj.name,
                note = redbObj.note,
                @bool = redbObj.@bool,
                hash = redbObj.hash
            };

            return treeObject;
        }

        /// <summary>
        /// Заглушка для пользователя (временная)
        /// </summary>
        private class DummyUser : IRedbUser
        {
            public long Id { get; set; }
            public string Login { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public bool Enabled { get; set; } = true;
            public DateTime DateRegister { get; set; } = DateTime.UtcNow;
            public DateTime? DateDismiss { get; set; }
            public string? Phone { get; set; }
            public string? Email { get; set; }
            
            // === НОВЫЕ ПОЛЯ ===
            public long? Key { get; set; }
            public long? CodeInt { get; set; }
            public string? CodeString { get; set; }
            public Guid? CodeGuid { get; set; }
            public string? Note { get; set; }
            public Guid? Hash { get; set; }
        }

        /// <summary>
        /// Динамический TreeRedbObject который сохраняет ссылку на исходный типизированный объект
        /// </summary>
        private class TreeRedbObjectDynamic : TreeRedbObject, ITreeRedbObject
        {
            public IRedbObject SourceObject { get; }

            public TreeRedbObjectDynamic(IRedbObject source)
            {
                SourceObject = source;
            }
        }

    }
}
