using GTE.Application.DTOs.Responses.Comentarios;
using GTE.Application.Interfaces;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class ComentarioQueryService(FabricaContexto fabrica) : IComentarioQueryService
{
    public async Task<IReadOnlyList<ComentarioResponse>> ObtenerPorEntidadAsync(
        string entidad, int idEntidad, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var baseQuery = contexto.TblComentario.AsNoTracking()
            .Where(c => c.Entidad == entidad && c.IdEntidad == idEntidad && c.Activo);
        return await Proyectar(baseQuery, contexto)
            .OrderBy(c => c.FechaRegistro)
            .ToListAsync(cancellationToken);
    }

    public async Task<ComentarioResponse?> ObtenerPorIdAsync(
        int idComentario, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var baseQuery = contexto.TblComentario.AsNoTracking().Where(c => c.IdComentario == idComentario);
        return await Proyectar(baseQuery, contexto).FirstOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<ComentarioResponse> Proyectar(
        IQueryable<TblComentario> comentarios, DbContextGTE contexto)
    {
        return from c in comentarios
               join u in contexto.TblUsuario.AsNoTracking() on c.UsuarioRegistro equals u.Dominio into usuarios
               from u in usuarios.DefaultIfEmpty()
               select new ComentarioResponse
               {
                   IdComentario = c.IdComentario,
                   IdWorkItem = c.IdEntidad,
                   Contenido = c.Contenido,
                   IdComentarioPadre = c.IdComentarioPadre,
                   Autor = u != null ? u.Nombre : c.UsuarioRegistro,
                   UsuarioRegistro = c.UsuarioRegistro,
                   FechaRegistro = c.FechaRegistro
               };
    }
}
