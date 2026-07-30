using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblPortafolio
{
    public int IdPortafolio { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblPrograma> TblPrograma { get; set; } = new List<TblPrograma>();
}
