using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblTarifaNivel
{
    public int IdTarifaNivel { get; set; }

    public int IdNivel { get; set; }

    public decimal CostoHora { get; set; }

    public DateOnly VigenciaDesde { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblNivel IdNivelNavigation { get; set; } = null!;
}
