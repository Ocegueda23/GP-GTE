using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblSolicitud
{
    public int IdSolicitud { get; set; }

    public string? Folio { get; set; }

    public int IdSolicitante { get; set; }

    public int? IdProyecto { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public int IdTipoSolicitud { get; set; }

    public int IdPrioridad { get; set; }

    public int IdEstatusSolicitud { get; set; }

    public DateOnly? FechaDeseada { get; set; }

    public string? JustificacionNegocio { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblEstatusSolicitud IdEstatusSolicitudNavigation { get; set; } = null!;

    public virtual TblPrioridad IdPrioridadNavigation { get; set; } = null!;

    public virtual TblProyecto? IdProyectoNavigation { get; set; }

    public virtual TblUsuario IdSolicitanteNavigation { get; set; } = null!;

    public virtual TblTipoSolicitud IdTipoSolicitudNavigation { get; set; } = null!;

    public virtual ICollection<TblWorkItem> TblWorkItem { get; set; } = new List<TblWorkItem>();
}
