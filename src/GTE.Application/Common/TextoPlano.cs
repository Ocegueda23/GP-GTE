using System.Net;
using System.Text.RegularExpressions;

namespace GTE.Application.Common;

/// <summary>
/// Convierte a texto plano el HTML que guarda EditorEnriquecido (TipTap), para los destinos
/// que NO saben renderizar marcado: hoy las exportaciones a Excel.
///
/// Sin esto, la celda de "Descripcion" llega con las etiquetas crudas
/// ("&lt;p&gt;Ajustar el reporte&lt;/p&gt;") y con las entidades sin resolver (&amp;nbsp;,
/// &amp;amp;), que es exactamente lo que el usuario ve como "simbolos del xml". Es el gemelo
/// de htmlATextoPlano del frontend (shared/editor/textoPlano.ts), que hace lo mismo para los
/// tooltips; si cambia el criterio de uno tiene que cambiar el del otro.
/// </summary>
public static class TextoPlano
{
    /// <summary>
    /// Regex compilada una vez: estas conversiones corren por renglon de un Excel que puede
    /// traer miles, y recompilar el patron en cada celda se nota.
    /// </summary>
    private static readonly Regex Etiquetas = new("<[^>]*>", RegexOptions.Compiled);

    private static readonly Regex EspaciosRepetidos = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Bloques que en HTML separan parrafos: se cambian por un espacio ANTES de borrar el
    /// resto de las etiquetas, para que "&lt;p&gt;uno&lt;/p&gt;&lt;p&gt;dos&lt;/p&gt;" no
    /// termine como "unodos".
    /// </summary>
    private static readonly Regex Saltos = new(
        @"</(p|div|li|tr|h[1-6])\s*>|<br\s*/?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Texto legible del HTML recibido. Devuelve null si no queda nada visible (el editor
    /// vacio manda "&lt;p&gt;&lt;/p&gt;", que como texto es cadena vacia y en la celda se ve
    /// mejor en blanco que como un espacio suelto).
    /// </summary>
    public static string? DesdeHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var texto = Saltos.Replace(html, " ");
        texto = Etiquetas.Replace(texto, string.Empty);

        // Va DESPUES de quitar etiquetas: si se decodificara antes, un "&lt;script&gt;" escrito
        // como texto por el usuario se volveria una etiqueta real y el paso anterior lo borraria.
        texto = WebUtility.HtmlDecode(texto);

        // \s de .NET ya cubre el espacio duro que deja &nbsp; (U+00A0 esta en la categoria Zs),
        // asi que colapsar y recortar basta para dejar la celda limpia.
        texto = EspaciosRepetidos.Replace(texto, " ").Trim();

        return texto.Length == 0 ? null : texto;
    }
}
