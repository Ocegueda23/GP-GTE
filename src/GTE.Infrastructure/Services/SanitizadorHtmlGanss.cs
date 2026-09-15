using Ganss.Xss;
using GTE.Application.Interfaces;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Limpia el HTML de los comentarios antes de guardarlo: solo permite formato basico,
/// el marcado de menciones (span[data-mention-id]) y la referencia a imagenes pegadas
/// (img[data-guid], sin src -- el frontend arma el blob autenticado a partir del GUID).
/// Nunca se confia en el HTML que manda el front.
/// </summary>
public class SanitizadorHtmlGanss : ISanitizadorHtml
{
    private readonly HtmlSanitizer _sanitizador;

    public SanitizadorHtmlGanss()
    {
        _sanitizador = new HtmlSanitizer();

        _sanitizador.AllowedTags.Clear();
        foreach (var etiqueta in new[]
        {
            "p", "br", "strong", "em", "u", "ul", "ol", "li",
            "blockquote", "code", "pre", "h1", "h2", "h3", "a", "img", "span",
            // Tablas: las capturan las instrucciones de implementacion de un release, y
            // tambien llegan pegadas del portapapeles (Excel, Word). Son etiquetas de
            // estructura, sin capacidad de ejecutar nada.
            "table", "thead", "tbody", "tfoot", "tr", "th", "td", "colgroup", "col", "caption"
        })
        {
            _sanitizador.AllowedTags.Add(etiqueta);
        }

        _sanitizador.AllowedAttributes.Clear();
        _sanitizador.AllowedAttributes.Add("href");
        _sanitizador.AllowedAttributes.Add("class");
        _sanitizador.AllowedAttributes.Add("style");

        // Celdas combinadas y ancho de columna del editor de tablas (TipTap los escribe
        // como atributos, no como estilos): sin esto una tabla pegada pierde su forma.
        _sanitizador.AllowedAttributes.Add("colspan");
        _sanitizador.AllowedAttributes.Add("rowspan");
        _sanitizador.AllowedAttributes.Add("colwidth");
        _sanitizador.AllowedAttributes.Add("span");
        _sanitizador.AllowedAttributes.Add("width");

        // Solo tamano de letra (editor enriquecido) y el ancho de las columnas de tabla:
        // no abrir la puerta a otros estilos.
        _sanitizador.AllowedCssProperties.Clear();
        _sanitizador.AllowedCssProperties.Add("font-size");
        _sanitizador.AllowedCssProperties.Add("width");
        _sanitizador.AllowedCssProperties.Add("min-width");

        _sanitizador.AllowedSchemes.Clear();
        _sanitizador.AllowedSchemes.Add("http");
        _sanitizador.AllowedSchemes.Add("https");

        _sanitizador.AllowDataAttributes = true;
    }

    public string Sanitizar(string html) => _sanitizador.Sanitize(html).Trim();
}
