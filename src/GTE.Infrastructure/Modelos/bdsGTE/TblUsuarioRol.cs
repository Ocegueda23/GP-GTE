using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblUsuarioRol
{
    public int IdUsuarioRol { get; set; }

    public int IdUsuario { get; set; }

    public int IdRol { get; set; }

    public int? IdProyecto { get; set; }

    public int? IdEquipo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblEquipo? IdEquipoNavigation { get; set; }

    public virtual TblProyecto? IdProyectoNavigation { get; set; }

    public virtual TblRol IdRolNavigation { get; set; } = null!;

    public virtual TblUsuario IdUsuarioNavigation { get; set; } = null!;
}
