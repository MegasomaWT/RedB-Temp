using System;
using System.Collections.Generic;

namespace redb.Core.DBModels;

public partial class _RUsersRole
{
    public long Id { get; set; }

    public long IdRole { get; set; }

    public long IdUser { get; set; }

    public virtual _RRole RoleNavigation { get; set; } = null!;

    public virtual _RUser UserNavigation { get; set; } = null!;
}
