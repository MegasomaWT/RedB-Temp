using System;
using System.Collections.Generic;

namespace redb.Core.DBModels;

public partial class _RPermission
{
    public long Id { get; set; }

    public long? IdRole { get; set; }

    public long? IdUser { get; set; }

    public long IdRef { get; set; }

    public bool? Select { get; set; }

    public bool? Insert { get; set; }

    public bool? Update { get; set; }

    public bool? Delete { get; set; }

    public virtual _RRole? RoleNavigation { get; set; }

    public virtual _RUser? UserNavigation { get; set; }
}
