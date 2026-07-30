using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblAusencia
{
    public int IdAusencia { get; set; }

    public int IdUsuario { get; set; }

    public int IdTipoAusencia { get; set; }

    public int IdEstatusAusencia { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }

    public string? Motivo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblEstatusAusencia IdEstatusAusenciaNavigation { get; set; } = null!;

    public virtual TblTipoAusencia IdTipoAusenciaNavigation { get; set; } = null!;

    public virtual TblUsuario IdUsuarioNavigation { get; set; } = null!;
}
