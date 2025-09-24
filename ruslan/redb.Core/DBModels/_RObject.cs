using System;
using System.Collections.Generic;

namespace redb.Core.DBModels;

public partial class _RObject
{
    public long Id { get; set; }

    public long? IdParent { get; set; }

    public long IdScheme { get; set; }

    public long IdOwner { get; set; }

    public long IdWhoChange { get; set; }

    public DateTime DateCreate { get; set; }

    public DateTime DateModify { get; set; }

    public DateTime? DateBegin { get; set; }

    public DateTime? DateComplete { get; set; }

    public long? Key { get; set; }

    public long? CodeInt { get; set; }

    public string? CodeString { get; set; }

    public Guid? CodeGuid { get; set; }

    public string? Name { get; set; }

    public string? Note { get; set; }

    public bool? Bool { get; set; }

    public Guid? Hash { get; set; }

    public virtual _RUser OwnerNavigation { get; set; } = null!;

    public virtual _RObject? ParentNavigation { get; set; }

    public virtual _RScheme SchemeNavigation { get; set; } = null!;

    public virtual _RUser WhoChangeNavigation { get; set; } = null!;

    public virtual ICollection<_RObject> InverseParentNavigation { get; set; } = new List<_RObject>();

    public virtual ICollection<_RListItem> ListItems { get; set; } = new List<_RListItem>();

    public virtual ICollection<_RValue> Values { get; set; } = new List<_RValue>();
}
