using GTE.Application.Common;
using GTE.Domain.Calidad;
using GTE.Domain.Entregas;
using GTE.Domain.Interfaces;
using GTE.Domain.WorkItems;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

public class EntregaRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), IEntregaRepository
{
    private const string EntidadAprobacion = "Release";

    public async Task<int> CrearReleaseAsync(ReleaseNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblRelease
        {
            IdProyecto = datos.IdProyecto,
            Version = datos.Version,
            Folio = datos.Folio,
            NotasVersion = datos.NotasVersion,
            IdEstatusRelease = EstatusRelease.EnPreparacion,   // el estatus inicial lo fija el backend
            FechaPlan = datos.FechaPlan,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblRelease.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        contexto.TblHistorialEstatus.Add(new TblHistorialEstatus
        {
            Proceso = "Release",
            IdRegistro = entidad.IdRelease,
            IdEstatus = EstatusRelease.EnPreparacion,
            Accion = "ALTA",
            Usuario = Auditoria.Usuario
        });
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Release", entidad.IdRelease, "CREAR",
            $"{datos.Version} ({datos.Folio})", cancellationToken);
        return entidad.IdRelease;
    }

    public async Task<EstadoRelease?> ObtenerEstadoAsync(
        int idRelease, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblRelease.AsNoTracking()
            .Where(r => r.IdRelease == idRelease)
            .Select(r => new EstadoRelease(
                r.IdRelease, r.IdProyecto, r.Version, r.Folio, r.IdEstatusRelease, r.Activo))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExisteVersionAsync(
        int idProyecto, string version, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblRelease.AsNoTracking()
            .AnyAsync(r => r.IdProyecto == idProyecto && r.Version == version, cancellationToken);
    }

    public async Task ActualizarNotasAsync(
        int idRelease, string notas, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblRelease
            .FirstOrDefaultAsync(r => r.IdRelease == idRelease, cancellationToken)
            ?? throw new InvalidOperationException($"Release {idRelease} no existe.");

        entidad.NotasVersion = notas;
        MarcarMovimiento(entidad);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task AplicarEfectosTransicionAsync(
        int idRelease, string accion, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblRelease
            .FirstOrDefaultAsync(r => r.IdRelease == idRelease, cancellationToken)
            ?? throw new InvalidOperationException($"Release {idRelease} no existe.");

        MarcarMovimiento(entidad);
        await contexto.SaveChangesAsync(cancellationToken);
        await RegistrarBitacoraAsync("Release", idRelease, accion, null, cancellationToken);
    }

    public async Task MarcarLiberadoAsync(int idRelease, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblRelease
            .FirstOrDefaultAsync(r => r.IdRelease == idRelease, cancellationToken)
            ?? throw new InvalidOperationException($"Release {idRelease} no existe.");

        entidad.FechaLiberacion = DateTime.Now;
        MarcarMovimiento(entidad);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    /* ---------- Contenido ---------- */

    public async Task<CandidatoRelease?> ObtenerCandidatoAsync(
        int idWorkItem, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.IdWorkItem == idWorkItem && w.Activo)
            .Select(w => new CandidatoRelease(
                w.IdWorkItem, w.Folio, w.Titulo, w.IdEstatusWorkItem, w.Revisado,
                contexto.TblRevision.Count(r => r.IdWorkItem == w.IdWorkItem && !r.Corregido && r.Activo
                    && r.IdSeveridad != null && r.IdSeveridad <= Severidad.S2Alta)))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AgregarWorkItemAsync(
        int idRelease, int idWorkItem, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var item = await contexto.TblWorkItem
            .FirstOrDefaultAsync(w => w.IdWorkItem == idWorkItem, cancellationToken)
            ?? throw new InvalidOperationException($"WorkItem {idWorkItem} no existe.");

        item.IdRelease = idRelease;
        MarcarMovimientoItem(item);
        await contexto.SaveChangesAsync(cancellationToken);
        await RegistrarBitacoraAsync("Release", idRelease, "AGREGAR_ITEM", item.Folio, cancellationToken);
    }

    public async Task QuitarWorkItemAsync(
        int idRelease, int idWorkItem, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var item = await contexto.TblWorkItem
            .FirstOrDefaultAsync(w => w.IdWorkItem == idWorkItem && w.IdRelease == idRelease, cancellationToken);
        if (item is null)
        {
            return;
        }

        item.IdRelease = null;
        MarcarMovimientoItem(item);
        await contexto.SaveChangesAsync(cancellationToken);
        await RegistrarBitacoraAsync("Release", idRelease, "QUITAR_ITEM", item.Folio, cancellationToken);
    }

    public async Task<IReadOnlyList<CandidatoRelease>> ObtenerContenidoAsync(
        int idRelease, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.IdRelease == idRelease && w.Activo)
            .Select(w => new CandidatoRelease(
                w.IdWorkItem, w.Folio, w.Titulo, w.IdEstatusWorkItem, w.Revisado,
                contexto.TblRevision.Count(r => r.IdWorkItem == w.IdWorkItem && !r.Corregido && r.Activo)))
            .ToListAsync(cancellationToken);
    }

    /* ---------- Artefactos ---------- */

    public async Task<int> AgregarArtefactoAsync(
        ArtefactoNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var artefacto = new TblArtefacto
        {
            Nombre = datos.Nombre,
            IdTipoArtefacto = datos.IdTipoArtefacto,
            HashSha256 = datos.HashSha256,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblArtefacto.Add(artefacto);
        await contexto.SaveChangesAsync(cancellationToken);

        contexto.TblReleaseArtefacto.Add(new TblReleaseArtefacto
        {
            IdRelease = datos.IdRelease,
            IdArtefacto = artefacto.IdArtefacto,
            OrdenEjecucion = datos.OrdenEjecucion,
            IdArtefactoRollback = datos.IdArtefactoRollback,
            JustificacionIrreversible = datos.JustificacionIrreversible,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        });
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Release", datos.IdRelease, "AGREGAR_ARTEFACTO",
            datos.Nombre, cancellationToken);
        return artefacto.IdArtefacto;
    }

    public async Task<IReadOnlyList<ArtefactoRelease>> ObtenerArtefactosAsync(
        int idRelease, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await (
            from ra in contexto.TblReleaseArtefacto.AsNoTracking()
            join a in contexto.TblArtefacto.AsNoTracking() on ra.IdArtefacto equals a.IdArtefacto
            where ra.IdRelease == idRelease && ra.Activo
            select new ArtefactoRelease(
                ra.IdReleaseArtefacto, a.IdArtefacto, a.Nombre, a.IdTipoArtefacto,
                ra.OrdenEjecucion, ra.IdArtefactoRollback, ra.JustificacionIrreversible)
            ).ToListAsync(cancellationToken);
    }

    /* ---------- Aprobaciones ---------- */

    public async Task CrearCadenaAprobacionAsync(
        int idRelease, IReadOnlyList<string> roles, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var existentes = await contexto.TblAprobacion.AsNoTracking()
            .Where(a => a.Entidad == EntidadAprobacion && a.IdEntidad == idRelease && a.Activo)
            .Select(a => a.RolAprobacion)
            .ToListAsync(cancellationToken);

        // El aprobador se asigna al firmar: la fila se crea con el solicitante como marcador
        var idUsuarioMarcador = await contexto.TblUsuario.AsNoTracking()
            .Where(u => u.Dominio == Auditoria.Usuario)
            .Select(u => u.IdUsuario)
            .FirstOrDefaultAsync(cancellationToken);

        foreach (var rol in roles.Where(r => !existentes.Contains(r)))
        {
            contexto.TblAprobacion.Add(new TblAprobacion
            {
                Entidad = EntidadAprobacion,
                IdEntidad = idRelease,
                IdAprobador = idUsuarioMarcador,
                RolAprobacion = rol,
                IdEstatusAprobacion = EstatusAprobacion.Pendiente,
                UsuarioRegistro = Auditoria.Usuario,
                Activo = true
            });
        }
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Release", idRelease, "CREAR_CADENA_APROBACION",
            string.Join(", ", roles), cancellationToken);
    }

    public async Task InvalidarCadenaAprobacionAsync(
        int idRelease, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var vigentes = await contexto.TblAprobacion
            .Where(a => a.Entidad == EntidadAprobacion && a.IdEntidad == idRelease && a.Activo)
            .ToListAsync(cancellationToken);

        foreach (var aprobacion in vigentes)
        {
            aprobacion.Activo = false;
        }
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Release", idRelease, "INVALIDAR_CADENA_APROBACION",
            $"{vigentes.Count} firma(s) dada(s) de baja", cancellationToken);
    }

