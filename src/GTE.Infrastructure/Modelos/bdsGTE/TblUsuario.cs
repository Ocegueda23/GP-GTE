using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblUsuario
{
    public int IdUsuario { get; set; }

    public string Dominio { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Correo { get; set; }

    public int? IdPuesto { get; set; }

    public int? IdNivel { get; set; }

    public int? IdHorario { get; set; }

    public int? IdJefe { get; set; }

    public bool EsExterno { get; set; }

    public DateTime? FechaAlta { get; set; }

    public DateTime? FechaBaja { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public string? PasswordHash { get; set; }

    public bool? RequiereCambioPassword { get; set; }

    public int? IntentosFallidos { get; set; }

    public DateTime? BloqueadoHasta { get; set; }

    public DateTime? FechaUltimoCambioPassword { get; set; }

    public virtual TblHorario? IdHorarioNavigation { get; set; }

    public virtual TblUsuario? IdJefeNavigation { get; set; }

    public virtual TblNivel? IdNivelNavigation { get; set; }

    public virtual TblPuesto? IdPuestoNavigation { get; set; }

    public virtual ICollection<TblUsuario> InverseIdJefeNavigation { get; set; } = new List<TblUsuario>();

    public virtual ICollection<TblAmbiente> TblAmbiente { get; set; } = new List<TblAmbiente>();

    public virtual ICollection<TblAprobacion> TblAprobacion { get; set; } = new List<TblAprobacion>();

    public virtual ICollection<TblAusencia> TblAusencia { get; set; } = new List<TblAusencia>();

    public virtual ICollection<TblCapacidadSprint> TblCapacidadSprint { get; set; } = new List<TblCapacidadSprint>();

    public virtual ICollection<TblDespliegue> TblDespliegue { get; set; } = new List<TblDespliegue>();

    public virtual ICollection<TblEjecucionPrueba> TblEjecucionPrueba { get; set; } = new List<TblEjecucionPrueba>();

    public virtual ICollection<TblEquipo> TblEquipo { get; set; } = new List<TblEquipo>();

    public virtual ICollection<TblEquipoMiembro> TblEquipoMiembro { get; set; } = new List<TblEquipoMiembro>();

    public virtual ICollection<TblNotificacion> TblNotificacion { get; set; } = new List<TblNotificacion>();

    public virtual ICollection<TblProyecto> TblProyecto { get; set; } = new List<TblProyecto>();

    public virtual ICollection<TblRefreshToken> TblRefreshToken { get; set; } = new List<TblRefreshToken>();

    public virtual ICollection<TblRegistroTiempo> TblRegistroTiempo { get; set; } = new List<TblRegistroTiempo>();

    public virtual ICollection<TblRevision> TblRevision { get; set; } = new List<TblRevision>();

    public virtual ICollection<TblRiesgo> TblRiesgo { get; set; } = new List<TblRiesgo>();

    public virtual ICollection<TblSolicitud> TblSolicitud { get; set; } = new List<TblSolicitud>();

    public virtual ICollection<TblTicket> TblTicketIdAsignadoNavigation { get; set; } = new List<TblTicket>();

    public virtual ICollection<TblTicket> TblTicketIdSolicitanteNavigation { get; set; } = new List<TblTicket>();

    public virtual ICollection<TblUsuarioRol> TblUsuarioRol { get; set; } = new List<TblUsuarioRol>();

    public virtual ICollection<TblWorkItem> TblWorkItemIdAsignadoNavigation { get; set; } = new List<TblWorkItem>();

    public virtual ICollection<TblWorkItem> TblWorkItemIdSolicitanteNavigation { get; set; } = new List<TblWorkItem>();
}
