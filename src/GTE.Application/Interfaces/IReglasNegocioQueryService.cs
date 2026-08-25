using GTE.Application.DTOs.Request.ReglasNegocio;
using GTE.Application.DTOs.Responses.ReglasNegocio;

namespace GTE.Application.Interfaces;

/// <summary>
/// Contrato de LECTURA del Catalogo de reglas de negocio.
///
/// La consulta central es ObtenerCatalogoProyectoAsync: resuelve, para un proyecto, sus
/// reglas propias mas las que otros proyectos declararon que lo afectan. No hay
/// precedencias ni herencia por criterio que resolver -- una regla pertenece a un
/// proyecto y se propaga solo por la lista explicita de tblReglaNegocioImpacto.
/// </summary>
public interface IReglasNegocioQueryService
{
    Task<CatalogoReglasProyectoResponse?> ObtenerCatalogoProyectoAsync(
        int idProyecto, ReglasNegocioFiltroRequest filtro, CancellationToken cancellationToken = default);

    Task<ReglaNegocioResponse?> ObtenerPorIdAsync(int idRegla, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReglaVersionResponse>> ObtenerVersionesAsync(
        int idRegla, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busqueda global que cruza proyectos. El handler acota el resultado a los proyectos
    /// donde el usuario tiene alcance.
    /// </summary>
    Task<IReadOnlyList<ReglaNegocioResumenResponse>> BuscarAsync(
        ReglasNegocioFiltroRequest filtro, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AmbitoReglaResponse>> ObtenerAmbitosAsync(
        int idProyecto, CancellationToken cancellationToken = default);

    Task<CatalogosReglasNegocioResponse> ObtenerCatalogosAsync(CancellationToken cancellationToken = default);

    /// <summary>Proyectos que tienen al menos una regla propia o heredada (selector de la UI).</summary>
    Task<IReadOnlyList<(int IdProyecto, string Clave, string Nombre, int TotalReglas)>> ObtenerProyectosConReglasAsync(
        CancellationToken cancellationToken = default);
}
