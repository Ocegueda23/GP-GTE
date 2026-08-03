using GTE.Application.DTOs.Responses.Okr;
using GTE.Application.Interfaces;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class OkrQueryService(FabricaContexto fabrica) : IOkrQueryService
{
    public async Task<IReadOnlyList<ObjetivoOkrResponse>> ObtenerObjetivosAsync(
        int? idProyecto, int? idEquipo, int? anio, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var consulta = Proyectar(contexto);
        if (idProyecto.HasValue)
        {
            consulta = consulta.Where(o => o.IdProyecto == idProyecto.Value);
        }
        if (idEquipo.HasValue)
        {
            consulta = consulta.Where(o => o.IdEquipo == idEquipo.Value);
        }
        if (anio.HasValue)
        {
            consulta = consulta.Where(o => o.Anio == anio.Value);
        }

        var objetivos = await consulta
            .OrderByDescending(o => o.Anio).ThenByDescending(o => o.Trimestre)
            .ToListAsync(cancellationToken);

        await CargarResultadosClaveAsync(objetivos, contexto, cancellationToken);
        return objetivos;
    }

    public async Task<ObjetivoOkrResponse?> ObtenerObjetivoAsync(
        int idObjetivoOkr, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var objetivo = await Proyectar(contexto)
            .FirstOrDefaultAsync(o => o.IdObjetivoOkr == idObjetivoOkr, cancellationToken);
        if (objetivo is null)
        {
            return null;
        }

        await CargarResultadosClaveAsync([objetivo], contexto, cancellationToken);
        return objetivo;
    }

    /// <summary>Carga en memoria los resultados clave de una lista de objetivos ya proyectados (patron de ItemsGenerados en SolicitudQueryService).</summary>
    private static async Task CargarResultadosClaveAsync(
        IReadOnlyList<ObjetivoOkrResponse> objetivos, DbContextGTE contexto, CancellationToken cancellationToken)
    {
        if (objetivos.Count == 0)
        {
            return;
        }
        var ids = objetivos.Select(o => o.IdObjetivoOkr).ToList();

        var resultados = await contexto.TblResultadoClave.AsNoTracking()
            .Where(r => ids.Contains(r.IdObjetivoOkr) && r.Activo)
            .OrderBy(r => r.IdResultadoClave)
            .Select(r => new
            {
                r.IdObjetivoOkr,
                Item = new ResultadoClaveResponse
                {
                    IdResultadoClave = r.IdResultadoClave,
                    Nombre = r.Nombre,
                    ValorMeta = r.ValorMeta,
                    ValorActual = r.ValorActual,
                    ClaveKpi = r.ClaveKpi
                }
            })
            .ToListAsync(cancellationToken);

        var porObjetivo = resultados
            .GroupBy(x => x.IdObjetivoOkr)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ResultadoClaveResponse>)g.Select(x => x.Item).ToList());

        foreach (var objetivo in objetivos)
        {
            if (porObjetivo.TryGetValue(objetivo.IdObjetivoOkr, out var items))
            {
                objetivo.ResultadosClave = items;
            }
        }
    }

    private static IQueryable<ObjetivoOkrResponse> Proyectar(DbContextGTE contexto)
    {
        return from o in contexto.TblObjetivoOkr.AsNoTracking()
               join p in contexto.TblProyecto.AsNoTracking() on o.IdProyecto equals p.IdProyecto into proyectos
               from p in proyectos.DefaultIfEmpty()
               join e in contexto.TblEquipo.AsNoTracking() on o.IdEquipo equals e.IdEquipo into equipos
               from e in equipos.DefaultIfEmpty()
               where o.Activo
               select new ObjetivoOkrResponse
               {
                   IdObjetivoOkr = o.IdObjetivoOkr,
                   IdProyecto = o.IdProyecto,
                   Proyecto = p != null ? p.Nombre : null,
                   IdEquipo = o.IdEquipo,
                   Equipo = e != null ? e.Nombre : null,
                   Nombre = o.Nombre,
                   Descripcion = o.Descripcion,
                   Anio = o.Anio,
                   Trimestre = o.Trimestre
               };
    }
}
