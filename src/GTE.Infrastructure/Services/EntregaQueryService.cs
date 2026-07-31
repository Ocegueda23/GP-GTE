using System.Text;
using GTE.Application.DTOs.Responses.Entregas;
using GTE.Application.Interfaces;
using GTE.Domain.Entregas;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class EntregaQueryService(FabricaContexto fabrica) : IEntregaQueryService
{
    public async Task<IReadOnlyList<ReleaseResponse>> ObtenerReleasesAsync(
        int? idProyecto, bool soloAbiertos, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var consulta = Proyectar(contexto);
        if (idProyecto.HasValue)
        {
            consulta = consulta.Where(r => r.IdProyecto == idProyecto.Value);
        }
        if (soloAbiertos)
        {
            consulta = consulta.Where(r => r.IdEstatus != EstatusRelease.Cancelado);
        }

        return await consulta.OrderByDescending(r => r.IdRelease).ToListAsync(cancellationToken);
    }

    public async Task<ReleaseDetalleResponse?> ObtenerDetalleAsync(
        int idRelease, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var cabecera = await Proyectar(contexto)
            .FirstOrDefaultAsync(r => r.IdRelease == idRelease, cancellationToken);
        if (cabecera is null)
        {
            return null;
        }

        var detalle = new ReleaseDetalleResponse
        {
            IdRelease = cabecera.IdRelease,
            IdProyecto = cabecera.IdProyecto,
            Proyecto = cabecera.Proyecto,
            ClaveProyecto = cabecera.ClaveProyecto,
            Version = cabecera.Version,
            Folio = cabecera.Folio,
            NotasVersion = cabecera.NotasVersion,
            IdEstatus = cabecera.IdEstatus,
            Estatus = cabecera.Estatus,
            FechaPlan = cabecera.FechaPlan,
            FechaLiberacion = cabecera.FechaLiberacion,
            TotalItems = cabecera.TotalItems,
            TotalArtefactos = cabecera.TotalArtefactos,
            AprobacionesPendientes = cabecera.AprobacionesPendientes
        };

        detalle.Items = await (
            from w in contexto.TblWorkItem.AsNoTracking()
            join t in contexto.TblTipoWorkItem.AsNoTracking() on w.IdTipoWorkItem equals t.Id
            join e in contexto.TblEstatusWorkItem.AsNoTracking() on w.IdEstatusWorkItem equals e.Id
            where w.IdRelease == idRelease && w.Activo
            orderby t.Id, w.IdWorkItem
            select new ItemReleaseResponse
            {
                IdWorkItem = w.IdWorkItem,
                Folio = w.Folio,
                Titulo = w.Titulo,
                Tipo = t.Nombre,
                Estatus = e.Descripcion
            }).ToListAsync(cancellationToken);

        var artefactos = await (
            from ra in contexto.TblReleaseArtefacto.AsNoTracking()
            join a in contexto.TblArtefacto.AsNoTracking() on ra.IdArtefacto equals a.IdArtefacto
            join t in contexto.TblTipoArtefacto.AsNoTracking() on a.IdTipoArtefacto equals t.Id
            join ar in contexto.TblArtefacto.AsNoTracking() on ra.IdArtefactoRollback equals ar.IdArtefacto
                into rollbacks
            from ar in rollbacks.DefaultIfEmpty()
            where ra.IdRelease == idRelease && ra.Activo
            orderby ra.OrdenEjecucion, a.IdArtefacto
            select new ArtefactoResponse
            {
                IdArtefacto = a.IdArtefacto,
                Nombre = a.Nombre,
                Tipo = t.Nombre,
                IdTipoArtefacto = a.IdTipoArtefacto,
                HashSha256 = a.HashSha256,
                OrdenEjecucion = ra.OrdenEjecucion,
                IdArtefactoRollback = ra.IdArtefactoRollback,
                NombreRollback = ar != null ? ar.Nombre : null,
                JustificacionIrreversible = ra.JustificacionIrreversible
            }).ToListAsync(cancellationToken);

        // RN-REL-02 evaluada para la interfaz: los scripts SQL necesitan rollback o justificacion
        foreach (var artefacto in artefactos)
        {
            artefacto.RequiereRollback = artefacto.IdTipoArtefacto == TipoArtefacto.ScriptSql;
            artefacto.CumpleRollback = !artefacto.RequiereRollback
                || artefacto.IdArtefactoRollback.HasValue
                || !string.IsNullOrWhiteSpace(artefacto.JustificacionIrreversible);
        }
        detalle.Artefactos = artefactos;

        detalle.Aprobaciones = await (
            from ap in contexto.TblAprobacion.AsNoTracking()
            join e in contexto.TblEstatusAprobacion.AsNoTracking() on ap.IdEstatusAprobacion equals e.Id
            join u in contexto.TblUsuario.AsNoTracking() on ap.IdAprobador equals u.IdUsuario into usuarios
            from u in usuarios.DefaultIfEmpty()
            where ap.Entidad == "Release" && ap.IdEntidad == idRelease && ap.Activo
            orderby ap.IdAprobacion
            select new AprobacionResponse
            {
                IdAprobacion = ap.IdAprobacion,
                RolAprobacion = ap.RolAprobacion,
                IdEstatus = ap.IdEstatusAprobacion,
                Estatus = e.Descripcion,
                Aprobador = ap.FechaResolucion != null && u != null ? u.Nombre : null,
                Comentario = ap.Comentario,
                FechaResolucion = ap.FechaResolucion,
                FirmaHash = ap.FirmaHash
            }).ToListAsync(cancellationToken);

        detalle.Despliegues = await (
            from d in contexto.TblDespliegue.AsNoTracking()
            join a in contexto.TblAmbiente.AsNoTracking() on d.IdAmbiente equals a.IdAmbiente
            join e in contexto.TblEstatusDespliegue.AsNoTracking() on d.IdEstatusDespliegue equals e.Id
            join u in contexto.TblUsuario.AsNoTracking() on d.IdEjecutor equals u.IdUsuario into usuarios
            from u in usuarios.DefaultIfEmpty()
            where d.IdRelease == idRelease
            orderby d.IdDespliegue descending
            select new DespliegueResponse
            {
                IdDespliegue = d.IdDespliegue,
                Ambiente = a.Nombre,
                Estatus = e.Descripcion,
                EsRollback = d.EsRollback,
                Ejecutor = u != null ? u.Nombre : null,
                FechaInicio = d.FechaInicio,
                FechaFin = d.FechaFin,
                Bitacora = d.Bitacora
            }).ToListAsync(cancellationToken);

        return detalle;
    }

    public async Task<IReadOnlyList<MatrizAmbienteResponse>> ObtenerMatrizAmbientesAsync(
        CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var ambientes = await contexto.TblAmbiente.AsNoTracking()
            .Where(a => a.Activo)
            .Select(a => new { a.IdAmbiente, a.Nombre, a.IdProyecto })
            .ToListAsync(cancellationToken);

        var resultado = new List<MatrizAmbienteResponse>();
        foreach (var ambiente in ambientes)
        {
            var ultimo = await (
                from d in contexto.TblDespliegue.AsNoTracking()
                join r in contexto.TblRelease.AsNoTracking() on d.IdRelease equals r.IdRelease
                join p in contexto.TblProyecto.AsNoTracking() on r.IdProyecto equals p.IdProyecto
                where d.IdAmbiente == ambiente.IdAmbiente && !d.EsRollback
                orderby d.IdDespliegue descending
                select new { r.Version, p.Clave, d.FechaInicio }
                ).FirstOrDefaultAsync(cancellationToken);

            resultado.Add(new MatrizAmbienteResponse
            {
                IdAmbiente = ambiente.IdAmbiente,
                Ambiente = ambiente.Nombre,
                ClaveProyecto = ultimo?.Clave,
                VersionDesplegada = ultimo?.Version,
                FechaDespliegue = ultimo?.FechaInicio
            });
        }
        return resultado;
    }

    /// <summary>Notas armadas del contenido real, agrupadas por tipo de elemento.</summary>
    public async Task<string> GenerarNotasAsync(int idRelease, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var release = await contexto.TblRelease.AsNoTracking()
            .Where(r => r.IdRelease == idRelease)
            .Select(r => new { r.Version, r.FechaPlan })
            .FirstOrDefaultAsync(cancellationToken);
        if (release is null)
        {
            return string.Empty;
        }

        var items = await (
            from w in contexto.TblWorkItem.AsNoTracking()
            join t in contexto.TblTipoWorkItem.AsNoTracking() on w.IdTipoWorkItem equals t.Id
            where w.IdRelease == idRelease && w.Activo
            orderby t.Id, w.IdWorkItem
            select new { Tipo = t.Nombre, w.Folio, w.Titulo }
            ).ToListAsync(cancellationToken);

        var texto = new StringBuilder();
        texto.AppendLine($"Version {release.Version}");
        if (release.FechaPlan.HasValue)
        {
            texto.AppendLine($"Fecha planeada: {release.FechaPlan:yyyy-MM-dd}");
        }
        texto.AppendLine();

        if (items.Count == 0)
        {
            texto.AppendLine("Sin contenido registrado.");
            return texto.ToString();
        }

        foreach (var grupo in items.GroupBy(i => i.Tipo))
        {
            texto.AppendLine($"{grupo.Key}:");
            foreach (var item in grupo)
            {
                texto.AppendLine($"- {item.Folio} {item.Titulo}");
            }
            texto.AppendLine();
        }

        return texto.ToString().TrimEnd();
    }

    private static IQueryable<ReleaseResponse> Proyectar(DbContextGTE contexto)
    {
        return from r in contexto.TblRelease.AsNoTracking()
               join p in contexto.TblProyecto.AsNoTracking() on r.IdProyecto equals p.IdProyecto
               join e in contexto.TblEstatusRelease.AsNoTracking() on r.IdEstatusRelease equals e.Id
               where r.Activo
               select new ReleaseResponse
               {
                   IdRelease = r.IdRelease,
                   IdProyecto = r.IdProyecto,
                   Proyecto = p.Nombre,
                   ClaveProyecto = p.Clave,
                   Version = r.Version,
                   Folio = r.Folio,
                   NotasVersion = r.NotasVersion,
                   IdEstatus = r.IdEstatusRelease,
                   Estatus = e.Descripcion,
                   FechaPlan = r.FechaPlan,
                   FechaLiberacion = r.FechaLiberacion,
                   TotalItems = contexto.TblWorkItem.Count(w => w.IdRelease == r.IdRelease && w.Activo),
                   TotalArtefactos = contexto.TblReleaseArtefacto
                       .Count(ra => ra.IdRelease == r.IdRelease && ra.Activo),
                   AprobacionesPendientes = contexto.TblAprobacion
                       .Count(ap => ap.Entidad == "Release" && ap.IdEntidad == r.IdRelease
                                    && ap.Activo && ap.IdEstatusAprobacion == EstatusAprobacion.Pendiente)
               };
    }
}
