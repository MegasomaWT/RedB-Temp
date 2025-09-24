using System;
using System.Collections.Generic;

namespace redb.Core.DBModels;

public partial class _RType
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string? DbType { get; set; }

    public string? Type1 { get; set; }

    public virtual ICollection<_RStructure> Structures { get; set; } = new List<_RStructure>();
}
