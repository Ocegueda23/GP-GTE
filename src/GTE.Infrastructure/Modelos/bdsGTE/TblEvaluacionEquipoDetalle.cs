using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblEvaluacionEquipoDetalle
{
    public long IdEvaluacionEquipoDetalle { get; set; }

    public int IdEvaluacionEquipo { get; set; }

    public int IdIndicadorGestion { get; set; }

    public decimal? Valor { get; set; }

    public decimal? ValorNormalizado { get; set; }

    public string? Semaforo { get; set; }

    public bool SinDatos { get; set; }

    public decimal? ValorPeriodoAnterior { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public virtual TblEvaluacionEquipo IdEvaluacionEquipoNavigation { get; set; } = null!;

    public virtual TblIndicadorGestion IdIndicadorGestionNavigation { get; set; } = null!;
}
