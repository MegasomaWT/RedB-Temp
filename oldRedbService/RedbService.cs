using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using redb.Core.Models;
using redb.Core.Utils;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using redb.Core.Serialization;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using redb.Core.Query;
using redb.Core.Postgres.Query;

namespace redb.Core.Postgres
{
    public partial class RedbService(IServiceProvider serviceProvider) : IRedbService
    {
        private readonly Core.RedbContext _redbContext = serviceProvider.GetService<RedbContext>() ?? throw new NotImplementedException();
        private readonly IRedbObjectSerializer _serializer = serviceProvider.GetService<IRedbObjectSerializer>() ?? new SystemTextJsonRedbSerializer();
        private readonly IRedbQueryProvider _queryProvider = new PostgresQueryProvider(
            serviceProvider.GetService<RedbContext>() ?? throw new NotImplementedException(), 
            serviceProvider.GetService<IRedbObjectSerializer>() ?? new SystemTextJsonRedbSerializer());
        public Core.RedbContext RedbContext => _redbContext;

        public string dbVersion => _redbContext.Database.SqlQueryRaw<string>("SELECT version() \"Value\"").First();
        public string dbType => _redbContext.Database.IsNpgsql() ? "Postgresql" : "undefined";
        public string dbMigration => _redbContext.Database.GetMigrations().Last();
        public int? dbSize => _redbContext.Database.SqlQueryRaw<int>("select pg_database_size(current_database())  \"Value\"").First();

        public IQueryable<T> GetAll<T>() where T : class => _redbContext.Set<T>();
        public Task<T?> GetById<T>(long id) where T : class => _redbContext.FindAsync<T>(id).AsTask();
        public Task<int> DeleteById<T>(long id) where T : class => _redbContext.Set<T>().Filter("Id", id).ExecuteDeleteAsync();

        // Получить id объектов, доступных пользователю на чтение
        public IQueryable<long> GetReadableObjectIds(long userId)
        {
            // комментарии к коду на русском
            return _redbContext.Set<VUserPermission>()
                .Where(p => p.UserId == userId && p.CanSelect)
                .Select(p => p.ObjectId);
        }

        // Получить объекты конкретной схемы, доступные пользователю на чтение
        public IQueryable<Models._RObject> GetReadableObjects(long userId, long schemeId)
        {
            var readable = _redbContext.Set<VUserPermission>()
                .Where(p => p.UserId == userId && p.CanSelect)
                .Select(p => p.ObjectId);

            return _redbContext.Objects
                .Where(o => o.IdScheme == schemeId && readable.Contains(o.Id));
        }

        // Точечная проверка: может ли пользователь редактировать объект
        public async Task<bool> CanUserEditObject(long userId, long objectId)
        {
            var sql = @"SELECT 
                object_id AS ""ObjectId"",
                user_id AS ""UserId"",
                permission_source_id AS ""PermissionSourceId"",
                permission_type AS ""PermissionType"",
                _id_role AS ""IdRole"",
                _id_user AS ""IdUser"",
                can_select AS ""CanSelect"",
                can_insert AS ""CanInsert"",
                can_update AS ""CanUpdate"",
                can_delete AS ""CanDelete""
            FROM get_user_permissions_for_object({0},{1})";

            var r = await _redbContext.Database.SqlQueryRaw<UserPermissionResult>(sql, objectId, userId)
                .FirstOrDefaultAsync();

            return r?.CanUpdate == true;
        }

        public async Task<bool> CanUserDeleteObject(long userId, long objectId)
        {
            var sql = @"SELECT 
                object_id AS ""ObjectId"",
                user_id AS ""UserId"",
                permission_source_id AS ""PermissionSourceId"",
                permission_type AS ""PermissionType"",
                _id_role AS ""IdRole"",
                _id_user AS ""IdUser"",
                can_select AS ""CanSelect"",
                can_insert AS ""CanInsert"",
                can_update AS ""CanUpdate"",
                can_delete AS ""CanDelete""
            FROM get_user_permissions_for_object({0},{1})";

            var r = await _redbContext.Database.SqlQueryRaw<UserPermissionResult>(sql, objectId, userId)
                .FirstOrDefaultAsync();

            return r?.CanDelete == true;
        }

