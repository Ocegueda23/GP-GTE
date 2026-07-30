using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblPrograma
{
    public int IdPrograma { get; set; }

    public int? IdPortafolio { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblPortafolio? IdPortafolioNavigation { get; set; }

    public virtual ICollection<TblProyecto> TblProyecto { get; set; } = new List<TblProyecto>();
}
