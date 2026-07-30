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
                .ToListAsync(cancellationToken)
        };
    }
}
