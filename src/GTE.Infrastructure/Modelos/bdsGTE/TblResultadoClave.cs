using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblResultadoClave
{
    public int IdResultadoClave { get; set; }

    public int IdObjetivoOkr { get; set; }

    public string Nombre { get; set; } = null!;

    public decimal ValorMeta { get; set; }

    public decimal ValorActual { get; set; }

    public string? ClaveKpi { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblObjetivoOkr IdObjetivoOkrNavigation { get; set; } = null!;
}
