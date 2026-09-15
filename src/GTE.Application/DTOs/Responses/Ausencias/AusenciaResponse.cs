namespace GTE.Application.DTOs.Responses.Ausencias;

public class AusenciaResponse
{
    public int IdAusencia { get; set; }
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public int IdTipoAusencia { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public int IdEstatus { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    /// <summary>Dias naturales del periodo, extremos incluidos (el descuento de capacidad
    /// laborable lo calcula Planeacion con ICalendarioLaboral, no este contador).</summary>
    public int DiasNaturales { get; set; }
    public string? Motivo { get; set; }
    public DateTime FechaRegistro { get; set; }
}

/// <summary>Catalogos de la pantalla de ausencias (tipos y estatus).</summary>
public class CatalogosAusenciaResponse
{
    public IReadOnlyList<OpcionAusenciaResponse> Tipos { get; set; } = [];
    public IReadOnlyList<OpcionAusenciaResponse> Estatus { get; set; } = [];
}

public class OpcionAusenciaResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
