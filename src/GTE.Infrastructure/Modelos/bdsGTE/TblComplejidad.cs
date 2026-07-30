using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblComplejidad
{
    public int IdComplejidad { get; set; }

    public string Nombre { get; set; } = null!;

    public int? IdCategoriaProyecto { get; set; }

    public int Orden { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblCategoriaProyecto? IdCategoriaProyectoNavigation { get; set; }

    public virtual ICollection<TblMatrizPresupuesto> TblMatrizPresupuesto { get; set; } = new List<TblMatrizPresupuesto>();

    public virtual ICollection<TblWorkItem> TblWorkItem { get; set; } = new List<TblWorkItem>();
}
