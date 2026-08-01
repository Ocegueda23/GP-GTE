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
            "blockquote", "code", "pre", "h1", "h2", "h3", "a", "img", "span"
        })
        {
            _sanitizador.AllowedTags.Add(etiqueta);
        }

        _sanitizador.AllowedAttributes.Clear();
        _sanitizador.AllowedAttributes.Add("href");
        _sanitizador.AllowedAttributes.Add("class");

        _sanitizador.AllowedSchemes.Clear();
        _sanitizador.AllowedSchemes.Add("http");
        _sanitizador.AllowedSchemes.Add("https");

        _sanitizador.AllowDataAttributes = true;
    }

    public string Sanitizar(string html) => _sanitizador.Sanitize(html).Trim();
}
