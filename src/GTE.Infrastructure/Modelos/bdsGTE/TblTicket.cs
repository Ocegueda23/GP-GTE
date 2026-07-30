using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblTicket
{
    public int IdTicket { get; set; }

    public string? Folio { get; set; }

    public int IdSolicitante { get; set; }

    public int? IdCategoriaTicket { get; set; }

    public int IdPrioridad { get; set; }

    public int IdEstatusTicket { get; set; }

    public int? IdAsignado { get; set; }

    public int? IdSla { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateTime? FechaLimiteRespuesta { get; set; }

    public DateTime? FechaLimiteResolucion { get; set; }

    public DateTime? FechaPrimeraRespuesta { get; set; }

    public DateTime? FechaResolucion { get; set; }

    public int? IdWorkItemDerivado { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblUsuario? IdAsignadoNavigation { get; set; }

    public virtual TblCategoriaTicket? IdCategoriaTicketNavigation { get; set; }

    public virtual TblEstatusTicket IdEstatusTicketNavigation { get; set; } = null!;

    public virtual TblPrioridad IdPrioridadNavigation { get; set; } = null!;

    public virtual TblSla? IdSlaNavigation { get; set; }

    public virtual TblUsuario IdSolicitanteNavigation { get; set; } = null!;

    public virtual TblWorkItem? IdWorkItemDerivadoNavigation { get; set; }

    public virtual TblEncuestaSatisfaccion? TblEncuestaSatisfaccion { get; set; }
}
