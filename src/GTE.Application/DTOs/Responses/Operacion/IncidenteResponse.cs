namespace GTE.Application.DTOs.Responses.Operacion;

public class IncidenteResponse
{
    public int IdIncidente { get; set; }
    public string? Folio { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int IdProyecto { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public int IdSeveridad { get; set; }
    public string Severidad { get; set; } = string.Empty;

    /// <summary>Nulos en los incidentes registrados antes de que existiera el catalogo.</summary>
    public int? IdCategoriaIncidente { get; set; }
    public string? CategoriaIncidente { get; set; }
    public int IdEstatus { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public DateTime FechaOcurrencia { get; set; }
    public DateTime? FechaDeteccion { get; set; }
    public DateTime? FechaResolucion { get; set; }
    /// <summary>Minutos de caida que captura a mano quien atiende; null mientras no se capturen.</summary>
    public int? MinutosIndisponibilidad { get; set; }

    /// <summary>
    /// Tiempo de atencion medido por el sistema: minutos que el incidente lleva o llevo en
    /// estatus En Atencion, segun el historial de estatus. Corre en vivo mientras siga En
    /// Atencion. Es reloj corrido y no horas laborables (a diferencia de los tickets): un
    /// incidente no tiene SLA ni horario asociado, y una caida corre 24x7. Null si nunca se
    /// inicio la atencion.
    /// </summary>
    public int? MinutosAtencion { get; set; }

    /// <summary>true mientras el incidente siga En Atencion: MinutosAtencion sigue creciendo.</summary>
    public bool AtencionEnCurso { get; set; }
    public string? CausaRaiz { get; set; }
    public int? IdWorkItemCorrectivo { get; set; }
    public string? FolioWorkItemCorrectivo { get; set; }
    public int? IdReleaseCausante { get; set; }
    public string? VersionReleaseCausante { get; set; }
    public DateTime FechaRegistro { get; set; }
}

public class VincularCorrectivoResponse
{
    public int IdWorkItem { get; set; }
    public string Folio { get; set; } = string.Empty;
}
