using System;
using System.Collections.Generic;

namespace redb.Core.DBModels;

public partial class _RDeletedObject
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

    public DateTime DateDelete { get; set; }

    public string? Values { get; set; }
}
