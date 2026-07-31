namespace GTE.Application.DTOs.Request.Planeacion;

public class SprintCrearRequest
{
    public int IdEquipo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Objetivo { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
}

public class CambiarEstatusSprintRequest
{
    public string Accion { get; set; } = string.Empty;

    /// <summary>Al CERRAR: "Backlog" (default) o "SiguienteSprint" (RN-PLA-02).</summary>
    public string? DestinoItemsAbiertos { get; set; }
}

/// <summary>Reordenamiento del backlog: los ids en el orden deseado.</summary>
public class ReordenarBacklogRequest
{
    public List<int> IdsEnOrden { get; set; } = [];
}

public class AsignarSprintRequest
{
    /// <summary>null = regresar el elemento al backlog.</summary>
    public int? IdSprint { get; set; }
}

/// <summary>Movimiento de tarjeta en el tablero: se traduce a una accion del workflow.</summary>
public class MoverTarjetaRequest
{
    public int IdEstatusDestino { get; set; }
}
