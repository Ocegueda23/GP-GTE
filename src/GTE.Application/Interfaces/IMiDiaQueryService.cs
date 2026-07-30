using GTE.Application.DTOs.Responses.MiDia;

namespace GTE.Application.Interfaces;

public interface IMiDiaQueryService
{
    Task<MiDiaResponse> ObtenerAsync(int idUsuario, string nombreUsuario, CancellationToken cancellationToken = default);
}
