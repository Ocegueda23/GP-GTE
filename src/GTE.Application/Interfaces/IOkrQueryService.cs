using GTE.Application.DTOs.Responses.Okr;

namespace GTE.Application.Interfaces;

public interface IOkrQueryService
{
    Task<IReadOnlyList<ObjetivoOkrResponse>> ObtenerObjetivosAsync(
        int? idProyecto, int? idEquipo, int? anio, CancellationToken cancellationToken = default);

    Task<ObjetivoOkrResponse?> ObtenerObjetivoAsync(int idObjetivoOkr, CancellationToken cancellationToken = default);
}
