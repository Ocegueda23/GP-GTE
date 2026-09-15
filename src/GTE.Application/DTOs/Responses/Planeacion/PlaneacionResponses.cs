using GTE.Application.DTOs.Responses.WorkItems;

namespace GTE.Application.DTOs.Responses.Planeacion;

public class SprintResponse
{
    public int IdSprint { get; set; }
    public string? Folio { get; set; }
    public int? IdLider { get; set; }
    public string? Lider { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Objetivo { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public int IdEstatus { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public string CreadoPor { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    /// <summary>Fecha real en que el sprint paso a Cerrado (historial de estatus), null si sigue abierto.</summary>
    public DateTime? FechaCierre { get; set; }
    public int TotalItems { get; set; }
    public int ItemsTerminados { get; set; }
    public decimal PuntosComprometidos { get; set; }
    public decimal PuntosTerminados { get; set; }
}

/// <summary>Capacidad del sprint calculada con el calendario laboral real y las ausencias aprobadas.</summary>
public class CapacidadSprintResponse
{
    public int IdSprint { get; set; }
    public decimal HorasCapacidad { get; set; }
    public decimal HorasComprometidas { get; set; }
    public IReadOnlyList<CapacidadPersonaResponse> Personas { get; set; } = [];
}

public class CapacidadPersonaResponse
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int DiasLaborables { get; set; }
    public int DiasAusente { get; set; }
    public decimal HorasPorDia { get; set; }
    public decimal HorasCapacidad { get; set; }
}

public class BacklogResponse
{
    public IReadOnlyList<BandejaItemResponse> Items { get; set; } = [];
    public decimal PuntosTotales { get; set; }
}

public class ColumnaTableroResponse
{
    public int IdTableroColumna { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int IdEstatusWorkItem { get; set; }
    public int Orden { get; set; }
    public int? LimiteWip { get; set; }
    public IReadOnlyList<BandejaItemResponse> Items { get; set; } = [];
}

public class TableroResponse
{
    public int? IdEquipo { get; set; }
    public string Equipo { get; set; } = string.Empty;
    public int? IdSprintActivo { get; set; }
    public string? SprintActivo { get; set; }
    public IReadOnlyList<ColumnaTableroResponse> Columnas { get; set; } = [];
}

/// <summary>Punto de la curva de burndown (puntos restantes por dia del sprint).</summary>
public class PuntoBurndownResponse
{
    public DateOnly Fecha { get; set; }
    public decimal PuntosRestantes { get; set; }
    public decimal PuntosIdeales { get; set; }
}
