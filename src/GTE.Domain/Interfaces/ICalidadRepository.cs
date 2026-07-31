using GTE.Domain.Calidad;

namespace GTE.Domain.Interfaces;

/// <summary>Contrato de ESCRITURA del modulo Calidad (QA).</summary>
public interface ICalidadRepository
{
    Task<int> CrearPlanAsync(PlanPruebaNuevo datos, CancellationToken cancellationToken = default);

    Task<EstadoPlan?> ObtenerEstadoPlanAsync(int idPlanPrueba, CancellationToken cancellationToken = default);

    Task<int> CrearCasoAsync(CasoPruebaNuevo datos, CancellationToken cancellationToken = default);

    Task<EstadoCaso?> ObtenerEstadoCasoAsync(int idCasoPrueba, CancellationToken cancellationToken = default);

    Task<int> CrearCicloAsync(CicloPruebaNuevo datos, CancellationToken cancellationToken = default);

    Task<bool> ExisteCicloEnPlanAsync(int idCicloPrueba, int idPlanPrueba, CancellationToken cancellationToken = default);

    Task<int> RegistrarEjecucionAsync(EjecucionNueva datos, CancellationToken cancellationToken = default);

    Task<EstadoEjecucion?> ObtenerEstadoEjecucionAsync(int idEjecucion, CancellationToken cancellationToken = default);

    /// <summary>Vincula el bug creado desde una ejecucion fallida.</summary>
    Task VincularBugAsync(int idEjecucion, int idWorkItemBug, CancellationToken cancellationToken = default);

    /// <summary>Bug ya reportado desde esa ejecucion (evita duplicados).</summary>
    Task<int?> ObtenerBugDeEjecucionAsync(int idEjecucion, CancellationToken cancellationToken = default);
}
