using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblKpiValor
{
    public long IdKpiValor { get; set; }

    public int IdKpiDefinicion { get; set; }

    public DateOnly Fecha { get; set; }

    public string Alcance { get; set; } = null!;

    public decimal Valor { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual TblKpiDefinicion IdKpiDefinicionNavigation { get; set; } = null!;
}
