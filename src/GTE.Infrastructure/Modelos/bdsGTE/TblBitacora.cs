using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblBitacora
{
    public long IdBitacora { get; set; }

    public string Usuario { get; set; } = null!;

    public string? Ip { get; set; }

    public string? Endpoint { get; set; }

    public string Entidad { get; set; } = null!;

    public int? IdEntidad { get; set; }

    public string Accion { get; set; } = null!;

    public string? Detalle { get; set; }

    public DateTime Fecha { get; set; }
}
