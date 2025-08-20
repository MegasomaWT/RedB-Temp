using redb.Core.DBModels;
using redb.Core.Models.Contracts;
using System;

namespace redb.Core.Models.Entities
{
    /// <summary>
    /// Реализация интерфейса IRedbRole на основе _RRole
    /// </summary>
    public class RedbRole : IRedbRole
    {
        private readonly _RRole _role;

        public RedbRole(_RRole role)
        {
            _role = role ?? throw new ArgumentNullException(nameof(role));
        }

        public long Id => _role.Id;
        public string Name => _role.Name;

        /// <summary>
        /// Создать RedbRole из _RRole (статический метод)
        /// </summary>
        public static RedbRole FromEntity(_RRole role) => new RedbRole(role);

        /// <summary>
        /// Создать IRedbRole из _RRole
        /// </summary>
        public static implicit operator RedbRole(_RRole role) => new RedbRole(role);

        /// <summary>
        /// Получить _RRole из IRedbRole
        /// </summary>
        public static implicit operator _RRole(RedbRole redbRole) => redbRole._role;

        public override string ToString()
        {
            return $"Role {Id}: {Name}";
        }
    }
}
