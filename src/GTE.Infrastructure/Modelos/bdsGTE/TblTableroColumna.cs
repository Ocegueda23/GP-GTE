using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblTableroColumna
{
    public int IdTableroColumna { get; set; }

    public int IdTablero { get; set; }

    public string Nombre { get; set; } = null!;

    public int IdEstatusWorkItem { get; set; }

    public int Orden { get; set; }

    public int? LimiteWip { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblEstatusWorkItem IdEstatusWorkItemNavigation { get; set; } = null!;

    public virtual TblTablero IdTableroNavigation { get; set; } = null!;
}
