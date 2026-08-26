namespace GTE.Application.DTOs.Responses.Archivos;

public class ArchivoResponse
{
    public int IdArchivoVinculo { get; set; }
    public Guid GuidArchivo { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string? Extension { get; set; }
    public long TamanoBytes { get; set; }
    public string Autor { get; set; } = string.Empty;
    public string UsuarioRegistro { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
}

/// <summary>
/// Imagen subida en borrador: todavia no hay vinculo (ni IdArchivoVinculo) porque la entidad
/// destino no existe. El editor solo necesita el GUID para referenciarla en el contenido.
/// </summary>
public class ArchivoBorradorResponse
{
    public Guid GuidArchivo { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
}
