using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblPullRequest
{
    public int IdPullRequest { get; set; }

    public int IdRepositorio { get; set; }

    public int Numero { get; set; }

    public string Titulo { get; set; } = null!;

    public int? IdWorkItem { get; set; }

    public string Autor { get; set; } = null!;

    public string EstatusPr { get; set; } = null!;

    public string? RamaOrigen { get; set; }

    public string? RamaDestino { get; set; }

    public string? Url { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public virtual TblRepositorio IdRepositorioNavigation { get; set; } = null!;

    public virtual TblWorkItem? IdWorkItemNavigation { get; set; }
}
