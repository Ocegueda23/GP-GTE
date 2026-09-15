using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblNotaVersionDetalle
{
    public int IdNotaVersionDetalle { get; set; }

    public int IdNotaVersion { get; set; }

    public int IdTipoCambioVersion { get; set; }

    public string? Modulo { get; set; }

    public string Descripcion { get; set; } = null!;

    public int Orden { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblNotaVersion IdNotaVersionNavigation { get; set; } = null!;

    public virtual TblTipoCambioVersion IdTipoCambioVersionNavigation { get; set; } = null!;
}
