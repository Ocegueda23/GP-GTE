using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblNotaVersion
{
    public int IdNotaVersion { get; set; }

    public string Version { get; set; } = null!;

    public DateOnly FechaLiberacion { get; set; }

    public string? Resumen { get; set; }

    public bool Publicada { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblNotaVersionDetalle> TblNotaVersionDetalle { get; set; } = new List<TblNotaVersionDetalle>();
}
