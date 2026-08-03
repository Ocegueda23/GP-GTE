namespace GTE.Application.DTOs.Request.Soporte;

public class TicketCrearRequest
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? IdCategoriaTicket { get; set; }
    public int IdPrioridad { get; set; }
    /// <summary>Quien reporta el problema en realidad (catalogo tblUsuarioSolicitante, no
    /// necesariamente con cuenta de GTE); opcional, lo captura el ingeniero de soporte
    /// cuando registra el ticket por otra persona.</summary>
    public int? IdUsuarioSolicitante { get; set; }
    public int? IdLocacion { get; set; }
}

/// <summary>
/// El frontend manda la ACCION; ASIGNAR ademas exige el agente destino y RESOLVER exige
/// Solucion y MinutosSolucion (el ingeniero cierra el ticket una vez resuelto con estos
/// datos capturados).
/// </summary>
public class CambiarEstatusTicketRequest
{
    public string Accion { get; set; } = string.Empty;
    public string? Motivo { get; set; }
    public int? IdAsignado { get; set; }
    public string? Solucion { get; set; }
    public int? MinutosSolucion { get; set; }
}

/// <summary>
/// Datos del WorkItem tipo Soporte que se crea al escalar. Titulo, Descripcion y
/// Prioridad se heredan del propio ticket (no se vuelven a capturar).
/// </summary>
public class EscalarTicketRequest
{
    public int IdProyecto { get; set; }
    public int? IdAsignado { get; set; }
    public DateTime? FechaCompromiso { get; set; }
}

public class EncuestaTicketRequest
{
    public int Calificacion { get; set; }
    public string? Comentario { get; set; }
}
