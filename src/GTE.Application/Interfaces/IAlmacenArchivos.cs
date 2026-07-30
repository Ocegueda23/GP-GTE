namespace GTE.Application.Interfaces;

/// <summary>Metadatos de un archivo almacenado.</summary>
public record ArchivoAlmacenado(
    Guid GuidArchivo,
    string NombreArchivo,
    string Extension,
    long TamanoBytes,
    string HashSha256);

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
}
