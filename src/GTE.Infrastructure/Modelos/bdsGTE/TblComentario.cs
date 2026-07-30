using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblComentario
{
    public int IdComentario { get; set; }

    public string Entidad { get; set; } = null!;

    public int IdEntidad { get; set; }

    public string Contenido { get; set; } = null!;

    public int? IdComentarioPadre { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblComentario? IdComentarioPadreNavigation { get; set; }

    public virtual ICollection<TblComentario> InverseIdComentarioPadreNavigation { get; set; } = new List<TblComentario>();
}
