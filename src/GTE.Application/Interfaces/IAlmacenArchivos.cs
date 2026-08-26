namespace GTE.Application.Interfaces;

/// <summary>Metadatos de un archivo almacenado.</summary>
public record ArchivoAlmacenado(
    Guid GuidArchivo,
    string NombreArchivo,
    string Extension,
    long TamanoBytes,
    string HashSha256,
    string RutaRelativa);

/// <summary>Resultado de la sonda de escritura del almacen (diagnostico de despliegue).</summary>
public record ResultadoSondaAlmacen(
    string Raiz,
    bool Existe,
    bool SePuedeEscribir,
    string? Error);

/// <summary>
/// Almacen de archivos por GUID (share de red en fase 1, migrable a blob storage).
/// Los binarios nunca viven en la base de datos; la BD guarda solo metadatos y vinculos.
/// </summary>
public interface IAlmacenArchivos
{
    Task<ArchivoAlmacenado> GuardarAsync(
        Stream contenido,
        string nombreArchivo,
        CancellationToken cancellationToken = default);

    Task<Stream> ObtenerAsync(Guid guidArchivo, CancellationToken cancellationToken = default);

    Task EliminarAsync(Guid guidArchivo, CancellationToken cancellationToken = default);

    /// <summary>Ruta raiz ya resuelta, para diagnostico.</summary>
    string Raiz { get; }

    /// <summary>
    /// Comprueba que el almacen existe y acepta escritura. Se usa en el arranque y en el
    /// endpoint de diagnostico: una ruta mal configurada rompe TODA subida, y hasta ahora
    /// solo se notaba archivo por archivo.
    /// </summary>
    ResultadoSondaAlmacen Verificar();
}
