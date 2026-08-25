using System.Text;
using System.Text.RegularExpressions;

namespace GTE.Domain.ReglasNegocio;

/// <summary>
/// Clave de una regla de negocio. Desde 2026-08-24 se forma SOLA al dar de alta:
/// RN-{CLAVE DEL PROYECTO}-{CONSECUTIVO DE 3 DIGITOS}, por ejemplo RN-GTE-NNN.
///
/// El consecutivo NO se calcula con MAX()+1: se pide a dbo.spGenerarFolio con la serie
/// "RN-{CLAVE}" y 3 digitos, que es el motor de folios del sistema y toma
/// ROWLOCK/UPDLOCK/HOLDLOCK -- dos altas simultaneas en el mismo proyecto no pueden sacar
/// el mismo numero.
///
/// Logica pura, sin EF: la usan el handler de alta, el validador y la prueba de
/// GTE.Domain.Tests que escanea las claves presentes en el codigo. La unicidad la garantiza
/// UQ_tblReglaNegocio_ProyectoClave, y es POR PROYECTO.
/// </summary>
public static partial class ClaveReglaNegocio
{
    public const string Prefijo = "RN";

    /// <summary>Ancho del consecutivo. Cambiarlo NO renumera lo ya generado.</summary>
    public const int DigitosConsecutivo = 3;

    /// <summary>
    /// Un solo formato en todo el sistema: RN-{CLAVE DEL PROYECTO}-{CONSECUTIVO}.
    ///
    /// Las 40 reglas de GTE nacieron con las claves historicas del Documento Maestro
    /// (que llevaban el modulo dentro de la clave) y se renumeraron con el script 45
    /// -- decision del equipo 2026-08-24: uniformidad. El modulo no se perdio, vive en
    /// el ambito de cada regla (tblAmbitoRegla), que es donde el catalogo agrupa.
    ///
    /// El rango de digitos es 2 a 4 y el de la clave de proyecto 2 a 12 para no romper
    /// si alguna base todavia no aplico el script 45 o si un proyecto pasa de 999 reglas.
    /// </summary>
    [GeneratedRegex(@"^RN-[A-Z0-9]{2,12}-\d{2,4}$", RegexOptions.CultureInvariant)]
    private static partial Regex PatronClave();

    /// <summary>Patron reutilizable para escanear claves dentro de un texto (comentarios de codigo).</summary>
    [GeneratedRegex(@"RN-[A-Z0-9]{2,12}-\d{2,4}", RegexOptions.CultureInvariant)]
    public static partial Regex PatronBusqueda();

    public static bool EsValida(string? clave) =>
        !string.IsNullOrWhiteSpace(clave) && PatronClave().IsMatch(clave);

    /// <summary>
    /// Serie de folio de un proyecto: "RN-{CLAVE NORMALIZADA}". Se le pasa tal cual a
    /// spGenerarFolio, que devuelve serie + "-" + consecutivo, o sea la clave completa.
    /// </summary>
    public static string SerieDeProyecto(string claveProyecto) =>
        $"{Prefijo}-{NormalizarClaveProyecto(claveProyecto)}";

    /// <summary>
    /// La clave del proyecto viene de captura libre y puede traer minusculas, espacios,
    /// acentos o guiones (el proyecto de pruebas se llama "xx"). Se normaliza a mayusculas
    /// y solo A-Z0-9 para que la clave resultante siga siendo una sola palabra legible y
    /// no rompa el patron. Si al limpiar no queda nada, se usa "GEN" como serie de respaldo
    /// -- preferible a fallar el alta por un dato de catalogo mal capturado.
    /// </summary>
    public static string NormalizarClaveProyecto(string? claveProyecto)
    {
        if (string.IsNullOrWhiteSpace(claveProyecto))
        {
            return "GEN";
        }

        var limpia = new StringBuilder();
        foreach (var caracter in claveProyecto.Trim().ToUpperInvariant())
        {
            if (char.IsAsciiLetterOrDigit(caracter))
            {
                limpia.Append(caracter);
            }

            if (limpia.Length == 12)
            {
                break;
            }
        }

        return limpia.Length >= 2 ? limpia.ToString() : "GEN";
    }

    /// <summary>
    /// Modulo o proyecto de la clave (lo de en medio): RN-GTE-008 -> "REQ", RN-GTE-NNN -> "GTE".
    /// Devuelve null si la clave no tiene el formato esperado.
    /// </summary>
    public static string? ObtenerModulo(string? clave) =>
        EsValida(clave) ? clave!.Split('-')[1] : null;

    /// <summary>Normaliza a mayusculas y sin espacios, que es como se guarda.</summary>
    public static string Normalizar(string clave) =>
        (clave ?? string.Empty).Trim().ToUpperInvariant();
}
