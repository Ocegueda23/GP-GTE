using GTE.Application.Interfaces;
using GTE.Domain.Workflow;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Lectura del editor de Workflows (P21): grafo (dbo.tblTransicion) + metadatos de UI
/// (dbo.tblTransicionConfig, unida en memoria por no tener FK real) por proceso.
/// </summary>
public class WorkflowQueryService(FabricaContexto fabrica) : IWorkflowQueryService
{
    public async Task<IReadOnlyList<ProcesoResumen>> ObtenerProcesosAsync(CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblProceso.AsNoTracking()
            .Where(p => p.Activo)
            .OrderBy(p => p.Proceso)
            .Select(p => new ProcesoResumen(p.IdProceso, p.Proceso))
            .ToListAsync(cancellationToken);
    }

    public async Task<DefinicionWorkflow?> ObtenerDefinicionAsync(
        string proceso, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var procesoEntidad = await contexto.TblProceso.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Proceso == proceso, cancellationToken);
        if (procesoEntidad is null)
        {
            return null;
        }

        var estatus = await ObtenerCatalogoEstatusAsync(contexto, proceso, cancellationToken);
        var descripcionPorId = estatus.ToDictionary(e => e.Id, e => e.Descripcion);

        var transiciones = await contexto.TblTransicion.AsNoTracking()
            .Where(t => t.IdProceso == procesoEntidad.IdProceso && t.Activo)
            .ToListAsync(cancellationToken);

        var configs = await contexto.TblTransicionConfig.AsNoTracking()
            .Where(c => c.Proceso == proceso && c.Activo)
            .ToListAsync(cancellationToken);
        var configPorClave = configs.ToDictionary(c => (c.IdEstatusOrigen, c.Accion));

        var resultado = transiciones
            .Select(t =>
            {
                configPorClave.TryGetValue((t.IdEstatusOrigen, t.Accion), out var cfg);
                return new TransicionWorkflow(
                    t.IdEstatusOrigen,
                    descripcionPorId.GetValueOrDefault(t.IdEstatusOrigen, $"#{t.IdEstatusOrigen}"),
                    t.Accion,
                    t.IdEstatusDestino,
                    descripcionPorId.GetValueOrDefault(t.IdEstatusDestino, $"#{t.IdEstatusDestino}"),
                    cfg?.EtiquetaBoton ?? t.Accion,
                    cfg?.RequierePermiso,
                    cfg?.RequiereMotivo ?? false,
                    cfg?.EsAccionPrincipal ?? false,
                    cfg?.Orden ?? 0);
            })
            .OrderBy(t => t.IdEstatusOrigen)
            .ThenBy(t => t.Orden)
            .ToList();

        return new DefinicionWorkflow(proceso, estatus, resultado);
    }

    public async Task<IReadOnlyList<PermisoResumen>> ObtenerPermisosAsync(CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblPermiso.AsNoTracking()
            .Where(p => p.Activo)
            .OrderBy(p => p.Modulo).ThenBy(p => p.Clave)
            .Select(p => new PermisoResumen(p.Clave, p.Modulo, p.Descripcion))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// No hay forma generica de leer "la tabla de catalogo de estatus de este proceso" sin
    /// SQL dinamico (prohibido, ver InterfloClaude.md seccion 7.4/regla dura "sin SQL
    /// interpolado") -- tblProceso.TablaEstatus es solo texto descriptivo. Mapeo explicito
    /// en vez de reflection: cada catalogo ya tiene su DbSet scaffoldeado.
    /// </summary>
    private static async Task<List<EstatusWorkflowItem>> ObtenerCatalogoEstatusAsync(
        DbContextGTE contexto, string proceso, CancellationToken cancellationToken)
    {
        return proceso switch
        {
            "WorkItem" => await contexto.TblEstatusWorkItem.AsNoTracking().OrderBy(e => e.Orden)
                .Select(e => new EstatusWorkflowItem(e.Id, e.Descripcion)).ToListAsync(cancellationToken),
            "Solicitud" => await contexto.TblEstatusSolicitud.AsNoTracking().OrderBy(e => e.Orden)
                .Select(e => new EstatusWorkflowItem(e.Id, e.Descripcion)).ToListAsync(cancellationToken),
            "Ticket" => await contexto.TblEstatusTicket.AsNoTracking().OrderBy(e => e.Orden)
                .Select(e => new EstatusWorkflowItem(e.Id, e.Descripcion)).ToListAsync(cancellationToken),
            "Release" => await contexto.TblEstatusRelease.AsNoTracking().OrderBy(e => e.Orden)
                .Select(e => new EstatusWorkflowItem(e.Id, e.Descripcion)).ToListAsync(cancellationToken),
            "Incidente" => await contexto.TblEstatusIncidente.AsNoTracking().OrderBy(e => e.Orden)
                .Select(e => new EstatusWorkflowItem(e.Id, e.Descripcion)).ToListAsync(cancellationToken),
            "Ausencia" => await contexto.TblEstatusAusencia.AsNoTracking().OrderBy(e => e.Orden)
                .Select(e => new EstatusWorkflowItem(e.Id, e.Descripcion)).ToListAsync(cancellationToken),
            "Riesgo" => await contexto.TblEstatusRiesgo.AsNoTracking().OrderBy(e => e.Orden)
                .Select(e => new EstatusWorkflowItem(e.Id, e.Descripcion)).ToListAsync(cancellationToken),
            "Sprint" => await contexto.TblEstatusSprint.AsNoTracking().OrderBy(e => e.Orden)
                .Select(e => new EstatusWorkflowItem(e.Id, e.Descripcion)).ToListAsync(cancellationToken),
            "Revision" => await contexto.TblEstatusRevision.AsNoTracking().OrderBy(e => e.Orden)
                .Select(e => new EstatusWorkflowItem(e.Id, e.Descripcion)).ToListAsync(cancellationToken),
            "Aprobacion" => await contexto.TblEstatusAprobacion.AsNoTracking().OrderBy(e => e.Orden)
                .Select(e => new EstatusWorkflowItem(e.Id, e.Descripcion)).ToListAsync(cancellationToken),
            "Proyecto" => await contexto.TblEstatusProyecto.AsNoTracking().OrderBy(e => e.Orden)
                .Select(e => new EstatusWorkflowItem(e.Id, e.Descripcion)).ToListAsync(cancellationToken),
            _ => []
        };
    }
}
