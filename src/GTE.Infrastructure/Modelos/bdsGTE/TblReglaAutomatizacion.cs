using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblReglaAutomatizacion
{
    public int IdReglaAutomatizacion { get; set; }

    public string Nombre { get; set; } = null!;

    public string Evento { get; set; } = null!;

    public string? CondicionJson { get; set; }

    public string AccionJson { get; set; } = null!;

    public int ContadorEjecuciones { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }
}
