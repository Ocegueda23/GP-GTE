namespace GTE.Application.DTOs.Request.Solicitudes;

public class SolicitudCrearRequest
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int IdTipoSolicitud { get; set; }
    public int IdPrioridad { get; set; }
    public DateTime? FechaDeseada { get; set; }
    public string? JustificacionNegocio { get; set; }
    /// <summary>Quien pide el trabajo en realidad (catalogo tblUsuarioSolicitante, no
    /// necesariamente con cuenta de GTE); opcional, para cuando quien registra la
    /// solicitud lo hace a nombre de otra persona.</summary>
    public int? IdUsuarioSolicitante { get; set; }
}

/// <summary>El frontend manda la ACCION; APROBAR ademas exige el proyecto destino.</summary>
public class CambiarEstatusSolicitudRequest
{
    public string Accion { get; set; } = string.Empty;
    public string? Motivo { get; set; }
    public int? IdProyecto { get; set; }
}

/// <summary>Item del desglose al convertir (patron uiId: el back lo ecoa con el Id real).</summary>
public class ItemConversionRequest
{
    public string UiId { get; set; } = string.Empty;
    public int IdTipoWorkItem { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int IdPrioridad { get; set; }
    public int? IdAsignado { get; set; }
    public DateTime? FechaCompromiso { get; set; }
}

public class ConvertirSolicitudRequest
{
    public List<ItemConversionRequest> Items { get; set; } = [];
}
