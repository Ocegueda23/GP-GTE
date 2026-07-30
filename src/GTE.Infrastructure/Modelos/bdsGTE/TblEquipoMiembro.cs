using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblEquipoMiembro
{
    public int IdEquipoMiembro { get; set; }

    public int IdEquipo { get; set; }

    public int IdUsuario { get; set; }

    public string? RolEquipo { get; set; }

    public decimal PorcentajeDedicacion { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblEquipo IdEquipoNavigation { get; set; } = null!;

    public virtual TblUsuario IdUsuarioNavigation { get; set; } = null!;
}
