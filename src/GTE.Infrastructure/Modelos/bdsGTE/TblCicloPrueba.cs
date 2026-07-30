using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblCicloPrueba
{
    public int IdCicloPrueba { get; set; }

    public int IdPlanPrueba { get; set; }

    public string Nombre { get; set; } = null!;

    public DateOnly? FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblPlanPrueba IdPlanPruebaNavigation { get; set; } = null!;

    public virtual ICollection<TblEjecucionPrueba> TblEjecucionPrueba { get; set; } = new List<TblEjecucionPrueba>();
}
