using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblCommitWorkItem
{
    public int IdCommitWorkItem { get; set; }

    public int IdCommit { get; set; }

    public int IdWorkItem { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual TblCommit IdCommitNavigation { get; set; } = null!;

    public virtual TblWorkItem IdWorkItemNavigation { get; set; } = null!;
}
