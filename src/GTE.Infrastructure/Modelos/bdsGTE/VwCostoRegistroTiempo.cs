using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

/// <summary>
/// Costo real por registro de tiempo: resuelve la tarifa vigente del nivel del usuario
/// a la fecha del registro (ver vwCostoRegistroTiempo, script 14). Vista de solo
/// lectura, sin clave (HasNoKey en DbContextGTE).
/// </summary>
public partial class VwCostoRegistroTiempo
{
    public int IdRegistroTiempo { get; set; }

    public int IdProyecto { get; set; }

    public int IdUsuario { get; set; }

    public DateOnly Fecha { get; set; }

    public int Minutos { get; set; }

    public decimal? CostoHora { get; set; }

    public decimal Costo { get; set; }
}
