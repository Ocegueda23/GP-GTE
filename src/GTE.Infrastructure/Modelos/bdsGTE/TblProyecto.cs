using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblProyecto
{
    public int IdProyecto { get; set; }

    public string? Folio { get; set; }

    public string Clave { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public int? IdPrograma { get; set; }

    public int IdCategoriaProyecto { get; set; }

    public int IdEstatusProyecto { get; set; }

    public int? IdResponsable { get; set; }

    public int? IdEquipo { get; set; }

    public DateTime? FechaInicioPlan { get; set; }

    public DateTime? FechaFinPlan { get; set; }

    public DateTime? FechaInicioReal { get; set; }

    public DateTime? FechaFinReal { get; set; }

    public bool EsMantenimiento { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblCategoriaProyecto IdCategoriaProyectoNavigation { get; set; } = null!;

    public virtual TblEquipo? IdEquipoNavigation { get; set; }

    public virtual TblEstatusProyecto IdEstatusProyectoNavigation { get; set; } = null!;

    public virtual TblPrograma? IdProgramaNavigation { get; set; }

    public virtual TblUsuario? IdResponsableNavigation { get; set; }

    public virtual ICollection<TblAmbiente> TblAmbiente { get; set; } = new List<TblAmbiente>();

    public virtual ICollection<TblHito> TblHito { get; set; } = new List<TblHito>();

    public virtual ICollection<TblIncidente> TblIncidente { get; set; } = new List<TblIncidente>();

    public virtual ICollection<TblObjetivoOkr> TblObjetivoOkr { get; set; } = new List<TblObjetivoOkr>();

    public virtual ICollection<TblPlanPrueba> TblPlanPrueba { get; set; } = new List<TblPlanPrueba>();

    public virtual ICollection<TblPresupuestoProyecto> TblPresupuestoProyecto { get; set; } = new List<TblPresupuestoProyecto>();

    public virtual ICollection<TblRelease> TblRelease { get; set; } = new List<TblRelease>();

    public virtual ICollection<TblRepositorio> TblRepositorio { get; set; } = new List<TblRepositorio>();

    public virtual ICollection<TblRiesgo> TblRiesgo { get; set; } = new List<TblRiesgo>();

    public virtual ICollection<TblSolicitud> TblSolicitud { get; set; } = new List<TblSolicitud>();

    public virtual ICollection<TblUsuarioRol> TblUsuarioRol { get; set; } = new List<TblUsuarioRol>();

    public virtual ICollection<TblWorkItem> TblWorkItem { get; set; } = new List<TblWorkItem>();
}
