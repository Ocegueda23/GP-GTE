using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblCatalogoGenericoColumna
{
    public int IdColumna { get; set; }

    public int IdCatalogo { get; set; }

    public string NombreColumna { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public bool EsVisible { get; set; }

    public bool EsSoloLectura { get; set; }

    public bool EsRequerido { get; set; }

    public int OrdinalPos { get; set; }

    public string? TablaFk { get; set; }

    public string? ColumnaClaveFk { get; set; }

    public string? ColumnaMostrarFk { get; set; }

    public bool EsCifrado { get; set; }

    public bool AutoFechaAlta { get; set; }

    public bool AutoFechaEdicion { get; set; }

    public bool AutoUsuarioAlta { get; set; }

    public bool AutoUsuarioEdicion { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblCatalogoGenerico IdCatalogoNavigation { get; set; } = null!;
}