        public async Task<bool> CanUserSelectObject(long userId, long objectId)
        {
            var sql = @"SELECT 
                object_id AS ""ObjectId"",
                user_id AS ""UserId"",
                permission_source_id AS ""PermissionSourceId"",
                permission_type AS ""PermissionType"",
                _id_role AS ""IdRole"",
                _id_user AS ""IdUser"",
                can_select AS ""CanSelect"",
                can_insert AS ""CanInsert"",
                can_update AS ""CanUpdate"",
                can_delete AS ""CanDelete""
            FROM get_user_permissions_for_object({0},{1})";

            var r = await _redbContext.Database.SqlQueryRaw<UserPermissionResult>(sql, objectId, userId)
                .FirstOrDefaultAsync();

            return r?.CanSelect == true;
        }

        public async Task<bool> CanUserInsertScheme(long userId, long schemeId)
        {
            // Для проверки прав на создание объектов в схеме можно использовать схему как объект
            // Или создать отдельную логику для проверки прав на схемы
            var sql = @"SELECT 
                object_id AS ""ObjectId"",
                user_id AS ""UserId"",
                permission_source_id AS ""PermissionSourceId"",
                permission_type AS ""PermissionType"",
                _id_role AS ""IdRole"",
                _id_user AS ""IdUser"",
                can_select AS ""CanSelect"",
                can_insert AS ""CanInsert"",
                can_update AS ""CanUpdate"",
                can_delete AS ""CanDelete""
            FROM get_user_permissions_for_object({0},{1})";

            var r = await _redbContext.Database.SqlQueryRaw<UserPermissionResult>(sql, schemeId, userId)
                .FirstOrDefaultAsync();

            return r?.CanInsert == true;
        }

        // Детали прав для конкретного объекта
        public Task<UserPermissionResult?> GetUserPermissionDetails(long userId, long objectId)
        {
            var sql = @"SELECT 
                object_id AS ""ObjectId"",
                user_id AS ""UserId"",
                permission_source_id AS ""PermissionSourceId"",
                permission_type AS ""PermissionType"",
                _id_role AS ""IdRole"",
                _id_user AS ""IdUser"",
                can_select AS ""CanSelect"",
                can_insert AS ""CanInsert"",
                can_update AS ""CanUpdate"",
                can_delete AS ""CanDelete""
            FROM get_user_permissions_for_object({0},{1})";

            return _redbContext.Database.SqlQueryRaw<UserPermissionResult>(sql, objectId, userId)
                .FirstOrDefaultAsync();
        }

        // Загрузка JSON объекта и десериализация в RedbObject<TProps>
        public async Task<RedbObject<TProps>> LoadAsync<TProps>(long objectId, int depth = 10, long? userId = null, bool checkPermissions = false) where TProps : class, new()
        {
            // Проверяем права на чтение, если включена проверка
            if (checkPermissions && userId.HasValue)
            {
                var canSelect = await CanUserSelectObject(userId.Value, objectId);
                if (!canSelect)
                {
                    throw new UnauthorizedAccessException($"Пользователь {userId.Value} не имеет прав на чтение объекта {objectId}");
                }
            }
            var json = await _redbContext.Database.SqlQueryRaw<string>(
                "SELECT get_object_json({0}, {1})::text AS \"Value\"", objectId, depth
            ).FirstAsync();

            return _serializer.Deserialize<TProps>(json);
        }

