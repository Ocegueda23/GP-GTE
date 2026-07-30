using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblArticuloConocimiento
{
    public int IdArticuloConocimiento { get; set; }

    public string Titulo { get; set; } = null!;

    public string Contenido { get; set; } = null!;

    public int VersionActual { get; set; }

    public bool EsGlosario { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblArticuloVersion> TblArticuloVersion { get; set; } = new List<TblArticuloVersion>();
}
