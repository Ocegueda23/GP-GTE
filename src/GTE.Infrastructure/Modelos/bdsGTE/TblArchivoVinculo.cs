using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblArchivoVinculo
{
    public int IdArchivoVinculo { get; set; }

    public int IdArchivo { get; set; }

    public string Entidad { get; set; } = null!;

    public int IdEntidad { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public bool Activo { get; set; }

    public virtual TblArchivo IdArchivoNavigation { get; set; } = null!;
}
