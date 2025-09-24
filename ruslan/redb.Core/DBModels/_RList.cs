using System;
using System.Collections.Generic;

namespace redb.Core.DBModels;

public partial class _RList
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Alias { get; set; }

    public virtual ICollection<_RListItem> ListItems { get; set; } = new List<_RListItem>();

    public virtual ICollection<_RStructure> Structures { get; set; } = new List<_RStructure>();
}
