using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblReleaseArtefacto
{
    public int IdReleaseArtefacto { get; set; }

    public int IdRelease { get; set; }

    public int IdArtefacto { get; set; }

    public int? OrdenEjecucion { get; set; }

    public int? IdArtefactoRollback { get; set; }

    public string? JustificacionIrreversible { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public bool Activo { get; set; }

    public virtual TblArtefacto IdArtefactoNavigation { get; set; } = null!;

    public virtual TblArtefacto? IdArtefactoRollbackNavigation { get; set; }

    public virtual TblRelease IdReleaseNavigation { get; set; } = null!;
}
