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

    public async Task CrearBorradorAsync(ArchivoBorrador datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        contexto.TblArchivo.Add(new TblArchivo
        {
            GuidArchivo = datos.GuidArchivo,
            NombreArchivo = datos.NombreArchivo,
            Extension = datos.Extension,
            TamanoBytes = datos.TamanoBytes,
            RutaRelativa = datos.RutaRelativa,
            HashSha256 = datos.HashSha256,
            UsuarioRegistro = Auditoria.Usuario,
            // Trampa EF (CLAUDE.md): el DEFAULT 1 de la columna bit no aplica de forma
            // confiable en los INSERT de EF, se fija explicitamente.
            Activo = true
        });
        await contexto.SaveChangesAsync(cancellationToken);

        // Sin bitacora: todavia no hay entidad de negocio a la que colgar el movimiento. El
        // ADJUNTAR se registra al vincular, que es cuando el archivo pasa a ser parte de algo.
    }

    public async Task<int> VincularBorradoresAsync(
        string entidad, int idEntidad, IReadOnlyCollection<Guid> guids,
        CancellationToken cancellationToken = default)
    {
        if (guids.Count == 0)
        {
            return 0;
        }

        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        // Solo archivos propios y todavia sin vincular: un GUID ajeno inyectado en el HTML no
        // debe poder adjuntar aqui el archivo de otra entidad ni el de otro usuario.
        var candidatos = await contexto.TblArchivo
            .Where(a => guids.Contains(a.GuidArchivo)
                && a.Activo
                && a.UsuarioRegistro == Auditoria.Usuario
                && !a.TblArchivoVinculo.Any())
            .Select(a => new { a.IdArchivo, a.NombreArchivo })
            .ToListAsync(cancellationToken);

        if (candidatos.Count == 0)
        {
            return 0;
        }

        foreach (var candidato in candidatos)
        {
            contexto.TblArchivoVinculo.Add(new TblArchivoVinculo
            {
                IdArchivo = candidato.IdArchivo,
                Entidad = entidad,
                IdEntidad = idEntidad,
                UsuarioRegistro = Auditoria.Usuario,
                Activo = true
            });
        }
        await contexto.SaveChangesAsync(cancellationToken);

        foreach (var candidato in candidatos)
        {
            await RegistrarBitacoraAsync(entidad, idEntidad, "ADJUNTAR", candidato.NombreArchivo, cancellationToken);
        }

        return candidatos.Count;
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
        // Un archivo en borrador (sin ningun vinculo todavia) solo lo ve quien lo subio: asi la
        // imagen pegada se muestra en el formulario de alta antes de que exista la entidad, sin
        // abrir el archivo a nadie mas. Ya vinculado, manda el vinculo activo como siempre.
        // Sin identidad (lectura publica de la base de conocimiento) los borradores no aplican:
        // Auditoria.Usuario viene vacio y no debe empatar con nada.
        var usuario = Auditoria.TieneIdentidad ? Auditoria.Usuario : null;

        return await contexto.TblArchivo.AsNoTracking()
            .Where(a => a.GuidArchivo == guidArchivo && a.Activo
                && (a.TblArchivoVinculo.Any(v => v.Activo)
                    || (usuario != null && !a.TblArchivoVinculo.Any() && a.UsuarioRegistro == usuario)))
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
