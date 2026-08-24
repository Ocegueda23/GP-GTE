using GTE.Application.DTOs.Responses.MiDia;
using GTE.Application.Interfaces;
using GTE.Domain.Solicitudes;
using GTE.Domain.WorkItems;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Arma la vista personal del dia con una sola consulta a la bandeja del usuario,
/// clasificada en memoria (el volumen por persona es de decenas de filas).
/// </summary>
public class MiDiaQueryService(
    FabricaContexto fabrica,
    ITicketQueryService tickets,
    IIncidenteQueryService incidentes,
    ISolicitudQueryService solicitudes,
    IEntregaQueryService entregas) : IMiDiaQueryService
{
    private static readonly int[] EstatusAbiertos =
    [
        EstatusWorkItem.Pendiente, EstatusWorkItem.EnProceso, EstatusWorkItem.EnPruebas,
        EstatusWorkItem.Correccion, EstatusWorkItem.Suspendido
    ];

    private static readonly int[] EstatusSolicitudPendiente =
    [
        EstatusSolicitud.Enviada, EstatusSolicitud.EnAnalisis, EstatusSolicitud.Aprobada
    ];

    public async Task<MiDiaResponse> ObtenerAsync(
        int idUsuario, string nombreUsuario, bool puedeTriage, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var abiertos = await contexto.VwBandejaTrabajo.AsNoTracking()
            .Where(v => v.IdAsignado == idUsuario && EstatusAbiertos.Contains(v.IdEstatusWorkItem))
            .OrderBy(v => v.FechaCompromiso == null)
            .ThenBy(v => v.FechaCompromiso)
            .Select(v => new MiDiaItemResponse
            {
                IdWorkItem = v.IdWorkItem,
                Folio = v.Folio,
                Tipo = v.Tipo,
                Titulo = v.Titulo,
                ClaveProyecto = v.ClaveProyecto,
                Proyecto = v.Proyecto,
                IdEstatus = v.IdEstatusWorkItem,
                Estatus = v.Estatus,
                Prioridad = v.Prioridad,
                Complejidad = v.Complejidad,
                Asignado = v.Asignado,
                FechaCompromiso = v.FechaCompromiso,
                EsVencida = v.EsVencida == true,
                PuntosHistoria = v.PuntosHistoria,
                MinutosPresupuesto = v.MinutosPresupuesto,
                MinutosInvertidos = v.MinutosInvertidos,
                RevisionesPendientes = v.RevisionesPendientes ?? 0
            })
            .ToListAsync(cancellationToken);

        // La accion que lleva a En Proceso la dicta el grafo (INICIAR desde Pendiente o
        // Correccion, REANUDAR desde Suspendido): una sola consulta, sin N+1.
        var accionesInicio = await (
            from t in contexto.TblTransicion.AsNoTracking()
            join p in contexto.TblProceso.AsNoTracking() on t.IdProceso equals p.IdProceso
            join c in contexto.TblTransicionConfig.AsNoTracking()
                    .Where(c => c.Proceso == "WorkItem" && c.Activo)
                on new { t.IdEstatusOrigen, t.Accion } equals new { c.IdEstatusOrigen, c.Accion }
                into configs
            from c in configs.DefaultIfEmpty()
            where p.Proceso == "WorkItem" && p.Activo && t.Activo
                  && t.IdEstatusDestino == EstatusWorkItem.EnProceso
            select new { t.IdEstatusOrigen, t.Accion, Etiqueta = c != null ? c.EtiquetaBoton : null }
            ).ToDictionaryAsync(x => x.IdEstatusOrigen, x => x, cancellationToken);

        foreach (var item in abiertos)
        {
            if (accionesInicio.TryGetValue(item.IdEstatus, out var accion))
            {
                item.AccionInicio = accion.Accion;
                item.EtiquetaAccionInicio = accion.Etiqueta
                    ?? (accion.Accion == "REANUDAR" ? "Reanudar" : "Iniciar");
            }
        }

        var hoy = DateTime.Today;
        var enProceso = abiertos.FirstOrDefault(i => i.IdEstatus == EstatusWorkItem.EnProceso);
        var resto = abiertos.Where(i => i.IdWorkItem != enProceso?.IdWorkItem).ToList();

        var minutosHoy = await contexto.TblRegistroTiempo.AsNoTracking()
            .Where(t => t.IdUsuario == idUsuario && t.Fecha == DateOnly.FromDateTime(hoy) && t.Activo)
            .SumAsync(t => (int?)t.Minutos, cancellationToken) ?? 0;

        // Modulos adicionales: cada uno reutiliza el QueryService que ya existe para su
        // propia bandeja, sin duplicar la logica de proyeccion/filtro.
        var ticketsAsignados = await tickets.ObtenerBandejaAsync(
            new FiltroBandejaTicket(PageSize: 50, IdAsignado: idUsuario), cancellationToken);
        var incidentesRelevantes = await incidentes.ObtenerRelevantesAsync(idUsuario, cancellationToken);
        var solicitudesPendientes = (await solicitudes.ObtenerMiasAsync(idUsuario, null, cancellationToken))
            .Where(s => EstatusSolicitudPendiente.Contains(s.IdEstatus))
            .ToList();
        var triagePendientes = puedeTriage
            ? await solicitudes.ContarPendientesTriageAsync(cancellationToken)
            : 0;
        var releasesRelevantes = await entregas.ObtenerRelevantesAsync(idUsuario, cancellationToken);

        return new MiDiaResponse
        {
            Usuario = nombreUsuario,
            Fecha = hoy,
            EnProceso = enProceso,
            Vencidas = resto.Where(i => i.FechaCompromiso.HasValue
                                        && i.FechaCompromiso.Value.Date < hoy).ToList(),
            ParaHoy = resto.Where(i => i.FechaCompromiso.HasValue
                                       && i.FechaCompromiso.Value.Date == hoy).ToList(),
            Proximas = resto.Where(i => !i.FechaCompromiso.HasValue
                                        || (i.FechaCompromiso.Value.Date > hoy
                                            && i.FechaCompromiso.Value.Date <= hoy.AddDays(7))).ToList(),
            MinutosHoy = minutosHoy,
            TotalAbiertos = abiertos.Count,
            TicketsAsignados = ticketsAsignados.Items,
            IncidentesRelevantes = incidentesRelevantes,
            SolicitudesPendientes = solicitudesPendientes,
            TriagePendientes = triagePendientes,
            ReleasesRelevantes = releasesRelevantes
        };
    }
}
