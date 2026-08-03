using GTE.Application.Catalogos.Queries;
using GTE.Application.DTOs.Responses.Catalogos;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class CatalogosQueryService(FabricaContexto fabrica) : ICatalogosQueryService
{
    public async Task<CatalogosBandejaResponse> ObtenerCatalogosBandejaAsync(
        CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        return new CatalogosBandejaResponse
        {
            Estatus = await contexto.TblEstatusWorkItem.AsNoTracking()
                .Where(e => e.Activo)
                .OrderBy(e => e.Orden)
                .Select(e => new CatalogoItemResponse { Id = e.Id, Nombre = e.Descripcion })
                .ToListAsync(cancellationToken),
            Tipos = await contexto.TblTipoWorkItem.AsNoTracking()
                .Where(t => t.Activo)
                .OrderBy(t => t.Id)
                .Select(t => new CatalogoItemResponse { Id = t.Id, Nombre = t.Nombre })
                .ToListAsync(cancellationToken),
            Prioridades = await contexto.TblPrioridad.AsNoTracking()
                .Where(p => p.Activo)
                .OrderBy(p => p.Id)
                .Select(p => new CatalogoItemResponse { Id = p.Id, Nombre = p.Nombre })
                .ToListAsync(cancellationToken),
            Proyectos = await contexto.TblProyecto.AsNoTracking()
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .Select(p => new ProyectoItemResponse { Id = p.IdProyecto, Clave = p.Clave, Nombre = p.Nombre })
                .ToListAsync(cancellationToken),
            Usuarios = await contexto.TblUsuario.AsNoTracking()
                .Where(u => u.Activo)
                .OrderBy(u => u.Nombre)
                .Select(u => new CatalogoItemResponse { Id = u.IdUsuario, Nombre = u.Nombre })
                .ToListAsync(cancellationToken),
            TiposSolicitud = await contexto.TblTipoSolicitud.AsNoTracking()
                .Where(t => t.Activo)
                .OrderBy(t => t.Id)
                .Select(t => new CatalogoItemResponse { Id = t.Id, Nombre = t.Nombre })
                .ToListAsync(cancellationToken),
            Equipos = await contexto.TblEquipo.AsNoTracking()
                .Where(e => e.Activo)
                .OrderBy(e => e.Nombre)
                .Select(e => new CatalogoItemResponse { Id = e.IdEquipo, Nombre = e.Nombre })
                .ToListAsync(cancellationToken),
            Complejidades = await contexto.TblComplejidad.AsNoTracking()
                .Where(c => c.Activo)
                .OrderBy(c => c.Orden)
                .Select(c => new CatalogoItemResponse { Id = c.IdComplejidad, Nombre = c.Nombre })
                .ToListAsync(cancellationToken),
            CategoriasTicket = await contexto.TblCategoriaTicket.AsNoTracking()
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .Select(c => new CatalogoItemResponse { Id = c.IdCategoriaTicket, Nombre = c.Nombre })
                .ToListAsync(cancellationToken),
            EstatusTicket = await contexto.TblEstatusTicket.AsNoTracking()
                .Where(e => e.Activo)
                .OrderBy(e => e.Orden)
                .Select(e => new CatalogoItemResponse { Id = e.Id, Nombre = e.Descripcion })
                .ToListAsync(cancellationToken),
            Severidades = await contexto.TblSeveridad.AsNoTracking()
                .Where(s => s.Activo)
                .OrderBy(s => s.Id)
                .Select(s => new CatalogoItemResponse { Id = s.Id, Nombre = s.Nombre })
                .ToListAsync(cancellationToken),
            UsuariosSolicitantes = await contexto.TblUsuarioSolicitante.AsNoTracking()
                .Where(u => u.Activo)
                .OrderBy(u => u.Nombre)
                .Select(u => new CatalogoItemResponse { Id = u.IdUsuarioSolicitante, Nombre = (u.Nombre ?? u.Usuario) ?? string.Empty })
                .ToListAsync(cancellationToken),
            Locaciones = await contexto.TblLocacion.AsNoTracking()
                .Where(l => l.Activo == true)
                .OrderBy(l => l.Locacion)
                .Select(l => new CatalogoItemResponse { Id = l.IdLocacion, Nombre = (l.Locacion ?? l.Descripcion) ?? string.Empty })
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<CatalogosAdministracionResponse> ObtenerCatalogosAdministracionAsync(
        CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        return new CatalogosAdministracionResponse
        {
            CategoriasProyecto = await contexto.TblCategoriaProyecto.AsNoTracking()
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .Select(c => new CatalogoItemResponse { Id = c.Id, Nombre = c.Nombre })
                .ToListAsync(cancellationToken),
            EstatusProyecto = await contexto.TblEstatusProyecto.AsNoTracking()
                .Where(e => e.Activo)
                .OrderBy(e => e.Orden)
                .Select(e => new CatalogoItemResponse { Id = e.Id, Nombre = e.Descripcion })
                .ToListAsync(cancellationToken),
            Niveles = await contexto.TblNivel.AsNoTracking()
                .Where(n => n.Activo)
                .OrderBy(n => n.Orden)
                .Select(n => new CatalogoItemResponse { Id = n.IdNivel, Nombre = n.Nombre })
                .ToListAsync(cancellationToken),
            Areas = await contexto.TblArea.AsNoTracking()
                .Where(a => a.Activo)
                .OrderBy(a => a.Nombre)
                .Select(a => new CatalogoItemResponse { Id = a.IdArea, Nombre = a.Nombre })
                .ToListAsync(cancellationToken),
            Puestos = await contexto.TblPuesto.AsNoTracking()
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .Select(p => new CatalogoItemResponse { Id = p.IdPuesto, Nombre = p.Nombre })
                .ToListAsync(cancellationToken),
            Usuarios = await contexto.TblUsuario.AsNoTracking()
                .Where(u => u.Activo)
                .OrderBy(u => u.Nombre)
                .Select(u => new CatalogoItemResponse { Id = u.IdUsuario, Nombre = u.Nombre })
                .ToListAsync(cancellationToken),
            Equipos = await contexto.TblEquipo.AsNoTracking()
                .Where(e => e.Activo)
                .OrderBy(e => e.Nombre)
                .Select(e => new CatalogoItemResponse { Id = e.IdEquipo, Nombre = e.Nombre })
                .ToListAsync(cancellationToken),
            Roles = await contexto.TblRol.AsNoTracking()
                .Where(r => r.Activo)
                .OrderBy(r => r.Nombre)
                .Select(r => new CatalogoItemResponse { Id = r.IdRol, Nombre = r.Nombre })
                .ToListAsync(cancellationToken),
            Horarios = await contexto.TblHorario.AsNoTracking()
                .Where(h => h.Activo)
                .OrderBy(h => h.Nombre)
                .Select(h => new CatalogoItemResponse { Id = h.IdHorario, Nombre = h.Nombre })
                .ToListAsync(cancellationToken)
        };
    }
}
