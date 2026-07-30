using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblArticuloVersion
{
    public int IdArticuloVersion { get; set; }

    public int IdArticuloConocimiento { get; set; }

    public int Version { get; set; }

    public string Contenido { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public virtual TblArticuloConocimiento IdArticuloConocimientoNavigation { get; set; } = null!;
}
