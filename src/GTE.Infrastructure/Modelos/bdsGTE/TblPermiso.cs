using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblPermiso
{
    public int IdPermiso { get; set; }

    public string Clave { get; set; } = null!;

    public string Modulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblRolPermiso> TblRolPermiso { get; set; } = new List<TblRolPermiso>();
}
