using redb.Core.Providers;
using redb.Core.DBModels;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using redb.Core.Models.Attributes;
using redb.Core.Models.Entities;
using redb.Core.Models.Configuration;
using redb.Core.Models.Contracts;
using redb.Core.Caching;
using System;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace redb.Core.Postgres.Providers
{
    /// <summary>
    /// PostgreSQL реализация провайдера синхронизации схем с использованием глобального кеша
    /// </summary>
    public class PostgresSchemeSyncProvider : ISchemeSyncProvider, ISchemeCacheProvider
    {
        private readonly RedbContext _context;
        private readonly RedbServiceConfiguration _configuration;
        
        // 🌳 КЕШ ДЕРЕВА СТРУКТУР для быстрого доступа к иерархии
        private static readonly ConcurrentDictionary<long, List<StructureTreeNode>> _structureTreeCache = new();
        private static readonly ConcurrentDictionary<(long, long?), List<StructureTreeNode>> _subtreeCache = new();
        
        public PostgresSchemeSyncProvider(RedbContext context, RedbServiceConfiguration? configuration = null)
        {
            _context = context;
            _configuration = configuration ?? new RedbServiceConfiguration();
            
            // Инициализируем глобальный кеш с нашей конфигурацией
            GlobalMetadataCache.Initialize(_configuration);
        }

        public async Task<IRedbScheme> EnsureSchemeFromTypeAsync<TProps>() where TProps : class
        {
            return await EnsureSchemeFromTypeAsync(typeof(TProps).Name, GetSchemeAliasForType<TProps>());
        }

        private async Task<IRedbScheme> EnsureSchemeFromTypeAsync(string? schemeName = null, string? alias = null) 
        {
            // Используем имя класса, если schemeName не указано
            var actualSchemeName = schemeName;
            
            if (string.IsNullOrEmpty(actualSchemeName))
            {
                throw new ArgumentException("Имя схемы не может быть пустым. Используйте EnsureSchemeFromTypeAsync<TProps>() для автоопределения имени по типу.");
            }
            
            // Попытка найти существующую схему по имени
            var existingScheme = await _context.Schemes
                .FirstOrDefaultAsync(s => s.Name == actualSchemeName);

            if (existingScheme != null)
            {
                return RedbScheme.FromEntity(existingScheme);
            }

            // Создание новой схемы
            var newScheme = new _RScheme
            {
                Id = _context.GetNextKey(),
                Name = actualSchemeName,
                Alias = alias
            };

            _context.Schemes.Add(newScheme);
            await _context.SaveChangesAsync();

            return RedbScheme.FromEntity(newScheme);
        }

        public async Task<List<IRedbStructure>> SyncStructuresFromTypeAsync<TProps>(IRedbScheme scheme, bool strictDeleteExtra = true) where TProps : class
        {

            
            // Получение существующих структур схемы
            var existingStructures = await _context.Structures
                .Where(s => s.IdScheme == scheme.Id)
                .ToListAsync();

            var structuresToKeep = new List<long>();

            // ✅ НОВАЯ ПАРАДИГМА: Рекурсивное создание структур для всех типов включая бизнес-классы  
            await SyncStructuresRecursively(typeof(TProps), scheme.Id, null, existingStructures, structuresToKeep);

            // Удаление лишних структур (если strictDeleteExtra = true)
            if (strictDeleteExtra)
            {
                var structuresToDelete = existingStructures
                    .Where(s => !structuresToKeep.Contains(s.Id))
                    .ToList();

                _context.Structures.RemoveRange(structuresToDelete);
            }

            await _context.SaveChangesAsync();
            
            // Возвращаем обновленный список структур
            var updatedStructures = await _context.Structures
                .Where(s => s.IdScheme == scheme.Id)
                .ToListAsync();
            
            return updatedStructures.Select(s => (IRedbStructure)RedbStructure.FromEntity(s)).ToList();
        }

        /// <summary>
        /// ✅ НОВАЯ ПАРАДИГМА: Рекурсивное создание структур для всех полей включая дочерние поля бизнес-классов
        /// </summary>
        private async Task SyncStructuresRecursively(Type type, long schemeId, long? parentId, List<_RStructure> existingStructures, List<long> structuresToKeep, HashSet<Type>? visitedTypes = null)
        {
            // Защита от циклических ссылок
            visitedTypes ??= new HashSet<Type>();
            if (visitedTypes.Contains(type)) return;
            visitedTypes.Add(type);

            // Получение свойств типа через рефлексию (исключаем поля с [JsonIgnore])
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !p.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Any())
                .ToArray();
            var nullabilityContext = new NullabilityInfoContext();

            foreach (var property in properties)
            {
                var nullabilityInfo = nullabilityContext.Create(property);
                var isArray = IsArrayType(property.PropertyType);
                var baseType = isArray ? GetArrayElementType(property.PropertyType) : property.PropertyType;
                
                // Определяем обязательность: не nullable и не имеет Nullable<T>
                var isRequired = nullabilityInfo.WriteState != NullabilityState.Nullable && 
                                Nullable.GetUnderlyingType(baseType) == null;
                
                var typeId = await GetTypeIdForTypeAsync(baseType);
                var structureName = property.Name; // ✅ БЕЗ префикса - только имя свойства

                // Поиск существующей структуры с учетом родителя
                var existingStructure = existingStructures
                    .FirstOrDefault(s => s.Name == structureName && s.IdParent == parentId);

                if (existingStructure != null)
                {
                    // Обновление существующей структуры
                    existingStructure.IdType = typeId;
                    existingStructure.AllowNotNull = isRequired;
                    existingStructure.IsArray = isArray;
                    structuresToKeep.Add(existingStructure.Id);
                }
                else
                {
                    // Создание новой структуры
                    var newStructure = new _RStructure
                    {
                        Id = _context.GetNextKey(),
                        IdScheme = schemeId,
                        IdParent = parentId,  // ✅ Используем переданный parentId
                        Name = structureName,
                        IdType = typeId,
                        AllowNotNull = isRequired,
                        IsArray = isArray,
                        Order = properties.ToList().IndexOf(property)
                    };

                    _context.Structures.Add(newStructure);
                    existingStructures.Add(newStructure); // Добавляем в список для поиска
                    structuresToKeep.Add(newStructure.Id);
                }

                // ✅ РЕКУРСИЯ: Если это бизнес-класс, создаем структуры для его дочерних полей
                if (IsBusinessClass(baseType))
                {
                    // Получаем ID текущей структуры для передачи как parentId
                    var currentStructureId = existingStructure?.Id ?? 
                        existingStructures.Last(s => s.Name == structureName && s.IdParent == parentId).Id;
                    
                    await SyncStructuresRecursively(baseType, schemeId, currentStructureId, existingStructures, structuresToKeep, visitedTypes);
                }
            }
            
            visitedTypes.Remove(type);
        }

        private async Task<IRedbScheme> SyncSchemeAsync<TProps>(string? schemeName = null, string? alias = null, bool strictDeleteExtra = true) where TProps : class
        {
            var scheme = await EnsureSchemeFromTypeAsync(schemeName, alias);
            await SyncStructuresFromTypeAsync<TProps>(scheme, strictDeleteExtra);
            return scheme;
        }

        public async Task<IRedbScheme> SyncSchemeAsync<TProps>() where TProps : class
        {
            // Автоопределение алиаса из атрибута RedbSchemeAttribute
            // Имя схемы всегда = имя класса
            var attr = GetRedbSchemeAttribute<TProps>();
            var alias = attr?.Alias;
            
            // Используем имя класса как имя схемы, strictDeleteExtra = true
            return await SyncSchemeAsync<TProps>(typeof(TProps).Name, alias: alias, strictDeleteExtra: true);
        }

        /// <summary>
        /// Извлекает атрибут RedbSchemeAttribute из типа
        /// </summary>
        private static RedbSchemeAttribute? GetRedbSchemeAttribute<TProps>() where TProps : class
        {
            return typeof(TProps).GetCustomAttribute<RedbSchemeAttribute>();
        }

        /// <summary>
        /// Извлекает атрибут RedbSchemeAttribute из типа
        /// </summary>
        private static RedbSchemeAttribute? GetRedbSchemeAttribute(Type type)
        {
            return type.GetCustomAttribute<RedbSchemeAttribute>();
        }

        private static bool IsArrayType(Type type)
        {
            return type.IsArray || 
                   (type.IsGenericType && 
                    (type.GetGenericTypeDefinition() == typeof(List<>) ||
                     type.GetGenericTypeDefinition() == typeof(IList<>) ||
                     type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                     type.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
        }

        private static Type GetArrayElementType(Type arrayType)
        {
            if (arrayType.IsArray)
                return arrayType.GetElementType()!;
            
            if (arrayType.IsGenericType)
                return arrayType.GetGenericArguments()[0];
            
            return arrayType;
        }

        private async Task<long> GetTypeIdForTypeAsync(Type type)
        {
            // Получаем базовый тип для nullable типов
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            
            // Динамический маппинг C# типов на имена типов REDB из базы данных
            var typeName = await MapCSharpTypeToRedbTypeAsync(underlyingType);
            
            // === ПРИМЕНЕНИЕ НАСТРОЕК КЕШИРОВАНИЯ ===
            var cachedId = GlobalMetadataCache.GetTypeId(typeName);
            if (cachedId.HasValue)
                return cachedId.Value;
            
            // Загрузка из БД
            var typeEntity = await _context.Set<_RType>()
                .FirstOrDefaultAsync(t => t.Name == typeName);
            
            if (typeEntity == null)
            {
                throw new InvalidOperationException($"Тип '{typeName}' не найден в таблице _types. Проверьте схему БД.");
            }
            
            // === ПРИМЕНЕНИЕ НАСТРОЕК КЕШИРОВАНИЯ ===
            GlobalMetadataCache.CacheType(typeName, typeEntity.Id);
            return typeEntity.Id;
        }

        private static Dictionary<Type, string>? _csharpToRedbTypeCache;
        
        private async Task<string> MapCSharpTypeToRedbTypeAsync(Type csharpType)
        {

            
            // Инициализируем кеш маппинга из БД если еще не загружен
            if (_csharpToRedbTypeCache == null)
            {

                await InitializeCSharpToRedbTypeMappingAsync();
            }

            // 🚀 ИСПРАВЛЕНИЕ: Правильная проверка дженериков RedbObject<>
            if (csharpType.IsGenericType && csharpType.GetGenericTypeDefinition() == typeof(RedbObject<>))
            {

                return "Object";
            }

            // ✅ НОВАЯ ПАРАДИГМА: Бизнес-классы (не примитивы) маппим в "Class"  
            if (IsBusinessClass(csharpType))
            {

                return "Class";
            }

            // Ищем точное соответствие ПОСЛЕ новой логики
            if (_csharpToRedbTypeCache!.TryGetValue(csharpType, out var exactMatch))
            {

                return exactMatch;
            }

            // Если тип не найден, возвращаем String как fallback

            return "String";
        }

        private async Task InitializeCSharpToRedbTypeMappingAsync()
        {
            var allTypes = await _context.Set<_RType>().ToListAsync();
            _csharpToRedbTypeCache = new Dictionary<Type, string>();

            foreach (var dbType in allTypes)
            {
                var dotNetTypeName = dbType.Type1; // Используем Type1 из модели
                if (string.IsNullOrEmpty(dotNetTypeName))
                    continue;

                // Маппинг строковых представлений типов на реальные C# типы
                var csharpType = MapStringToType(dotNetTypeName);
                if (csharpType != null)
                {
                    _csharpToRedbTypeCache[csharpType] = dbType.Name;
                }
            }
        }

        private static Type? MapStringToType(string typeName)
        {
            return typeName switch
            {
                "string" => typeof(string),
                "int" => typeof(int),
                "long" => typeof(long),
                "short" => typeof(short),
                "byte" => typeof(byte),
                "double" => typeof(double),
                "float" => typeof(float),
                "decimal" => typeof(decimal),
                "boolean" => typeof(bool),
                "DateTime" => typeof(DateTime),
                "Guid" => typeof(Guid),
                "byte[]" => typeof(byte[]),
                "char" => typeof(char),
                "TimeSpan" => typeof(TimeSpan),
#if NET6_0_OR_GREATER
                "DateOnly" => typeof(DateOnly),
                "TimeOnly" => typeof(TimeOnly),
#endif
                "_RObject" => typeof(RedbObject<>),
                "_RListItem" => null, // Специальный тип, не маппится напрямую
                "Enum" => typeof(Enum),
                _ => null // Неизвестный тип
            };
        }

        /// <summary>
        /// ✅ НОВАЯ ПАРАДИГМА: Определить, является ли тип бизнес-классом (Class тип)
        /// </summary>
        private static bool IsBusinessClass(Type csharpType)
        {
            // 🔍 ДИАГНОСТИКА

            
            // Примитивные типы - НЕ бизнес-классы
            if (csharpType.IsPrimitive || csharpType == typeof(string) || csharpType == typeof(decimal)) 
            {

                return false;
            }

            // Системные типы - НЕ бизнес-классы  
            if (csharpType == typeof(DateTime) || csharpType == typeof(Guid) || csharpType == typeof(TimeSpan) || csharpType == typeof(byte[]))
            {

                return false;
            }

            // Nullable типы - проверяем базовый тип
            if (Nullable.GetUnderlyingType(csharpType) != null)
            {

                return false;
            }

            // Массивы и коллекции - НЕ бизнес-классы
            if (csharpType.IsArray || IsArrayType(csharpType))
            {

                return false;
            }

            // RedbObject<> - НЕ бизнес-класс (это ссылка) 
            if (csharpType.IsGenericType && csharpType.GetGenericTypeDefinition() == typeof(RedbObject<>))
            {

                return false;
            }

            // Енумы - НЕ бизнес-классы
            if (csharpType.IsEnum)
            {

                return false;
            }

            // Системные неймспейсы - НЕ бизнес-классы
            if (csharpType.Namespace?.StartsWith("System") == true)
            {

                return false;
            }

            // Остальные пользовательские классы - ЭТО бизнес-классы
            bool result = csharpType.IsClass;

            return result;
        }

        // ===== НОВЫЕ МЕТОДЫ ИЗ КОНТРАКТА =====

        public async Task<IRedbScheme?> GetSchemeByTypeAsync<TProps>() where TProps : class
        {
            var schemeName = typeof(TProps).Name;
            var scheme = await _context.Schemes
                .FirstOrDefaultAsync(s => s.Name == schemeName);
            return scheme != null ? RedbScheme.FromEntity(scheme) : null;
        }

        public async Task<IRedbScheme?> GetSchemeByTypeAsync(Type type)
        {
            var schemeName = type.Name;
            var scheme = await _context.Schemes
                .FirstOrDefaultAsync(s => s.Name == schemeName);
            return scheme != null ? RedbScheme.FromEntity(scheme) : null;
        }

        public async Task<IRedbScheme> LoadSchemeByTypeAsync<TProps>() where TProps : class
        {
            var scheme = await GetSchemeByTypeAsync<TProps>();
            if (scheme == null)
                throw new ArgumentException($"Схема для типа '{typeof(TProps).Name}' не найдена");
            return scheme;
        }

        public async Task<IRedbScheme> LoadSchemeByTypeAsync(Type type)
        {
            var scheme = await GetSchemeByTypeAsync(type);
            if (scheme == null)
                throw new ArgumentException($"Схема для типа '{type.Name}' не найдена");
            return scheme;
        }

        public async Task<List<IRedbStructure>> GetStructuresByTypeAsync<TProps>() where TProps : class
        {
            var scheme = await GetSchemeByTypeAsync<TProps>();
            if (scheme == null)
                return new List<IRedbStructure>();

            var structures = await _context.Structures
                .Where(s => s.IdScheme == scheme.Id)
                .ToListAsync();

            return structures.Select(s => RedbStructure.FromEntity(s)).Cast<IRedbStructure>().ToList();
        }

        public async Task<List<IRedbStructure>> GetStructuresByTypeAsync(Type type)
        {
            var scheme = await GetSchemeByTypeAsync(type);
            if (scheme == null)
                return new List<IRedbStructure>();

            var structures = await _context.Structures
                .Where(s => s.IdScheme == scheme.Id)
                .ToListAsync();

            return structures.Select(s => RedbStructure.FromEntity(s)).Cast<IRedbStructure>().ToList();
        }

        public async Task<bool> SchemeExistsForTypeAsync<TProps>() where TProps : class
        {
            var schemeName = typeof(TProps).Name;
            return await _context.Schemes.AnyAsync(s => s.Name == schemeName);
        }

        public async Task<bool> SchemeExistsForTypeAsync(Type type)
        {
            var schemeName = type.Name;
            return await _context.Schemes.AnyAsync(s => s.Name == schemeName);
        }

        public async Task<bool> SchemeExistsByNameAsync(string schemeName)
        {
            return await _context.Schemes.AnyAsync(s => s.Name == schemeName);
        }

        public string GetSchemeNameForType<TProps>() where TProps : class
        {
            return typeof(TProps).Name;
        }

        public string GetSchemeNameForType(Type type)
        {
            return type.Name;
        }

        public string? GetSchemeAliasForType<TProps>() where TProps : class
        {
            var attribute = GetRedbSchemeAttribute<TProps>();
            return attribute?.Alias;
        }

        public string? GetSchemeAliasForType(Type type)
        {
            var attribute = GetRedbSchemeAttribute(type);
            return attribute?.Alias;
        }

        // ===== НЕДОСТАЮЩИЕ МЕТОДЫ ИЗ КОНТРАКТА =====
        
        public async Task<IRedbScheme?> GetSchemeByIdAsync(long schemeId)
        {
            // Проверяем глобальный кеш
            var cachedScheme = GlobalMetadataCache.GetScheme(schemeId);
            if (cachedScheme != null)
                return cachedScheme;
            
            // Загружаем из БД с включением связанных структур
            var scheme = await _context.Schemes
                .Include(s => s.Structures)
                    .ThenInclude(str => str.TypeNavigation)
                .FirstOrDefaultAsync(s => s.Id == schemeId);
                
            if (scheme == null)
                return null;
                
            var result = RedbScheme.FromEntity(scheme);
            
            // Кешируем в глобальном кеше
            GlobalMetadataCache.CacheScheme(result);
            
            return result;
        }
        
        public async Task<IRedbScheme?> GetSchemeByNameAsync(string schemeName)
        {
            // Проверяем глобальный кеш
            var cachedScheme = GlobalMetadataCache.GetScheme(schemeName);
            if (cachedScheme != null)
                return cachedScheme;
            
            // Загружаем из БД с включением связанных структур
            var scheme = await _context.Schemes
                .Include(s => s.Structures)
                    .ThenInclude(str => str.TypeNavigation)
                .FirstOrDefaultAsync(s => s.Name == schemeName);
                
            if (scheme == null)
                return null;
                
            var result = RedbScheme.FromEntity(scheme);
            
            // Кешируем в глобальном кеше
            GlobalMetadataCache.CacheScheme(result);
            
            return result;
        }
        
        public async Task<List<IRedbScheme>> GetSchemesAsync()
        {
            var schemes = await _context.Schemes.ToListAsync();
            return schemes.Select(s => (IRedbScheme)RedbScheme.FromEntity(s)).ToList();
        }
        
        public async Task<List<IRedbStructure>> GetStructuresAsync(IRedbScheme scheme)
        {
            // Теперь структуры инкапсулированы в схеме, просто возвращаем их
            // Если схема из кеша - структуры уже загружены
            // Если схема из БД - структуры загружены через Include в GetSchemeByXXXAsync
            return scheme.Structures.ToList();
        }

        // ===== LEGACY МЕТОДЫ (приватные для внутреннего использования) =====

        /*
        public async Task<long> EnsureSchemeFromTypeAsync<TProps>(string? schemeName = null, string? alias = null) where TProps : class
        {
            var scheme = await EnsureSchemeFromTypeAsync<TProps>(schemeName, alias);
            return scheme.Id;
        }

        public async Task SyncStructuresFromTypeAsync<TProps>(long schemeId, bool strictDeleteExtra = true) where TProps : class
        {

            var scheme = await _context.Schemes.FirstAsync(s => s.Id == schemeId);
            await SyncStructuresFromTypeAsync<TProps>(RedbScheme.FromEntity(scheme), strictDeleteExtra);
        }

        public async Task<long> SyncSchemeAsync<TProps>(string? schemeName = null, string? alias = null, bool strictDeleteExtra = true) where TProps : class
        {
            var scheme = await SyncSchemeAsync<TProps>(schemeName, alias, strictDeleteExtra);
            return scheme.Id;
        }

        public async Task<long> SyncSchemeAsync<TProps>() where TProps : class
        {
            var scheme = await SyncSchemeAsync<TProps>();
            return scheme.Id;
        }
        */
        
        // ===== РЕАЛИЗАЦИЯ ISchemeCacheProvider =====
        
        /// <summary>
        /// Включить/выключить кеширование метаданных на лету (hot toggle)
        /// </summary>
        public void SetCacheEnabled(bool enabled)
        {
            GlobalMetadataCache.SetEnabled(enabled);
        }
        
        /// <summary>
        /// Проверить, включено ли кеширование
        /// </summary>
        public bool IsCacheEnabled => GlobalMetadataCache.IsEnabled;
        
        /// <summary>
        /// Полная очистка всех кешей метаданных
        /// </summary>
        public void InvalidateCache()
        {
            GlobalMetadataCache.Clear();
        }
        
        /// <summary>
        /// Очистить кеш метаданных для конкретного типа C#
        /// </summary>
        public void InvalidateSchemeCache<TProps>() where TProps : class
        {
            GlobalMetadataCache.InvalidateScheme<TProps>();
        }
        
        /// <summary>
        /// Очистить кеш метаданных для схемы по ID
        /// </summary>
        public void InvalidateSchemeCache(long schemeId)
        {
            GlobalMetadataCache.InvalidateScheme(schemeId);
        }
        
        /// <summary>
        /// Очистить кеш метаданных для схемы по имени
        /// </summary>
        public void InvalidateSchemeCache(string schemeName)
        {
            GlobalMetadataCache.InvalidateScheme(schemeName);
        }
        
        /// <summary>
        /// Получить детальную статистику работы кеша
        /// </summary>
        public CacheStatistics GetCacheStatistics()
        {
            return GlobalMetadataCache.GetStatistics();
        }
        
        /// <summary>
        /// Сбросить статистику кеша (обнулить счетчики)
        /// </summary>
        public void ResetCacheStatistics()
        {
            GlobalMetadataCache.ResetStatistics();
        }
        
        /// <summary>
        /// Предварительно загрузить метаданные для типа C#
        /// </summary>
        public async Task WarmupCacheAsync<TProps>() where TProps : class
        {
            await GlobalMetadataCache.WarmupAsync<TProps>(async type =>
            {
                return await GetSchemeByTypeAsync(type);
            });
        }
        
        /// <summary>
        /// Предварительно загрузить метаданные для массива типов C#
        /// </summary>
        public async Task WarmupCacheAsync(Type[] types)
        {
            await GlobalMetadataCache.WarmupAsync(types, async type =>
            {
                return await GetSchemeByTypeAsync(type);
            });
        }
        
        /// <summary>
        /// Предварительно загрузить метаданные для всех известных схем
        /// </summary>
        public async Task WarmupAllSchemesAsync()
        {
            var allSchemes = await _context.Schemes
                .Include(s => s.Structures)
                    .ThenInclude(str => str.TypeNavigation)
                .ToListAsync();
                
            foreach (var scheme in allSchemes)
            {
                var redbScheme = RedbScheme.FromEntity(scheme);
                GlobalMetadataCache.CacheScheme(redbScheme);
            }
        }
        
        /// <summary>
        /// Получить диагностическую информацию о состоянии кеша
        /// </summary>
        public CacheDiagnosticInfo GetCacheDiagnosticInfo()
        {
            // Возвращаем заглушку для совместимости
            var diagnosticText = GlobalMetadataCache.GetDiagnosticInfo();
            return new CacheDiagnosticInfo
            {
                Issues = new List<string> { diagnosticText },
                Recommendations = new List<string>()
            };
        }
        
        /// <summary>
        /// Оценить текущее потребление памяти кешем в байтах
        /// </summary>
        public long EstimateMemoryUsage()
        {
            // Используем статический метод из GlobalMetadataCache
            var schemeCount = GlobalMetadataCache.GetStatistics().SchemeHits + GlobalMetadataCache.GetStatistics().SchemeMisses;
            var typeCount = GlobalMetadataCache.GetStatistics().TypeHits + GlobalMetadataCache.GetStatistics().TypeMisses;
            
            // Приблизительная оценка: схема ~2KB, тип ~100B
            return schemeCount * 2048 + typeCount * 100;
        }
        
        // ===== 🌳 НОВЫЕ МЕТОДЫ ДЛЯ РАБОТЫ С ДЕРЕВОМ СТРУКТУР =====
        
        /// <summary>
        /// 🌳 Получение полного дерева структур схемы
        /// </summary>
        public async Task<List<StructureTreeNode>> GetStructureTreeAsync(long schemeId)
        {
            // Проверяем кеш
            if (_structureTreeCache.TryGetValue(schemeId, out var cachedTree))
            {
                return cachedTree;
            }
            
            // Загружаем схему со структурами
            var scheme = await GetSchemeByIdAsync(schemeId);
            if (scheme == null)
            {
                return new List<StructureTreeNode>();
            }
            
            // Строим дерево из плоского списка
            var tree = StructureTreeBuilder.BuildFromFlat(scheme.Structures.ToList());
            
            // Кешируем результат
            _structureTreeCache.TryAdd(schemeId, tree);
            
            return tree;
        }
        
        /// <summary>
        /// 🌿 Получение поддерева структур для конкретного родителя
        /// </summary>
        public async Task<List<StructureTreeNode>> GetSubtreeAsync(long schemeId, long? parentStructureId)
        {
            var cacheKey = (schemeId, parentStructureId);
            
            // Проверяем кеш поддеревьев
            if (_subtreeCache.TryGetValue(cacheKey, out var cachedSubtree))
            {
                return cachedSubtree;
            }
            
            // Получаем полное дерево
            var fullTree = await GetStructureTreeAsync(schemeId);
            
            List<StructureTreeNode> subtree;
            
            if (parentStructureId == null)
            {
                // Корневые узлы
                subtree = fullTree.Where(n => n.IsRoot).ToList();
            }
            else
            {
                // Ищем поддерево для конкретного родителя
                var allNodes = StructureTreeBuilder.FlattenTree(fullTree);
                var parentNode = allNodes.FirstOrDefault(n => n.Structure.Id == parentStructureId);
                subtree = parentNode?.Children ?? new List<StructureTreeNode>();
            }
            
            // Кешируем поддерево
            _subtreeCache.TryAdd(cacheKey, subtree);
            
            return subtree;
        }
        
        /// <summary>
        /// 📋 Получение только прямых дочерних структур (плоский список)
        /// </summary>
        public async Task<List<IRedbStructure>> GetChildrenStructuresAsync(long schemeId, long parentStructureId)
        {
            var subtree = await GetSubtreeAsync(schemeId, parentStructureId);
            return subtree.Select(n => n.Structure).ToList();
        }
        
        /// <summary>
        /// 🔍 Поиск узла дерева по ID структуры
        /// </summary>
        public async Task<StructureTreeNode?> FindStructureNodeAsync(long schemeId, long structureId)
        {
            var tree = await GetStructureTreeAsync(schemeId);
            var allNodes = StructureTreeBuilder.FlattenTree(tree);
            return allNodes.FirstOrDefault(n => n.Structure.Id == structureId);
        }
        
        /// <summary>
        /// 🔍 Поиск узла дерева по пути (например "Address1.Details.Floor")
        /// </summary>
        public async Task<StructureTreeNode?> FindStructureByPathAsync(long schemeId, string path)
        {
            var tree = await GetStructureTreeAsync(schemeId);
            return StructureTreeBuilder.FindNodeByPath(tree, path);
        }
        
        /// <summary>
        /// 📊 Получение дерева в JSON формате для диагностики
        /// </summary>
        public async Task<JsonElement> GetStructureTreeJsonAsync(long schemeId)
        {
            var sql = "SELECT get_scheme_structure_tree(@schemeId)";
            var result = await _context.Database.SqlQueryRaw<string>(sql, 
                new { schemeId }).FirstOrDefaultAsync();
                
            if (string.IsNullOrEmpty(result))
            {
                return JsonSerializer.SerializeToElement("[]");
            }
            
            return JsonSerializer.SerializeToElement(result);
        }
        
        /// <summary>
        /// 🧪 Валидация дерева структур на соответствие C# типу
        /// </summary>
        public async Task<TreeDiagnosticReport> ValidateStructureTreeAsync<TProps>(long schemeId) where TProps : class
        {
            var tree = await GetStructureTreeAsync(schemeId);
            return StructureTreeBuilder.DiagnoseTree(tree, typeof(TProps));
        }
        
        /// <summary>
        /// 🧹 Очистка кеша дерева структур
        /// </summary>
        public void InvalidateStructureTreeCache(long schemeId)
        {
            _structureTreeCache.TryRemove(schemeId, out _);
            
            // Удаляем все связанные поддеревья
            var keysToRemove = _subtreeCache.Keys.Where(k => k.Item1 == schemeId).ToList();
            foreach (var key in keysToRemove)
            {
                _subtreeCache.TryRemove(key, out _);
            }
        }
        
        /// <summary>
        /// 📊 Статистика кеша дерева структур
        /// </summary>
        public (int TreesCount, int SubtreesCount, long MemoryEstimate) GetStructureTreeCacheStats()
        {
            var treesCount = _structureTreeCache.Count;
            var subtreesCount = _subtreeCache.Count;
            
            // Приблизительная оценка: каждое дерево ~1KB, каждое поддерево ~200B
            var memoryEstimate = treesCount * 1024 + subtreesCount * 200;
            
            return (treesCount, subtreesCount, memoryEstimate);
        }
        
        /// <summary>
        /// 🔧 Проверка существования дочерних структур
        /// </summary>
        public async Task<bool> HasChildrenStructuresAsync(long schemeId, long structureId)
        {
            var children = await GetSubtreeAsync(schemeId, structureId);
            return children.Any();
        }
    }
}
