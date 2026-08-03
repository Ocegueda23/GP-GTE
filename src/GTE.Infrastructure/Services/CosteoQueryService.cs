using GTE.Application.DTOs.Responses.Costeo;
using GTE.Application.Interfaces;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class CosteoQueryService(FabricaContexto fabrica) : ICosteoQueryService
{
    public async Task<IReadOnlyList<TarifaNivelResponse>> ObtenerTarifasAsync(CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await Proyectar(contexto.TblTarifaNivel.AsNoTracking().Where(t => t.Activo), contexto)
            .OrderBy(t => t.Nivel).ThenByDescending(t => t.VigenciaDesde)
            .ToListAsync(cancellationToken);
    }

    public async Task<TarifaNivelResponse?> ObtenerTarifaAsync(int idTarifaNivel, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await Proyectar(contexto.TblTarifaNivel.AsNoTracking().Where(t => t.IdTarifaNivel == idTarifaNivel), contexto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PresupuestoProyectoResponse>> ObtenerPresupuestosAsync(
        int idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await ProyectarPresupuestos(contexto.TblPresupuestoProyecto.AsNoTracking()
                .Where(p => p.IdProyecto == idProyecto && p.Activo), contexto)
            .OrderByDescending(p => p.Anio)
            .ToListAsync(cancellationToken);
    }

    public async Task<PresupuestoProyectoResponse?> ObtenerPresupuestoAsync(
        int idPresupuestoProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await ProyectarPresupuestos(contexto.TblPresupuestoProyecto.AsNoTracking()
                .Where(p => p.IdPresupuestoProyecto == idPresupuestoProyecto), contexto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CostoProyectoResponse> ObtenerCostoProyectoAsync(
        int idProyecto, int anio, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var nombreProyecto = await contexto.TblProyecto.AsNoTracking()
            .Where(p => p.IdProyecto == idProyecto)
            .Select(p => p.Nombre)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var presupuesto = await contexto.TblPresupuestoProyecto.AsNoTracking()
            .Where(p => p.IdProyecto == idProyecto && p.Anio == anio && p.Activo)
            .FirstOrDefaultAsync(cancellationToken);

        var desde = new DateOnly(anio, 1, 1);
        var hasta = new DateOnly(anio, 12, 31);

        var agrupado = await (
            from c in contexto.VwCostoRegistroTiempo.AsNoTracking()
            join u in contexto.TblUsuario.AsNoTracking() on c.IdUsuario equals u.IdUsuario
            where c.IdProyecto == idProyecto && c.Fecha >= desde && c.Fecha <= hasta
            group new { c.Minutos, c.Costo } by new { c.IdUsuario, u.Nombre } into g
            select new CostoUsuarioResponse
            {
                IdUsuario = g.Key.IdUsuario,
                Usuario = g.Key.Nombre,
                Minutos = g.Sum(x => (decimal)x.Minutos),
                Horas = g.Sum(x => (decimal)x.Minutos) / 60m,
                Costo = g.Sum(x => x.Costo)
            })
            .ToListAsync(cancellationToken);

        var detalle = agrupado.OrderByDescending(d => d.Costo).ToList();

        return new CostoProyectoResponse
        {
            IdProyecto = idProyecto,
            Proyecto = nombreProyecto,
            Anio = anio,
            MontoAutorizado = presupuesto?.MontoAutorizado ?? 0,
            HorasAutorizadas = presupuesto?.HorasAutorizadas ?? 0,
            HorasReales = detalle.Sum(d => d.Horas),
            CostoReal = detalle.Sum(d => d.Costo),
            DetallePorUsuario = detalle
        };
    }

    private static IQueryable<TarifaNivelResponse> Proyectar(
        IQueryable<Modelos.bdsGTE.TblTarifaNivel> tarifas, DbContextGTE contexto)
    {
        return from t in tarifas
               join n in contexto.TblNivel.AsNoTracking() on t.IdNivel equals n.IdNivel
               select new TarifaNivelResponse
               {
                   IdTarifaNivel = t.IdTarifaNivel,
                   IdNivel = t.IdNivel,
                   Nivel = n.Nombre,
                   CostoHora = t.CostoHora,
                   VigenciaDesde = t.VigenciaDesde
               };
    }

    private static IQueryable<PresupuestoProyectoResponse> ProyectarPresupuestos(
        IQueryable<Modelos.bdsGTE.TblPresupuestoProyecto> presupuestos, DbContextGTE contexto)
    {
        return from p in presupuestos
               join pr in contexto.TblProyecto.AsNoTracking() on p.IdProyecto equals pr.IdProyecto
               select new PresupuestoProyectoResponse
               {
                   IdPresupuestoProyecto = p.IdPresupuestoProyecto,
                   IdProyecto = p.IdProyecto,
                   Proyecto = pr.Nombre,
                   Anio = p.Anio,
                   MontoAutorizado = p.MontoAutorizado,
                   HorasAutorizadas = p.HorasAutorizadas
               };
    }
}
