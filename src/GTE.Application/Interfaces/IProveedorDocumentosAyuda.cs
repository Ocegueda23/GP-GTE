namespace GTE.Application.Interfaces;

/// <summary>
/// Lectura de los documentos HTML de ayuda que la aplicacion sirve por endpoint
/// autenticado (no desde wwwroot: un archivo estatico seria legible por cualquiera que
/// adivine la URL, y el Centro de Mando TI exige permiso).
/// </summary>
public interface IProveedorDocumentosAyuda
{
    /// <summary>Devuelve el HTML del documento, o null si no existe en el despliegue.</summary>
    Task<string?> ObtenerHtmlAsync(string clave, CancellationToken cancellationToken = default);
}
