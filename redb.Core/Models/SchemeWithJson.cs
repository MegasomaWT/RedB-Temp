using System;

namespace redb.Core.Models
{
    /// <summary>
    /// Модель для результата оптимизированного SQL запроса
    /// Содержит scheme_id и JSON данные объекта за один запрос
    /// </summary>
    public class SchemeWithJson
    {
        /// <summary>
        /// ID схемы объекта
        /// </summary>
        public long SchemeId { get; set; }

        /// <summary>
        /// JSON данные объекта (результат get_object_json)
        /// </summary>
        public string JsonData { get; set; } = string.Empty;
    }

    /// <summary>
    /// Модель для результата SQL запроса при загрузке детей объекта
    /// Содержит object_id, scheme_id и JSON данные за один запрос
    /// </summary>
    public class ChildObjectInfo
    {
        /// <summary>
        /// ID объекта
        /// </summary>
        public long ObjectId { get; set; }

        /// <summary>
        /// ID схемы объекта
        /// </summary>
        public long SchemeId { get; set; }

        /// <summary>
        /// JSON данные объекта (результат get_object_json)
        /// </summary>
        public string JsonData { get; set; } = string.Empty;
    }
}
