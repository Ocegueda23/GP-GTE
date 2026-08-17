using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblDashboardLayoutUsuario
{
    public int IdUsuario { get; set; }

    public string LayoutJson { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public virtual TblUsuario IdUsuarioNavigation { get; set; } = null!;
}
