using GTE.Application.DTOs.Responses.Calidad;
using GTE.Application.Interfaces;
using GTE.Domain.Calidad;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class CalidadQueryService(FabricaContexto fabrica) : ICalidadQueryService
{
    public async Task<IReadOnlyList<PlanPruebaResponse>> ObtenerPlanesAsync(
        int? idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var consulta = Proyectar(contexto);
        if (idProyecto.HasValue)
        {
            consulta = consulta.Where(p => p.IdProyecto == idProyecto.Value);
        }

        return await consulta.OrderByDescending(p => p.IdPlanPrueba).ToListAsync(cancellationToken);
    }

    public async Task<PlanPruebaResponse?> ObtenerPlanAsync(
        int idPlanPrueba, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await Proyectar(contexto)
            .FirstOrDefaultAsync(p => p.IdPlanPrueba == idPlanPrueba, cancellationToken);
    }

    public async Task<IReadOnlyList<CicloPruebaResponse>> ObtenerCiclosAsync(
        int idPlanPrueba, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var totalCasos = await contexto.TblCasoPrueba.AsNoTracking()
            .CountAsync(c => c.IdPlanPrueba == idPlanPrueba && c.Activo, cancellationToken);

        var ciclos = await contexto.TblCicloPrueba.AsNoTracking()
            .Where(c => c.IdPlanPrueba == idPlanPrueba && c.Activo)
            .OrderByDescending(c => c.IdCicloPrueba)
            .Select(c => new { c.IdCicloPrueba, c.IdPlanPrueba, c.Nombre, c.FechaInicio, c.FechaFin })
            .ToListAsync(cancellationToken);

        var resultado = new List<CicloPruebaResponse>();
        foreach (var ciclo in ciclos)
        {
            // Ultima ejecucion por caso dentro del ciclo
            var ultimas = await contexto.TblEjecucionPrueba.AsNoTracking()
                .Where(e => e.IdCicloPrueba == ciclo.IdCicloPrueba)
                .GroupBy(e => e.IdCasoPrueba)
                .Select(g => g.OrderByDescending(e => e.IdEjecucionPrueba).First().IdResultadoPrueba)
                .ToListAsync(cancellationToken);

            resultado.Add(new CicloPruebaResponse
            {
                IdCicloPrueba = ciclo.IdCicloPrueba,
                IdPlanPrueba = ciclo.IdPlanPrueba,
                Nombre = ciclo.Nombre,
                FechaInicio = ciclo.FechaInicio,
                FechaFin = ciclo.FechaFin,
                TotalCasos = totalCasos,
                Ejecutados = ultimas.Count,
                Pasa = ultimas.Count(r => r == ResultadoPrueba.Pasa),
                Falla = ultimas.Count(r => r == ResultadoPrueba.Falla),
                Bloqueado = ultimas.Count(r => r == ResultadoPrueba.Bloqueado)
            });
        }
        return resultado;
    }

    public async Task<IReadOnlyList<CasoPruebaResponse>> ObtenerCasosAsync(
        int idPlanPrueba, int? idCicloPrueba, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var casos = await (
            from c in contexto.TblCasoPrueba.AsNoTracking()
            join t in contexto.TblTipoPrueba.AsNoTracking() on c.IdTipoPrueba equals t.Id
            join w in contexto.TblWorkItem.AsNoTracking() on c.IdWorkItem equals w.IdWorkItem into items
            from w in items.DefaultIfEmpty()
            where c.IdPlanPrueba == idPlanPrueba && c.Activo
            orderby c.IdCasoPrueba
            select new CasoPruebaResponse
            {
                IdCasoPrueba = c.IdCasoPrueba,
                Folio = c.Folio,
                IdPlanPrueba = c.IdPlanPrueba,
                Titulo = c.Titulo,
                Precondiciones = c.Precondiciones,
                ResultadoEsperado = c.ResultadoEsperado,
                TipoPrueba = t.Nombre,
                IdWorkItem = c.IdWorkItem,
                FolioWorkItem = w != null ? w.Folio : null
            }).ToListAsync(cancellationToken);

        if (casos.Count == 0)
        {
            return casos;
        }

        var ids = casos.Select(c => c.IdCasoPrueba).ToList();

        var pasos = await contexto.TblCasoPruebaPaso.AsNoTracking()
            .Where(p => ids.Contains(p.IdCasoPrueba))
            .OrderBy(p => p.NumeroPaso)
            .Select(p => new { p.IdCasoPrueba, Paso = new PasoCasoResponse
            {
                NumeroPaso = p.NumeroPaso,
                Accion = p.Accion,
                ResultadoEsperado = p.ResultadoEsperado
            } })
            .ToListAsync(cancellationToken);

        // Ultima ejecucion de cada caso (del ciclo indicado, si se pidio uno)
        var ejecuciones = await (
            from e in contexto.TblEjecucionPrueba.AsNoTracking()
            join r in contexto.TblResultadoPrueba.AsNoTracking() on e.IdResultadoPrueba equals r.Id
            where ids.Contains(e.IdCasoPrueba)
                  && (idCicloPrueba == null || e.IdCicloPrueba == idCicloPrueba)
            select new { e.IdEjecucionPrueba, e.IdCasoPrueba, e.IdResultadoPrueba, Resultado = r.Nombre }
            ).ToListAsync(cancellationToken);

        var bugs = await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.IdEjecucionPruebaOrigen != null && w.Activo)
            .Select(w => new { IdEjecucion = w.IdEjecucionPruebaOrigen!.Value, w.Folio })
            .ToListAsync(cancellationToken);

        foreach (var caso in casos)
        {
            caso.Pasos = pasos.Where(p => p.IdCasoPrueba == caso.IdCasoPrueba)
                .Select(p => p.Paso).ToList();

            var ultima = ejecuciones
                .Where(e => e.IdCasoPrueba == caso.IdCasoPrueba)
                .OrderByDescending(e => e.IdEjecucionPrueba)
                .FirstOrDefault();
            if (ultima is not null)
            {
                caso.IdEjecucion = ultima.IdEjecucionPrueba;
                caso.IdUltimoResultado = ultima.IdResultadoPrueba;
                caso.UltimoResultado = ultima.Resultado;
                caso.FolioBug = bugs.FirstOrDefault(b => b.IdEjecucion == ultima.IdEjecucionPrueba)?.Folio;
            }
        }

        return casos;
    }

    public async Task<IReadOnlyList<TrazabilidadResponse>> ObtenerTrazabilidadAsync(
        int idPlanPrueba, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var plan = await contexto.TblPlanPrueba.AsNoTracking()
            .Where(p => p.IdPlanPrueba == idPlanPrueba)
            .Select(p => new { p.IdProyecto, p.IdRelease })
            .FirstOrDefaultAsync(cancellationToken);
        if (plan is null)
        {
            return [];
        }

        // Requisitos del proyecto (o del release si el plan esta ligado a uno)
        var requisitos = await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.IdProyecto == plan.IdProyecto && w.Activo
                        && (plan.IdRelease == null || w.IdRelease == plan.IdRelease)
                        && (w.IdTipoWorkItem == 3 || w.IdTipoWorkItem == 2))   // Historia o Feature
            .Select(w => new { w.IdWorkItem, w.Folio, w.Titulo })
            .ToListAsync(cancellationToken);

        var cobertura = await (
            from c in contexto.TblCasoPrueba.AsNoTracking()
            where c.IdPlanPrueba == idPlanPrueba && c.Activo && c.IdWorkItem != null
            select new
            {
                IdWorkItem = c.IdWorkItem!.Value,
                c.IdCasoPrueba,
                UltimoResultado = contexto.TblEjecucionPrueba
                    .Where(e => e.IdCasoPrueba == c.IdCasoPrueba)
                    .OrderByDescending(e => e.IdEjecucionPrueba)
                    .Select(e => (int?)e.IdResultadoPrueba)
                    .FirstOrDefault()
            }).ToListAsync(cancellationToken);

        return requisitos.Select(r =>
        {
            var casos = cobertura.Where(c => c.IdWorkItem == r.IdWorkItem).ToList();
            return new TrazabilidadResponse
            {
                IdWorkItem = r.IdWorkItem,
                Folio = r.Folio,
                Titulo = r.Titulo,
                TotalCasos = casos.Count,
                CasosPasa = casos.Count(c => c.UltimoResultado == ResultadoPrueba.Pasa),
                CasosFalla = casos.Count(c => c.UltimoResultado == ResultadoPrueba.Falla),
                SinCobertura = casos.Count == 0
            };
        }).ToList();
    }

    private static IQueryable<PlanPruebaResponse> Proyectar(DbContextGTE contexto)
    {
        return from p in contexto.TblPlanPrueba.AsNoTracking()
               join pr in contexto.TblProyecto.AsNoTracking() on p.IdProyecto equals pr.IdProyecto
               join rel in contexto.TblRelease.AsNoTracking() on p.IdRelease equals rel.IdRelease into releases
               from rel in releases.DefaultIfEmpty()
               where p.Activo
               select new PlanPruebaResponse
               {
                   IdPlanPrueba = p.IdPlanPrueba,
                   IdProyecto = p.IdProyecto,
                   Proyecto = pr.Nombre,
                   IdRelease = p.IdRelease,
                   Release = rel != null ? rel.Version : null,
                   Nombre = p.Nombre,
                   Descripcion = p.Descripcion,
                   FechaRegistro = p.FechaRegistro,
                   TotalCasos = contexto.TblCasoPrueba.Count(c => c.IdPlanPrueba == p.IdPlanPrueba && c.Activo),
                   CasosEjecutados = contexto.TblCasoPrueba
                       .Count(c => c.IdPlanPrueba == p.IdPlanPrueba && c.Activo
                                   && contexto.TblEjecucionPrueba.Any(e => e.IdCasoPrueba == c.IdCasoPrueba)),
                   CasosPasa = contexto.TblCasoPrueba
                       .Count(c => c.IdPlanPrueba == p.IdPlanPrueba && c.Activo
                                   && contexto.TblEjecucionPrueba
                                       .Where(e => e.IdCasoPrueba == c.IdCasoPrueba)
                                       .OrderByDescending(e => e.IdEjecucionPrueba)
                                       .Select(e => e.IdResultadoPrueba)
                                       .FirstOrDefault() == ResultadoPrueba.Pasa),
                   CasosFalla = contexto.TblCasoPrueba
                       .Count(c => c.IdPlanPrueba == p.IdPlanPrueba && c.Activo
                                   && contexto.TblEjecucionPrueba
                                       .Where(e => e.IdCasoPrueba == c.IdCasoPrueba)
                                       .OrderByDescending(e => e.IdEjecucionPrueba)
                                       .Select(e => e.IdResultadoPrueba)
                                       .FirstOrDefault() == ResultadoPrueba.Falla)
               };
    }
}
