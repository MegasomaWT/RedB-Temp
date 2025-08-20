using System;
using System.Collections.Generic;

namespace redb.Core.DBModels;

public partial class _RListItem
{
    public long Id { get; set; }

    public long IdList { get; set; }

    public string? Value { get; set; }

    public long? IdObject { get; set; }

    public virtual _RList ListNavigation { get; set; } = null!;

    public virtual _RObject? ObjectNavigation { get; set; }
}
