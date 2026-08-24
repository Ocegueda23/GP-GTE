using GTE.Application.DTOs.Responses.Calidad;
using GTE.Application.Interfaces;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class CalidadQueryService(FabricaContexto fabrica) : ICalidadQueryService
{
    public async Task<IReadOnlyList<CasoPruebaResponse>> ObtenerCasosDisponiblesAsync(
        int idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var casos = await (
            from c in contexto.TblCasoPrueba.AsNoTracking()
            join t in contexto.TblTipoPrueba.AsNoTracking() on c.IdTipoPrueba equals t.Id
            where c.IdProyecto == idProyecto && c.Activo && c.Reutilizable
            orderby c.Titulo
            select new CasoPruebaResponse
            {
                IdCasoPrueba = c.IdCasoPrueba,
                Folio = c.Folio,
                Titulo = c.Titulo,
                Precondiciones = c.Precondiciones,
                ResultadoEsperado = c.ResultadoEsperado,
                TipoPrueba = t.Nombre
            }).ToListAsync(cancellationToken);

        await CargarPasosAsync(contexto, casos, cancellationToken);
        return casos;
    }

    public async Task<IReadOnlyList<CasoAsignadoResponse>> ObtenerCasosAsignadosAsync(
        int idWorkItem, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var casos = await (
            from a in contexto.TblWorkItemCasoPrueba.AsNoTracking()
            join c in contexto.TblCasoPrueba.AsNoTracking() on a.IdCasoPrueba equals c.IdCasoPrueba
            join t in contexto.TblTipoPrueba.AsNoTracking() on c.IdTipoPrueba equals t.Id
            where a.IdWorkItem == idWorkItem && a.Activo && c.Activo
            orderby c.Titulo
            select new CasoAsignadoResponse
            {
                IdWorkItemCasoPrueba = a.IdWorkItemCasoPrueba,
                IdCasoPrueba = c.IdCasoPrueba,
                Folio = c.Folio,
                Titulo = c.Titulo,
                TipoPrueba = t.Nombre
            }).ToListAsync(cancellationToken);

        if (casos.Count == 0)
        {
            return casos;
        }

        var idsCaso = casos.Select(c => c.IdCasoPrueba).ToList();

        var pasos = await contexto.TblCasoPruebaPaso.AsNoTracking()
            .Where(p => idsCaso.Contains(p.IdCasoPrueba))
            .OrderBy(p => p.NumeroPaso)
            .Select(p => new { p.IdCasoPrueba, Paso = new PasoCasoResponse
            {
                NumeroPaso = p.NumeroPaso,
                Accion = p.Accion,
                ResultadoEsperado = p.ResultadoEsperado
            } })
            .ToListAsync(cancellationToken);

        var ejecuciones = await (
            from e in contexto.TblEjecucionPrueba.AsNoTracking()
            join r in contexto.TblResultadoPrueba.AsNoTracking() on e.IdResultadoPrueba equals r.Id
            where e.IdWorkItem == idWorkItem && idsCaso.Contains(e.IdCasoPrueba)
            select new
            {
                e.IdEjecucionPrueba, e.IdCasoPrueba, e.IdResultadoPrueba,
                Resultado = r.Nombre, e.FechaEjecucion
            }).ToListAsync(cancellationToken);

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
                caso.FechaUltimaEjecucion = ultima.FechaEjecucion;
            }
        }

        return casos;
    }

    public async Task<IReadOnlyList<CasoAdminResponse>> ObtenerCatalogoCasosAsync(
        int idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var casos = await (
            from c in contexto.TblCasoPrueba.AsNoTracking()
            join t in contexto.TblTipoPrueba.AsNoTracking() on c.IdTipoPrueba equals t.Id
            where c.IdProyecto == idProyecto
            orderby c.Titulo
            select new CasoAdminResponse
            {
                IdCasoPrueba = c.IdCasoPrueba,
                Folio = c.Folio,
                Titulo = c.Titulo,
                Precondiciones = c.Precondiciones,
                ResultadoEsperado = c.ResultadoEsperado,
                IdTipoPrueba = c.IdTipoPrueba,
                TipoPrueba = t.Nombre,
                Reutilizable = c.Reutilizable,
                Activo = c.Activo
            }).ToListAsync(cancellationToken);

        if (casos.Count == 0)
        {
            return casos;
        }

        var idsCaso = casos.Select(c => c.IdCasoPrueba).ToList();

        var asignaciones = await contexto.TblWorkItemCasoPrueba.AsNoTracking()
            .Where(a => idsCaso.Contains(a.IdCasoPrueba) && a.Activo)
            .GroupBy(a => a.IdCasoPrueba)
            .Select(g => new { IdCasoPrueba = g.Key, Total = g.Count() })
            .ToListAsync(cancellationToken);

        var pasos = await contexto.TblCasoPruebaPaso.AsNoTracking()
            .Where(p => idsCaso.Contains(p.IdCasoPrueba))
            .OrderBy(p => p.NumeroPaso)
            .Select(p => new { p.IdCasoPrueba, Paso = new PasoCasoResponse
            {
                NumeroPaso = p.NumeroPaso,
                Accion = p.Accion,
                ResultadoEsperado = p.ResultadoEsperado
            } })
            .ToListAsync(cancellationToken);

        foreach (var caso in casos)
        {
            caso.TotalAsignaciones = asignaciones.FirstOrDefault(a => a.IdCasoPrueba == caso.IdCasoPrueba)?.Total ?? 0;
            caso.Pasos = pasos.Where(p => p.IdCasoPrueba == caso.IdCasoPrueba).Select(p => p.Paso).ToList();
        }

        return casos;
    }

    private static async Task CargarPasosAsync(
        DbContextGTE contexto, IReadOnlyList<CasoPruebaResponse> casos, CancellationToken cancellationToken)
    {
        if (casos.Count == 0)
        {
            return;
        }

        var idsCaso = casos.Select(c => c.IdCasoPrueba).ToList();
        var pasos = await contexto.TblCasoPruebaPaso.AsNoTracking()
            .Where(p => idsCaso.Contains(p.IdCasoPrueba))
            .OrderBy(p => p.NumeroPaso)
            .Select(p => new { p.IdCasoPrueba, Paso = new PasoCasoResponse
            {
                NumeroPaso = p.NumeroPaso,
                Accion = p.Accion,
                ResultadoEsperado = p.ResultadoEsperado
            } })
            .ToListAsync(cancellationToken);

        foreach (var caso in casos)
        {
            caso.Pasos = pasos.Where(p => p.IdCasoPrueba == caso.IdCasoPrueba)
                .Select(p => p.Paso).ToList();
        }
    }
}
