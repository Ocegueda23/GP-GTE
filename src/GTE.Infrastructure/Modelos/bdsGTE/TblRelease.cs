using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblRelease
{
    public int IdRelease { get; set; }

    public int IdProyecto { get; set; }

    public string Version { get; set; } = null!;

    public string? Folio { get; set; }

    public string? NotasVersion { get; set; }

    public int IdEstatusRelease { get; set; }

    public DateOnly? FechaPlan { get; set; }

    public DateTime? FechaLiberacion { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblEstatusRelease IdEstatusReleaseNavigation { get; set; } = null!;

    public virtual TblProyecto IdProyectoNavigation { get; set; } = null!;

    public virtual ICollection<TblBitacoraCambio> TblBitacoraCambio { get; set; } = new List<TblBitacoraCambio>();

    public virtual ICollection<TblDespliegue> TblDespliegue { get; set; } = new List<TblDespliegue>();

    public virtual ICollection<TblIncidente> TblIncidente { get; set; } = new List<TblIncidente>();

    public virtual ICollection<TblPlanPrueba> TblPlanPrueba { get; set; } = new List<TblPlanPrueba>();

    public virtual ICollection<TblReleaseArtefacto> TblReleaseArtefacto { get; set; } = new List<TblReleaseArtefacto>();

    public virtual ICollection<TblWorkItem> TblWorkItem { get; set; } = new List<TblWorkItem>();
}
