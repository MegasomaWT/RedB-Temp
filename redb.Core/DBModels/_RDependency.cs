using System;
using System.Collections.Generic;

namespace redb.Core.DBModels;

public partial class _RDependency
{
    public long Id { get; set; }

    public long? IdScheme1 { get; set; }

    public long IdScheme2 { get; set; }

    public virtual _RScheme? Scheme1Navigation { get; set; }

    public virtual _RScheme Scheme2Navigation { get; set; } = null!;
}
