using System.Reflection;
using System.Text.Json.Serialization;
using redb.Core.Attributes;

namespace redb.Core.Postgres.Extensions
{
    /// <summary>
    /// Расширения для работы с PropertyInfo
    /// </summary>
    internal static class PropertyInfoExtensions
    {
        /// <summary>
        /// Проверяет, должно ли свойство игнорироваться REDB
        /// </summary>
        /// <param name="property">Свойство для проверки</param>
        /// <returns>true если свойство должно быть проигнорировано</returns>
        public static bool ShouldIgnoreForRedb(this PropertyInfo property)
        {
            return property.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Length > 0 ||
                   property.GetCustomAttributes(typeof(RedbIgnoreAttribute), false).Length > 0;
        }
    }
}
