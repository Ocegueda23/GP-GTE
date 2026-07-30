using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblCapacidadSprint
{
    public int IdCapacidadSprint { get; set; }

    public int IdSprint { get; set; }

    public int IdUsuario { get; set; }

    public decimal HorasPorDia { get; set; }

    public decimal PorcentajeDedicacion { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public virtual TblSprint IdSprintNavigation { get; set; } = null!;

    public virtual TblUsuario IdUsuarioNavigation { get; set; } = null!;
}
