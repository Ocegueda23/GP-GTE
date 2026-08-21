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
    public async Task<int> CrearCasoAsync(CasoPruebaNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblCasoPrueba
        {
            Folio = datos.Folio,
            IdProyecto = datos.IdProyecto,
            Titulo = datos.Titulo,
            Precondiciones = datos.Precondiciones,
            ResultadoEsperado = datos.ResultadoEsperado,
            IdTipoPrueba = datos.IdTipoPrueba,
            Reutilizable = datos.Reutilizable,
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

        await RegistrarBitacoraAsync("CasoPrueba", entidad.IdCasoPrueba, "CREAR", datos.Titulo, cancellationToken);
        return entidad.IdCasoPrueba;
    }

    public async Task<EstadoCaso?> ObtenerEstadoCasoAsync(
        int idCasoPrueba, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblCasoPrueba.AsNoTracking()
            .Where(c => c.IdCasoPrueba == idCasoPrueba)
            .Select(c => new EstadoCaso(c.IdCasoPrueba, c.IdProyecto, c.Titulo, c.Activo))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task ActualizarCasoAsync(CasoPruebaEdicion datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblCasoPrueba
            .FirstOrDefaultAsync(c => c.IdCasoPrueba == datos.IdCasoPrueba, cancellationToken)
            ?? throw new InvalidOperationException($"CasoPrueba {datos.IdCasoPrueba} no existe.");

        entidad.Titulo = datos.Titulo;
        entidad.Precondiciones = datos.Precondiciones;
        entidad.ResultadoEsperado = datos.ResultadoEsperado;
        entidad.IdTipoPrueba = datos.IdTipoPrueba;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;

        var pasosPrevios = await contexto.TblCasoPruebaPaso
            .Where(p => p.IdCasoPrueba == datos.IdCasoPrueba)
            .ToListAsync(cancellationToken);
        contexto.TblCasoPruebaPaso.RemoveRange(pasosPrevios);

        foreach (var paso in datos.Pasos)
        {
            contexto.TblCasoPruebaPaso.Add(new TblCasoPruebaPaso
            {
                IdCasoPrueba = datos.IdCasoPrueba,
                NumeroPaso = paso.NumeroPaso,
                Accion = paso.Accion,
                ResultadoEsperado = paso.ResultadoEsperado,
                UsuarioRegistro = Auditoria.Usuario
            });
        }

        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("CasoPrueba", datos.IdCasoPrueba, "EDITAR", datos.Titulo, cancellationToken);
    }

    public async Task RetirarCasoAsync(int idCasoPrueba, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblCasoPrueba
            .FirstOrDefaultAsync(c => c.IdCasoPrueba == idCasoPrueba, cancellationToken)
            ?? throw new InvalidOperationException($"CasoPrueba {idCasoPrueba} no existe.");

        entidad.Activo = false;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("CasoPrueba", idCasoPrueba, "RETIRAR", null, cancellationToken);
    }

    public async Task AsignarCasoAsync(
        int idWorkItem, int idCasoPrueba, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        contexto.TblWorkItemCasoPrueba.Add(new TblWorkItemCasoPrueba
        {
            IdWorkItem = idWorkItem,
            IdCasoPrueba = idCasoPrueba,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        });
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("WorkItemCasoPrueba", idWorkItem, "ASIGNAR",
            $"Caso {idCasoPrueba}", cancellationToken);
    }

    public async Task<bool> ExisteAsignacionActivaAsync(
        int idWorkItem, int idCasoPrueba, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblWorkItemCasoPrueba.AsNoTracking()
            .AnyAsync(a => a.IdWorkItem == idWorkItem && a.IdCasoPrueba == idCasoPrueba && a.Activo,
                cancellationToken);
    }

    public async Task<int?> ObtenerIdWorkItemDeAsignacionAsync(
        int idWorkItemCasoPrueba, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblWorkItemCasoPrueba.AsNoTracking()
            .Where(a => a.IdWorkItemCasoPrueba == idWorkItemCasoPrueba)
            .Select(a => (int?)a.IdWorkItem)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task RetirarAsignacionAsync(
        int idWorkItemCasoPrueba, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblWorkItemCasoPrueba
            .FirstOrDefaultAsync(a => a.IdWorkItemCasoPrueba == idWorkItemCasoPrueba, cancellationToken)
            ?? throw new InvalidOperationException($"WorkItemCasoPrueba {idWorkItemCasoPrueba} no existe.");

        entidad.Activo = false;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("WorkItemCasoPrueba", idWorkItemCasoPrueba, "RETIRAR", null, cancellationToken);
    }

    public async Task<int> RegistrarEjecucionAsync(
        EjecucionNueva datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblEjecucionPrueba
        {
            IdCasoPrueba = datos.IdCasoPrueba,
            IdWorkItem = datos.IdWorkItem,
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
            where e.IdEjecucionPrueba == idEjecucion
            select new EstadoEjecucion(
                e.IdEjecucionPrueba, e.IdCasoPrueba, e.IdWorkItem ?? 0, e.IdResultadoPrueba, c.Titulo)
            ).FirstOrDefaultAsync(cancellationToken);
    }

    private static string Recortar(string usuario) => usuario.Length > 50 ? usuario[..50] : usuario;
}
