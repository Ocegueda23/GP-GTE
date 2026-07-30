using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblAmbiente
{
    public int IdAmbiente { get; set; }

    public int? IdProyecto { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Url { get; set; }

    public string? Servidor { get; set; }

    public string? BaseDatos { get; set; }

    public int? IdResponsable { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblProyecto? IdProyectoNavigation { get; set; }

    public virtual TblUsuario? IdResponsableNavigation { get; set; }

    public virtual ICollection<TblBitacoraCambio> TblBitacoraCambio { get; set; } = new List<TblBitacoraCambio>();

    public virtual ICollection<TblDespliegue> TblDespliegue { get; set; } = new List<TblDespliegue>();

    public virtual ICollection<TblPipelineEjecucion> TblPipelineEjecucion { get; set; } = new List<TblPipelineEjecucion>();
}
