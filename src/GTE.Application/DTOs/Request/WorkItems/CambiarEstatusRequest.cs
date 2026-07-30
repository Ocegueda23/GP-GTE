namespace GTE.Application.DTOs.Request.WorkItems;

/// <summary>El frontend manda la ACCION del grafo, nunca el estatus destino.</summary>
public class CambiarEstatusRequest
{
    public string Accion { get; set; } = string.Empty;
    public string? Motivo { get; set; }
}
