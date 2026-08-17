namespace GTE.Application.DTOs.Responses.Reportes;

/// <summary>Un registro de tiempo dentro del reporte de actividad diaria de un usuario.</summary>
public class ActividadDetalleResponse
{
    public int IdRegistroTiempo { get; set; }
    public int IdWorkItem { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public int Minutos { get; set; }
    public string? Descripcion { get; set; }
}

/// <summary>Actividad de un usuario en un dia especifico dentro del rango consultado.</summary>
public class ActividadDiaResponse
{
    public DateOnly Fecha { get; set; }
    public int MinutosDia { get; set; }
    public IReadOnlyList<ActividadDetalleResponse> Registros { get; set; } = [];
}

/// <summary>Reporte de actividad diaria de un usuario (P.Reportes): horas reales trabajadas por dia en un rango de fechas.</summary>
public class ActividadUsuarioResponse
{
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public int MinutosTotales { get; set; }
    public IReadOnlyList<ActividadDiaResponse> Dias { get; set; } = [];
}
