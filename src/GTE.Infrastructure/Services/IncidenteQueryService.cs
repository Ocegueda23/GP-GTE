using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Operacion;
using GTE.Application.Interfaces;
using GTE.Domain.Operacion;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class IncidenteQueryService(FabricaContexto fabrica) : IIncidenteQueryService
{
    public async Task<PagedResult<IncidenteResponse>> ObtenerBandejaAsync(
        FiltroBandejaIncidente filtro, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var consulta = Proyectar(contexto);

        if (filtro.Estatus is null || filtro.Estatus.Count == 0)
        {
            consulta = consulta.Where(i => i.IdEstatus != EstatusIncidente.Cerrado);
        }
        else if (!filtro.Estatus.Contains(-1))
        {
            var estatus = filtro.Estatus.ToArray();
            consulta = consulta.Where(i => estatus.Contains(i.IdEstatus));
        }

        if (filtro.IdSeveridad.HasValue)
        {
            consulta = consulta.Where(i => i.IdSeveridad == filtro.IdSeveridad.Value);
        }

        if (filtro.IdProyecto.HasValue)
        {
            consulta = consulta.Where(i => i.IdProyecto == filtro.IdProyecto.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();
            consulta = consulta.Where(i =>
                (i.Folio != null && i.Folio.Contains(texto)) || i.Titulo.Contains(texto));
        }

        var total = await consulta.CountAsync(cancellationToken);
        var page = Math.Max(1, filtro.Page);
        var pageSize = Math.Clamp(filtro.PageSize, 1, 200);

        var ordenada = filtro.OrdenarPor switch
        {
            "folio" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(i => i.Folio)
                : consulta.OrderBy(i => i.Folio),
            "titulo" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(i => i.Titulo)
                : consulta.OrderBy(i => i.Titulo),
            "proyecto" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(i => i.Proyecto)
                : consulta.OrderBy(i => i.Proyecto),
            "categoria" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(i => i.CategoriaIncidente)
                : consulta.OrderBy(i => i.CategoriaIncidente),
            "severidad" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(i => i.IdSeveridad)
                : consulta.OrderBy(i => i.IdSeveridad),
            "estatus" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(i => i.IdEstatus)
                : consulta.OrderBy(i => i.IdEstatus),
            "fechaOcurrencia" => filtro.OrdenDescendente
                ? consulta.OrderByDescending(i => i.FechaOcurrencia)
                : consulta.OrderBy(i => i.FechaOcurrencia),
            "fechaResolucion" => filtro.OrdenDescendente
                ? consulta.OrderBy(i => i.FechaResolucion == null).ThenByDescending(i => i.FechaResolucion)
                : consulta.OrderBy(i => i.FechaResolucion == null).ThenBy(i => i.FechaResolucion),
            _ => consulta
                .OrderBy(i => i.IdSeveridad)
                .ThenByDescending(i => i.FechaOcurrencia)
        };

        var items = await ordenada
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        await RellenarTiempoAtencionAsync(contexto, items, cancellationToken);

        return new PagedResult<IncidenteResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<IncidenteResponse?> ObtenerPorFolioAsync(
        string folio, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var incidente = await Proyectar(contexto)
            .FirstOrDefaultAsync(i => i.Folio == folio, cancellationToken);

        if (incidente is not null) await RellenarTiempoAtencionAsync(contexto, [incidente], cancellationToken);
        return incidente;
    }

    public async Task<IncidenteResponse?> ObtenerPorIdAsync(
        int idIncidente, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var incidente = await Proyectar(contexto)
            .FirstOrDefaultAsync(i => i.IdIncidente == idIncidente, cancellationToken);

        if (incidente is not null) await RellenarTiempoAtencionAsync(contexto, [incidente], cancellationToken);
        return incidente;
    }

    public async Task<IReadOnlyList<IncidenteResponse>> ObtenerRelevantesAsync(
        int idUsuario, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var idsProyectosResponsable = await contexto.TblProyecto.AsNoTracking()
            .Where(p => p.IdResponsable == idUsuario)
            .Select(p => p.IdProyecto)
            .ToListAsync(cancellationToken);

        var items = await Proyectar(contexto)
            .Where(i => i.IdEstatus != EstatusIncidente.Cerrado && idsProyectosResponsable.Contains(i.IdProyecto))
            .OrderBy(i => i.IdSeveridad)
            .ThenByDescending(i => i.FechaOcurrencia)
            .ToListAsync(cancellationToken);

        await RellenarTiempoAtencionAsync(contexto, items, cancellationToken);
        return items;
    }

    /// <summary>
    /// Llena MinutosAtencion con los intervalos En Atencion de dbo.tblHistorialEstatus,
    /// incluyendo el que sigue abierto (por eso un incidente en atencion ya no marca cero).
    /// Reloj corrido: un incidente no trae SLA ni horario contra el cual medir horas
    /// laborables, y una caida de servicio no se detiene al terminar la jornada.
    /// </summary>
    private static async Task RellenarTiempoAtencionAsync(
        DbContextGTE contexto, IReadOnlyList<IncidenteResponse> incidentes, CancellationToken cancellationToken)
    {
        if (incidentes.Count == 0)
        {
            return;
        }

        var ids = incidentes.Select(i => i.IdIncidente).ToList();
        var intervalos = await contexto.TblHistorialEstatus.AsNoTracking()
            .Where(h => h.Proceso == ProcesoIncidente
                        && ids.Contains(h.IdRegistro)
                        && h.IdEstatus == EstatusIncidente.EnAtencion)
            .Select(h => new { h.IdRegistro, h.FechaInicio, h.FechaFin })
            .ToListAsync(cancellationToken);

        if (intervalos.Count == 0)
        {
            return;
        }

        var ahora = DateTime.Now;
        var acumulado = new Dictionary<int, double>();
        var abiertos = new HashSet<int>();
        foreach (var intervalo in intervalos)
        {
            var fin = intervalo.FechaFin ?? ahora;
            if (fin > intervalo.FechaInicio)
            {
                acumulado[intervalo.IdRegistro] = acumulado.GetValueOrDefault(intervalo.IdRegistro)
                    + (fin - intervalo.FechaInicio).TotalMinutes;
            }
            if (intervalo.FechaFin is null) abiertos.Add(intervalo.IdRegistro);
        }

        foreach (var incidente in incidentes)
        {
            if (!acumulado.TryGetValue(incidente.IdIncidente, out var total)) continue;
            incidente.MinutosAtencion = (int)Math.Round(total);
            incidente.AtencionEnCurso = abiertos.Contains(incidente.IdIncidente);
        }
    }

    /// <summary>Nombre del proceso en dbo.tblProceso / dbo.tblHistorialEstatus.</summary>
    private const string ProcesoIncidente = "Incidente";

    private static IQueryable<IncidenteResponse> Proyectar(DbContextGTE contexto)
    {
        return from i in contexto.TblIncidente.AsNoTracking()
               join e in contexto.TblEstatusIncidente.AsNoTracking() on i.IdEstatusIncidente equals e.Id
               join s in contexto.TblSeveridad.AsNoTracking() on i.IdSeveridad equals s.Id
               join p in contexto.TblProyecto.AsNoTracking() on i.IdProyecto equals p.IdProyecto
               // Left join: los incidentes anteriores al catalogo no tienen categoria.
               join cat in contexto.TblCategoriaIncidente.AsNoTracking() on i.IdCategoriaIncidente equals cat.IdCategoriaIncidente into categorias
               from cat in categorias.DefaultIfEmpty()
               join wi in contexto.TblWorkItem.AsNoTracking() on i.IdWorkItemCorrectivo equals wi.IdWorkItem into workitems
               from wi in workitems.DefaultIfEmpty()
               join rel in contexto.TblRelease.AsNoTracking() on i.IdReleaseCausante equals rel.IdRelease into releases
               from rel in releases.DefaultIfEmpty()
               where i.Activo
               select new IncidenteResponse
               {
                   IdIncidente = i.IdIncidente,
                   Folio = i.Folio,
                   Titulo = i.Titulo,
                   Descripcion = i.Descripcion,
                   IdProyecto = i.IdProyecto,
                   Proyecto = p.Nombre,
                   IdSeveridad = i.IdSeveridad,
                   Severidad = s.Nombre,
                   IdCategoriaIncidente = i.IdCategoriaIncidente,
                   CategoriaIncidente = cat != null ? cat.Nombre : null,
                   IdEstatus = i.IdEstatusIncidente,
                   Estatus = e.Descripcion,
                   FechaOcurrencia = i.FechaOcurrencia,
                   FechaDeteccion = i.FechaDeteccion,
                   FechaResolucion = i.FechaResolucion,
                   MinutosIndisponibilidad = i.MinutosIndisponibilidad,
                   CausaRaiz = i.CausaRaiz,
                   IdWorkItemCorrectivo = i.IdWorkItemCorrectivo,
                   FolioWorkItemCorrectivo = wi != null ? wi.Folio : null,
                   IdReleaseCausante = i.IdReleaseCausante,
                   VersionReleaseCausante = rel != null ? rel.Version : null,
                   FechaRegistro = i.FechaRegistro
               };
    }
}
