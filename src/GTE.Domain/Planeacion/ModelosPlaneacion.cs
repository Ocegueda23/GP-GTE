namespace GTE.Domain.Planeacion;

public record SprintNuevo(
    int IdEquipo,
    string Nombre,
    string? Objetivo,
    DateOnly FechaInicio,
    DateOnly FechaFin);

public record EstadoSprint(
    int IdSprint,
    int IdEquipo,
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
