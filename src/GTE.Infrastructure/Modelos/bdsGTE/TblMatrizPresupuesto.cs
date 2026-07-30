using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblMatrizPresupuesto
{
    public int IdMatrizPresupuesto { get; set; }

    public int IdComplejidad { get; set; }

    public int IdNivel { get; set; }

    public int Minutos { get; set; }

    public decimal? Puntos { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblComplejidad IdComplejidadNavigation { get; set; } = null!;

    public virtual TblNivel IdNivelNavigation { get; set; } = null!;
}