    public async Task<IReadOnlyList<AprobacionRelease>> ObtenerAprobacionesAsync(
        int idRelease, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblAprobacion.AsNoTracking()
            .Where(a => a.Entidad == EntidadAprobacion && a.IdEntidad == idRelease && a.Activo)
            .Select(a => new AprobacionRelease(
                a.IdAprobacion, a.RolAprobacion, a.IdEstatusAprobacion, a.IdAprobador, a.Comentario))
            .ToListAsync(cancellationToken);
    }

    public async Task ResolverAprobacionAsync(
        int idAprobacion, int idAprobador, bool aprobada, string? comentario, string firmaHash,
        CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblAprobacion
            .FirstOrDefaultAsync(a => a.IdAprobacion == idAprobacion, cancellationToken)
            ?? throw new InvalidOperationException($"Aprobacion {idAprobacion} no existe.");

        entidad.IdAprobador = idAprobador;
        entidad.IdEstatusAprobacion = aprobada ? EstatusAprobacion.Aprobada : EstatusAprobacion.Rechazada;
        entidad.Comentario = comentario;
        entidad.FirmaHash = firmaHash;
        entidad.FechaResolucion = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Aprobacion", idAprobacion,
            aprobada ? "APROBAR" : "RECHAZAR", $"Firma {firmaHash[..16]}", cancellationToken);
    }

    public async Task<AprobacionRelease?> ObtenerAprobacionAsync(
        int idAprobacion, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblAprobacion.AsNoTracking()
            .Where(a => a.IdAprobacion == idAprobacion)
            .Select(a => new AprobacionRelease(
                a.IdAprobacion, a.RolAprobacion, a.IdEstatusAprobacion, a.IdAprobador, a.Comentario))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int?> ObtenerIdReleaseDeAprobacionAsync(
        int idAprobacion, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblAprobacion.AsNoTracking()
            .Where(a => a.IdAprobacion == idAprobacion && a.Entidad == EntidadAprobacion)
            .Select(a => (int?)a.IdEntidad)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /* ---------- Despliegues ---------- */

    public async Task<int> RegistrarDespliegueAsync(
        DespliegueNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblDespliegue
        {
            IdRelease = datos.IdRelease,
            IdAmbiente = datos.IdAmbiente,
            IdEstatusDespliegue = EstatusDespliegue.Exitoso,
            FechaInicio = DateTime.Now,
            FechaFin = DateTime.Now,
            IdEjecutor = datos.IdEjecutor,
            EsRollback = datos.EsRollback,
            Bitacora = datos.Bitacora,
            UsuarioRegistro = Auditoria.Usuario
        };
        contexto.TblDespliegue.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        // Bitacora de cambios del ambiente: responde "que cambio ayer en produccion"
        contexto.TblBitacoraCambio.Add(new TblBitacoraCambio
        {
            IdAmbiente = datos.IdAmbiente,
            IdRelease = datos.IdRelease,
            Descripcion = datos.EsRollback
                ? "Rollback de release"
                : "Despliegue de release",
            Usuario = Auditoria.Usuario,
            Fecha = DateTime.Now
        });
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Release", datos.IdRelease,
            datos.EsRollback ? "ROLLBACK_AMBIENTE" : "DESPLIEGUE",
            $"Ambiente {datos.IdAmbiente}", cancellationToken);
        return entidad.IdDespliegue;
    }

    public async Task<int?> ObtenerAmbienteProduccionAsync(
        int idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        // Ambiente PROD del proyecto, o el global si el proyecto no tiene uno propio
        return await contexto.TblAmbiente.AsNoTracking()
            .Where(a => a.Nombre == "PROD" && a.Activo
                        && (a.IdProyecto == idProyecto || a.IdProyecto == null))
            .OrderByDescending(a => a.IdProyecto.HasValue)
            .Select(a => (int?)a.IdAmbiente)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /* ---------- Calidad del release ---------- */

    public async Task<IReadOnlyList<string>> ObtenerHallazgosCriticosAbiertosAsync(
        int idRelease, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        // WorkItems del contenido del release con un hallazgo S1/S2 sin corregir. La
        // cobertura de pruebas (si el item se probo o no) la decide QA al aprobar la fase
        // En Pruebas del propio item -- este gate solo mira defectos ya encontrados.
        return await (
            from r in contexto.TblRevision.AsNoTracking()
            join w in contexto.TblWorkItem.AsNoTracking() on r.IdWorkItem equals w.IdWorkItem
            where w.IdRelease == idRelease && w.Activo
                  && !r.Corregido && r.Activo
                  && r.IdSeveridad != null && r.IdSeveridad <= Severidad.S2Alta
            select $"{w.Folio} - {w.Titulo}"
            ).Distinct().ToListAsync(cancellationToken);
    }

    private void MarcarMovimiento(TblRelease entidad)
    {
        entidad.UsuarioMovto = Auditoria.Usuario.Length > 50 ? Auditoria.Usuario[..50] : Auditoria.Usuario;
        entidad.FechaMovto = DateTime.Now;
    }

    private void MarcarMovimientoItem(TblWorkItem entidad)
    {
        entidad.UsuarioMovto = Auditoria.Usuario.Length > 50 ? Auditoria.Usuario[..50] : Auditoria.Usuario;
        entidad.FechaMovto = DateTime.Now;
    }
}
