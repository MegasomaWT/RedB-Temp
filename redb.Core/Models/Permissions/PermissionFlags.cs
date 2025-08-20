using System;

namespace redb.Core.Models.Permissions
{
    /// <summary>
    /// Флаги разрешений для объектов REDB
    /// Соответствуют полям _select, _insert, _update, _delete в таблице _permissions
    /// </summary>
    [Flags]
    public enum PermissionFlags
    {
        /// <summary>
        /// Нет разрешений
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Разрешение на чтение (SELECT)
        /// </summary>
        Select = 1,
        
        /// <summary>
        /// Разрешение на создание (INSERT)
        /// </summary>
        Insert = 2,
        
        /// <summary>
        /// Разрешение на изменение (UPDATE)
        /// </summary>
        Update = 4,
        
        /// <summary>
        /// Разрешение на удаление (DELETE)
        /// </summary>
        Delete = 8,
        
        /// <summary>
        /// Все разрешения (RIUD)
        /// </summary>
        All = Select | Insert | Update | Delete,
        
        /// <summary>
        /// Только чтение и изменение (RI_U)
        /// </summary>
        ReadWrite = Select | Insert | Update,
        
        /// <summary>
        /// Только чтение (R)
        /// </summary>
        ReadOnly = Select
    }
    
    /// <summary>
    /// Расширения для работы с PermissionFlags
    /// </summary>
    public static class PermissionFlagsExtensions
    {
        /// <summary>
        /// Проверить есть ли разрешение на чтение
        /// </summary>
        public static bool CanSelect(this PermissionFlags flags) => flags.HasFlag(PermissionFlags.Select);
        
        /// <summary>
        /// Проверить есть ли разрешение на создание
        /// </summary>
        public static bool CanInsert(this PermissionFlags flags) => flags.HasFlag(PermissionFlags.Insert);
        
        /// <summary>
        /// Проверить есть ли разрешение на изменение
        /// </summary>
        public static bool CanUpdate(this PermissionFlags flags) => flags.HasFlag(PermissionFlags.Update);
        
        /// <summary>
        /// Проверить есть ли разрешение на удаление
        /// </summary>
        public static bool CanDelete(this PermissionFlags flags) => flags.HasFlag(PermissionFlags.Delete);
        
        /// <summary>
        /// Преобразовать в строку для отображения (например: "RIUD")
        /// </summary>
        public static string ToDisplayString(this PermissionFlags flags)
        {
            var result = "";
            if (flags.CanSelect()) result += "R";
            if (flags.CanInsert()) result += "I";
            if (flags.CanUpdate()) result += "U";
            if (flags.CanDelete()) result += "D";
            return string.IsNullOrEmpty(result) ? "----" : result;
        }
        
        /// <summary>
        /// Создать PermissionFlags из булевых значений (как в БД)
        /// </summary>
        public static PermissionFlags FromBooleans(bool select, bool insert, bool update, bool delete)
        {
            var flags = PermissionFlags.None;
            if (select) flags |= PermissionFlags.Select;
            if (insert) flags |= PermissionFlags.Insert;
            if (update) flags |= PermissionFlags.Update;
            if (delete) flags |= PermissionFlags.Delete;
            return flags;
        }
    }
}
