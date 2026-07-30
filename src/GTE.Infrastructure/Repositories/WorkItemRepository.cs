using GTE.Application.Common;
using GTE.Domain.Interfaces;
using GTE.Domain.WorkItems;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

public class WorkItemRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), IWorkItemRepository
{
    public async Task<int> CrearAsync(WorkItemNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblWorkItem
        {
            Folio = datos.Folio,
            IdTipoWorkItem = datos.IdTipoWorkItem,
            IdPadre = datos.IdPadre,
            IdProyecto = datos.IdProyecto,
            IdSolicitud = datos.IdSolicitud,
            Titulo = datos.Titulo,
            Descripcion = datos.Descripcion,
            CriteriosAceptacion = datos.CriteriosAceptacion,
            IdEstatusWorkItem = EstatusWorkItem.Pendiente,   // el estatus inicial lo fija el backend
            IdPrioridad = datos.IdPrioridad,
            IdComplejidad = datos.IdComplejidad,
            IdAsignado = datos.IdAsignado,
            IdSolicitante = datos.IdSolicitante,
            PuntosHistoria = datos.PuntosHistoria,
            MinutosPresupuesto = datos.MinutosPresupuesto,
            FechaCompromiso = datos.FechaCompromiso,
            UsuarioRegistro = Auditoria.Usuario,
            // Trampa de EF con bit NOT NULL DEFAULT 1: el default de BD no aplica
            // de forma confiable en el INSERT; toda alta lo fija explicitamente
            Activo = true
        };
        contexto.TblWorkItem.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        // Siembra del historial: base del calculo de tiempos y metricas de flujo
        contexto.TblHistorialEstatus.Add(new TblHistorialEstatus
        {
            Proceso = "WorkItem",
            IdRegistro = entidad.IdWorkItem,
            IdEstatus = EstatusWorkItem.Pendiente,
            Accion = "ALTA",
            Usuario = Auditoria.Usuario
        });
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("WorkItem", entidad.IdWorkItem, "CREAR", datos.Folio, cancellationToken);
        return entidad.IdWorkItem;
    }

    public async Task ActualizarAsync(WorkItemEdicion datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = await contexto.TblWorkItem
            .FirstOrDefaultAsync(w => w.IdWorkItem == datos.IdWorkItem, cancellationToken)
            ?? throw new InvalidOperationException($"WorkItem {datos.IdWorkItem} no existe.");

        entidad.Titulo = datos.Titulo;
        entidad.Descripcion = datos.Descripcion;
        entidad.CriteriosAceptacion = datos.CriteriosAceptacion;
        entidad.IdPrioridad = datos.IdPrioridad;
        entidad.IdComplejidad = datos.IdComplejidad;
        entidad.IdAsignado = datos.IdAsignado;
        entidad.PuntosHistoria = datos.PuntosHistoria;
        entidad.FechaCompromiso = datos.FechaCompromiso;
        if (datos.ActualizarPresupuesto)
        {
            entidad.MinutosPresupuesto = datos.MinutosPresupuesto;   // RN-REQ-08
        }
        MarcarMovimiento(entidad);

        await contexto.SaveChangesAsync(cancellationToken);
        await RegistrarBitacoraAsync("WorkItem", datos.IdWorkItem, "ACTUALIZAR", null, cancellationToken);
    }

