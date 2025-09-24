using redb.Core.DBModels;
using redb.Core.Models.Contracts;
using System;

namespace redb.Core.Models.Entities
{
    /// <summary>
    /// Реализация интерфейса IRedbUserRole на основе _RUsersRole
    /// </summary>
    public class RedbUserRole : IRedbUserRole
    {
        private readonly _RUsersRole _userRole;

        public RedbUserRole(_RUsersRole userRole)
        {
            _userRole = userRole ?? throw new ArgumentNullException(nameof(userRole));
        }

        public long Id => _userRole.Id;
        public long IdRole => _userRole.IdRole;
        public long IdUser => _userRole.IdUser;

        /// <summary>
        /// Создать RedbUserRole из _RUsersRole (статический метод)
        /// </summary>
        public static RedbUserRole FromEntity(_RUsersRole userRole) => new RedbUserRole(userRole);

        /// <summary>
        /// Создать IRedbUserRole из _RUsersRole
        /// </summary>
        public static implicit operator RedbUserRole(_RUsersRole userRole) => new RedbUserRole(userRole);

        /// <summary>
        /// Получить _RUsersRole из IRedbUserRole
        /// </summary>
        public static implicit operator _RUsersRole(RedbUserRole redbUserRole) => redbUserRole._userRole;

        public override string ToString()
        {
            return $"UserRole {Id}: User {IdUser} -> Role {IdRole}";
        }
    }
}
