using Microsoft.Win32;

namespace GTE.Instalador.Servicios;

/// <summary>
/// Lee y escribe las variables de entorno de un Windows Service en
/// HKLM\SYSTEM\CurrentControlSet\Services\&lt;nombre&gt;\Environment (REG_MULTI_SZ).
/// Reemplaza a configurar-servicio-completo.ps1 / configurar-variable-servicio.ps1 /
/// configurar-almacen-archivos.ps1.
/// </summary>
public sealed class ConfiguracionServicioWindows
{
    private const string ClaveAmbiente = "ASPNETCORE_ENVIRONMENT";
    private const string ClaveCadenaConexion = "ConnectionStrings__bdsGTE";
    private const string ClaveJwt = "Jwt__ClaveFirma";
    private const string ClaveAlmacen = "AlmacenArchivos__Ruta";

    // Todo lo que ya estaba en el registro y no es una de las 4 claves de arriba se
    // conserva tal cual al guardar (misma regla que ya seguian los .ps1 originales):
    // esta herramienta nunca debe borrar una variable que otro proceso haya puesto.
    private string[] _otrasVariables = [];

    public bool ServicioExiste { get; private set; }
    public string? Ambiente { get; set; }
    public string? CadenaConexion { get; set; }
    public string? JwtClaveFirma { get; set; }
    public string? AlmacenArchivosRuta { get; set; }

    private static string ObtenerRutaRegistro(string nombreServicio) =>
        $@"SYSTEM\CurrentControlSet\Services\{nombreServicio}";

    public void Cargar(string nombreServicio)
    {
        Ambiente = null;
        CadenaConexion = null;
        JwtClaveFirma = null;
        AlmacenArchivosRuta = null;
        _otrasVariables = [];

        using var clave = Registry.LocalMachine.OpenSubKey(ObtenerRutaRegistro(nombreServicio));
        ServicioExiste = clave is not null;
        if (clave is null)
        {
            return;
        }

        var variables = clave.GetValue("Environment") as string[] ?? [];
        var otras = new List<string>();

        foreach (var linea in variables)
        {
            var separador = linea.IndexOf('=');
            if (separador < 0)
            {
                otras.Add(linea);
                continue;
            }

            var nombreVariable = linea[..separador];
            var valor = linea[(separador + 1)..];

            switch (nombreVariable)
            {
                case ClaveAmbiente:
                    Ambiente = valor;
                    break;
                case ClaveCadenaConexion:
                    CadenaConexion = valor;
                    break;
                case ClaveJwt:
                    JwtClaveFirma = valor;
                    break;
                case ClaveAlmacen:
                    AlmacenArchivosRuta = valor;
                    break;
                default:
                    otras.Add(linea);
                    break;
            }
        }

        _otrasVariables = [.. otras];
    }

    public void Guardar(string nombreServicio)
    {
        // OpenSubKey(writable: true) contra HKLM exige consola elevada -- si esto truena
        // con UnauthorizedAccessException, el manifest de la app deberia haber pedido ya
        // el UAC (ver app.manifest); si aun asi pasa, algo externo bloqueo la elevacion.
        using var clave = Registry.LocalMachine.OpenSubKey(ObtenerRutaRegistro(nombreServicio), writable: true)
            ?? throw new InvalidOperationException(
                $"No existe el servicio de Windows '{nombreServicio}'. Creelo primero en la seccion 'Servicio de Windows'.");

        var nuevas = new List<string>(_otrasVariables);

        void Agregar(string nombreVariable, string? valor)
        {
            if (!string.IsNullOrWhiteSpace(valor))
            {
                nuevas.Add($"{nombreVariable}={valor}");
            }
        }

        Agregar(ClaveAmbiente, Ambiente);
        Agregar(ClaveCadenaConexion, CadenaConexion);
        Agregar(ClaveJwt, JwtClaveFirma);
        Agregar(ClaveAlmacen, AlmacenArchivosRuta);

        clave.SetValue("Environment", nuevas.ToArray(), RegistryValueKind.MultiString);
    }
}
