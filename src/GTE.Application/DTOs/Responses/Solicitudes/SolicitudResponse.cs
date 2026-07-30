namespace GTE.Application.DTOs.Responses.Solicitudes;

public class SolicitudResponse
{
    public int IdSolicitud { get; set; }
    public string? Folio { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public int IdEstatus { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public string Solicitante { get; set; } = string.Empty;
    public string? Proyecto { get; set; }
    public int? IdProyecto { get; set; }
    public DateTime? FechaDeseada { get; set; }
    public string? JustificacionNegocio { get; set; }
    public DateTime FechaRegistro { get; set; }
    public int DiasEspera { get; set; }
    public IReadOnlyList<ItemGeneradoResponse> ItemsGenerados { get; set; } = [];
}

public class ItemGeneradoResponse
{
    public string Folio { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Estatus { get; set; } = string.Empty;
}

/// <summary>Eco de la conversion: uiId del front junto al Id y folio reales.</summary>
public class ConversionResponse
{
    public IReadOnlyList<ItemConvertidoResponse> Items { get; set; } = [];
}

public class ItemConvertidoResponse
{
    public string UiId { get; set; } = string.Empty;
    public int IdWorkItem { get; set; }
    public string Folio { get; set; } = string.Empty;
}
