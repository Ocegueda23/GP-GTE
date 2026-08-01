using GTE.Application.Common;
using GTE.Domain.Archivos;
using GTE.Domain.Interfaces;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

public class ArchivoRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), IArchivoRepository
{
    public async Task<EstadoArchivoVinculo> VincularAsync(
        ArchivoNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var archivo = new TblArchivo
        {
            GuidArchivo = datos.GuidArchivo,
            NombreArchivo = datos.NombreArchivo,
            Extension = datos.Extension,
            TamanoBytes = datos.TamanoBytes,
            RutaRelativa = datos.RutaRelativa,
            HashSha256 = datos.HashSha256,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblArchivo.Add(archivo);
        await contexto.SaveChangesAsync(cancellationToken);

        var vinculo = new TblArchivoVinculo
        {
            IdArchivo = archivo.IdArchivo,
            Entidad = datos.Entidad,
            IdEntidad = datos.IdEntidad,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblArchivoVinculo.Add(vinculo);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(datos.Entidad, datos.IdEntidad, "ADJUNTAR", datos.NombreArchivo, cancellationToken);

        return new EstadoArchivoVinculo(
            vinculo.IdArchivoVinculo, archivo.IdArchivo, archivo.GuidArchivo, vinculo.UsuarioRegistro, vinculo.Activo);
    }

    public async Task<EstadoArchivoVinculo?> ObtenerVinculoAsync(
        int idArchivoVinculo, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblArchivoVinculo.AsNoTracking()
            .Where(v => v.IdArchivoVinculo == idArchivoVinculo)
            .Select(v => new EstadoArchivoVinculo(
                v.IdArchivoVinculo, v.IdArchivo, v.IdArchivoNavigation.GuidArchivo, v.UsuarioRegistro, v.Activo))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ArchivoDescarga?> ObtenerDescargaAsync(
        Guid guidArchivo, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblArchivo.AsNoTracking()
            .Where(a => a.GuidArchivo == guidArchivo && a.Activo && a.TblArchivoVinculo.Any(v => v.Activo))
            .Select(a => new ArchivoDescarga(a.GuidArchivo, a.NombreArchivo, a.Extension))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task DesvincularAsync(int idArchivoVinculo, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var vinculo = await contexto.TblArchivoVinculo
            .FirstOrDefaultAsync(v => v.IdArchivoVinculo == idArchivoVinculo, cancellationToken)
            ?? throw new InvalidOperationException($"Vinculo de archivo {idArchivoVinculo} no existe.");

        vinculo.Activo = false;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(vinculo.Entidad, vinculo.IdEntidad, "ELIMINAR_ADJUNTO", null, cancellationToken);
    }
}
