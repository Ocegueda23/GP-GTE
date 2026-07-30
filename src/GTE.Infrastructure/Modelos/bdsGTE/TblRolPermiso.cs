using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblRolPermiso
{
    public int IdRolPermiso { get; set; }

    public int IdRol { get; set; }

    public int IdPermiso { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public virtual TblPermiso IdPermisoNavigation { get; set; } = null!;

    public virtual TblRol IdRolNavigation { get; set; } = null!;
}
