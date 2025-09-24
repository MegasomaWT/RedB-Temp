using redb.Core.DBModels;
using redb.Core.Utils;
using System.Text.Json.Serialization;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace redb.Core.Postgres.Providers
{
    /// <summary>
    /// Расширения для PostgresObjectStorageProvider под новую парадигму сохранения
    /// </summary>
    public static class PostgresObjectStorageProviderExtensions
    {
        // ===== ✅ НОВЫЕ МЕТОДЫ ДЛЯ ОБРАБОТКИ ПОЛЕЙ ПОД НОВУЮ ПАРАДИГМУ =====

        /// <summary>
        /// Определить, нужно ли создавать запись в _values на основе значения и _store_null
        /// </summary>
        internal static bool ShouldCreateValueRecord(object? rawValue, bool storeNull)
        {
            // Если значение не NULL - всегда создаем запись
            if (rawValue != null) return true;
            
            // Если значение NULL - создаем запись только если _store_null = true
            return storeNull;
        }

        /// <summary>
        /// Проверить, является ли тип Class типом (бизнес-класс, не примитив)
        /// </summary>
        internal static bool IsClassType(string typeSemantic)
        {
            // ✅ ИСПРАВЛЕНО: Class тип имеет Type1 = "Object" (смотрим на TypeSemantic из _types._type)
            // Бизнес-классы мапятся в тип "Class" с _type="Object"
            return typeSemantic == "Object";
        }

        /// <summary>
        /// Проверить, является ли тип RedbObject<> ссылкой
        /// </summary>
        internal static bool IsRedbObjectReference(string typeSemantic)
        {
            return typeSemantic == "_RObject";
        }
    }
}
