using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblAmbitoRegla
{
    public int IdAmbitoRegla { get; set; }

    public int IdProyecto { get; set; }

    public int IdTipoAmbitoRegla { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblProyecto IdProyectoNavigation { get; set; } = null!;

    public virtual TblTipoAmbitoRegla IdTipoAmbitoReglaNavigation { get; set; } = null!;

    public virtual ICollection<TblReglaNegocio> TblReglaNegocio { get; set; } = new List<TblReglaNegocio>();
}
