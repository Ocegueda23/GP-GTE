using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblEventoDominio
{
    public long IdEventoDominio { get; set; }

    public string TipoEvento { get; set; } = null!;

    public string PayloadJson { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public DateTime? FechaProcesado { get; set; }

    public int Intentos { get; set; }

    public string? UltimoError { get; set; }
}
