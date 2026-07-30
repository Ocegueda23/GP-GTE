using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblCasoPruebaPaso
{
    public int IdCasoPruebaPaso { get; set; }

    public int IdCasoPrueba { get; set; }

    public int NumeroPaso { get; set; }

    public string Accion { get; set; } = null!;

    public string? ResultadoEsperado { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public virtual TblCasoPrueba IdCasoPruebaNavigation { get; set; } = null!;
}
