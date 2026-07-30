using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblRol
{
    public int IdRol { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool EsSistema { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblRolPermiso> TblRolPermiso { get; set; } = new List<TblRolPermiso>();

    public virtual ICollection<TblUsuarioRol> TblUsuarioRol { get; set; } = new List<TblUsuarioRol>();
}
