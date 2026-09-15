using System.Diagnostics;

namespace GTE.Instalador.Servicios;

public enum EstadoServicioWindows
{
    NoExiste,
    Detenido,
    EnEjecucion,
    Transicion,
    Desconocido
}

/// <summary>
/// Envoltura sobre sc.exe (crear, consultar, iniciar, detener, reiniciar). Se usa sc.exe
/// en vez de System.ServiceProcess.ServiceController porque este ultimo no soporta crear
/// servicios, y usar la misma herramienta para todo evita mezclar dos mecanismos.
/// </summary>
public static class ServicioWindowsHelper
{
    public static EstadoServicioWindows ObtenerEstado(string nombreServicio)
    {
        var (codigo, salida, _) = Ejecutar("query", nombreServicio);
        if (codigo != 0)
        {
            return EstadoServicioWindows.NoExiste;
        }

        if (salida.Contains("RUNNING", StringComparison.OrdinalIgnoreCase))
        {
            return EstadoServicioWindows.EnEjecucion;
        }

        if (salida.Contains("STOPPED", StringComparison.OrdinalIgnoreCase))
        {
            return EstadoServicioWindows.Detenido;
        }

        if (salida.Contains("PENDING", StringComparison.OrdinalIgnoreCase))
        {
            return EstadoServicioWindows.Transicion;
        }

        return EstadoServicioWindows.Desconocido;
    }

    public static (bool Exito, string Mensaje) Crear(string nombreServicio, string rutaEjecutable, string nombreMostrar)
    {
        // "binPath=", "start=" y "DisplayName=" van como elementos SEPARADOS de
        // ArgumentList, no concatenados en una sola cadena: sc.exe exige el espacio
        // despues del signo igual (ver Doctos/MANUAL_INSTALACION_GTE.md), y con
        // ArgumentList .NET arma la linea de comando con ese espacio y las comillas
        // correctas automaticamente -- sin pasar por un shell intermedio, así que una
        // ruta con espacios o caracteres raros no puede inyectar un comando extra.
        var (codigo, salida, error) = Ejecutar(
            "create", nombreServicio,
            "binPath=", rutaEjecutable,
            "start=", "auto",
            "DisplayName=", nombreMostrar);

        return (codigo == 0, MensajeDe(codigo, salida, error));
    }

    public static (bool Exito, string Mensaje) Iniciar(string nombreServicio)
    {
        var (codigo, salida, error) = Ejecutar("start", nombreServicio);
        return (codigo == 0, MensajeDe(codigo, salida, error));
    }

    public static (bool Exito, string Mensaje) Detener(string nombreServicio)
    {
        var (codigo, salida, error) = Ejecutar("stop", nombreServicio);
        return (codigo == 0, MensajeDe(codigo, salida, error));
    }

    public static (bool Exito, string Mensaje) Reiniciar(string nombreServicio)
    {
        var estado = ObtenerEstado(nombreServicio);
        if (estado is EstadoServicioWindows.EnEjecucion or EstadoServicioWindows.Transicion)
        {
            var detener = Detener(nombreServicio);
            if (!detener.Exito)
            {
                return detener;
            }

            var limite = DateTime.UtcNow + TimeSpan.FromSeconds(15);
            while (DateTime.UtcNow < limite && ObtenerEstado(nombreServicio) != EstadoServicioWindows.Detenido)
            {
                Thread.Sleep(500);
            }
        }

        return Iniciar(nombreServicio);
    }

    private static string MensajeDe(int codigo, string salida, string error) =>
        codigo == 0 ? salida : (error.Length > 0 ? error : salida);

    private static (int Codigo, string Salida, string Error) Ejecutar(params string[] argumentos)
    {
        var info = new ProcessStartInfo("sc.exe")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argumento in argumentos)
        {
            info.ArgumentList.Add(argumento);
        }

        using var proceso = Process.Start(info) ?? throw new InvalidOperationException("No se pudo iniciar sc.exe.");
        var salida = proceso.StandardOutput.ReadToEnd();
        var error = proceso.StandardError.ReadToEnd();
        proceso.WaitForExit();
        return (proceso.ExitCode, salida, error);
    }
}
