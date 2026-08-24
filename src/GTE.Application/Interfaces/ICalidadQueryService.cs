using GTE.Application.DTOs.Responses.Calidad;

namespace GTE.Application.Interfaces;

public interface ICalidadQueryService
{
    /// <summary>Catalogo de casos reutilizables de un proyecto, para el selector de "usar caso existente".</summary>
    Task<IReadOnlyList<CasoPruebaResponse>> ObtenerCasosDisponiblesAsync(
        int idProyecto, CancellationToken cancellationToken = default);

    /// <summary>Casos asignados a un WorkItem, con el resultado de su ultima ejecucion contra el.</summary>
    Task<IReadOnlyList<CasoAsignadoResponse>> ObtenerCasosAsignadosAsync(
        int idWorkItem, CancellationToken cancellationToken = default);

    /// <summary>Catalogo completo de casos de un proyecto (incluye retirados y no reutilizables),
    /// con el total de asignaciones activas de cada uno, para la pantalla de administracion.</summary>
    Task<IReadOnlyList<CasoAdminResponse>> ObtenerCatalogoCasosAsync(
        int idProyecto, CancellationToken cancellationToken = default);
}
