using GTE.Application.Common;
using GTE.Domain.Interfaces;
using GTE.Domain.Planeacion;
using GTE.Domain.WorkItems;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

public class PlaneacionRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), IPlaneacionRepository
{
    /// <summary>Mapeo estandar de columnas de tablero (estatus abiertos + Terminado).</summary>
    private static readonly (string Nombre, int IdEstatus, int Orden, int? Wip)[] ColumnasEstandar =
    [
        ("Pendiente",  EstatusWorkItem.Pendiente,  1, null),
        ("En proceso", EstatusWorkItem.EnProceso,  2, 5),
        ("En pruebas", EstatusWorkItem.EnPruebas,  3, 5),
        ("Correccion", EstatusWorkItem.Correccion, 4, null),
        ("Terminado",  EstatusWorkItem.Terminado,  5, null)
    ];

    public async Task<int> CrearSprintAsync(SprintNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblSprint
        {
            IdEquipo = datos.IdEquipo,
            Nombre = datos.Nombre,
            Objetivo = datos.Objetivo,
            FechaInicio = datos.FechaInicio,
            FechaFin = datos.FechaFin,
            IdEstatusSprint = EstatusSprint.Planeado,   // el estatus inicial lo fija el backend
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblSprint.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        contexto.TblHistorialEstatus.Add(new TblHistorialEstatus
        {
            Proceso = "Sprint",
            IdRegistro = entidad.IdSprint,
            IdEstatus = EstatusSprint.Planeado,
            Accion = "ALTA",
            Usuario = Auditoria.Usuario
        });
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Sprint", entidad.IdSprint, "CREAR", datos.Nombre, cancellationToken);
        return entidad.IdSprint;
    }

    public async Task<EstadoSprint?> ObtenerEstadoSprintAsync(
        int idSprint, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblSprint.AsNoTracking()
            .Where(s => s.IdSprint == idSprint)
            .Select(s => new EstadoSprint(
                s.IdSprint, s.IdEquipo, s.Nombre, s.IdEstatusSprint,
                s.FechaInicio, s.FechaFin, s.Activo))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int?> ObtenerSprintActivoAsync(
        int idEquipo, int idExcluido, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblSprint.AsNoTracking()
            .Where(s => s.IdEquipo == idEquipo
                        && s.IdEstatusSprint == EstatusSprint.Activo
                        && s.IdSprint != idExcluido
                        && s.Activo)
            .Select(s => (int?)s.IdSprint)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int?> ObtenerSiguienteSprintPlaneadoAsync(
        int idEquipo, int idSprintActual, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblSprint.AsNoTracking()
            .Where(s => s.IdEquipo == idEquipo
                        && s.IdEstatusSprint == EstatusSprint.Planeado
                        && s.IdSprint != idSprintActual
                        && s.Activo)
            .OrderBy(s => s.FechaInicio)
            .Select(s => (int?)s.IdSprint)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AplicarEfectosTransicionSprintAsync(
        int idSprint, string accion, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblSprint
            .FirstOrDefaultAsync(s => s.IdSprint == idSprint, cancellationToken)
            ?? throw new InvalidOperationException($"Sprint {idSprint} no existe.");

        entidad.UsuarioMovto = Auditoria.Usuario.Length > 50 ? Auditoria.Usuario[..50] : Auditoria.Usuario;
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);
        await RegistrarBitacoraAsync("Sprint", idSprint, accion, null, cancellationToken);
    }

    public async Task<int> MoverItemsAbiertosAsync(
        int idSprint, int? idSprintDestino, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var abiertos = await contexto.TblWorkItem
            .Where(w => w.IdSprint == idSprint
                        && w.IdEstatusWorkItem != EstatusWorkItem.Terminado
                        && w.IdEstatusWorkItem != EstatusWorkItem.Cancelado
                        && w.Activo)
            .ToListAsync(cancellationToken);

        foreach (var item in abiertos)
        {
            item.IdSprint = idSprintDestino;
            item.UsuarioMovto = Auditoria.Usuario.Length > 50 ? Auditoria.Usuario[..50] : Auditoria.Usuario;
            item.FechaMovto = DateTime.Now;
        }
        await contexto.SaveChangesAsync(cancellationToken);

        if (abiertos.Count > 0)
        {
            var destino = idSprintDestino.HasValue ? $"sprint {idSprintDestino}" : "backlog";
            await RegistrarBitacoraAsync("Sprint", idSprint, "MOVER_ABIERTOS",
                $"{abiertos.Count} elemento(s) al {destino}", cancellationToken);
        }
        return abiertos.Count;
    }

    public async Task AsignarSprintAsync(
        int idWorkItem, int? idSprint, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var item = await contexto.TblWorkItem
            .FirstOrDefaultAsync(w => w.IdWorkItem == idWorkItem, cancellationToken)
            ?? throw new InvalidOperationException($"WorkItem {idWorkItem} no existe.");

        item.IdSprint = idSprint;
        item.UsuarioMovto = Auditoria.Usuario.Length > 50 ? Auditoria.Usuario[..50] : Auditoria.Usuario;
        item.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("WorkItem", idWorkItem, "ASIGNAR_SPRINT",
            idSprint?.ToString() ?? "backlog", cancellationToken);
    }

    public async Task ReordenarBacklogAsync(
        IReadOnlyList<int> idsEnOrden, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var items = await contexto.TblWorkItem
            .Where(w => idsEnOrden.Contains(w.IdWorkItem))
            .ToListAsync(cancellationToken);

        for (var i = 0; i < idsEnOrden.Count; i++)
        {
            var item = items.FirstOrDefault(w => w.IdWorkItem == idsEnOrden[i]);
            if (item is not null)
            {
                item.OrdenBacklog = (i + 1) * 10;   // huecos de 10 para inserciones futuras
            }
        }
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MiembroEquipo>> ObtenerMiembrosEquipoAsync(
        int idEquipo, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await (
            from em in contexto.TblEquipoMiembro.AsNoTracking()
            join u in contexto.TblUsuario.AsNoTracking() on em.IdUsuario equals u.IdUsuario
            where em.IdEquipo == idEquipo && em.Activo && u.Activo
            select new MiembroEquipo(u.IdUsuario, u.Nombre, u.IdHorario, em.PorcentajeDedicacion)
            ).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AusenciaAprobada>> ObtenerAusenciasAprobadasAsync(
        int idEquipo, DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await (
            from a in contexto.TblAusencia.AsNoTracking()
            join em in contexto.TblEquipoMiembro.AsNoTracking() on a.IdUsuario equals em.IdUsuario
            where em.IdEquipo == idEquipo && em.Activo && a.Activo
                  && a.IdEstatusAusencia == 2      // Aprobada
                  && a.FechaInicio <= hasta && a.FechaFin >= desde
            select new AusenciaAprobada(a.IdUsuario, a.FechaInicio, a.FechaFin)
            ).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ColumnaTablero>> ObtenerOCrearColumnasAsync(
        int idEquipo, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var tablero = await contexto.TblTablero
            .FirstOrDefaultAsync(t => t.IdEquipo == idEquipo && t.Activo, cancellationToken);

        if (tablero is null)
        {
            tablero = new TblTablero
            {
                IdEquipo = idEquipo,
                Nombre = "Tablero del equipo",
                UsuarioRegistro = Auditoria.Usuario,
                Activo = true
            };
            contexto.TblTablero.Add(tablero);
            await contexto.SaveChangesAsync(cancellationToken);
        }

        var columnas = await contexto.TblTableroColumna
            .Where(c => c.IdTablero == tablero.IdTablero && c.Activo)
            .OrderBy(c => c.Orden)
            .ToListAsync(cancellationToken);

        if (columnas.Count == 0)
        {
            foreach (var (nombre, idEstatus, orden, wip) in ColumnasEstandar)
            {
                contexto.TblTableroColumna.Add(new TblTableroColumna
                {
                    IdTablero = tablero.IdTablero,
                    Nombre = nombre,
                    IdEstatusWorkItem = idEstatus,
                    Orden = orden,
                    LimiteWip = wip,
                    UsuarioRegistro = Auditoria.Usuario,
                    Activo = true
                });
            }
            await contexto.SaveChangesAsync(cancellationToken);

            columnas = await contexto.TblTableroColumna
                .Where(c => c.IdTablero == tablero.IdTablero && c.Activo)
                .OrderBy(c => c.Orden)
                .ToListAsync(cancellationToken);
        }

        return columnas
            .Select(c => new ColumnaTablero(c.IdTableroColumna, c.Nombre, c.IdEstatusWorkItem, c.Orden, c.LimiteWip))
            .ToList();
    }

    public async Task<int?> ObtenerEquipoDeProyectoAsync(
        int idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblProyecto.AsNoTracking()
            .Where(p => p.IdProyecto == idProyecto)
            .Select(p => p.IdEquipo)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task RegistrarSaltoWipAsync(
        int idWorkItem, string columna, int limite, int enColumna,
        CancellationToken cancellationToken = default)
    {
        await RegistrarBitacoraAsync("WorkItem", idWorkItem, "SALTO_LIMITE_WIP",
            $"Columna {columna}: {enColumna} elementos con limite {limite}", cancellationToken);
    }

    public async Task<int> ContarItemsEnEstatusAsync(
        int idEquipo, int idEstatus, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await (
            from w in contexto.TblWorkItem.AsNoTracking()
            join p in contexto.TblProyecto.AsNoTracking() on w.IdProyecto equals p.IdProyecto
            where p.IdEquipo == idEquipo && w.IdEstatusWorkItem == idEstatus && w.Activo
            select w.IdWorkItem
            ).CountAsync(cancellationToken);
    }
}