        // Сохранение объекта (upsert _objects + _values с учетом _store_null)
        public async Task<long> SaveAsync<TProps>(RedbObject<TProps> obj, bool checkPermissions = false) where TProps : class, new()
        {
            // Проверяем права в зависимости от операции (создание или обновление)
            if (checkPermissions)
            {
                if (obj.id > 0)
                {
                    // Обновление - проверяем право UPDATE
                    var canUpdate = await CanUserEditObject(obj.id, obj.who_change_id);
                    if (!canUpdate)
                    {
                        throw new UnauthorizedAccessException($"Пользователь {obj.who_change_id} не имеет прав на изменение объекта {obj.id}");
                    }
                }
                else
                {
                    // Создание - проверяем право INSERT на схему
                    var canInsert = await CanUserInsertScheme(obj.who_change_id, obj.scheme_id);
                    if (!canInsert)
                    {
                        throw new UnauthorizedAccessException($"Пользователь {obj.who_change_id} не имеет прав на создание объектов в схеме {obj.scheme_id}");
                    }
                }
            }
            // Генерация MD5 hash по значениям свойств (которые идут в _values)
            obj.RecomputeHash();

            var now = DateTime.Now; // Используем локальное время для PostgreSQL timestamp without time zone

            // 1) Определяем существование объекта и подготавливаем данные
            var exists = obj.id > 0 && await _redbContext.Objects.AnyAsync(o => o.Id == obj.id);
            long objectId = obj.id;

            if (!exists)
            {
                // Создание нового объекта
                objectId = await _redbContext.GetNextKeyAsync();
                var newObj = new _RObject
                {
                    Id = objectId,
                    IdParent = obj.parent_id,
                    IdScheme = obj.scheme_id,
                    IdOwner = obj.owner_id,
                    IdWhoChange = obj.who_change_id,
                    DateCreate = obj.date_create == default ? now : NormalizeDateTime(obj.date_create),
                    DateModify = obj.date_modify == default ? now : NormalizeDateTime(obj.date_modify),
                    DateBegin = obj.date_begin.HasValue ? NormalizeDateTime(obj.date_begin.Value) : null,
                    DateComplete = obj.date_complete.HasValue ? NormalizeDateTime(obj.date_complete.Value) : null,
                    Key = obj.key,
                    CodeInt = obj.code_int,
                    CodeString = obj.code_string,
                    CodeGuid = obj.code_guid,
                    Name = obj.name,
                    Note = obj.note,
                    Bool = obj.@bool,
                    Hash = obj.hash
                };
                _redbContext.Objects.Add(newObj);
                obj.id = objectId; // Обновляем ID в исходном объекте
            }
            else
            {
                // Обновление существующего объекта
                var dbObj = await _redbContext.Objects.FirstAsync(o => o.Id == obj.id);
                UpdateObjectFields<TProps>(dbObj, obj, now);
            }

            // 2) Обработка свойств (_values)
            await ProcessPropertiesAsync(obj, objectId);

            // 3) Сохранение всех изменений одной операцией (EF сам управляет транзакцией)
            await _redbContext.SaveChangesAsync();
            return objectId;
        }

        // Вспомогательные методы для улучшения читаемости

        /// <summary>
        /// Нормализует DateTime для PostgreSQL (убирает Kind=UTC)
        /// </summary>
        private static DateTime NormalizeDateTime(DateTime dateTime)
        {
            return dateTime.Kind == DateTimeKind.Utc 
                ? DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified)
                : dateTime;
        }

        /// <summary>
        /// Обновляет поля существующего объекта
        /// </summary>
        private static void UpdateObjectFields<TProps>(_RObject dbObj, RedbObject<TProps> obj, DateTime now) where TProps : class, new()
        {
            dbObj.IdParent = obj.parent_id;
            dbObj.IdScheme = obj.scheme_id;
            dbObj.IdOwner = obj.owner_id;
            dbObj.IdWhoChange = obj.who_change_id;
            dbObj.DateBegin = obj.date_begin.HasValue ? NormalizeDateTime(obj.date_begin.Value) : null;
            dbObj.DateComplete = obj.date_complete.HasValue ? NormalizeDateTime(obj.date_complete.Value) : null;
            dbObj.Key = obj.key;
            dbObj.CodeInt = obj.code_int;
            dbObj.CodeString = obj.code_string;
            dbObj.CodeGuid = obj.code_guid;
            dbObj.Name = obj.name;
            dbObj.Note = obj.note;
            dbObj.Bool = obj.@bool;
            dbObj.Hash = obj.hash;
            dbObj.DateModify = now;
        }

