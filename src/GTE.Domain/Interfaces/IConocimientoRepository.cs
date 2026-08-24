using GTE.Domain.Conocimiento;

namespace GTE.Domain.Interfaces;

/// <summary>Contrato de ESCRITURA del modulo Base de conocimiento (P23).</summary>
public interface IConocimientoRepository
{
    /// <summary>Crea el articulo en version 1 y siembra su primera fila de tblArticuloVersion.</summary>
    Task<int> CrearAsync(ArticuloNuevo datos, CancellationToken cancellationToken = default);

    Task<EstadoArticulo?> ObtenerEstadoAsync(int idArticulo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza titulo/flags y, SOLO si el contenido cambio, sube VersionActual y agrega
    /// la fila nueva a tblArticuloVersion (el historial guarda todas las versiones,
    /// incluida la vigente).
    /// </summary>
    Task ActualizarAsync(int idArticulo, ArticuloActualizacion datos, CancellationToken cancellationToken = default);

    /// <summary>Baja logica (Activo = 0). El historial de versiones se conserva.</summary>
    Task EliminarAsync(int idArticulo, CancellationToken cancellationToken = default);

    /// <summary>Titulo unico (UQ_tblArticuloConocimiento_Titulo); excluye el propio al editar.</summary>
    Task<bool> ExisteTituloAsync(
        string titulo, int? idArticuloExcluir = null, CancellationToken cancellationToken = default);
}
