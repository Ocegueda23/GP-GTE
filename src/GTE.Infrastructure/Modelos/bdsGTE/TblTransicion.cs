using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblTransicion
{
    public int IdTransicion { get; set; }

    public int IdProceso { get; set; }

    public int IdEstatusOrigen { get; set; }

    public string Accion { get; set; } = null!;

    public int IdEstatusDestino { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblProceso IdProcesoNavigation { get; set; } = null!;
}
