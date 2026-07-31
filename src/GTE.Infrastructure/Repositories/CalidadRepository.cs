using GTE.Application.Common;
using GTE.Domain.Calidad;
using GTE.Domain.Interfaces;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

public class CalidadRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), ICalidadRepository
{
    public async Task<int> CrearPlanAsync(PlanPruebaNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblPlanPrueba
        {
            IdProyecto = datos.IdProyecto,
            IdRelease = datos.IdRelease,
            Nombre = datos.Nombre,
            Descripcion = datos.Descripcion,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblPlanPrueba.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("PlanPrueba", entidad.IdPlanPrueba, "CREAR", datos.Nombre, cancellationToken);
        return entidad.IdPlanPrueba;
    }

    public async Task<EstadoPlan?> ObtenerEstadoPlanAsync(
        int idPlanPrueba, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblPlanPrueba.AsNoTracking()
            .Where(p => p.IdPlanPrueba == idPlanPrueba)
            .Select(p => new EstadoPlan(p.IdPlanPrueba, p.IdProyecto, p.IdRelease, p.Nombre, p.Activo))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CrearCasoAsync(CasoPruebaNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblCasoPrueba
        {
            Folio = datos.Folio,
            IdPlanPrueba = datos.IdPlanPrueba,
            Titulo = datos.Titulo,
            Precondiciones = datos.Precondiciones,
            ResultadoEsperado = datos.ResultadoEsperado,
            IdTipoPrueba = datos.IdTipoPrueba,
            IdWorkItem = datos.IdWorkItem,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblCasoPrueba.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        foreach (var paso in datos.Pasos)
        {
            contexto.TblCasoPruebaPaso.Add(new TblCasoPruebaPaso
            {
                IdCasoPrueba = entidad.IdCasoPrueba,
                NumeroPaso = paso.NumeroPaso,
                Accion = paso.Accion,
                ResultadoEsperado = paso.ResultadoEsperado,
                UsuarioRegistro = Auditoria.Usuario
            });
        }
        if (datos.Pasos.Count > 0)
        {
            await contexto.SaveChangesAsync(cancellationToken);
        }

        await RegistrarBitacoraAsync("CasoPrueba", entidad.IdCasoPrueba, "CREAR", datos.Folio, cancellationToken);
        return entidad.IdCasoPrueba;
    }

    public async Task<EstadoCaso?> ObtenerEstadoCasoAsync(
        int idCasoPrueba, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await (
            from c in contexto.TblCasoPrueba.AsNoTracking()
            join p in contexto.TblPlanPrueba.AsNoTracking() on c.IdPlanPrueba equals p.IdPlanPrueba
            where c.IdCasoPrueba == idCasoPrueba
            select new EstadoCaso(c.IdCasoPrueba, c.IdPlanPrueba, p.IdProyecto, c.Titulo, c.IdWorkItem, c.Activo)
            ).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CrearCicloAsync(CicloPruebaNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblCicloPrueba
        {
            IdPlanPrueba = datos.IdPlanPrueba,
            Nombre = datos.Nombre,
            FechaInicio = datos.FechaInicio,
            FechaFin = datos.FechaFin,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblCicloPrueba.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("CicloPrueba", entidad.IdCicloPrueba, "CREAR", datos.Nombre, cancellationToken);
        return entidad.IdCicloPrueba;
    }

    public async Task<bool> ExisteCicloEnPlanAsync(
        int idCicloPrueba, int idPlanPrueba, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblCicloPrueba.AsNoTracking()
            .AnyAsync(c => c.IdCicloPrueba == idCicloPrueba && c.IdPlanPrueba == idPlanPrueba && c.Activo,
                cancellationToken);
    }

    public async Task<int> RegistrarEjecucionAsync(
        EjecucionNueva datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblEjecucionPrueba
        {
            IdCasoPrueba = datos.IdCasoPrueba,
            IdCicloPrueba = datos.IdCicloPrueba,
            IdEjecutor = datos.IdEjecutor,
            IdResultadoPrueba = datos.IdResultadoPrueba,
            FechaEjecucion = DateTime.Now,
            Observaciones = datos.Observaciones,
            UsuarioRegistro = Auditoria.Usuario
        };
        contexto.TblEjecucionPrueba.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("EjecucionPrueba", entidad.IdEjecucionPrueba, "REGISTRAR",
            $"Caso {datos.IdCasoPrueba} resultado {datos.IdResultadoPrueba}", cancellationToken);
        return entidad.IdEjecucionPrueba;
    }

    public async Task<EstadoEjecucion?> ObtenerEstadoEjecucionAsync(
        int idEjecucion, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await (
            from e in contexto.TblEjecucionPrueba.AsNoTracking()
            join c in contexto.TblCasoPrueba.AsNoTracking() on e.IdCasoPrueba equals c.IdCasoPrueba
            join p in contexto.TblPlanPrueba.AsNoTracking() on c.IdPlanPrueba equals p.IdPlanPrueba
            where e.IdEjecucionPrueba == idEjecucion
            select new EstadoEjecucion(
                e.IdEjecucionPrueba, e.IdCasoPrueba, e.IdCicloPrueba, e.IdResultadoPrueba,
                p.IdProyecto, c.Titulo, e.Observaciones)
            ).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task VincularBugAsync(
        int idEjecucion, int idWorkItemBug, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var bug = await contexto.TblWorkItem
            .FirstOrDefaultAsync(w => w.IdWorkItem == idWorkItemBug, cancellationToken)
            ?? throw new InvalidOperationException($"WorkItem {idWorkItemBug} no existe.");

        bug.IdEjecucionPruebaOrigen = idEjecucion;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("EjecucionPrueba", idEjecucion, "VINCULAR_BUG",
            $"WorkItem {idWorkItemBug}", cancellationToken);
    }

    public async Task<int?> ObtenerBugDeEjecucionAsync(
        int idEjecucion, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.IdEjecucionPruebaOrigen == idEjecucion && w.Activo)
            .Select(w => (int?)w.IdWorkItem)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
