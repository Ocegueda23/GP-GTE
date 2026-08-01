namespace GTE.Domain.Archivos;

/// <summary>Archivo ya guardado en el almacen, listo para vincularse a una entidad.</summary>
public record ArchivoNuevo(
    string Entidad,
    int IdEntidad,
    Guid GuidArchivo,
    string NombreArchivo,
    string? Extension,
    long TamanoBytes,
    string RutaRelativa,
    string? HashSha256);

/// <summary>Vinculo entre un archivo y la entidad a la que quedo adjunto.</summary>
public record EstadoArchivoVinculo(
    int IdArchivoVinculo,
    int IdArchivo,
    Guid GuidArchivo,
    string UsuarioRegistro,
    bool Activo);

/// <summary>Metadatos minimos para servir una descarga por streaming.</summary>
public record ArchivoDescarga(Guid GuidArchivo, string NombreArchivo, string? Extension);