        /// <summary>
        /// Обрабатывает свойства объекта и сохраняет их в _values
        /// </summary>
        private async Task ProcessPropertiesAsync<TProps>(RedbObject<TProps> obj, long objectId) where TProps : class, new()
        {
            // Кеш структур для схемы
            var structures = await _redbContext.Structures
                .Where(s => s.IdScheme == obj.scheme_id)
                .Select(s => new { s.Id, s.Name, s.TypeNavigation.DbType, s.IsArray, s.StoreNull })
                .ToListAsync();
            var structuresByName = structures.ToDictionary(x => x.Name, x => (object)x, StringComparer.Ordinal);

            // Загрузка текущих значений
            var values = await _redbContext.Values
                .Where(v => v.IdObject == objectId)
                .ToListAsync();
            var valuesByStructureId = values.ToDictionary(v => v.IdStructure);

            // Обработка каждого свойства
            var properties = typeof(TProps).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                await ProcessSinglePropertyAsync(property, obj.properties, objectId, structuresByName, valuesByStructureId);
            }
        }

        /// <summary>
        /// Обрабатывает одно свойство объекта
        /// </summary>
        private async Task ProcessSinglePropertyAsync(
            PropertyInfo property,
            object propertiesObject,
            long objectId,
            Dictionary<string, object> structuresByName,
            Dictionary<long, _RValue> valuesByStructureId)
        {
            if (!structuresByName.TryGetValue(property.Name, out var structure))
                return; // поле отсутствует в структуре

            var hasValue = valuesByStructureId.TryGetValue(((dynamic)structure).Id, out _RValue? valueRow);
            var propertyValue = property.GetValue(propertiesObject);

            // Обработка NULL значений с учетом _store_null
            if (propertyValue == null)
            {
                await HandleNullValueAsync((dynamic)structure, hasValue, valueRow, objectId, valuesByStructureId);
                return;
            }

            // Есть значение - создаем/обновляем запись в _values
            if (!hasValue)
            {
                var id = await _redbContext.GetNextKeyAsync();
                valueRow = new _RValue { Id = id, IdStructure = ((dynamic)structure).Id, IdObject = objectId };
                _redbContext.Values.Add(valueRow);
                valuesByStructureId[((dynamic)structure).Id] = valueRow;
            }

            SetValueByType(valueRow, (dynamic)structure, propertyValue);
        }

        /// <summary>
        /// Обрабатывает NULL значение в соответствии с настройкой _store_null
        /// </summary>
        private async Task HandleNullValueAsync(
            dynamic structure,
            bool hasValue,
            _RValue? valueRow,
            long objectId,
            Dictionary<long, _RValue> valuesByStructureId)
        {
            if (structure.StoreNull == true)
            {
                if (!hasValue)
                {
                    var id = await _redbContext.GetNextKeyAsync();
                    valueRow = new _RValue { Id = id, IdStructure = structure.Id, IdObject = objectId };
                    _redbContext.Values.Add(valueRow);
                    valuesByStructureId[structure.Id] = valueRow;
                }
                // Очищаем все столбцы (записываем NULL)
                ClearAllValueFields(valueRow!);
            }
            else if (hasValue)
            {
                // Удаляем запись если _store_null = false
                _redbContext.Values.Remove(valueRow!);
                valuesByStructureId.Remove(structure.Id);
            }
        }

        /// <summary>
        /// Очищает все поля в записи _values
        /// </summary>
        private static void ClearAllValueFields(_RValue valueRow)
        {
            valueRow.String = null;
            valueRow.Long = null;
            valueRow.Guid = null;
            valueRow.Double = null;
            valueRow.DateTime = null;
            valueRow.Boolean = null;
            valueRow.ByteArray = null;
            valueRow.Text = null;
            valueRow.Array = null;
        }

        /// <summary>
        /// Устанавливает значение в соответствующий столбец _values по типу
        /// </summary>
        private static void SetValueByType(_RValue valueRow, dynamic structure, object propertyValue)
        {
            // Сначала очищаем все поля
            ClearAllValueFields(valueRow);

            // Затем записываем в нужный столбец по типу
            switch ((string)structure.DbType)
            {
                case "String":
                    valueRow.String = (string?)propertyValue;
                    break;
                case "Long":
                    valueRow.Long = propertyValue is int intValue ? intValue : (long?)propertyValue;
                    break;
                case "Guid":
                    valueRow.Guid = (Guid?)propertyValue;
                    break;
                case "Double":
                    valueRow.Double = propertyValue is float floatValue ? floatValue : (double?)propertyValue;
                    break;
                case "DateTime":
                    valueRow.DateTime = propertyValue is DateTime dt ? NormalizeDateTime(dt) : null;
                    break;
                case "Boolean":
                    valueRow.Boolean = (bool?)propertyValue;
                    break;
                case "ByteArray":
                    valueRow.ByteArray = (byte[]?)propertyValue;
                    break;
                case "Text":
                    valueRow.Text = (string?)propertyValue;
                    break;
                case "Object":
                case "ListItem":
                    valueRow.Long = (long?)propertyValue; // ожидаем ID
                    break;
                default:
                    if (structure.IsArray == true)
                    {
                        valueRow.Array = JsonSerializer.Serialize(propertyValue);
                    }
                    break;
            }
        }

        

        // === МЕТОДЫ РАБОТЫ С ДРЕВОВИДНЫМИ СТРУКТУРАМИ ===
        
        /// <summary>
        /// Загружает дерево/поддерево объектов с указанной глубиной
        /// </summary>
        public async Task<Models.TreeRedbObject<TProps>> LoadTreeAsync<TProps>(long rootId, int maxDepth = 10, long? userId = null, bool checkPermissions = false) where TProps : class, new()
        {
            // Загружаем корневой объект
            var rootObj = await LoadAsync<TProps>(rootId, maxDepth, userId, checkPermissions);
            
            // Преобразуем в TreeRedbObject
            var treeRoot = ConvertToTreeObject<TProps>(rootObj);
            
            // Рекурсивно загружаем детей
            await LoadChildrenRecursive(treeRoot, maxDepth - 1, userId, checkPermissions);
            
            return treeRoot;
        }
        
        /// <summary>
        /// Получает прямых детей объекта (один уровень)
        /// </summary>
        public async Task<IEnumerable<Models.TreeRedbObject<TProps>>> GetChildrenAsync<TProps>(long parentId, long? userId = null, bool checkPermissions = false) where TProps : class, new()
        {
            // SQL для поиска прямых детей
            var sql = $@"
                SELECT json_data 
                FROM (
                    SELECT get_object_json(o._id, 1) as json_data
                    FROM _objects o
                    WHERE o._id_parent = {parentId}
                    ORDER BY o._name, o._id
                ) subquery
                WHERE json_data IS NOT NULL";
                
            var jsonResults = await _redbContext.Database.SqlQueryRaw<string>(sql).ToListAsync();
            
            var children = new List<Models.TreeRedbObject<TProps>>();
            
            foreach (var json in jsonResults)
            {
                if (string.IsNullOrEmpty(json)) continue;
                
                try
                {
                    var redbObj = System.Text.Json.JsonSerializer.Deserialize<RedbObject<TProps>>(json);
                    if (redbObj == null) continue;
                    
                    // Проверяем права доступа, если включена проверка
                    if (checkPermissions && userId.HasValue)
                    {
                        var canSelect = await CanUserSelectObject(userId.Value, redbObj.id);
                        if (!canSelect) continue;
                    }
                    
                    var treeObj = ConvertToTreeObject<TProps>(redbObj);
                    children.Add(treeObj);
                }
                catch (Exception ex)
                {
                    // Логируем ошибку десериализации, но продолжаем
                    Console.WriteLine($"Ошибка десериализации объекта: {ex.Message}");
                }
            }
            
            return children;
        }
        
        /// <summary>
        /// Получает путь от объекта к корню (для хлебных крошек)
        /// </summary>
        public async Task<IEnumerable<Models.TreeRedbObject<TProps>>> GetPathToRootAsync<TProps>(long objectId, long? userId = null, bool checkPermissions = false) where TProps : class, new()
        {
            var path = new List<Models.TreeRedbObject<TProps>>();
            long? currentId = objectId;
            
            while (currentId.HasValue)
            {
                try
                {
                    var obj = await LoadAsync<TProps>(currentId.Value, 1, userId, checkPermissions);
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
        
        /// <summary>
        /// Получает всех потомков объекта рекурсивно
        /// </summary>
        public async Task<IEnumerable<Models.TreeRedbObject<TProps>>> GetDescendantsAsync<TProps>(long parentId, int maxDepth = 50, long? userId = null, bool checkPermissions = false) where TProps : class, new()
        {
            var descendants = new List<Models.TreeRedbObject<TProps>>();
            await CollectDescendants<TProps>(parentId, descendants, maxDepth, 0, userId, checkPermissions);
            return descendants;
        }
        
        /// <summary>
        /// Перемещает объект в дереве (изменяет parent_id)
        /// </summary>
        public async Task MoveObjectAsync(long objectId, long? newParentId, long userId, bool checkPermissions = true)
        {
            // Проверяем права на редактирование объекта
            if (checkPermissions)
            {
                var canEdit = await CanUserEditObject(userId, objectId);
                if (!canEdit)
                {
                    throw new UnauthorizedAccessException($"Пользователь {userId} не имеет прав на редактирование объекта {objectId}");
                }
            }
            
            // Проверяем, что новый родитель существует (если указан)
            if (newParentId.HasValue)
            {
                var parentExists = await _redbContext.Objects.AnyAsync(o => o.Id == newParentId.Value);
                if (!parentExists)
                {
                    throw new ArgumentException($"Родительский объект {newParentId} не существует");
                }
                
                // Проверяем, что не создается циклическая ссылка
                await ValidateNoCyclicReference(objectId, newParentId.Value);
            }
            
            // Обновляем parent_id
            var obj = await _redbContext.Objects.FirstOrDefaultAsync(o => o.Id == objectId);
            if (obj == null)
            {
                throw new ArgumentException($"Объект {objectId} не найден");
            }
            
            obj.IdParent = newParentId;
            obj.DateModify = DateTime.Now;
            obj.IdWhoChange = userId;
            
            await _redbContext.SaveChangesAsync();
        }
        
        /// <summary>
        /// Создает дочерний объект
        /// </summary>
        public async Task<long> CreateChildAsync<TProps>(Models.TreeRedbObject<TProps> obj, long parentId, bool checkPermissions = false) where TProps : class, new()
        {
            // Устанавливаем parent_id
            obj.parent_id = parentId;
            
            // Используем существующий метод сохранения
            return await SaveAsync<TProps>(obj, checkPermissions);
        }
        
        // === Вспомогательные методы для работы с деревом ===
        
        /// <summary>
        /// Преобразует RedbObject в TreeRedbObject
        /// </summary>
        private Models.TreeRedbObject<TProps> ConvertToTreeObject<TProps>(RedbObject<TProps> source) where TProps : class, new()
        {
            return new Models.TreeRedbObject<TProps>
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
        /// Рекурсивно загружает детей для TreeRedbObject
        /// </summary>
        private async Task LoadChildrenRecursive<TProps>(Models.TreeRedbObject<TProps> parent, int remainingDepth, long? userId, bool checkPermissions) where TProps : class, new()
        {
            if (remainingDepth <= 0) return;
            
            var children = await GetChildrenAsync<TProps>(parent.id, userId, checkPermissions);
            
            foreach (var child in children)
            {
                child.Parent = parent;
                parent.Children.Add(child);
                
                // Рекурсивно загружаем детей для каждого ребенка
                await LoadChildrenRecursive(child, remainingDepth - 1, userId, checkPermissions);
            }
        }
        
        /// <summary>
        /// Рекурсивно собирает всех потомков
        /// </summary>
        private async Task CollectDescendants<TProps>(long parentId, List<Models.TreeRedbObject<TProps>> descendants, int maxDepth, int currentDepth, long? userId, bool checkPermissions) where TProps : class, new()
        {
            if (currentDepth >= maxDepth) return;
            
            var children = await GetChildrenAsync<TProps>(parentId, userId, checkPermissions);
            
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
                
                var parent = await _redbContext.Objects.Where(o => o.Id == currentId.Value).Select(o => o.IdParent).FirstOrDefaultAsync();
                currentId = parent;
            }
        }

        // === Code-First: схемы и структуры ===

        // Убедиться, что схема существует
        public async Task<long> EnsureSchemeFromTypeAsync<TProperties>(string schemeName, string? alias = null) where TProperties : class
        {
            // ищем существующую схему
            var existing = await _redbContext.Schemes.FirstOrDefaultAsync(s => s.Name == schemeName);
            if (existing != null)
            {
                // обновим alias при необходимости
                if (!string.IsNullOrWhiteSpace(alias) && existing.Alias != alias)
                {
                    existing.Alias = alias;
                    await _redbContext.SaveChangesAsync();
                }
                return existing.Id;
            }

            // создаем новую схему
            var newId = await NextGlobalIdAsync();
            var scheme = new _RScheme
            {
                Id = newId,
                Name = schemeName,
                Alias = alias
            };
            _redbContext.Schemes.Add(scheme);
            await _redbContext.SaveChangesAsync();
            return newId;
        }

        // Объединенный метод: создание/обновление схемы + синхронизация структур
        public async Task<long> SyncSchemeAsync<TProperties>(string schemeName, string? alias = null, bool strictDeleteExtra = true) where TProperties : class
        {
            // 1. Создаем/получаем схему
            var schemeId = await EnsureSchemeFromTypeAsync<TProperties>(schemeName, alias);
            
            // 2. Синхронизируем структуры
            await SyncStructuresFromTypeAsync<TProperties>(schemeId, strictDeleteExtra);
            
            return schemeId;
        }

        // Синхронизация структур из типа свойств
        public async Task SyncStructuresFromTypeAsync<TProperties>(long schemeId, bool strictDeleteExtra = true) where TProperties : class
        {
            // получим текущие структуры
            var existing = await _redbContext.Structures.Where(s => s.IdScheme == schemeId).ToListAsync();
            var existingByName = existing.ToDictionary(s => s.Name, StringComparer.Ordinal);

            // перечислим свойства типа
            var type = typeof(TProperties);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var orderCounter = 1L;

            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var prop in properties)
            {
                var name = prop.Name; // имена должны совпадать с JSON/БД
                seen.Add(name);

                var (typeId, isArray, allowNotNull) = MapDotNetTypeToRedb(prop);

                if (existingByName.TryGetValue(name, out var s))
                {
                    // обновим при необходимости
                    bool changed = false;
                    if (s.IdType != typeId) { s.IdType = typeId; changed = true; }
                    if (s.IsArray != isArray) { s.IsArray = isArray; changed = true; }
                    if (s.AllowNotNull != allowNotNull) { s.AllowNotNull = allowNotNull; changed = true; }
                    if (s.Order != orderCounter) { s.Order = orderCounter; changed = true; }
                    if (changed) _redbContext.Structures.Update(s);
                }
                else
                {
                    var sid = await NextGlobalIdAsync();
                    _redbContext.Structures.Add(new _RStructure
                    {
                        Id = sid,
                        IdScheme = schemeId,
                        IdType = typeId,
                        Name = name,
                        Alias = name,
                        Order = orderCounter,
                        AllowNotNull = allowNotNull,
                        IsArray = isArray
                    });
                }

                orderCounter++;
            }

            if (strictDeleteExtra)
            {
                // удалить лишние структуры, которых нет в типе
                var toDelete = existing.Where(s => !seen.Contains(s.Name)).ToList();
                if (toDelete.Count > 0)
                {
                    _redbContext.Structures.RemoveRange(toDelete);
                }
            }

            await _redbContext.SaveChangesAsync();
        }

        private static Type UnwrapNullable(Type t) => Nullable.GetUnderlyingType(t) ?? t;

        // Определяет, является ли тип коллекцией IEnumerable<T> или массивом, и возвращает тип элемента
        private static bool TryGetEnumerableElementType(Type type, out Type? elementType)
        {
            // string и byte[] не считаем коллекциями для _is_array
            if (type == typeof(string) || type == typeof(byte[]))
            {
                elementType = null;
                return false;
            }

            if (type.IsArray)
            {
                elementType = type.GetElementType();
                return elementType != null;
            }

            if (type.IsGenericType)
            {
                // Ищем IEnumerable<T> на самом типе и его интерфейсах
                var seq = type
                    .GetInterfaces()
                    .Concat(new[] { type })
                    .FirstOrDefault(it => it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IEnumerable<>));

                if (seq != null)
                {
                    elementType = seq.GetGenericArguments()[0];
                    return true;
                }
            }

            elementType = null;
            return false;
        }

        private static (long typeId, bool isArray, bool allowNotNull) MapDotNetTypeToRedb(PropertyInfo propertyInfo)
        {
            var propertyType = propertyInfo.PropertyType;
            var isArray = TryGetEnumerableElementType(propertyType, out var elemType);
            var elementType = isArray ? elemType! : propertyType;

            // Определяем обязательность по аннотациям nullability свойства
            var nullableInfo = new System.Reflection.NullabilityInfoContext().Create(propertyInfo);
            var isNonNullableValueType = elementType.IsValueType && Nullable.GetUnderlyingType(elementType) == null;
            var allowNotNull = isNonNullableValueType || nullableInfo.ReadState == NullabilityState.NotNull;

            var et = UnwrapNullable(elementType);
            // Маппинг на _types (ID предзаполнены в SQL)
            long typeId = et == typeof(string) ? -9223372036854775700
                : et == typeof(long) || et == typeof(int) ? -9223372036854775704
                : et == typeof(double) || et == typeof(float) || et == typeof(decimal) ? -9223372036854775707
                : et == typeof(DateTime) ? -9223372036854775708
                : et == typeof(bool) ? -9223372036854775709
                : et == typeof(Guid) ? -9223372036854775705
                : et == typeof(byte[]) ? -9223372036854775701
                : IsRedbObject(et) ? -9223372036854775703
                : -9223372036854775702; // Text как fallback

            return (typeId, isArray, allowNotNull);
        }

        private static bool IsRedbObject(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition().Name == "RedbObject`1";
        }

        private async Task<long> NextGlobalIdAsync()
        {
            // для PostgreSQL — nextval('global_identity')
            var id = await _redbContext.Database.SqlQueryRaw<long>("SELECT nextval('global_identity') AS \"Value\"").FirstAsync();
            return id;
        }
        
        #region Удаление объектов
        
        /// <summary>
        /// Удаляет объект с проверкой прав доступа
        /// </summary>
        /// <param name="objectId">ID объекта для удаления</param>
        /// <param name="userId">ID пользователя, выполняющего удаление</param>
        /// <returns>true если объект был удален, false если не найден</returns>
        public async Task<bool> DeleteAsync(long objectId, long userId, bool checkPermissions = true)
        {
            // Проверяем права доступа, если включена проверка
            if (checkPermissions)
            {
                var canDelete = await CanUserDeleteObject(userId, objectId);
                if (!canDelete)
                {
                    throw new UnauthorizedAccessException($"Пользователь {userId} не имеет прав на удаление объекта {objectId}");
                }
            }
            
            // Проверяем существование объекта
            var objectExists = await _redbContext.Objects
                .AnyAsync(o => o.Id == objectId);
                
            if (!objectExists)
            {
                return false; // Объект не найден
            }
            
            // Удаляем объект (триггер автоматически архивирует в _deleted_objects)
            // CASCADE автоматически удалит связанные _values
            // ON DELETE SET NULL автоматически обнулит _list_items._id_object
            await _redbContext.Database.ExecuteSqlRawAsync(
                "DELETE FROM _objects WHERE _id = {0}", objectId);
                
            return true;
        }
        
        /// <summary>
        /// Каскадно удаляет объект и всех его потомков по _id_parent
        /// </summary>
        /// <param name="parentId">ID родительского объекта</param>
        /// <param name="userId">ID пользователя, выполняющего удаление</param>
        /// <returns>Количество удаленных объектов</returns>
        public async Task<int> DeleteSubtreeAsync(long parentId, long userId, bool checkPermissions = true)
        {
            // Проверяем права доступа к корневому объекту, если включена проверка
            if (checkPermissions)
            {
                var canDelete = await CanUserDeleteObject(userId, parentId);
                if (!canDelete)
                {
                    throw new UnauthorizedAccessException($"Пользователь {userId} не имеет прав на удаление объекта {parentId} и его поддерева");
                }
            }
            
            // Получаем все объекты в поддереве (рекурсивно)
            var subtreeIds = await GetSubtreeObjectIds(parentId);
            
            if (subtreeIds.Count == 0)
            {
                return 0; // Поддерево пустое
            }
            
            // Удаляем все объекты поддерева (включая корневой)
            // Триггеры автоматически архивируют каждый объект в _deleted_objects
            // CASCADE автоматически удалит все связанные _values
            // ON DELETE SET NULL автоматически обнулит _list_items._id_object
            var deletedCount = 0;
            foreach (var id in subtreeIds)
            {
                deletedCount += await _redbContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM _objects WHERE _id = {0}", id);
            }
                
            return subtreeIds.Count;
        }
        
        /// <summary>
        /// Рекурсивно получает все ID объектов в поддереве (включая корневой)
        /// </summary>
        private async Task<List<long>> GetSubtreeObjectIds(long parentId)
        {
            var allIds = new List<long>();
            var queue = new Queue<long>();
            queue.Enqueue(parentId);
            
            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                allIds.Add(currentId);
                
                // Находим всех прямых потомков текущего объекта
                var childIds = await _redbContext.Objects
                    .Where(o => o.IdParent == currentId)
                    .Select(o => o.Id)
                    .ToListAsync();
                    
                foreach (var childId in childIds)
                {
                    queue.Enqueue(childId);
                }
            }
            
            return allIds;
        }

        public IRedbQueryable<TProps> Query<TProps>(long schemeId, long? userId = null, bool checkPermissions = false) where TProps : class, new()
        {
            return _queryProvider.CreateQuery<TProps>(schemeId, userId, checkPermissions);
        }

        public async Task<IRedbQueryable<TProps>> QueryAsync<TProps>(string schemeName, long? userId = null, bool checkPermissions = false) where TProps : class, new()
        {
            var schemeId = await EnsureSchemeFromTypeAsync<TProps>(schemeName);
            return Query<TProps>(schemeId, userId, checkPermissions);
        }

        #endregion
    }
}

