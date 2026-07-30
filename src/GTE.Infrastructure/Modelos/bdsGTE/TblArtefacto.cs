using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblArtefacto
{
    public int IdArtefacto { get; set; }

    public int? IdPipelineEjecucion { get; set; }

    public int? IdArchivo { get; set; }

    public string Nombre { get; set; } = null!;

    public int IdTipoArtefacto { get; set; }

    public string? HashSha256 { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblArchivo? IdArchivoNavigation { get; set; }

    public virtual TblPipelineEjecucion? IdPipelineEjecucionNavigation { get; set; }

    public virtual TblTipoArtefacto IdTipoArtefactoNavigation { get; set; } = null!;

    public virtual ICollection<TblReleaseArtefacto> TblReleaseArtefactoIdArtefactoNavigation { get; set; } = new List<TblReleaseArtefacto>();

    public virtual ICollection<TblReleaseArtefacto> TblReleaseArtefactoIdArtefactoRollbackNavigation { get; set; } = new List<TblReleaseArtefacto>();
}
