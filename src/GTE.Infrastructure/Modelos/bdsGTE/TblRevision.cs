using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblRevision
{
    public int IdRevision { get; set; }

    public int IdWorkItem { get; set; }

    public int IdRevisor { get; set; }

    public string? Comentarios { get; set; }

    public int IdEstatusRevision { get; set; }

    public bool Corregido { get; set; }

    public DateTime? FechaCorreccion { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblEstatusRevision IdEstatusRevisionNavigation { get; set; } = null!;

    public virtual TblUsuario IdRevisorNavigation { get; set; } = null!;

    public virtual TblWorkItem IdWorkItemNavigation { get; set; } = null!;
}
