using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblNivel
{
    public int IdNivel { get; set; }

    public string Nombre { get; set; } = null!;

    public int Orden { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblMatrizPresupuesto> TblMatrizPresupuesto { get; set; } = new List<TblMatrizPresupuesto>();

    public virtual ICollection<TblTarifaNivel> TblTarifaNivel { get; set; } = new List<TblTarifaNivel>();

    public virtual ICollection<TblUsuario> TblUsuario { get; set; } = new List<TblUsuario>();
}
