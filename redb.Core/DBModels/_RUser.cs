using System;
using System.Collections.Generic;

namespace redb.Core.DBModels;

public partial class _RUser
{
    public long Id { get; set; }

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public DateTime DateRegister { get; set; }

    public DateTime? DateDismiss { get; set; }

    public bool Enabled { get; set; }

    public virtual ICollection<_RObject> ObjectOwnerNavigations { get; set; } = new List<_RObject>();

    public virtual ICollection<_RObject> ObjectWhoChangeNavigations { get; set; } = new List<_RObject>();

    public virtual ICollection<_RPermission> Permissions { get; set; } = new List<_RPermission>();

    public virtual ICollection<_RUsersRole> UsersRoles { get; set; } = new List<_RUsersRole>();
}
