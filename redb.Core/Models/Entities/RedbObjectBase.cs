using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using redb.Core.Models.Contracts;
using redb.Core.Providers;
using redb.Core.Caching;
using redb.Core.Utils;

namespace redb.Core.Models.Entities
{
    /// <summary>
    /// Базовый класс для всех объектов Redb с доступом к метаданным
    /// Содержит все поля из таблицы _objects и методы работы с кешированными метаданными
    /// </summary>
    public abstract class RedbObject : IRedbObject
    {
        // ===== ГЛОБАЛЬНЫЙ ПРОВАЙДЕР ДЛЯ ДОСТУПА К МЕТАДАННЫМ =====
        private static ISchemeSyncProvider? _globalProvider;

        /// <summary>
        /// Установить глобальный провайдер схем для всех объектов
        /// Позволяет объектам получать доступ к своим метаданным
        /// </summary>
        public static void SetSchemeSyncProvider(ISchemeSyncProvider provider)
        {
            _globalProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Получить глобальный провайдер схем
        /// </summary>
        protected static ISchemeSyncProvider? GetSchemeSyncProvider() => _globalProvider;

        /// <summary>
        /// Проверить, доступен ли провайдер схем
        /// </summary>
        public static bool IsProviderAvailable => _globalProvider != null;

        // ===== ПОЛЯ КОРНЯ (_objects) =====
        public long id { get; set; }
        public long? parent_id { get; set; }
        public long scheme_id { get; set; }
        public long owner_id { get; set; }
        public long who_change_id { get; set; }
        public DateTime date_create { get; set; }
        public DateTime date_modify { get; set; }
        public DateTime? date_begin { get; set; }
        public DateTime? date_complete { get; set; }
        public long? key { get; set; }
        public long? code_int { get; set; }
        public string? code_string { get; set; }
        public Guid? code_guid { get; set; }
        public string? name { get; set; }
        public string? note { get; set; }
        public bool? @bool { get; set; }
        public Guid? hash { get; set; }

        // Пересчитать MD5-хеш по значениям properties и записать в поле hash
        public abstract void RecomputeHash();

        // Получить MD5-хеш по значениям properties, не меняя поле hash
        public abstract Guid ComputeHash();

        // ===== РЕАЛИЗАЦИЯ ОБОГАЩЕННОГО IRedbObject =====
        // (исключаем из JSON сериализации)

        public void ResetId(long id)
        {
            this.id = id;
        }

        // Основные идентификаторы
        [JsonIgnore]
        public long Id { get => id; set => id = value; }
        [JsonIgnore]
        public long SchemeId { get => scheme_id; set => scheme_id = value; }
        [JsonIgnore]
        public string Name { get => name ?? $"Object_{id}"; set => name = value; }

        // Древовидная структура
        [JsonIgnore]
        public long? ParentId { get => parent_id; set => parent_id = value; }
        [JsonIgnore]
        public bool HasParent => parent_id.HasValue;
        [JsonIgnore]
        public bool IsRoot => !parent_id.HasValue;

        // Временные метки
        [JsonIgnore]
        public DateTime DateCreate { get => date_create; set => date_create = value; }
        [JsonIgnore]
        public DateTime DateModify { get => date_modify; set => date_modify = value; }
        [JsonIgnore]
        public DateTime? DateBegin { get => date_begin; set => date_begin = value; }
        [JsonIgnore]
        public DateTime? DateComplete { get => date_complete; set => date_complete = value; }

        // Владение и аудит
        [JsonIgnore]
        public long OwnerId { get => owner_id; set => owner_id = value; }
        [JsonIgnore]
        public long WhoChangeId { get => who_change_id; set => who_change_id = value; }

        // Дополнительные идентификаторы
        [JsonIgnore]
        public long? Key { get => key; set => key = value; }
        [JsonIgnore]
        public long? CodeInt { get => code_int; set => code_int = value; }
        [JsonIgnore]
        public string? CodeString { get => code_string; set => code_string = value; }
        [JsonIgnore]
        public Guid? CodeGuid { get => code_guid; set => code_guid = value; }

        // Состояние объекта
        [JsonIgnore]
        public bool? Bool { get => @bool; set => @bool = value; }
        [JsonIgnore]
        public string? Note { get => note; set => note = value; }
        [JsonIgnore]
        public Guid? Hash { get => hash; set => hash = value; }

        // ===== МЕТОДЫ ДОСТУПА К МЕТАДАННЫМ =====

        /// <summary>
        /// Получить схему объекта по scheme_id (с использованием кеша)
        /// </summary>
        public async Task<IRedbScheme?> GetSchemeAsync()
        {
            if (_globalProvider == null)
                return null;

            return await _globalProvider.GetSchemeByIdAsync(scheme_id);
        }

        /// <summary>
        /// Получить структуры схемы объекта (с использованием кеша)
        /// </summary>
        public async Task<IReadOnlyCollection<IRedbStructure>?> GetStructuresAsync()
        {
            var scheme = await GetSchemeAsync();
            return scheme?.Structures;
        }

        /// <summary>
        /// Получить структуру по имени поля (с использованием кеша)
        /// </summary>
        public async Task<IRedbStructure?> GetStructureByNameAsync(string fieldName)
        {
            var scheme = await GetSchemeAsync();
            return scheme?.GetStructureByName(fieldName);
        }

        /// <summary>
        /// Инвалидировать кеш схемы данного объекта
        /// </summary>
        public void InvalidateSchemeCache()
        {
            if (_globalProvider is ISchemeCacheProvider cacheProvider)
            {
                cacheProvider.InvalidateSchemeCache(scheme_id);
            }
        }

        /// <summary>
        /// Сбросить ID объекта и опционально ParentId (реализация IRedbObject.ResetId)
        /// </summary>
        /// <param name="withParent">Если true, также сбрасывает ParentId в null (по умолчанию true)</param>
        public void ResetId(bool withParent = true)
        {
            id = 0;
            if (withParent)
            {
                parent_id = null;
            }
        }
        
        /// <summary>
        /// Сбросить ID и ParentId объекта (базовая реализация IRedbObject.ResetIds)
        /// Рекурсивная обработка переопределяется в RedbObject&lt;TProps&gt;
        /// </summary>
        /// <param name="recursive">Если true, должен рекурсивно сбрасывать ID во всех вложенных IRedbObject</param>
        public virtual void ResetIds(bool recursive = false)
        {
            id = 0;
            parent_id = null;
            
            // Рекурсивная логика переопределяется в наследниках с доступом к properties
        }
    }
}
