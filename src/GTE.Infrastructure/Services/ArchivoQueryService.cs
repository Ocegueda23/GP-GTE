using GTE.Application.DTOs.Responses.Archivos;
using GTE.Application.Interfaces;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class ArchivoQueryService(FabricaContexto fabrica) : IArchivoQueryService
{
    public async Task<IReadOnlyList<ArchivoResponse>> ObtenerPorEntidadAsync(
        string entidad, int idEntidad, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var baseQuery = contexto.TblArchivoVinculo.AsNoTracking()
            .Where(v => v.Entidad == entidad && v.IdEntidad == idEntidad && v.Activo);
        return await Proyectar(baseQuery, contexto)
            .OrderByDescending(a => a.FechaRegistro)
            .ToListAsync(cancellationToken);
    }

    public async Task<ArchivoResponse?> ObtenerPorVinculoAsync(
        int idArchivoVinculo, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var baseQuery = contexto.TblArchivoVinculo.AsNoTracking()
            .Where(v => v.IdArchivoVinculo == idArchivoVinculo);
        return await Proyectar(baseQuery, contexto).FirstOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<ArchivoResponse> Proyectar(
        IQueryable<TblArchivoVinculo> vinculos, DbContextGTE contexto)
    {
        return from v in vinculos
               join a in contexto.TblArchivo.AsNoTracking() on v.IdArchivo equals a.IdArchivo
               join u in contexto.TblUsuario.AsNoTracking() on v.UsuarioRegistro equals u.Dominio into usuarios
               from u in usuarios.DefaultIfEmpty()
               select new ArchivoResponse
               {
                   IdArchivoVinculo = v.IdArchivoVinculo,
                   GuidArchivo = a.GuidArchivo,
                   NombreArchivo = a.NombreArchivo,
                   Extension = a.Extension,
                   TamanoBytes = a.TamanoBytes,
                   Autor = u != null ? u.Nombre : v.UsuarioRegistro,
                   UsuarioRegistro = v.UsuarioRegistro,
                   FechaRegistro = v.FechaRegistro
               };
    }
}
