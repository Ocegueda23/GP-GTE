using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Operacion;

namespace GTE.Application.Interfaces;

/// <summary>Filtro de la bandeja de incidentes. Sin estatus = abiertos (todos menos Cerrado).</summary>
public record FiltroBandejaIncidente(
    int Page = 1, int PageSize = 25, IReadOnlyList<int>? Estatus = null,
    int? IdSeveridad = null, int? IdProyecto = null, string? Texto = null);

public interface IIncidenteQueryService
{
    Task<PagedResult<IncidenteResponse>> ObtenerBandejaAsync(FiltroBandejaIncidente filtro, CancellationToken cancellationToken = default);

    /// <summary>Detalle por folio (ruta /operacion/incidentes/:folio de la SPA).</summary>
    Task<IncidenteResponse?> ObtenerPorFolioAsync(string folio, CancellationToken cancellationToken = default);

    Task<IncidenteResponse?> ObtenerPorIdAsync(int idIncidente, CancellationToken cancellationToken = default);
}
