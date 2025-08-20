using System;
using System.Threading.Tasks;
using redb.Core.Models.Contracts;
using redb.Core.Models.Security;
using redb.Core.Providers;
using redb.Core.Caching;
using redb.Core.Models.Entities;

namespace redb.Core.Models
{
    /// <summary>
    /// Фабрика для создания типизированных объектов RedbObject<TProps>
    /// Обеспечивает правильную инициализацию метаданных и интеграцию с кешированием
    /// 
    /// Пример использования:
    /// <code>
    /// // 1. Инициализация фабрики (один раз при старте)
    /// RedbObjectFactory.Initialize(schemeSyncProvider, ownerId: 1, whoChangeId: 1);
    /// 
    /// // 2. Быстрое создание без провайдера
    /// var employee = RedbObjectFactory.CreateFast(new Employee { Name = "Иван" });
    /// 
    /// // 3. Создание с автоматическим определением схемы
    /// var employee = await RedbObjectFactory.CreateAsync(new Employee { Name = "Иван" });
    /// 
    /// // 4. Создание дочернего объекта
    /// var child = await RedbObjectFactory.CreateChildAsync(parent, new Employee { Name = "Подчиненный" });
    /// 
    /// // 5. Массовое создание с оптимизацией кеша
    /// var employees = await RedbObjectFactory.CreateBatchAsync(
    ///     new Employee { Name = "Иван" },
    ///     new Employee { Name = "Петр" },
    ///     new Employee { Name = "Сидор" }
    /// );
    /// </code>
    /// </summary>
    public static class RedbObjectFactory
    {
        private static ISchemeSyncProvider? _provider;
        
        // Убираем статические ID - будем использовать контекст пользователя
        
        /// <summary>
        /// Инициализировать фабрику с провайдером схем
        /// Необходимо для автоматического определения схем
        /// Также устанавливает провайдер в RedbObjectBase для глобального доступа
        /// </summary>
        public static void Initialize(ISchemeSyncProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            
            // Устанавливаем провайдер в базовый класс для глобального доступа
            RedbObject.SetSchemeSyncProvider(provider);
        }
        
        /// <summary>
        /// Проверить, инициализирована ли фабрика
        /// </summary>
        public static bool IsInitialized => _provider != null;
        
        /// <summary>
        /// Установить провайдер схем
        /// </summary>
        public static void SetProvider(ISchemeSyncProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            RedbObject.SetSchemeSyncProvider(provider);
        }
        
        // ===== МЕТОДЫ СОЗДАНИЯ ОБЪЕКТОВ =====
        
        /// <summary>
        /// Создать новый объект без инициализации свойств
        /// Автоматически определяет схему по типу TProps
        /// </summary>
        public static async Task<IRedbObject<TProps>> CreateAsync<TProps>() where TProps : class, new()
        {
            return await CreateAsync(new TProps());
        }
        
