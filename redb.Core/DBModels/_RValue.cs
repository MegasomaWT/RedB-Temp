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

    /// <summary>
    /// ID родительского элемента для элементов массива. NULL для обычных (не-массивных) полей и корневых элементов массива
    /// </summary>
    public long? ArrayParentId { get; set; }

    /// <summary>
    /// Позиция элемента в массиве (0,1,2...). NULL для обычных (не-массивных) полей. Используется для всех типов массивов: простых типов и Class полей
    /// </summary>
    public int? ArrayIndex { get; set; }

    public virtual _RValue? ArrayParent { get; set; }

    public virtual _RObject ObjectNavigation { get; set; } = null!;

    public virtual _RStructure StructureNavigation { get; set; } = null!;

    public virtual ICollection<_RValue> InverseArrayParent { get; set; } = new List<_RValue>();
}
