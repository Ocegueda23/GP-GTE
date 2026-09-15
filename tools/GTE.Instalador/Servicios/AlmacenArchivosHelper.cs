namespace GTE.Instalador.Servicios;

/// <summary>
/// Crea y valida la carpeta de AlmacenArchivos__Ruta. Reemplaza la logica de
/// configurar-almacen-archivos.ps1.
/// </summary>
public static class AlmacenArchivosHelper
{
    public static bool EsShareDeRed(string ruta) => ruta.StartsWith(@"\\", StringComparison.Ordinal);

    public static (bool Exito, string Mensaje) CrearYProbarEscritura(string ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta))
        {
            return (false, "Especifica una ruta primero.");
        }

        if (EsShareDeRed(ruta))
        {
            // La sonda correria con la cuenta de quien usa esta herramienta, no con la
            // cuenta bajo la que corre el servicio (por defecto LocalSystem, que en red
            // se presenta como la cuenta de equipo DOMINIO\NOMBREEQUIPO$) -- que tu
            // puedas escribir aqui no garantiza que el servicio tambien pueda.
            return (true,
                "Es un share de red: no se puede comprobar desde aqui. Confirma a mano que " +
                "la cuenta bajo la que corre el servicio tenga lectura y escritura sobre esta ruta.");
        }

        try
        {
            if (!Directory.Exists(ruta))
            {
                Directory.CreateDirectory(ruta);
            }

            // Path.Combine (no una concatenacion con "\\") para que funcione igual si la
            // ruta ya trae diagonal final o no.
            var sonda = Path.Combine(ruta, $".sonda-{Guid.NewGuid():N}");
            File.WriteAllBytes(sonda, [0]);
            File.Delete(sonda);
            return (true, "OK: la ruta acepta escritura.");
        }
        catch (Exception ex)
        {
            return (false,
                $"No se pudo escribir en '{ruta}': {ex.Message}. Si la unidad no existe en " +
                "este servidor usa otra ruta; si es de permisos, da control total a la cuenta del servicio.");
        }
    }
}
