using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblCadenaAprobacionProyecto
{
    public int IdCadenaAprobacionProyecto { get; set; }

    public int IdProyecto { get; set; }

    public int Orden { get; set; }

    public string Rol { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public virtual TblProyecto IdProyectoNavigation { get; set; } = null!;
}
