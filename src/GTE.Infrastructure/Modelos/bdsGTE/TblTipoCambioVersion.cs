using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblTipoCambioVersion
{
    public int IdTipoCambioVersion { get; set; }

    public string Nombre { get; set; } = null!;

    public int Orden { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblNotaVersionDetalle> TblNotaVersionDetalle { get; set; } = new List<TblNotaVersionDetalle>();
}
