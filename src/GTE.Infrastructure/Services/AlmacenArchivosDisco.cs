using System.Security.Cryptography;
using GTE.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Almacen de archivos en disco/share de red (ADR-07: fase 1 en filesystem, migrable a
/// blob storage cambiando esta clase). El archivo fisico se nombra solo por su GUID (sin
/// extension) repartido en subcarpetas por los 2 primeros caracteres, para que
/// <see cref="ObtenerAsync"/> no necesite mas dato que el GUID; la extension es metadato
/// en BD, nunca parte del path fisico. Ruta configurable por AlmacenArchivos:Ruta; sin
/// configurar, cae a una carpeta local junto al ejecutable (uso en desarrollo).
/// </summary>
public class AlmacenArchivosDisco : IAlmacenArchivos
{
    private readonly string _raiz;

    public AlmacenArchivosDisco(IConfiguration configuracion)
    {
        var ruta = configuracion["AlmacenArchivos:Ruta"];
        _raiz = string.IsNullOrWhiteSpace(ruta)
            ? Path.Combine(AppContext.BaseDirectory, "ArchivosGte")
            : ruta;
    }

    public async Task<ArchivoAlmacenado> GuardarAsync(
        Stream contenido, string nombreArchivo, CancellationToken cancellationToken = default)
    {
        var guid = Guid.NewGuid();
        var (rutaRelativa, rutaFisica) = ResolverRutas(guid);
        Directory.CreateDirectory(Path.GetDirectoryName(rutaFisica)!);

        string hash;
        await using (var destino = new FileStream(rutaFisica, FileMode.CreateNew, FileAccess.Write))
        {
            using var sha256 = SHA256.Create();
            await using (var flujoHash = new CryptoStream(destino, sha256, CryptoStreamMode.Write, leaveOpen: true))
            {
                await contenido.CopyToAsync(flujoHash, cancellationToken);
                await flujoHash.FlushFinalBlockAsync(cancellationToken);
            }
            hash = Convert.ToHexString(sha256.Hash!);
        }

        var tamanoBytes = new FileInfo(rutaFisica).Length;
        return new ArchivoAlmacenado(
            guid, nombreArchivo, Path.GetExtension(nombreArchivo), tamanoBytes, hash, rutaRelativa);
    }

    public Task<Stream> ObtenerAsync(Guid guidArchivo, CancellationToken cancellationToken = default)
    {
        var (_, rutaFisica) = ResolverRutas(guidArchivo);
        Stream flujo = new FileStream(rutaFisica, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(flujo);
    }

    public Task EliminarAsync(Guid guidArchivo, CancellationToken cancellationToken = default)
    {
        var (_, rutaFisica) = ResolverRutas(guidArchivo);
        if (File.Exists(rutaFisica))
        {
            File.Delete(rutaFisica);
        }
        return Task.CompletedTask;
    }

    private (string RutaRelativa, string RutaFisica) ResolverRutas(Guid guid)
    {
        var texto = guid.ToString("N");
        var subcarpeta = texto[..2];
        var rutaRelativa = Path.Combine(subcarpeta, texto);
        return (rutaRelativa, Path.Combine(_raiz, subcarpeta, texto));
    }
}
