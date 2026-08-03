using GTE.Application.DTOs.Responses.Costeo;

namespace GTE.Application.Interfaces;

public interface ICosteoQueryService
{
    Task<IReadOnlyList<TarifaNivelResponse>> ObtenerTarifasAsync(CancellationToken cancellationToken = default);
    Task<TarifaNivelResponse?> ObtenerTarifaAsync(int idTarifaNivel, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PresupuestoProyectoResponse>> ObtenerPresupuestosAsync(int idProyecto, CancellationToken cancellationToken = default);
    Task<PresupuestoProyectoResponse?> ObtenerPresupuestoAsync(int idPresupuestoProyecto, CancellationToken cancellationToken = default);

    /// <summary>Costo real (tblRegistroTiempo x tarifa vigente) vs presupuesto autorizado de un proyecto/anio.</summary>
    Task<CostoProyectoResponse> ObtenerCostoProyectoAsync(int idProyecto, int anio, CancellationToken cancellationToken = default);
}
