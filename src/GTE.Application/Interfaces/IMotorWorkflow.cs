namespace GTE.Application.Interfaces;

/// <summary>Accion de workflow disponible para un registro en su estatus actual.</summary>
public record AccionDisponible(
    string Accion,
    string EtiquetaBoton,
    bool RequiereMotivo,
    bool EsAccionPrincipal,
    int Orden,
    string? ClavePermisoRequerida);

/// <summary>Resultado de ejecutar una accion de workflow.</summary>
public record ResultadoTransicion(
    int IdEstatusAnterior,
    int IdEstatusNuevo,
    string DescripcionEstatusNuevo);

/// <summary>Flecha del grafo: que accion lleva a que estatus destino.</summary>
public record TransicionDisponible(string Accion, int IdEstatusDestino);

/// <summary>
/// Motor de workflow propio de GTE. Unica puerta de cambio de estatus del sistema:
/// invoca dbo.spCambiarEstatus, que lee el grafo de dbo.tblProceso/dbo.tblTransicion
/// (todo en bdsGTE: independencia total, ADR-03) y materializa el historial con
/// minutos laborales. El frontend manda la accion, nunca el estatus destino.
/// </summary>
public interface IMotorWorkflow
{
    /// <param name="idHorario">Horario del responsable, para materializar los minutos
    /// laborales del intervalo que se cierra (null = no materializar).</param>
    Task<ResultadoTransicion> EjecutarAccionAsync(
        string proceso,
        int idRegistro,
        string accion,
        string? motivo = null,
        int? idHorario = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccionDisponible>> ObtenerAccionesAsync(
        string proceso,
        int idRegistro,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transiciones activas desde un estatus. Sirve para traducir un destino deseado
    /// (por ejemplo, soltar una tarjeta en otra columna del tablero) a la accion del grafo.
    /// </summary>
    Task<IReadOnlyList<TransicionDisponible>> ObtenerDestinosAsync(
        string proceso,
        int idEstatusOrigen,
        CancellationToken cancellationToken = default);
}
