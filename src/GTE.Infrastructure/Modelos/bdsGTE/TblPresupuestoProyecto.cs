using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblPresupuestoProyecto
{
    public int IdPresupuestoProyecto { get; set; }

    public int IdProyecto { get; set; }

    public int Anio { get; set; }

    public decimal MontoAutorizado { get; set; }

    public decimal HorasAutorizadas { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblProyecto IdProyectoNavigation { get; set; } = null!;
}
