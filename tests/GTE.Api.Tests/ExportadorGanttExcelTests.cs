using ClosedXML.Excel;
using GTE.Application.Interfaces;
using GTE.Infrastructure.Services;
using Xunit;

namespace GTE.Api.Tests;

/// <summary>
/// El Excel del R16 tiene que traer el diagrama dibujado, no solo la tabla de fechas: es lo que
/// revisa direccion y el defecto reportado era justamente que el archivo llegaba sin barras.
/// Estas pruebas abren el libro generado y miran las celdas, asi que no necesitan base de datos.
/// </summary>
public class ExportadorGanttExcelTests
{
    private static readonly string[] Encabezados = ["Folio", "Actividad"];

    private static IXLWorksheet Generar(GanttExcel gantt, params string[] folios)
    {
        var filas = folios
            .Select(f => (IReadOnlyList<object?>)[f, $"Actividad {f}"])
            .ToList();

        var bytes = new ExportadorExcelClosedXml()
            .GenerarLibroGantt("GanttActividades", Encabezados, filas, gantt);

        // El libro se relee desde los bytes que se le mandan al navegador: si algo no se
        // guardo, la prueba lo ve igual que lo veria quien abre el archivo.
        var libro = new XLWorkbook(new MemoryStream(bytes));
        return libro.Worksheet(1);
    }

    private static DateTime Dia(int dia) => new(2026, 9, dia);

    /// <summary>Un mes o menos se dibuja dia a dia: 30 columnas de eje despues de los datos.</summary>
    [Fact]
    public void DibujaUnaColumnaPorDiaEnUnRangoCorto()
    {
        var hoja = Generar(new GanttExcel(
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            [new BarraGanttExcel(Dia(10), Dia(12), 2)]), "WI-1");

        // Columnas 1-2 son los datos; el eje arranca en la 3 y son 30 dias.
        Assert.Equal("1", hoja.Cell(1, 3).GetString());
        Assert.Equal("30", hoja.Cell(1, 32).GetString());
        Assert.True(hoja.Cell(1, 33).IsEmpty());
    }

    /// <summary>La barra pinta exactamente los dias del tramo, ni uno antes ni uno despues.</summary>
    [Fact]
    public void PintaSoloLasCeldasDelTramoDeLaActividad()
    {
        var hoja = Generar(new GanttExcel(
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            [new BarraGanttExcel(Dia(10), Dia(12), 2)]), "WI-1");

        // Una celda sin pintar vuelve del archivo con el color automatico, no con NoColor: lo
        // que distingue pintada de vacia es el patron de relleno, no el color.
        // Dia 9 (columna 11) fuera; dias 10, 11 y 12 (columnas 12, 13, 14) dentro; dia 13 fuera.
        Assert.Equal(XLFillPatternValues.None, hoja.Cell(2, 11).Style.Fill.PatternType);
        foreach (var columna in new[] { 12, 13, 14 })
        {
            Assert.NotEqual(XLFillPatternValues.None, hoja.Cell(2, columna).Style.Fill.PatternType);
        }
        Assert.Equal(XLFillPatternValues.None, hoja.Cell(2, 15).Style.Fill.PatternType);
    }

    /// <summary>Cada estatus lleva su color, el mismo que el usuario ya vio en pantalla.</summary>
    [Fact]
    public void UsaUnColorDistintoPorEstatus()
    {
        var hoja = Generar(new GanttExcel(
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            [
                new BarraGanttExcel(Dia(10), Dia(10), 2),
                new BarraGanttExcel(Dia(10), Dia(10), 3),
                new BarraGanttExcel(Dia(10), Dia(10), 4),
            ]), "WI-1", "WI-2", "WI-3");

        var colores = new[] { 2, 3, 4 }
            .Select(fila => hoja.Cell(fila, 12).Style.Fill.BackgroundColor)
            .ToList();

        Assert.Equal(3, colores.Distinct().Count());
    }

    /// <summary>
    /// Sin fecha de fin la barra corre hasta hoy y va rayada: impresa en blanco y negro, que es
    /// como acaba un reporte de direccion, el color solo no distingue cerrada de en curso.
    /// </summary>
    [Fact]
    public void MarcaConTramaLaActividadSinFechaDeFin()
    {
        var hoy = DateTime.Now;
        var desde = DateOnly.FromDateTime(hoy.AddDays(-5));
        var hasta = DateOnly.FromDateTime(hoy.AddDays(5));

        var hoja = Generar(new GanttExcel(desde, hasta,
            [new BarraGanttExcel(hoy.AddDays(-3), null, 2)]), "WI-1");

        // Columna del dia de hoy: es el sexto dia del rango, o sea la columna 3 + 5.
        var celdaHoy = hoja.Cell(2, 8);
        Assert.Equal(XLFillPatternValues.LightUp, celdaHoy.Style.Fill.PatternType);

        // Y no sigue pintando despues de hoy: eso seria inventar trabajo futuro.
        Assert.Equal(XLFillPatternValues.None, hoja.Cell(2, 9).Style.Fill.PatternType);
    }

    /// <summary>Una actividad que se sale del rango se recorta al eje en vez de desbordarlo.</summary>
    [Fact]
    public void RecortaLaBarraQueSeSaleDelPeriodo()
    {
        var hoja = Generar(new GanttExcel(
            new DateOnly(2026, 9, 10), new DateOnly(2026, 9, 20),
            [new BarraGanttExcel(Dia(1), Dia(30), 2)]), "WI-1");

        // Los 11 dias del eje (columnas 3 a 13) quedan pintados y no hay una columna 14.
        for (var columna = 3; columna <= 13; columna++)
        {
            Assert.NotEqual(XLFillPatternValues.None, hoja.Cell(2, columna).Style.Fill.PatternType);
        }
        Assert.True(hoja.Cell(1, 14).IsEmpty());
    }

    /// <summary>Rango largo: el eje pasa a meses para no escupir cientos de columnas ilegibles.</summary>
    [Fact]
    public void AgrupaPorMesEnUnRangoLargo()
    {
        var hoja = Generar(new GanttExcel(
            new DateOnly(2025, 1, 1), new DateOnly(2026, 12, 31),
            [new BarraGanttExcel(new DateTime(2025, 3, 1), new DateTime(2025, 4, 30), 2)]), "WI-1");

        // 24 meses, no 730 dias.
        Assert.Equal("ene 25", hoja.Cell(1, 3).GetString().ToLowerInvariant());
        Assert.False(hoja.Cell(1, 26).IsEmpty());
        Assert.True(hoja.Cell(1, 27).IsEmpty());
    }

    /// <summary>Los datos se siguen escribiendo: el diagrama se agrega, no sustituye a la tabla.</summary>
    [Fact]
    public void ConservaLosDatosDeLaTabla()
    {
        var hoja = Generar(new GanttExcel(
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            [new BarraGanttExcel(Dia(10), Dia(12), 2)]), "WI-1");

        Assert.Equal("Folio", hoja.Cell(1, 1).GetString());
        Assert.Equal("WI-1", hoja.Cell(2, 1).GetString());
        Assert.Equal("Actividad WI-1", hoja.Cell(2, 2).GetString());
    }
}
