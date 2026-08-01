namespace GTE.Domain.Archivos;

/// <summary>Reglas de validacion de adjuntos (RN implicita de endurecimiento, Doc Maestro S8.5).</summary>
public static class ConstantesArchivos
{
    public const long TamanoMaximoBytes = 25 * 1024 * 1024;

    public static readonly IReadOnlySet<string> ExtensionesPermitidas = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".zip", ".rar"
    };
}
