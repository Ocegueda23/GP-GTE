using System.Security.Cryptography;
using GTE.Domain.Exceptions;
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

    /// <summary>
    /// Ruta raiz ya resuelta. La expone el diagnostico de almacen: cuando la ruta
    /// configurada apunta a una unidad o share que no existe en el servidor, TODA subida
    /// falla y sin esto el error llega como un INTERNAL_ERROR sin pistas.
    /// </summary>
    public string Raiz => _raiz;

    public async Task<ArchivoAlmacenado> GuardarAsync(
        Stream contenido, string nombreArchivo, CancellationToken cancellationToken = default)
    {
        var guid = Guid.NewGuid();
        var (rutaRelativa, rutaFisica) = ResolverRutas(guid);

        // Se envuelve el acceso al disco para que el mensaje diga QUE ruta fallo: es el
        // modo de falla mas comun al desplegar (unidad o share que no existe en el
        // servidor, o sin permiso de escritura para la cuenta del servicio).
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(rutaFisica)!);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            throw new AlmacenArchivosNoDisponibleException(_raiz, ex);
        }

        string hash;
        try
        {
            await using var destino = new FileStream(rutaFisica, FileMode.CreateNew, FileAccess.Write);
            using var sha256 = SHA256.Create();
            await using (var flujoHash = new CryptoStream(destino, sha256, CryptoStreamMode.Write, leaveOpen: true))
            {
                await contenido.CopyToAsync(flujoHash, cancellationToken);
                await flujoHash.FlushFinalBlockAsync(cancellationToken);
            }
            hash = Convert.ToHexString(sha256.Hash!);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new AlmacenArchivosNoDisponibleException(_raiz, ex);
        }

        var tamanoBytes = new FileInfo(rutaFisica).Length;
        return new ArchivoAlmacenado(
            guid, nombreArchivo, Path.GetExtension(nombreArchivo), tamanoBytes, hash, rutaRelativa);
    }

    public Task<Stream> ObtenerAsync(Guid guidArchivo, CancellationToken cancellationToken = default)
    {
        var (_, rutaFisica) = ResolverRutas(guidArchivo);
        if (!File.Exists(rutaFisica))
        {
            // El registro en BD existe pero el binario no esta donde deberia. Pasa cuando la
            // ruta del almacen cambio entre despliegues y los archivos viejos se quedaron en
            // la anterior. Se distingue de un fallo real del disco para que la imagen rota
            // no se reporte como error interno del sistema.
            throw new ArchivoNoEncontradoException(guidArchivo, rutaFisica);
        }

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

    /// <summary>
    /// Comprueba que la raiz existe y acepta escritura, escribiendo y borrando un archivo
    /// sonda. Lo usan el chequeo de arranque y el endpoint de diagnostico.
    /// </summary>
    public ResultadoSondaAlmacen Verificar()
    {
        try
        {
            Directory.CreateDirectory(_raiz);
            var sonda = Path.Combine(_raiz, $".sonda-{Guid.NewGuid():N}");
            File.WriteAllBytes(sonda, [0]);
            File.Delete(sonda);
            return new ResultadoSondaAlmacen(_raiz, true, true, null);
        }
        catch (Exception ex)
        {
            return new ResultadoSondaAlmacen(_raiz, Directory.Exists(_raiz), false, ex.Message);
        }
    }

    private (string RutaRelativa, string RutaFisica) ResolverRutas(Guid guid)
    {
        var texto = guid.ToString("N");
        var subcarpeta = texto[..2];
        var rutaRelativa = Path.Combine(subcarpeta, texto);
        return (rutaRelativa, Path.Combine(_raiz, subcarpeta, texto));
    }
}
