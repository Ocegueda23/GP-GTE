using GTE.Domain.NotasVersion;

namespace GTE.Domain.Interfaces;

public interface INotaVersionRepository
{
    /// <summary>Alta de la nota con su arbol de renglones. Devuelve el Id y los Ids de los renglones.</summary>
    Task<(int IdNotaVersion, IReadOnlyList<RenglonPersistido> Renglones)> CrearAsync(
        NotaVersionNueva datos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Guardado atomico del arbol (sync pattern): renglon sin Id = INSERT, con Id = UPDATE,
    /// ausente = se elimina. Los renglones son contenido editable de la nota, no historia, asi
    /// que la baja es fisica (hard delete) y no deja inactivos estorbando.
    /// </summary>
    Task<IReadOnlyList<RenglonPersistido>> ActualizarAsync(
        NotaVersionEdicion datos, CancellationToken cancellationToken = default);

    Task<EstadoNotaVersion?> ObtenerEstadoAsync(int idNotaVersion, CancellationToken cancellationToken = default);

    /// <summary>Baja logica de la nota completa (UQ sobre Version: ver ExisteVersionAsync).</summary>
    Task EliminarAsync(int idNotaVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida el UNIQUE de Version antes de escribir. Incluye las notas dadas de baja logica,
    /// porque el UNIQUE de la BD tampoco las distingue.
    /// </summary>
    Task<bool> ExisteVersionAsync(
        string version, int? idNotaVersionExcluir = null, CancellationToken cancellationToken = default);
}
