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
