using redb.Core.DBModels;
using redb.Core.Models.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace redb.Core.Models.Entities
{
    /// <summary>
    /// Реализация интерфейса IRedbPermission на основе _RPermission
    /// </summary>
    public class RedbPermission : IRedbPermission
    {
        private readonly _RPermission _permission;

        public RedbPermission(_RPermission permission)
        {
            _permission = permission ?? throw new ArgumentNullException(nameof(permission));
        }

        public long Id => _permission.Id;
        public long? IdRole => _permission.IdRole;
        public long? IdUser => _permission.IdUser;
        public long IdRef => _permission.IdRef;
        public bool? Select => _permission.Select;
        public bool? Insert => _permission.Insert;
        public bool? Update => _permission.Update;
        public bool? Delete => _permission.Delete;

        /// <summary>
        /// Проверить, есть ли указанное право
        /// </summary>
        public bool HasPermission(string action)
        {
            return action.ToLower() switch
            {
                "select" or "read" => Select == true,
                "insert" or "create" => Insert == true,
                "update" or "edit" => Update == true,
                "delete" or "remove" => Delete == true,
                _ => false
            };
        }

        /// <summary>
        /// Получить список активных прав
        /// </summary>
        public IEnumerable<string> GetActivePermissions()
        {
            var permissions = new List<string>();
            if (Select == true) permissions.Add("Select");
            if (Insert == true) permissions.Add("Insert");
            if (Update == true) permissions.Add("Update");
            if (Delete == true) permissions.Add("Delete");
            return permissions;
        }

        /// <summary>
        /// Создать RedbPermission из _RPermission (статический метод)
        /// </summary>
        public static RedbPermission FromEntity(_RPermission permission) => new RedbPermission(permission);

        /// <summary>
        /// Создать IRedbPermission из _RPermission
        /// </summary>
        public static implicit operator RedbPermission(_RPermission permission) => new RedbPermission(permission);

        /// <summary>
        /// Получить _RPermission из IRedbPermission
        /// </summary>
        public static implicit operator _RPermission(RedbPermission redbPermission) => redbPermission._permission;

        public override string ToString()
        {
            var target = IdRole.HasValue ? $"Role {IdRole}" : $"User {IdUser}";
            var permissions = string.Join(", ", GetActivePermissions());
            return $"Permission {Id}: {target} -> Ref {IdRef} [{permissions}]";
        }
    }
}
