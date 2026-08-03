using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Soporte;

namespace GTE.Application.Interfaces;

/// <summary>Filtro de la bandeja de mesa de ayuda. Sin estatus = abiertos (todos menos Cerrado).</summary>
public record FiltroBandejaTicket(
    int Page = 1, int PageSize = 25, IReadOnlyList<int>? Estatus = null,
    string? Texto = null, int? IdAsignado = null);

public interface ITicketQueryService
{
    Task<PagedResult<TicketResponse>> ObtenerBandejaAsync(FiltroBandejaTicket filtro, CancellationToken cancellationToken = default);

    /// <summary>Tickets del usuario actual (portal del solicitante).</summary>
    Task<IReadOnlyList<TicketResponse>> ObtenerMiosAsync(int idSolicitante, CancellationToken cancellationToken = default);

    Task<TicketResponse?> ObtenerPorIdAsync(int idTicket, CancellationToken cancellationToken = default);

    /// <summary>Detalle por folio (ruta /tickets/:folio de la SPA, mismo patron que WorkItem).</summary>
    Task<TicketResponse?> ObtenerPorFolioAsync(string folio, CancellationToken cancellationToken = default);
}
