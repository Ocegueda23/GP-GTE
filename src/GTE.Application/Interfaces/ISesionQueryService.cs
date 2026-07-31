using GTE.Application.DTOs.Responses.Seguridad;

namespace GTE.Application.Interfaces;

public interface ISesionQueryService
{
    Task<SesionResponse?> ObtenerSesionAsync(int idUsuario, CancellationToken cancellationToken = default);
}
