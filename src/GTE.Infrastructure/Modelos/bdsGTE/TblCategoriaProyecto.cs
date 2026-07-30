using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblCategoriaProyecto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblComplejidad> TblComplejidad { get; set; } = new List<TblComplejidad>();

    public virtual ICollection<TblProyecto> TblProyecto { get; set; } = new List<TblProyecto>();
}
