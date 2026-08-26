using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblReglaNegocioRelacion
{
    public int IdReglaNegocioRelacion { get; set; }

    public int IdReglaNegocio { get; set; }

    public int IdReglaNegocioRelacionada { get; set; }

    public int IdTipoRelacionRegla { get; set; }

    public string? Nota { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblReglaNegocio IdReglaNegocioNavigation { get; set; } = null!;

    public virtual TblReglaNegocio IdReglaNegocioRelacionadaNavigation { get; set; } = null!;

    public virtual TblTipoRelacionRegla IdTipoRelacionReglaNavigation { get; set; } = null!;
}
