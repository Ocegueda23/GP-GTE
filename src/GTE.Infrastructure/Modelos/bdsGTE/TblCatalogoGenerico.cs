using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblCatalogoGenerico
{
    public int IdCatalogo { get; set; }

    public string Clave { get; set; } = null!;

    public string NombreTabla { get; set; } = null!;

    public string Titulo { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblCatalogoGenericoColumna> TblCatalogoGenericoColumna { get; set; } = new List<TblCatalogoGenericoColumna>();
}
