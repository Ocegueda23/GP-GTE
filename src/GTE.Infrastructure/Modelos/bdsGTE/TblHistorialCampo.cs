using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblHistorialCampo
{
    public long IdHistorialCampo { get; set; }

    public string Entidad { get; set; } = null!;

    public int IdEntidad { get; set; }

    public string Campo { get; set; } = null!;

    public string? ValorAnterior { get; set; }

    public string? ValorNuevo { get; set; }

    public string Usuario { get; set; } = null!;

    public DateTime Fecha { get; set; }
}
