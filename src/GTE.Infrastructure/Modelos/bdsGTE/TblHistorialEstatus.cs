using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblHistorialEstatus
{
    public long IdHistorialEstatus { get; set; }

    public string Proceso { get; set; } = null!;

    public int IdRegistro { get; set; }

    public int IdEstatus { get; set; }

    public string? Accion { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public int? MinutosLaborales { get; set; }

    public string Usuario { get; set; } = null!;

    public string? Motivo { get; set; }
}
