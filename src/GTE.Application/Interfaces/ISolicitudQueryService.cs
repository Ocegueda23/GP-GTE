using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Solicitudes;

namespace GTE.Application.Interfaces;

/// <summary>Filtro de la bandeja de triage. Sin estatus = pendientes de atender (Enviada, En Analisis, Aprobada).</summary>
public record FiltroTriage(
    int Page = 1, int PageSize = 25, IReadOnlyList<int>? Estatus = null, string? Texto = null,
    string? OrdenarPor = null, bool OrdenDescendente = false);

public interface ISolicitudQueryService
{
    Task<PagedResult<SolicitudResponse>> ObtenerTriageAsync(FiltroTriage filtro, CancellationToken cancellationToken = default);

    /// <summary>Solicitudes del usuario actual (portal del solicitante).</summary>
    /// <summary>Sin estatus = pendientes (Enviada, En Analisis, Aprobada); [-1] = todas.</summary>
    Task<IReadOnlyList<SolicitudResponse>> ObtenerMiasAsync(
        int idSolicitante, IReadOnlyList<int>? estatus, CancellationToken cancellationToken = default);

    Task<SolicitudResponse?> ObtenerPorIdAsync(int idSolicitud, CancellationToken cancellationToken = default);

    /// <summary>Cuantas solicitudes (de cualquier proyecto/solicitante) esperan revision (Mi Dia).</summary>
    Task<int> ContarPendientesTriageAsync(CancellationToken cancellationToken = default);
}
