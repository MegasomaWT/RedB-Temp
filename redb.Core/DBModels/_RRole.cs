using System;
using System.Collections.Generic;

namespace redb.Core.DBModels;

public partial class _RRole
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<_RPermission> Permissions { get; set; } = new List<_RPermission>();

    public virtual ICollection<_RUsersRole> UsersRoles { get; set; } = new List<_RUsersRole>();
}