        /// <summary>
        /// Создать новый объект с инициализированными свойствами  
        /// Автоматически определяет схему по типу TProps
        /// </summary>
        public static async Task<IRedbObject<TProps>> CreateAsync<TProps>(TProps properties) where TProps : class, new()
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));
                
            var obj = new RedbObject<TProps>(properties);
            
            // Автоматическая инициализация метаданных
            await InitializeMetadataAsync(obj);
            
            return obj;
        }
        
        /// <summary>
        /// Создать новый объект как дочерний для существующего родителя
        /// </summary>
        public static async Task<IRedbObject<TProps>> CreateChildAsync<TProps>(
            IRedbObject parent, 
            TProps properties) where TProps : class, new()
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));
                
            var obj = new RedbObject<TProps>(properties)
            {
                parent_id = parent.Id
            };
            
            await InitializeMetadataAsync(obj);
            
            return obj;
        }
        
        /// <summary>
        /// Создать копию существующего объекта с новыми свойствами
        /// Сохраняет все метаданные кроме ID (для создания нового объекта)
        /// </summary>
        public static async Task<IRedbObject<TProps>> CreateCopyAsync<TProps>(
            IRedbObject<TProps> source, 
            TProps newProperties) where TProps : class, new()
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (newProperties == null)
                throw new ArgumentNullException(nameof(newProperties));
                
            var obj = source.CloneWithProperties(newProperties);
            
            // ID будет установлен при сохранении
            // Обновляем только временные метки
            var redbObj = (RedbObject<TProps>)obj;
            redbObj.date_create = DateTime.Now;
            redbObj.date_modify = DateTime.Now;
            
            return obj;
        }
        
        // ===== БЫСТРЫЕ МЕТОДЫ БЕЗ ПРОВАЙДЕРА =====
        
        /// <summary>
        /// Создать объект без автоматической инициализации схемы (быстро)
        /// Схему нужно будет установить вручную или через провайдер
        /// </summary>
        public static IRedbObject<TProps> CreateFast<TProps>() where TProps : class, new()
        {
            return CreateFast(new TProps());
        }
        
        /// <summary>
        /// Создать объект с свойствами без автоматической инициализации схемы (быстро)
        /// </summary>
        public static IRedbObject<TProps> CreateFast<TProps>(TProps properties) where TProps : class, new()
        {
            var obj = new RedbObject<TProps>(properties);
            
            // Базовая инициализация без обращения к провайдеру
            var now = DateTime.Now;
            var securityContext = AmbientSecurityContext.GetOrCreateDefault();
            var effectiveUser = securityContext.GetEffectiveUser();
            
            obj.date_create = now;
            obj.date_modify = now;
            obj.owner_id = effectiveUser.Id;
            obj.who_change_id = effectiveUser.Id;
            
            return obj;
        }
        
        /// <summary>
        /// Создать объект с полной ручной инициализацией всех полей
        /// </summary>
        public static IRedbObject<TProps> CreateWithMetadata<TProps>(
            TProps properties,
            long schemeId,
            string? name = null,
            long? parentId = null,
            long? ownerId = null,
            long? whoChangeId = null) where TProps : class, new()
        {
            var obj = new RedbObject<TProps>(properties);
            
            var now = DateTime.Now;
            var securityContext = AmbientSecurityContext.GetOrCreateDefault();
            var effectiveUser = securityContext.GetEffectiveUser();
            
            obj.scheme_id = schemeId;
            obj.name = name ?? $"Object_{typeof(TProps).Name}";
            obj.parent_id = parentId;
            obj.owner_id = ownerId ?? effectiveUser.Id;
            obj.who_change_id = whoChangeId ?? effectiveUser.Id;
            obj.date_create = now;
            obj.date_modify = now;
            
            return obj;
        }
        
        // ===== ПРИВАТНЫЕ МЕТОДЫ =====
        
        /// <summary>
        /// Инициализировать метаданные объекта автоматически
        /// </summary>
        private static async Task InitializeMetadataAsync<TProps>(RedbObject<TProps> obj) where TProps : class, new()
        {
            var now = DateTime.Now;
            var securityContext = AmbientSecurityContext.GetOrCreateDefault();
            var effectiveUser = securityContext.GetEffectiveUser();
            
            // Временные метки
            obj.date_create = now;
            obj.date_modify = now;
            
            // Используем текущего пользователя из контекста безопасности
            obj.owner_id = effectiveUser.Id;
            obj.who_change_id = effectiveUser.Id;
            
            // Имя объекта по умолчанию
            if (string.IsNullOrEmpty(obj.name))
            {
                obj.name = $"New{typeof(TProps).Name}";
            }
            
            // Автоматическое определение схемы (если провайдер доступен)
            if (_provider != null)
            {
                try
                {
                    var scheme = await _provider.GetSchemeByTypeAsync<TProps>();
                    if (scheme != null)
                    {
                        obj.scheme_id = scheme.Id;
                    }
                    else
                    {
                        // Попробуем создать схему автоматически
                        var newScheme = await _provider.EnsureSchemeFromTypeAsync<TProps>();
                        obj.scheme_id = newScheme.Id;
                    }
                }
                catch
                {
                    // Если не удалось определить схему, оставляем 0
                    // Схема будет определена при сохранении
                    obj.scheme_id = 0;
                }
            }
            else
            {
                // Без провайдера схема будет определена позже
                obj.scheme_id = 0;
            }
        }
        
        // ===== МЕТОДЫ ИНФОРМАЦИИ =====
        
        /// <summary>
        /// Получить текущие настройки фабрики
        /// </summary>
        public static (bool IsInitialized, long CurrentUserId, string CurrentUserName) GetSettings()
        {
            var securityContext = AmbientSecurityContext.GetOrCreateDefault();
            var effectiveUser = securityContext.GetEffectiveUser();
            
            return (IsInitialized, effectiveUser.Id, effectiveUser.Name ?? "Неизвестный пользователь");
        }
        
        // ===== ИНТЕГРАЦИЯ С КЕШИРОВАНИЕМ =====
        
        /// <summary>
        /// Создать объект с предзагрузкой метаданных в кеш
        /// Полезно для массового создания объектов одного типа
        /// </summary>
        public static async Task<IRedbObject<TProps>> CreateWithWarmupAsync<TProps>(TProps properties) where TProps : class, new()
        {
            // Предзагружаем метаданные в кеш
            if (_provider is ISchemeCacheProvider cacheProvider)
            {
                await cacheProvider.WarmupCacheAsync<TProps>();
            }
            
            return await CreateAsync(properties);
        }
        
        /// <summary>
        /// Массовое создание объектов с предзагрузкой кеша
        /// Оптимизировано для создания множества объектов одного типа
        /// </summary>
        public static async Task<IRedbObject<TProps>[]> CreateBatchAsync<TProps>(params TProps[] propertiesArray) where TProps : class, new()
        {
            if (propertiesArray == null || propertiesArray.Length == 0)
                return Array.Empty<IRedbObject<TProps>>();
            
            // Предзагружаем кеш один раз для всех объектов
            if (_provider is ISchemeCacheProvider cacheProvider)
            {
                await cacheProvider.WarmupCacheAsync<TProps>();
            }
            
            var results = new IRedbObject<TProps>[propertiesArray.Length];
            
            for (int i = 0; i < propertiesArray.Length; i++)
            {
                results[i] = await CreateAsync(propertiesArray[i]);
            }
            
            return results;
        }
    }
}
