using System;
using System.Collections.Generic;

namespace redb.Core.DBModels;

public partial class _RStructure
{
    public long Id { get; set; }

    public long? IdParent { get; set; }

    public long IdScheme { get; set; }

    public long? IdOverride { get; set; }

    public long IdType { get; set; }

    public long? IdList { get; set; }

    public string Name { get; set; } = null!;

    public string? Alias { get; set; }

    public long? Order { get; set; }

    public bool? Readonly { get; set; }

    public bool? AllowNotNull { get; set; }

    public bool? IsArray { get; set; }

    public bool? IsCompress { get; set; }

    public bool? StoreNull { get; set; }

    public byte[]? DefaultValue { get; set; }

    public string? DefaultEditor { get; set; }

    public virtual _RList? ListNavigation { get; set; }

    public virtual _RStructure? ParentNavigation { get; set; }

    public virtual _RScheme SchemeNavigation { get; set; } = null!;

    public virtual _RType TypeNavigation { get; set; } = null!;

    public virtual ICollection<_RStructure> InverseParentNavigation { get; set; } = new List<_RStructure>();

    public virtual ICollection<_RValue> Values { get; set; } = new List<_RValue>();
}
