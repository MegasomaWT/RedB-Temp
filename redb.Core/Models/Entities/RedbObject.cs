using redb.Core.Models.Entities;
using redb.Core.Models.Contracts;
using redb.Core.Utils;
using redb.Core.Caching;
using redb.Core.Providers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace redb.Core.Models.Entities
{
    /// <summary>
    /// Дженерик-обёртка для JSON из get_object_json с типизированным интерфейсом
    /// Имена полей совпадают с JSON/БД (snake_case), чтобы обходиться без атрибутов и настроек
    /// Наследуется от базового RedbObject для унификации API
    /// Реализует типизированный интерфейс IRedbObject<TProps> для типобезопасности
    /// </summary>
    public class RedbObject<TProps> : RedbObject, IRedbObject<TProps> where TProps : class, new()
    {
        public RedbObject() { }

        public RedbObject(TProps props)
        {
            properties = props;
        }

        public static explicit operator TProps(RedbObject<TProps> obj) => obj.properties;

        // Свойства объекта (секция properties)
        public TProps properties { get; set; } = new TProps();

        // Удобный доступ
        public TProps Pr => properties;

        // Пересчитать MD5-хеш по значениям properties и записать в поле hash
        public override void RecomputeHash()
        {
            hash = RedbHash.ComputeFor(this);
        }

        // Получить MD5-хеш по значениям properties, не меняя поле hash
        public override Guid ComputeHash() => RedbHash.ComputeFor(this) ?? Guid.Empty;
        
        // ===== ТИПИЗИРОВАННЫЕ МЕТОДЫ КЕША И МЕТАДАННЫХ =====
        
        /// <summary>
        /// Получить схему для типа TProps (с использованием кеша и провайдера)
        /// Пытается получить из кеша, затем из провайдера, и только потом выбрасывает исключение
        /// </summary>
        public async Task<IRedbScheme> GetSchemeForTypeAsync()
        {
            var typeName = typeof(TProps).Name;
            
            // 1. Сначала проверяем глобальный кеш
            var cachedScheme = GlobalMetadataCache.GetScheme(typeName);
            if (cachedScheme != null)
                return cachedScheme;
            
            // 2. Если не в кеше и есть провайдер - пробуем загрузить
            var provider = GetSchemeSyncProvider();
            if (provider != null)
            {
                var scheme = await provider.GetSchemeByTypeAsync<TProps>();
                if (scheme != null)
                    return scheme;
                    
                // Попробуем создать схему автоматически
                try
                {
                    var newScheme = await provider.EnsureSchemeFromTypeAsync<TProps>();
                    return newScheme;
                }
                catch
                {
                    // Если не удалось создать - переходим к исключениям
                }
            }
            
            // 3. Если ничего не получилось - исключения с подсказками
            if (provider == null)
            {
                throw new InvalidOperationException(
                    "Провайдер схем не инициализирован. Вызовите RedbObjectFactory.Initialize() или " +
                    "RedbObject.SetSchemeSyncProvider() для установки провайдера.");
            }
            
            throw new InvalidOperationException(
                $"Схема для типа '{typeName}' не найдена и не может быть создана автоматически. " +
                $"Используйте provider.EnsureSchemeFromTypeAsync<{typeName}>() для создания схемы вручную.");
        }
        
        /// <summary>
        /// Получить структуры схемы для типа TProps
        /// </summary>
        public async Task<IReadOnlyCollection<IRedbStructure>> GetStructuresForTypeAsync()
        {
            var scheme = await GetSchemeForTypeAsync();
            return scheme.Structures;
        }
        
        /// <summary>
        /// Получить структуру по имени поля для типа TProps
        /// </summary>
        public new async Task<IRedbStructure?> GetStructureByNameAsync(string fieldName)
        {
            var scheme = await GetSchemeForTypeAsync();
            return scheme.GetStructureByName(fieldName);
        }
        
        /// <summary>
        /// Пересчитать хеш на основе текущих свойств типа TProps
        /// </summary>
        public void RecomputeHashForType()
        {
            RecomputeHash(); // Используем существующую реализацию
        }
        
        /// <summary>
        /// Получить новый хеш на основе текущих свойств без изменения объекта
        /// </summary>
        public Guid ComputeHashForType()
        {
            return ComputeHash(); // Используем существующую реализацию
        }
        
        /// <summary>
        /// Проверить, соответствует ли текущий хеш свойствам типа TProps
        /// </summary>
        public bool IsHashValidForType()
        {
            if (!hash.HasValue)
                return false;
                
            var computedHash = ComputeHashForType();
            return hash.Value == computedHash;
        }
        
        /// <summary>
        /// Создать копию объекта с теми же метаданными но новыми свойствами
        /// </summary>
        public IRedbObject<TProps> CloneWithProperties(TProps newProperties)
        {
            return new RedbObject<TProps>(newProperties)
            {
                // Копируем все метаданные кроме ID (чтобы создать новый объект)
                parent_id = this.parent_id,
                scheme_id = this.scheme_id,
                owner_id = this.owner_id,
                who_change_id = this.who_change_id,
                date_create = DateTime.Now, // Новое время создания
                date_modify = DateTime.Now, // Новое время изменения
                date_begin = this.date_begin,
                date_complete = this.date_complete,
                key = this.key,
                code_int = this.code_int,
                code_string = this.code_string,
                code_guid = this.code_guid,
                name = this.name,
                note = this.note,
                @bool = this.@bool
                // hash будет пересчитан при необходимости
            };
        }
        
        /// <summary>
        /// Очистить кеш метаданных для типа TProps
        /// </summary>
        public void InvalidateCacheForType()
        {
            var provider = GetSchemeSyncProvider();
            if (provider is ISchemeCacheProvider cacheProvider)
            {
                cacheProvider.InvalidateSchemeCache<TProps>();
            }
        }
        
        /// <summary>
        /// Предзагрузить кеш метаданных для типа TProps
        /// </summary>
        public async Task WarmupCacheForTypeAsync()
        {
            var provider = GetSchemeSyncProvider();
            if (provider is ISchemeCacheProvider cacheProvider)
            {
                await cacheProvider.WarmupCacheAsync<TProps>();
            }
        }
        
        /// <summary>
        /// Переопределение с рекурсивной обработкой properties
        /// Сбрасывает ID и ParentId у текущего объекта и всех вложенных IRedbObject
        /// </summary>
        /// <param name="recursive">Если true, рекурсивно обрабатывает все вложенные IRedbObject в properties</param>
        public override void ResetIds(bool recursive = false)
        {
            // Сбрасываем базовые поля
            id = 0;
            parent_id = null;
            
            // Рекурсивная обработка properties
            if (recursive && properties != null)
            {
                ProcessNestedObjectsForReset(properties);
            }
        }
        
        /// <summary>
        /// Рекурсивно обрабатывает properties для сброса ID вложенных объектов
        /// </summary>
        private void ProcessNestedObjectsForReset(object obj)
        {
            if (obj == null) return;
            
            var objType = obj.GetType();
            var objProperties = objType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var property in objProperties)
            {
                try
                {
                    var value = property.GetValue(obj);
                    if (value == null) continue;
                    
                    // Обработка одиночных IRedbObject
                    if (value is IRedbObject redbObj)
                    {
                        redbObj.ResetIds(true); // Рекурсивно сбрасываем
                        continue;
                    }
                    
                    // Обработка коллекций IRedbObject
                    if (value is System.Collections.IEnumerable enumerable && value is not string)
                    {
                        foreach (var item in enumerable)
                        {
                            if (item is IRedbObject redbItem)
                            {
                                redbItem.ResetIds(true); // Рекурсивно сбрасываем
                            }
                        }
                    }
                }
                catch
                {
                    // Игнорируем ошибки доступа к свойствам
                    continue;
                }
            }
        }
    }
}
