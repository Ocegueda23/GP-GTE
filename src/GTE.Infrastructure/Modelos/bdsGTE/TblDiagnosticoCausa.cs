using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblDiagnosticoCausa
{
    public long IdDiagnosticoCausa { get; set; }

    public int IdEvaluacionEquipo { get; set; }

    public string Causa { get; set; } = null!;

    public string IndiceClave { get; set; } = null!;

    public decimal? Valor { get; set; }

    public decimal? Umbral { get; set; }

    public string? Evidencia { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public virtual TblEvaluacionEquipo IdEvaluacionEquipoNavigation { get; set; } = null!;
}
