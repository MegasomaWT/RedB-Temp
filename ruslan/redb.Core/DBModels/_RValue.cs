using System;
using System.Collections.Generic;

namespace redb.Core.DBModels;

public partial class _RValue
{
    public long Id { get; set; }

    public long IdStructure { get; set; }

    public long IdObject { get; set; }

    public string? String { get; set; }

    public long? Long { get; set; }

    public Guid? Guid { get; set; }

    public double? Double { get; set; }

    public DateTime? DateTime { get; set; }

    public bool? Boolean { get; set; }

    public byte[]? ByteArray { get; set; }

    public string? Array { get; set; }

    public virtual _RObject ObjectNavigation { get; set; } = null!;

    public virtual _RStructure StructureNavigation { get; set; } = null!;
}
