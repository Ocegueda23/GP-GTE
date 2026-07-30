using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblTablero
{
    public int IdTablero { get; set; }

    public int IdEquipo { get; set; }

    public string Nombre { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblEquipo IdEquipoNavigation { get; set; } = null!;

    public virtual ICollection<TblTableroColumna> TblTableroColumna { get; set; } = new List<TblTableroColumna>();
}