    public async Task<EstadoWorkItem?> ObtenerEstadoAsync(int idWorkItem, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        return await (
            from w in contexto.TblWorkItem.AsNoTracking()
            join p in contexto.TblProyecto.AsNoTracking() on w.IdProyecto equals p.IdProyecto
            where w.IdWorkItem == idWorkItem
            select new EstadoWorkItem(
                w.IdWorkItem, w.Folio, w.IdEstatusWorkItem, w.IdProyecto, p.EsMantenimiento,
                w.IdAsignado,
                contexto.TblUsuario.Where(u => u.IdUsuario == w.IdAsignado)
                    .Select(u => u.IdHorario).FirstOrDefault(),
                w.IdComplejidad, w.FechaCompromiso, w.Activo)
            ).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProyectoResumen?> ObtenerProyectoAsync(int idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblProyecto.AsNoTracking()
            .Where(p => p.IdProyecto == idProyecto)
            .Select(p => new ProyectoResumen(p.IdProyecto, p.Clave, p.EsMantenimiento, p.Activo))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UsuarioResumen?> ObtenerUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblUsuario.AsNoTracking()
            .Where(u => u.IdUsuario == idUsuario)
            .Select(u => new UsuarioResumen(u.IdUsuario, u.IdNivel, u.IdHorario, u.Activo))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int?> ObtenerMinutosMatrizAsync(int idComplejidad, int idNivel, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblMatrizPresupuesto.AsNoTracking()
            .Where(m => m.IdComplejidad == idComplejidad && m.IdNivel == idNivel && m.Activo)
            .Select(m => (int?)m.Minutos)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int?> ObtenerItemEnProcesoDeAsignadoAsync(int idAsignado, int idExcluido, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.IdAsignado == idAsignado
                        && w.IdEstatusWorkItem == EstatusWorkItem.EnProceso
                        && w.IdWorkItem != idExcluido
                        && w.Activo)
            .Select(w => (int?)w.IdWorkItem)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ValidacionCierre> ObtenerValidacionCierreAsync(int idWorkItem, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var tieneTiempo = await contexto.TblRegistroTiempo.AsNoTracking()
            .AnyAsync(t => t.IdWorkItem == idWorkItem && t.Activo, cancellationToken);

        var tieneSubtareaTerminada = await contexto.TblWorkItem.AsNoTracking()
            .AnyAsync(w => w.IdPadre == idWorkItem
                           && w.IdEstatusWorkItem == EstatusWorkItem.Terminado
                           && w.Activo, cancellationToken);

        var revisiones = await (
            from r in contexto.TblRevision.AsNoTracking()
            join u in contexto.TblUsuario.AsNoTracking() on r.IdRevisor equals u.IdUsuario
            where r.IdWorkItem == idWorkItem && !r.Corregido && r.Activo
            select new RevisionPendiente(r.IdRevision, u.Nombre, r.Comentarios)
            ).ToListAsync(cancellationToken);

        return new ValidacionCierre(tieneTiempo || tieneSubtareaTerminada, revisiones);
    }

    public async Task AplicarEfectosTransicionAsync(int idWorkItem, string accion, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = await contexto.TblWorkItem
            .FirstOrDefaultAsync(w => w.IdWorkItem == idWorkItem, cancellationToken)
            ?? throw new InvalidOperationException($"WorkItem {idWorkItem} no existe.");

        switch (accion)
        {
            case AccionesWorkItem.Iniciar:
            case AccionesWorkItem.Reanudar:
                entidad.FechaInicio ??= DateTime.Now;   // ISNULL(Inicio, ahora) - regla del GT
                break;
            case AccionesWorkItem.Terminar:
                entidad.FechaFin = DateTime.Now;
                entidad.FechaInicio ??= DateTime.Now;
                break;
            case AccionesWorkItem.Revertir:
                entidad.FechaFin = null;
                break;
        }
        MarcarMovimiento(entidad);

        await contexto.SaveChangesAsync(cancellationToken);
        await RegistrarBitacoraAsync("WorkItem", idWorkItem, accion, null, cancellationToken);
    }

    public async Task<int> RegistrarTiempoAsync(
        int idWorkItem, int idUsuario, DateOnly fecha, int minutos, string? descripcion,
        CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var registro = new TblRegistroTiempo
        {
            IdWorkItem = idWorkItem,
            IdUsuario = idUsuario,
            Fecha = fecha,
            Minutos = minutos,
            Descripcion = descripcion,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblRegistroTiempo.Add(registro);

        var item = await contexto.TblWorkItem
            .FirstOrDefaultAsync(w => w.IdWorkItem == idWorkItem, cancellationToken);
        if (item is not null)
        {
            MarcarMovimiento(item);
        }

        await contexto.SaveChangesAsync(cancellationToken);
        await RegistrarBitacoraAsync("WorkItem", idWorkItem, "REGISTRAR_TIEMPO",
            $"{minutos} minutos el {fecha:yyyy-MM-dd}", cancellationToken);
        return registro.IdRegistroTiempo;
    }

    private void MarcarMovimiento(TblWorkItem entidad)
    {
        entidad.UsuarioMovto = Auditoria.Usuario.Length > 50
            ? Auditoria.Usuario[..50]
            : Auditoria.Usuario;
        entidad.FechaMovto = DateTime.Now;
    }
}
