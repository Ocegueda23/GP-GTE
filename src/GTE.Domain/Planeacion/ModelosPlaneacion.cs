namespace GTE.Domain.Planeacion;

public record SprintNuevo(
    string Nombre,
    string? Objetivo,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    int? IdLider);

public record SprintEdicion(
    string Nombre,
    string? Objetivo,
    DateOnly FechaInicio,
    DateOnly FechaFin);

public record EstadoSprint(
    int IdSprint,
    int? IdLider,
    string Nombre,
    int IdEstatus,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    bool Activo);

/// <summary>Miembro del equipo con su horario y dedicacion, para calcular capacidad.</summary>
public record MiembroEquipo(
    int IdUsuario,
    string Nombre,
    int? IdHorario,
    decimal PorcentajeDedicacion);

/// <summary>Ausencia aprobada que descuenta capacidad.</summary>
public record AusenciaAprobada(int IdUsuario, DateOnly FechaInicio, DateOnly FechaFin);

/// <summary>Columna de tablero con su mapeo a estatus y limite de trabajo en curso.</summary>
public record ColumnaTablero(
    int IdTableroColumna,
    string Nombre,
    int IdEstatusWorkItem,
    int Orden,
    int? LimiteWip);

/// <summary>
/// Mapeo estandar de columnas de tablero (estatus abiertos + Terminado), unica fuente de
/// verdad: lo usa PlaneacionRepository para aprovisionar el tablero de un equipo nuevo y
/// PlaneacionQueryService para la vista consolidada "todos los equipos" (que no tiene un
/// TblTablero propio del que leer columnas).
/// Suspendido va entre Correccion y Terminado: no es una etapa del flujo, es el apartado
/// donde se ven los elementos detenidos (sin columna propia desaparecian del tablero).
/// </summary>
public static class ColumnasTableroEstandar
{
    public static readonly IReadOnlyList<(string Nombre, int IdEstatus, int Orden, int? Wip)> Columnas =
    [
        ("Pendiente",  WorkItems.EstatusWorkItem.Pendiente,  1, null),
        ("En proceso", WorkItems.EstatusWorkItem.EnProceso,  2, 5),
        ("En pruebas", WorkItems.EstatusWorkItem.EnPruebas,  3, 5),
        ("Correccion", WorkItems.EstatusWorkItem.Correccion, 4, null),
        ("Suspendido", WorkItems.EstatusWorkItem.Suspendido, 5, null),
        ("Terminado",  WorkItems.EstatusWorkItem.Terminado,  6, null)
    ];
}
