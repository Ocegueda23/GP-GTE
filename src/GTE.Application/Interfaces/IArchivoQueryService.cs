using GTE.Application.DTOs.Responses.Archivos;

namespace GTE.Application.Interfaces;

public interface IArchivoQueryService
{
    Task<IReadOnlyList<ArchivoResponse>> ObtenerPorEntidadAsync(
        string entidad, int idEntidad, CancellationToken cancellationToken = default);

    Task<ArchivoResponse?> ObtenerPorVinculoAsync(int idArchivoVinculo, CancellationToken cancellationToken = default);
}
