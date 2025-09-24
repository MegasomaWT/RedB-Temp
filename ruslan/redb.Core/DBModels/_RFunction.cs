using System;
using System.Collections.Generic;

namespace redb.Core.DBModels;

public partial class _RFunction
{
    public long Id { get; set; }

    public long IdScheme { get; set; }

    public string Language { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Body { get; set; } = null!;

    public virtual _RScheme SchemeNavigation { get; set; } = null!;
}
