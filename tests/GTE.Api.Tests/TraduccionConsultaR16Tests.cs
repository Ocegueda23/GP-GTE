using GTE.Domain.Reportes;
using GTE.Infrastructure.Persistence;
using GTE.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GTE.Api.Tests;

/// <summary>
/// Regresion de traduccion del R16 (Gantt de actividades). A diferencia de
/// <see cref="TraduccionConsultasR15Tests"/>, esta NO copia la consulta: llama al metodo real
/// de <see cref="ReportesQueryService"/> contra un servidor que no existe, asi que la consulta
/// se traduce de verdad y lo unico que falla es la conexion.
///
/// La diferencia entre las dos excepciones es justo lo que se esta midiendo: si EF no supiera
/// traducir algo (ordenar por un bool, el ThenBy sobre la navegacion opcional del asignado o
/// leer FechaInicio!.Value en la proyeccion) reventaria con InvalidOperationException ANTES de
/// intentar conectarse. Que llegue hasta SqlException significa que el SQL se genero completo.
///
/// Corre en cualquier maquina: no necesita base de datos, y por eso cubre el hueco que dejan
/// las pruebas E2E donde no hay LocalDB con bdsGTE.
/// </summary>
public class TraduccionConsultaR16Tests
{
    private static ReportesQueryService Servicio()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:bdsGTE"] =
                    "Server=noexiste;Database=bdsGTE;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=1",
            })
            .Build();

        // El calendario laboral no se usa en el R16 (el Gantt son fechas de calendario, no
        // tiempo habil), asi que no hace falta una implementacion real para esta prueba.
        return new ReportesQueryService(new FabricaContexto(configuracion), calendario: null!);
    }

    /// <summary>
    /// EF envuelve el fallo de conexion de formas distintas segun por donde salga (el Count o
    /// el ToList), asi que no se afirma el tipo exacto: se afirma que en la cadena de
    /// excepciones hay un SqlException -- el SQL se genero y se intento ejecutar -- y que NO
    /// aparece el "could not be translated" con el que EF anuncia que se rindio armandola.
    /// </summary>
    private static void ExigirFalloDeConexionNoDeTraduccion(Exception? error)
    {
        Assert.NotNull(error);
        var cadena = error!.ToString();
        Assert.DoesNotContain("could not be translated", cadena, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SqlException", cadena, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(AgrupacionGantt.Ninguno)]
    [InlineData(AgrupacionGantt.Proyecto)]
    [InlineData(AgrupacionGantt.Usuario)]
    public async Task GanttTraduceConCadaAgrupacion(AgrupacionGantt agrupacion)
    {
        var desde = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1));
        var hasta = DateOnly.FromDateTime(DateTime.Today);

        var error = await Record.ExceptionAsync(() => Servicio().ObtenerGanttActividadesAsync(
            desde, hasta, idProyecto: null, idAsignado: null, agrupacion, page: 1, pageSize: 50));

        ExigirFalloDeConexionNoDeTraduccion(error);
    }

    [Fact]
    public async Task GanttTraduceConLosTresFiltrosCombinados()
    {
        var desde = DateOnly.FromDateTime(DateTime.Today.AddMonths(-3));
        var hasta = DateOnly.FromDateTime(DateTime.Today);

        var error = await Record.ExceptionAsync(() => Servicio().ObtenerGanttActividadesAsync(
            desde, hasta, idProyecto: 1, idAsignado: 2, AgrupacionGantt.Usuario, page: 2, pageSize: 25));

        ExigirFalloDeConexionNoDeTraduccion(error);
    }
}
