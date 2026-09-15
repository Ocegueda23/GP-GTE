using GTE.Application.Interfaces;
using GTE.Domain.Ayuda;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Lee los documentos de ayuda desde la carpeta Contenido/Ayuda que se copia junto al
/// binario publicado. La clave se valida contra una lista blanca antes de armar la ruta:
/// nunca se concatena texto del cliente al sistema de archivos.
/// </summary>
public class ProveedorDocumentosAyuda : IProveedorDocumentosAyuda
{
    private static readonly HashSet<string> ClavesValidas =
    [
        ConstantesAyuda.DocumentoCentroMando,
    ];

    public async Task<string?> ObtenerHtmlAsync(string clave, CancellationToken cancellationToken = default)
    {
        if (!ClavesValidas.Contains(clave))
        {
            return null;
        }

        var ruta = Path.Combine(
            AppContext.BaseDirectory,
            ConstantesAyuda.CarpetaDocumentos.Replace('/', Path.DirectorySeparatorChar),
            $"{clave}.html");

        if (!File.Exists(ruta))
        {
            return null;
        }

        return await File.ReadAllTextAsync(ruta, cancellationToken);
    }
}
