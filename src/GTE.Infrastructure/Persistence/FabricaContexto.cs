using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GTE.Infrastructure.Persistence;

/// <summary>
/// Factory propia del ciclo de vida de los DbContext (patron del ecosistema):
/// los repositorios NO reciben el contexto por DI ni implementan IDisposable;
/// piden un contexto a la fabrica y lo disponen con using.
/// </summary>
public class FabricaContexto(IConfiguration configuracion)
{
    public T ConectarContexto<T>() where T : DbContext
    {
        var nombreCadena = typeof(T).Name switch
        {
            nameof(DbContextGTE) => "bdsGTE",
            _ => throw new InvalidOperationException(
                $"Contexto no registrado en la fabrica: {typeof(T).Name}")
        };

        var cadena = configuracion.GetConnectionString(nombreCadena)
            ?? throw new InvalidOperationException(
                $"Falta la cadena de conexion '{nombreCadena}' en la configuracion.");

        var opciones = new DbContextOptionsBuilder<T>()
            .UseSqlServer(cadena)
            .Options;

        return (T)Activator.CreateInstance(typeof(T), opciones)!;
    }
}
