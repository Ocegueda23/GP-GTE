using GTE.Application.Common;
using GTE.Domain.Comentarios;
using GTE.Domain.Interfaces;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

public class ComentarioRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), IComentarioRepository
{
    public async Task<int> CrearAsync(ComentarioNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblComentario
        {
            Entidad = datos.Entidad,
            IdEntidad = datos.IdEntidad,
            Contenido = datos.Contenido,
            IdComentarioPadre = datos.IdComentarioPadre,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblComentario.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(datos.Entidad, datos.IdEntidad, "COMENTAR", null, cancellationToken);
        return entidad.IdComentario;
    }

    public async Task<EstadoComentario?> ObtenerEstadoAsync(
        int idComentario, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblComentario.AsNoTracking()
            .Where(c => c.IdComentario == idComentario)
            .Select(c => new EstadoComentario(c.IdComentario, c.UsuarioRegistro, c.Activo))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task EliminarAsync(int idComentario, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblComentario
            .FirstOrDefaultAsync(c => c.IdComentario == idComentario, cancellationToken)
            ?? throw new InvalidOperationException($"Comentario {idComentario} no existe.");

        entidad.Activo = false;
        entidad.UsuarioMovto = Auditoria.Usuario.Length > 50 ? Auditoria.Usuario[..50] : Auditoria.Usuario;
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(entidad.Entidad, entidad.IdEntidad, "ELIMINAR_COMENTARIO", null, cancellationToken);
    }
}
