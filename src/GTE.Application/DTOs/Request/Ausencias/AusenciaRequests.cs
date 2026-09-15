namespace GTE.Application.DTOs.Request.Ausencias;

/// <summary>
/// Alta de una ausencia. El periodo viaja como fecha sin hora (yyyy-MM-dd); el usuario
/// dueño sale del token salvo que quien registra tenga ADM.Ausencias y capture a otro.
/// </summary>
public class AusenciaCrearRequest
{
    public int IdTipoAusencia { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public string? Motivo { get; set; }

    /// <summary>Registrar la ausencia a nombre de otra persona; exige ADM.Ausencias.</summary>
    public int? IdUsuario { get; set; }
}

/// <summary>Edicion mientras la ausencia sigue en Solicitada.</summary>
public class AusenciaEditarRequest
{
    public int IdTipoAusencia { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public string? Motivo { get; set; }
}

/// <summary>El frontend manda la ACCION (APROBAR/RECHAZAR/CANCELAR), nunca el estatus destino.</summary>
public class CambiarEstatusAusenciaRequest
{
    public string Accion { get; set; } = string.Empty;
    public string? Motivo { get; set; }
}
