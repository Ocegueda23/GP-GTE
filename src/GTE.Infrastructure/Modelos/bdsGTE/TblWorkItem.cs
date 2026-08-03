using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblWorkItem
{
    public int IdWorkItem { get; set; }

    public string Folio { get; set; } = null!;

    public int IdTipoWorkItem { get; set; }

    public int? IdPadre { get; set; }

    public int IdProyecto { get; set; }

    public int? IdSolicitud { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string? CriteriosAceptacion { get; set; }

    public int IdEstatusWorkItem { get; set; }

    public int IdPrioridad { get; set; }

    public int? IdComplejidad { get; set; }

    public int? IdAsignado { get; set; }

    public int? IdSolicitante { get; set; }

    public int? IdSprint { get; set; }

    public int? IdRelease { get; set; }

    public decimal? PuntosHistoria { get; set; }

    public int? MinutosPresupuesto { get; set; }

    public DateTime? FechaCompromiso { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public int? OrdenBacklog { get; set; }

    public bool Revisado { get; set; }

    public int? IdEjecucionPruebaOrigen { get; set; }

    public string? ClaveJira { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public string? Locacion { get; set; }

    public int? IdEquipo { get; set; }

    public virtual TblUsuario? IdAsignadoNavigation { get; set; }

    public virtual TblComplejidad? IdComplejidadNavigation { get; set; }

    public virtual TblEjecucionPrueba? IdEjecucionPruebaOrigenNavigation { get; set; }

    public virtual TblEquipo? IdEquipoNavigation { get; set; }

    public virtual TblEstatusWorkItem IdEstatusWorkItemNavigation { get; set; } = null!;

    public virtual TblWorkItem? IdPadreNavigation { get; set; }

    public virtual TblPrioridad IdPrioridadNavigation { get; set; } = null!;

    public virtual TblProyecto IdProyectoNavigation { get; set; } = null!;

    public virtual TblRelease? IdReleaseNavigation { get; set; }

    public virtual TblUsuario? IdSolicitanteNavigation { get; set; }

    public virtual TblSolicitud? IdSolicitudNavigation { get; set; }

    public virtual TblSprint? IdSprintNavigation { get; set; }

    public virtual TblTipoWorkItem IdTipoWorkItemNavigation { get; set; } = null!;

    public virtual ICollection<TblWorkItem> InverseIdPadreNavigation { get; set; } = new List<TblWorkItem>();

    public virtual ICollection<TblCasoPrueba> TblCasoPrueba { get; set; } = new List<TblCasoPrueba>();

    public virtual ICollection<TblCommitWorkItem> TblCommitWorkItem { get; set; } = new List<TblCommitWorkItem>();

    public virtual ICollection<TblIncidente> TblIncidente { get; set; } = new List<TblIncidente>();

    public virtual ICollection<TblPullRequest> TblPullRequest { get; set; } = new List<TblPullRequest>();

    public virtual ICollection<TblRegistroTiempo> TblRegistroTiempo { get; set; } = new List<TblRegistroTiempo>();

    public virtual ICollection<TblRevision> TblRevision { get; set; } = new List<TblRevision>();

    public virtual ICollection<TblTicket> TblTicket { get; set; } = new List<TblTicket>();

    public virtual ICollection<TblWorkItemVinculo> TblWorkItemVinculoIdWorkItemDestinoNavigation { get; set; } = new List<TblWorkItemVinculo>();

    public virtual ICollection<TblWorkItemVinculo> TblWorkItemVinculoIdWorkItemOrigenNavigation { get; set; } = new List<TblWorkItemVinculo>();
}
