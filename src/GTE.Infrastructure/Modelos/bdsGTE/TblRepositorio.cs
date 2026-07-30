using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblRepositorio
{
    public int IdRepositorio { get; set; }

    public int IdProyecto { get; set; }

    public string Nombre { get; set; } = null!;

    public string Url { get; set; } = null!;

    public string? SecretoWebhook { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblProyecto IdProyectoNavigation { get; set; } = null!;

    public virtual ICollection<TblCommit> TblCommit { get; set; } = new List<TblCommit>();

    public virtual ICollection<TblPipelineEjecucion> TblPipelineEjecucion { get; set; } = new List<TblPipelineEjecucion>();

    public virtual ICollection<TblPullRequest> TblPullRequest { get; set; } = new List<TblPullRequest>();
}
