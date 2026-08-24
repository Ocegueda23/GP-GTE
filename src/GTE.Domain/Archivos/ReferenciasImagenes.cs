using System.Text.RegularExpressions;

namespace GTE.Domain.Archivos;

/// <summary>
/// Lee los GUID de las imagenes embebidas en el HTML de un contenido enriquecido. El editor
/// del front nunca escribe una URL: el nodo de imagen se serializa como
/// &lt;img data-guid="..."&gt; y el binario se sirve aparte por streaming autenticado (ver
/// SanitizadorHtmlGanss, que es quien permite ese unico atributo).
/// </summary>
public static partial class ReferenciasImagenes
{
    [GeneratedRegex(
        "data-guid\\s*=\\s*[\"']([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})[\"']",
        RegexOptions.CultureInvariant)]
    private static partial Regex PatronGuid();

    /// <summary>GUID distintos referenciados en el HTML; lista vacia si no hay imagenes.</summary>
    public static IReadOnlyCollection<Guid> ObtenerGuids(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return Array.Empty<Guid>();
        }

        var guids = new HashSet<Guid>();
        foreach (Match coincidencia in PatronGuid().Matches(html))
        {
            if (Guid.TryParse(coincidencia.Groups[1].Value, out var guid))
            {
                guids.Add(guid);
            }
        }

        return guids;
    }
}
