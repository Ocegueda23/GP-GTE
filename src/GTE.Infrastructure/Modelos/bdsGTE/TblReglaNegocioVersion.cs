using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblReglaNegocioVersion
{
    public int IdReglaNegocioVersion { get; set; }

    public int IdReglaNegocio { get; set; }

    public int NumeroVersion { get; set; }

    public string Enunciado { get; set; } = null!;

    public string? Justificacion { get; set; }

    public string? MotivoCambio { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public virtual TblReglaNegocio IdReglaNegocioNavigation { get; set; } = null!;
}
