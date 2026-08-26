using GTE.Domain.ReglasNegocio;

namespace GTE.Domain.Interfaces;

/// <summary>Contrato de ESCRITURA del modulo Catalogo de reglas de negocio.</summary>
public interface IReglasNegocioRepository
{
    /// <summary>Crea la regla en version 1 y siembra su primera fila de tblReglaNegocioVersion.</summary>
    Task<int> CrearAsync(ReglaNegocioNueva datos, CancellationToken cancellationToken = default);

    Task<EstadoReglaNegocio?> ObtenerEstadoAsync(int idRegla, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza la regla y, SOLO si el enunciado o la justificacion cambiaron, sube
    /// VersionActual y agrega la fila nueva a tblReglaNegocioVersion -- renombrar o
    /// mover de ambito no ensucia el historial.
    /// </summary>
    Task ActualizarAsync(
        int idRegla, ReglaNegocioActualizacion datos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Derogacion: baja logica (Activo = 0) mas estado Derogada y FechaVigenciaHasta.
    /// Nunca borrado fisico -- una regla de negocio derogada sigue explicando decisiones
    /// pasadas. Sus impactos se dan de baja en el mismo movimiento.
    /// </summary>
    Task DerogarAsync(int idRegla, DateOnly fechaHasta, CancellationToken cancellationToken = default);

    Task ReactivarAsync(int idRegla, CancellationToken cancellationToken = default);

    /// <summary>Clave unica dentro del proyecto dueno; excluye la propia al editar.</summary>
    Task<bool> ExisteClaveAsync(
        int idProyecto, string clave, int? idReglaExcluir = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clave del proyecto (tblProyecto.Clave), que es la que forma la serie de folio de
    /// sus reglas. Null si el proyecto no existe.
    /// </summary>
    Task<string?> ObtenerClaveProyectoAsync(int idProyecto, CancellationToken cancellationToken = default);

    /* Impactos (proyectos secundarios) */

    Task<int> AgregarImpactoAsync(ImpactoReglaNuevo datos, CancellationToken cancellationToken = default);

    Task QuitarImpactoAsync(int idImpacto, CancellationToken cancellationToken = default);

    /// <summary>Proyecto dueno del impacto y regla a la que pertenece (para gates de permiso).</summary>
    Task<(int IdReglaNegocio, int IdProyectoDueno)?> ObtenerOrigenImpactoAsync(
        int idImpacto, CancellationToken cancellationToken = default);

    /* Ambitos (flujos de operacion y caracteristicas del sistema) */

    Task<int> CrearAmbitoAsync(AmbitoReglaNuevo datos, CancellationToken cancellationToken = default);

    Task ActualizarAmbitoAsync(
        int idAmbito, string nombre, string? descripcion, CancellationToken cancellationToken = default);

    /// <summary>Baja logica. Falla si alguna regla activa sigue apuntando al ambito.</summary>
    Task EliminarAmbitoAsync(int idAmbito, CancellationToken cancellationToken = default);

    Task<(int IdProyecto, int IdTipoAmbitoRegla)?> ObtenerProyectoAmbitoAsync(
        int idAmbito, CancellationToken cancellationToken = default);

    Task<bool> AmbitoTieneReglasAsync(int idAmbito, CancellationToken cancellationToken = default);
}
