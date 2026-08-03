using GTE.Domain.Costeo;

namespace GTE.Domain.Interfaces;

public interface ICosteoRepository
{
    /* ---------- Tarifas por nivel ---------- */
    Task<int> CrearTarifaNivelAsync(TarifaNivelNueva datos, CancellationToken cancellationToken = default);
    Task ActualizarTarifaNivelAsync(TarifaNivelEdicion datos, CancellationToken cancellationToken = default);
    Task RetirarTarifaNivelAsync(int idTarifaNivel, CancellationToken cancellationToken = default);

    /* ---------- Presupuesto por proyecto ---------- */
    Task<int> CrearPresupuestoAsync(PresupuestoProyectoNuevo datos, CancellationToken cancellationToken = default);
    Task ActualizarPresupuestoAsync(PresupuestoProyectoEdicion datos, CancellationToken cancellationToken = default);
    Task RetirarPresupuestoAsync(int idPresupuestoProyecto, CancellationToken cancellationToken = default);
}
