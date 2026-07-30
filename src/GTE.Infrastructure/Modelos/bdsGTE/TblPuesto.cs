using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblPuesto
{
    public int IdPuesto { get; set; }

    public string Nombre { get; set; } = null!;

    public int? IdArea { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblArea? IdAreaNavigation { get; set; }

    public virtual ICollection<TblUsuario> TblUsuario { get; set; } = new List<TblUsuario>();
}
