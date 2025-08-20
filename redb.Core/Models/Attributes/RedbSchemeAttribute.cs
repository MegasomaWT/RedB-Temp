using System;

namespace redb.Core.Models.Attributes
{
    /// <summary>
    /// Атрибут для настройки схемы REDB для класса свойств
    /// Имя схемы всегда равно имени класса
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RedbSchemeAttribute : Attribute
    {
        /// <summary>
        /// Алиас схемы (человекочитаемое название)
        /// </summary>
        public string? Alias { get; set; }

        /// <summary>
        /// Конструктор без параметров
        /// </summary>
        public RedbSchemeAttribute()
        {
        }

        /// <summary>
        /// Конструктор с алиасом
        /// </summary>
        /// <param name="alias">Алиас схемы</param>
        public RedbSchemeAttribute(string alias)
        {
            Alias = alias ?? throw new ArgumentNullException(nameof(alias));
        }

        /// <summary>
        /// Получить имя схемы для типа (всегда имя класса)
        /// </summary>
        /// <param name="type">Тип класса</param>
        /// <returns>Имя класса как имя схемы</returns>
        public string GetSchemeName(Type type)
        {
            return type.Name;
        }
    }
}
