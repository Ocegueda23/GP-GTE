using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblReglaNegocio
{
    public int IdReglaNegocio { get; set; }

    /// <summary>Proyecto DUENO de la regla. Toda regla nace en un proyecto (script 43).</summary>
    public int IdProyecto { get; set; }

    public string Clave { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string Enunciado { get; set; } = null!;

    public string? Justificacion { get; set; }

    /// <summary>Un flujo de operacion O una caracteristica del sistema, nunca ambos.</summary>
    public int? IdAmbitoRegla { get; set; }

    public int IdEstadoReglaNegocio { get; set; }

    public string? MensajeError { get; set; }

    public string? PermisoBypass { get; set; }

    public string? UbicacionCodigo { get; set; }

    public DateOnly? FechaVigenciaDesde { get; set; }

    public DateOnly? FechaVigenciaHasta { get; set; }

    public int VersionActual { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblProyecto IdProyectoNavigation { get; set; } = null!;

    public virtual TblAmbitoRegla? IdAmbitoReglaNavigation { get; set; }

    public virtual TblEstadoReglaNegocio IdEstadoReglaNegocioNavigation { get; set; } = null!;

    public virtual ICollection<TblReglaNegocioVersion> TblReglaNegocioVersion { get; set; } = new List<TblReglaNegocioVersion>();

    public virtual ICollection<TblReglaNegocioImpacto> TblReglaNegocioImpacto { get; set; } = new List<TblReglaNegocioImpacto>();
}
