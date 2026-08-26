using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblReglaNegocioImpacto
{
    public int IdReglaNegocioImpacto { get; set; }

    public int IdReglaNegocio { get; set; }

    /// <summary>
    /// Copia del proyecto dueno de la regla. Desnormalizada a proposito: es lo que
    /// permite el CHECK declarativo que impide que una regla se impacte a si misma
    /// (ver script 43). La FK compuesta impide que se desalinee del dueno real.
    /// </summary>
    public int IdProyectoDueno { get; set; }

    public int IdProyectoAfectado { get; set; }

    public string? DescripcionImpacto { get; set; }

    public int? IdAmbitoRegla { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblReglaNegocio IdReglaNegocioNavigation { get; set; } = null!;

    public virtual TblProyecto IdProyectoAfectadoNavigation { get; set; } = null!;

    public virtual TblAmbitoRegla? IdAmbitoReglaNavigation { get; set; }
}
