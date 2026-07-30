using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblPlantillaNotificacion
{
    public int IdPlantillaNotificacion { get; set; }

    public string Clave { get; set; } = null!;

    public string Asunto { get; set; } = null!;

    public string Cuerpo { get; set; } = null!;

    public string Canal { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }
}
