namespace GTE.Domain.Ausencias;

/// <summary>Datos para registrar una ausencia (el estatus inicial lo fija el backend).</summary>
public record AusenciaNueva(
    int IdUsuario,
    int IdTipoAusencia,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    string? Motivo);

/// <summary>Datos editables mientras la ausencia sigue en Solicitada.</summary>
public record AusenciaEdicion(
    int IdAusencia,
    int IdTipoAusencia,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    string? Motivo);

/// <summary>Estado minimo de una ausencia para evaluar reglas.</summary>
public record EstadoAusencia(
    int IdAusencia,
    int IdUsuario,
    int IdEstatus,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    bool Activo);

/// <summary>Ausencia vigente que se traslapa con el periodo pedido (detalle del 409).</summary>
public record TraslapeAusencia(
    int IdAusencia,
    string Tipo,
    string Estatus,
    DateOnly FechaInicio,
    DateOnly FechaFin);
