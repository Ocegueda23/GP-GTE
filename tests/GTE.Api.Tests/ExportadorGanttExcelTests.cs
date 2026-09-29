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

    private static readonly string[] Filtros =
    [
        "Periodo: 01/09/2026 a 30/09/2026",
        "Proyecto: Todos",
        "Responsable: Todos",
    ];

    /// <summary>
    /// La hoja mas la fila donde quedo el encabezado de la tabla. Se devuelve calculada y no
    /// fija porque arriba va un preambulo (filtros y leyenda) que puede crecer: una prueba que
    /// hardcodee "fila 1" se rompe cada vez que se le agregue un renglon a ese bloque, sin que
    /// nada del diagrama este mal.
    /// </summary>
    private static (IXLWorksheet Hoja, int FilaEncabezado, int FilaDatos) Generar(
        GanttExcel gantt, params string[] folios)
    {
        var filas = folios
            .Select(f => (IReadOnlyList<object?>)[f, $"Actividad {f}"])
            .ToList();

        var bytes = new ExportadorExcelClosedXml()
            .GenerarLibroGantt("GanttActividades", Encabezados, filas, gantt);

        // El libro se relee desde los bytes que se le mandan al navegador: si algo no se
        // guardo, la prueba lo ve igual que lo veria quien abre el archivo.
        var libro = new XLWorkbook(new MemoryStream(bytes));
        var hoja = libro.Worksheet(1);

        var filaEncabezado = hoja.RowsUsed()
            .First(r => r.Cell(1).GetString() == "Folio")
            .RowNumber();

        return (hoja, filaEncabezado, filaEncabezado + 1);
    }

    private static GanttExcel Rango(DateOnly desde, DateOnly hasta, params BarraGanttExcel[] barras)
        => new(desde, hasta, barras, Filtros);

    private static DateTime Dia(int dia) => new(2026, 9, dia);

    /// <summary>Un mes o menos se dibuja dia a dia: 30 columnas de eje despues de los datos.</summary>
    [Fact]
    public void DibujaUnaColumnaPorDiaEnUnRangoCorto()
    {
        var (hoja, encabezado, _) = Generar(Rango(
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            new BarraGanttExcel(Dia(10), Dia(12), 2)), "WI-1");

        // Columnas 1-2 son los datos; el eje arranca en la 3 y son 30 dias.
        Assert.Equal("1", hoja.Cell(encabezado, 3).GetString());
        Assert.Equal("30", hoja.Cell(encabezado, 32).GetString());
        Assert.True(hoja.Cell(encabezado, 33).IsEmpty());
    }

    /// <summary>La barra pinta exactamente los dias del tramo, ni uno antes ni uno despues.</summary>
    [Fact]
    public void PintaSoloLasCeldasDelTramoDeLaActividad()
    {
        var (hoja, _, datos) = Generar(Rango(
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            new BarraGanttExcel(Dia(10), Dia(12), 2)), "WI-1");

        // Una celda sin pintar vuelve del archivo con el color automatico, no con NoColor: lo
        // que distingue pintada de vacia es el patron de relleno, no el color.
        // Dia 9 (columna 11) fuera; dias 10, 11 y 12 (columnas 12, 13, 14) dentro; dia 13 fuera.
        Assert.Equal(XLFillPatternValues.None, hoja.Cell(datos, 11).Style.Fill.PatternType);
        foreach (var columna in new[] { 12, 13, 14 })
        {
            Assert.NotEqual(XLFillPatternValues.None, hoja.Cell(datos, columna).Style.Fill.PatternType);
        }
        Assert.Equal(XLFillPatternValues.None, hoja.Cell(datos, 15).Style.Fill.PatternType);
    }

    /// <summary>Cada estatus lleva su color, el mismo que el usuario ya vio en pantalla.</summary>
    [Fact]
    public void UsaUnColorDistintoPorEstatus()
    {
        var (hoja, _, datos) = Generar(Rango(
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            new BarraGanttExcel(Dia(10), Dia(10), 2),
            new BarraGanttExcel(Dia(10), Dia(10), 3),
            new BarraGanttExcel(Dia(10), Dia(10), 4)), "WI-1", "WI-2", "WI-3");

        var colores = Enumerable.Range(0, 3)
            .Select(i => hoja.Cell(datos + i, 12).Style.Fill.BackgroundColor)
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

        var (hoja, _, datos) = Generar(Rango(desde, hasta,
            new BarraGanttExcel(hoy.AddDays(-3), null, 2)), "WI-1");

        // Columna del dia de hoy: es el sexto dia del rango, o sea la columna 3 + 5.
        Assert.Equal(XLFillPatternValues.LightUp, hoja.Cell(datos, 8).Style.Fill.PatternType);

        // Y no sigue pintando despues de hoy: eso seria inventar trabajo futuro.
        Assert.Equal(XLFillPatternValues.None, hoja.Cell(datos, 9).Style.Fill.PatternType);
    }

    /// <summary>Una actividad que se sale del rango se recorta al eje en vez de desbordarlo.</summary>
    [Fact]
    public void RecortaLaBarraQueSeSaleDelPeriodo()
    {
        var (hoja, encabezado, datos) = Generar(Rango(
            new DateOnly(2026, 9, 10), new DateOnly(2026, 9, 20),
            new BarraGanttExcel(Dia(1), Dia(30), 2)), "WI-1");

        // Los 11 dias del eje (columnas 3 a 13) quedan pintados y no hay una columna 14.
        for (var columna = 3; columna <= 13; columna++)
        {
            Assert.NotEqual(XLFillPatternValues.None, hoja.Cell(datos, columna).Style.Fill.PatternType);
        }
        Assert.True(hoja.Cell(encabezado, 14).IsEmpty());
    }

    /// <summary>Rango largo: el eje pasa a meses para no escupir cientos de columnas ilegibles.</summary>
    [Fact]
    public void AgrupaPorMesEnUnRangoLargo()
    {
        var (hoja, encabezado, _) = Generar(Rango(
            new DateOnly(2025, 1, 1), new DateOnly(2026, 12, 31),
            new BarraGanttExcel(new DateTime(2025, 3, 1), new DateTime(2025, 4, 30), 2)), "WI-1");

        // 24 meses, no 730 dias.
        Assert.Equal("ene 25", hoja.Cell(encabezado, 3).GetString().ToLowerInvariant());
        Assert.False(hoja.Cell(encabezado, 26).IsEmpty());
        Assert.True(hoja.Cell(encabezado, 27).IsEmpty());
    }

    /// <summary>Los datos se siguen escribiendo: el diagrama se agrega, no sustituye a la tabla.</summary>
    [Fact]
    public void ConservaLosDatosDeLaTabla()
    {
        var (hoja, encabezado, datos) = Generar(Rango(
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            new BarraGanttExcel(Dia(10), Dia(12), 2)), "WI-1");

        Assert.Equal("Folio", hoja.Cell(encabezado, 1).GetString());
        Assert.Equal("WI-1", hoja.Cell(datos, 1).GetString());
        Assert.Equal("Actividad WI-1", hoja.Cell(datos, 2).GetString());
    }

    /// <summary>
    /// Los filtros con los que se corrio van arriba: el archivo se manda por correo fuera de la
    /// aplicacion y sin ellos nadie sabe si esta viendo un mes o un ano.
    /// </summary>
    [Fact]
    public void ImprimeLosFiltrosArribaDelEncabezado()
    {
        var (hoja, encabezado, _) = Generar(Rango(
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            new BarraGanttExcel(Dia(10), Dia(12), 2)), "WI-1");

        foreach (var filtro in Filtros)
        {
            var fila = hoja.RowsUsed().FirstOrDefault(r => r.Cell(ColumnaTexto).GetString() == filtro);
            Assert.True(fila is not null, $"No se imprimio el filtro: {filtro}");
            Assert.True(fila!.RowNumber() < encabezado, $"El filtro quedo debajo del encabezado: {filtro}");
        }
    }

    /// <summary>La leyenda pasa arriba del encabezado, que es donde se ve al abrir e imprimir.</summary>
    [Fact]
    public void ImprimeLaLeyendaArribaDelEncabezado()
    {
        var (hoja, encabezado, _) = Generar(Rango(
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            new BarraGanttExcel(Dia(10), Dia(12), 2)), "WI-1");

        var titulo = hoja.RowsUsed()
            .FirstOrDefault(r => r.Cell(ColumnaTexto).GetString() == "Colores del diagrama");

        Assert.True(titulo is not null, "No se imprimio el titulo de la leyenda");
        Assert.True(titulo!.RowNumber() < encabezado, "La leyenda quedo debajo del encabezado");

        // Y su muestra de color esta a la izquierda del texto, no encima.
        var enProceso = hoja.RowsUsed().First(r => r.Cell(ColumnaTexto).GetString() == "En proceso");
        Assert.NotEqual(XLFillPatternValues.None, enProceso.Cell(ColumnaMuestra).Style.Fill.PatternType);
        Assert.Equal(string.Empty, enProceso.Cell(ColumnaMuestra).GetString());
    }

    /// <summary>
    /// Todo el texto del preambulo cae en la MISMA columna. Si el titulo fuera a una y sus
    /// renglones a otra, el bloque se lee escalonado -- que es justo lo que se reporto.
    /// </summary>
    [Fact]
    public void PoneTodoElTextoDelPreambuloEnUnaSolaColumna()
    {
        var (hoja, encabezado, _) = Generar(Rango(
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            new BarraGanttExcel(Dia(10), Dia(12), 2)), "WI-1");

        foreach (var fila in hoja.RowsUsed().Where(r => r.RowNumber() < encabezado))
        {
            foreach (var celda in fila.CellsUsed(c => !string.IsNullOrEmpty(c.GetString())))
            {
                Assert.True(celda.Address.ColumnNumber == ColumnaTexto,
                    $"Texto fuera de la columna {ColumnaTexto} en {celda.Address}: {celda.GetString()}");
            }
        }
    }

    /// <summary>
    /// El ancho de las columnas de datos se mide contra la tabla, NO contra el preambulo: los
    /// renglones de la leyenda son largos y estirarian la columna del texto a lo ancho de la
    /// pantalla, dejando el diagrama fuera de vista.
    /// </summary>
    [Fact]
    public void NoEnsanchaLasColumnasPorElTextoDelPreambulo()
    {
        var (hoja, _, _) = Generar(Rango(
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            new BarraGanttExcel(Dia(10), Dia(12), 2)), "WI-1");

        // "Celda rayada = actividad sin fecha de fin..." pasa de 80 caracteres; la columna del
        // texto tiene que seguir midiendo lo que mide "Actividad WI-1".
        Assert.True(hoja.Column(ColumnaTexto).Width < 30,
            $"La columna del texto quedo en {hoja.Column(ColumnaTexto).Width}");
    }

    private const int ColumnaMuestra = 1;
    private const int ColumnaTexto = 2;
}
