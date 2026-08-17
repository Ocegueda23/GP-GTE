using GTE.Application.DTOs.Responses.IndicadoresEjecutivos;

namespace GTE.Application.Interfaces;

/// <summary>Dashboard Ejecutivo P18 (Doctos/GTE-DocumentoMaestro.md 3.10/5.10).</summary>
public interface IIndicadoresEjecutivosQueryService
{
    Task<IndicadoresEjecutivosResponse> ObtenerAsync(
        int idUsuarioActual, bool tieneAlcanceGlobal,
        int anio, int mes, int? idEquipo, int? idProyecto,
        CancellationToken cancellationToken = default);

    Task<string?> ObtenerLayoutAsync(int idUsuario, CancellationToken cancellationToken = default);
}
