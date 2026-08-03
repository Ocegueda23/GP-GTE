namespace GTE.Application.DTOs.Request.Operacion;

public class IncidenteCrearRequest
{
    public int IdProyecto { get; set; }
    public int IdSeveridad { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTime FechaOcurrencia { get; set; }
    public DateTime? FechaDeteccion { get; set; }
}

/// <summary>Campos propios del incidente, fuera del flujo de estatus.</summary>
public class IncidenteActualizarRequest
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? CausaRaiz { get; set; }
    public int? MinutosIndisponibilidad { get; set; }
    public DateTime? FechaDeteccion { get; set; }
}

/// <summary>El frontend manda la ACCION; CERRAR valida causa raiz en S1/S2 en el backend.</summary>
public class CambiarEstatusIncidenteRequest
{
    public string Accion { get; set; } = string.Empty;
    public string? Motivo { get; set; }
}

/// <summary>RN-OPS-03: cambio de severidad exige motivo.</summary>
public class CambiarSeveridadIncidenteRequest
{
    public int IdSeveridad { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

/// <summary>Datos del WorkItem tipo Correccion que se crea al vincular el correctivo.</summary>
public class VincularCorrectivoRequest
{
    public int IdPrioridad { get; set; }
    public int? IdAsignado { get; set; }
    public DateTime? FechaCompromiso { get; set; }
}

public class VincularReleaseCausanteRequest
{
    public int IdRelease { get; set; }
}
