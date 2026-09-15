using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Ausencias;

namespace GTE.Application.Interfaces;

/// <summary>
/// Filtro de la bandeja de ausencias. Sin estatus = pendientes de resolver (Solicitada);
/// [-1] = todas. El periodo filtra por traslape, no por fecha exacta de inicio.
/// </summary>
public record FiltroAusencias(
    int Page = 1,
    int PageSize = 25,
    IReadOnlyList<int>? Estatus = null,
    int? IdUsuario = null,
    int? IdTipoAusencia = null,
    DateOnly? Desde = null,
    DateOnly? Hasta = null,
    string? OrdenarPor = null,
    bool OrdenDescendente = false);

public interface IAusenciaQueryService
{
    /// <summary>Bandeja del aprobador (todas las personas).</summary>
    Task<PagedResult<AusenciaResponse>> ObtenerBandejaAsync(
        FiltroAusencias filtro, CancellationToken cancellationToken = default);

    /// <summary>Ausencias del usuario actual. Sin estatus = vigentes (Solicitada, Aprobada); [-1] = todas.</summary>
    Task<IReadOnlyList<AusenciaResponse>> ObtenerMiasAsync(
        int idUsuario, IReadOnlyList<int>? estatus, CancellationToken cancellationToken = default);

    Task<AusenciaResponse?> ObtenerPorIdAsync(int idAusencia, CancellationToken cancellationToken = default);

    /// <summary>Cuantas ausencias esperan aprobacion (badge de la bandeja).</summary>
    Task<int> ContarPendientesAsync(CancellationToken cancellationToken = default);

    Task<CatalogosAusenciaResponse> ObtenerCatalogosAsync(CancellationToken cancellationToken = default);
}
