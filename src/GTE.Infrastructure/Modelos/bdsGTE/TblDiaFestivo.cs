using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblDiaFestivo
{
    public int IdDiaFestivo { get; set; }

    public DateOnly Fecha { get; set; }

    public string Descripcion { get; set; } = null!;

    public int? IdHorario { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblHorario? IdHorarioNavigation { get; set; }
}
