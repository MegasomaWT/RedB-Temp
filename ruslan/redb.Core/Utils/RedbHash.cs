using redb.Core.Models.Entities;
using redb.Core.Models.Contracts;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace redb.Core.Utils
{
    // Утилита подсчёта MD5-хеша по значениям свойств подтипа (properties) дженерика
    public static class RedbHash
    {
        /// <summary>
        /// Вычислить хеш для любого IRedbObject - только от бизнес-данных (properties)
        /// Возвращает null если нет properties для хеширования
        /// </summary>
        public static Guid? ComputeFor(IRedbObject obj)
        {
            // Ищем свойство properties через рефлексию
            var propertiesProperty = obj.GetType().GetProperty("properties");
            if (propertiesProperty != null)
            {
                var propertiesValue = propertiesProperty.GetValue(obj);
                if (propertiesValue != null)
                {
                    return ComputeForObject(propertiesValue);
                }
            }
            
            // Если нет properties - возвращаем null
            return null;
        }

        public static Guid? ComputeFor<TProps>(RedbObject<TProps> obj) where TProps : class, new()
        {
            return ComputeForProps(obj.properties);
        }

        public static Guid? ComputeForProps<TProps>(TProps props) where TProps : class, new()
        {
            return ComputeForObject(props); 
        }

        /// <summary>
        /// Вычислить хеш для произвольного объекта через рефлексию
        /// Возвращает null если у объекта нет свойств
        /// </summary>
        private static Guid? ComputeForObject(object obj)
        {
            var properties = obj.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
            // Если нет свойств - нет данных для хеширования
            if (!properties.Any())
                return null;
            
            var ordered = properties
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .Select(p => p.GetValue(obj)?.ToString() ?? "");

            var payload = string.Join("|", ordered);
            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(payload);
            var hash = md5.ComputeHash(bytes);
            return new Guid(hash);
        }
    }
}

