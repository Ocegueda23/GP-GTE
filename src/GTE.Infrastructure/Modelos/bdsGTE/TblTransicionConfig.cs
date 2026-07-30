using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblTransicionConfig
{
    public int IdTransicionConfig { get; set; }

    public string Proceso { get; set; } = null!;

    public int IdEstatusOrigen { get; set; }

    public string Accion { get; set; } = null!;

    public string EtiquetaBoton { get; set; } = null!;

    public string? IconoAccion { get; set; }

    public string? RequierePermiso { get; set; }

    public bool RequiereMotivo { get; set; }

    public bool EsAccionPrincipal { get; set; }

    public int Orden { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }
}
