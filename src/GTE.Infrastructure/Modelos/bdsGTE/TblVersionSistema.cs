using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblVersionSistema
{
    public int IdVersionSistema { get; set; }

    public string Version { get; set; } = null!;

    public DateTime FechaLiberacion { get; set; }

    public string? Notas { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;
}
