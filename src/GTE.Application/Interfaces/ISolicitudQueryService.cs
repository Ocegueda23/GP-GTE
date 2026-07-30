using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Solicitudes;

namespace GTE.Application.Interfaces;

/// <summary>Filtro de la bandeja de triage. Sin estatus = pendientes de atender (Enviada, En Analisis, Aprobada).</summary>
public record FiltroTriage(int Page = 1, int PageSize = 25, IReadOnlyList<int>? Estatus = null, string? Texto = null);

public interface ISolicitudQueryService
{
    Task<PagedResult<SolicitudResponse>> ObtenerTriageAsync(FiltroTriage filtro, CancellationToken cancellationToken = default);

    /// <summary>Solicitudes del usuario actual (portal del solicitante).</summary>
    Task<IReadOnlyList<SolicitudResponse>> ObtenerMiasAsync(int idSolicitante, CancellationToken cancellationToken = default);

    Task<SolicitudResponse?> ObtenerPorIdAsync(int idSolicitud, CancellationToken cancellationToken = default);
}
