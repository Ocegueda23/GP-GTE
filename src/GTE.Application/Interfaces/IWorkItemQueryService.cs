using GTE.Application.Common;
using GTE.Application.DTOs.Responses.WorkItems;

namespace GTE.Application.Interfaces;

/// <summary>Filtros de la bandeja de trabajo (semantica heredada del GT).</summary>
public record FiltroBandeja(
    int Page = 1,
    int PageSize = 25,
    IReadOnlyList<int>? Estatus = null,   // null o vacio = abiertos (1-5); [-1] = todos
    int? IdProyecto = null,
    int? IdAsignado = null,
    int? IdTipoWorkItem = null,
    string? Texto = null,
    bool SoloVencidas = false);

/// <summary>Contrato de LECTURA del modulo WorkItems: proyecta directo a DTOs.</summary>
public interface IWorkItemQueryService
{
    Task<PagedResult<BandejaItemResponse>> ObtenerBandejaAsync(FiltroBandeja filtro, CancellationToken cancellationToken = default);

    Task<WorkItemResponse?> ObtenerPorIdAsync(int idWorkItem, CancellationToken cancellationToken = default);

    Task<WorkItemResponse?> ObtenerPorFolioAsync(string folio, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RegistroTiempoResponse>> ObtenerTiemposAsync(int idWorkItem, CancellationToken cancellationToken = default);
}
