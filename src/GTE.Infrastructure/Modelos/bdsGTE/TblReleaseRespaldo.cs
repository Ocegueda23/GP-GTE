using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblReleaseRespaldo
{
    public int IdReleaseRespaldo { get; set; }

    public int IdRelease { get; set; }

    public int IdTipoRespaldo { get; set; }

    public string Descripcion { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblRelease IdReleaseNavigation { get; set; } = null!;

    public virtual TblTipoRespaldo IdTipoRespaldoNavigation { get; set; } = null!;
}
