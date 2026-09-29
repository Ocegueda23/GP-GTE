using ClosedXML.Excel;
using GTE.Application.Interfaces;

namespace GTE.Infrastructure.Services;

public class ExportadorExcelClosedXml : IExportadorExcel
{
    public byte[] GenerarLibro(string tituloHoja, IReadOnlyList<string> encabezados, IReadOnlyList<IReadOnlyList<object?>> filas)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add(tituloHoja.Length > 31 ? tituloHoja[..31] : tituloHoja);

        for (var columna = 0; columna < encabezados.Count; columna++)
        {
            var celda = hoja.Cell(1, columna + 1);
            celda.Value = encabezados[columna];
            celda.Style.Font.Bold = true;
        }

        for (var fila = 0; fila < filas.Count; fila++)
        {
            for (var columna = 0; columna < filas[fila].Count; columna++)
            {
                EstablecerValor(hoja.Cell(fila + 2, columna + 1), filas[fila][columna]);
            }
        }

        hoja.Columns().AdjustToContents();

        using var flujo = new MemoryStream();
        libro.SaveAs(flujo);
        return flujo.ToArray();
    }

    public byte[] GenerarLibroGantt(
        string tituloHoja,
        IReadOnlyList<string> encabezados,
        IReadOnlyList<IReadOnlyList<object?>> filas,
        GanttExcel gantt)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add(tituloHoja.Length > 31 ? tituloHoja[..31] : tituloHoja);

        for (var columna = 0; columna < encabezados.Count; columna++)
        {
            var celda = hoja.Cell(1, columna + 1);
            celda.Value = encabezados[columna];
            celda.Style.Font.Bold = true;
        }

        for (var fila = 0; fila < filas.Count; fila++)
        {
            for (var columna = 0; columna < filas[fila].Count; columna++)
            {
                EstablecerValor(hoja.Cell(fila + 2, columna + 1), filas[fila][columna]);
            }
        }

        // El ancho automatico se aplica SOLO a las columnas de datos: si se dejara para toda la
        // hoja, ensancharia tambien las del eje y la barra dejaria de leerse como una barra.
        hoja.Columns(1, encabezados.Count).AdjustToContents();

        var primeraColumnaEje = encabezados.Count + 1;
        var periodos = CalcularPeriodos(gantt.Desde, gantt.Hasta);
        DibujarEje(hoja, primeraColumnaEje, periodos);
        DibujarBarras(hoja, primeraColumnaEje, periodos, gantt.Barras);
        DibujarLeyenda(hoja, filas.Count + 3);

        // Congelar la primera columna y el encabezado: al recorrer el Gantt a la derecha o hacia
        // abajo se tiene que seguir viendo de que actividad es cada barra.
        hoja.SheetView.FreezeRows(1);
        hoja.SheetView.FreezeColumns(1);

        using var flujo = new MemoryStream();
        libro.SaveAs(flujo);
        return flujo.ToArray();
    }

    /// <summary>Una columna del eje de tiempo: el tramo que cubre y la etiqueta de su encabezado.</summary>
    private sealed record Periodo(DateTime Inicio, DateTime Fin, string Etiqueta);

    /// <summary>
    /// Tope de columnas del eje. Un rango de varios anios en escala diaria haria una hoja
    /// ilegible (y lenta de abrir) mucho antes de topar con el limite real de Excel.
    /// </summary>
    private const int TopeColumnasEje = 400;

    private const int AnchoColumnaEje = 3;

    /// <summary>
    /// Densidad del eje segun el ancho del periodo: diaria hasta un mes, semanal hasta unos
    /// siete meses y mensual de ahi en adelante. Es el mismo criterio que usa el diagrama de
    /// la pantalla (DiagramaGantt.calcularTicks), para que el Excel y la pantalla no cuenten
    /// la misma historia con escalas distintas.
    /// </summary>
    private static List<Periodo> CalcularPeriodos(DateOnly desde, DateOnly hasta)
    {
        if (hasta < desde)
        {
            return [];
        }

        var dias = hasta.DayNumber - desde.DayNumber + 1;
        var periodos = new List<Periodo>();
        var cursor = desde;

        while (cursor <= hasta && periodos.Count < TopeColumnasEje)
        {
            DateOnly finPeriodo;
            string etiqueta;

            if (dias <= 31)
            {
                finPeriodo = cursor;
                etiqueta = cursor.Day.ToString();
            }
            else if (dias <= 220)
            {
                finPeriodo = cursor.AddDays(6);
                etiqueta = $"{cursor.Day}/{cursor.Month}";
            }
            else
            {
                finPeriodo = new DateOnly(cursor.Year, cursor.Month, 1).AddMonths(1).AddDays(-1);
                etiqueta = $"{cursor:MMM yy}";
            }

            if (finPeriodo > hasta) finPeriodo = hasta;

            periodos.Add(new Periodo(
                cursor.ToDateTime(TimeOnly.MinValue),
                finPeriodo.ToDateTime(TimeOnly.MaxValue),
                etiqueta));

            cursor = finPeriodo.AddDays(1);
        }

        return periodos;
    }

    private static void DibujarEje(IXLWorksheet hoja, int primeraColumna, List<Periodo> periodos)
    {
        var hoy = DateTime.Now;

        for (var i = 0; i < periodos.Count; i++)
        {
            var columna = primeraColumna + i;
            var celda = hoja.Cell(1, columna);
            celda.Value = periodos[i].Etiqueta;
            celda.Style.Font.Bold = true;
            celda.Style.Font.FontSize = 8;
            celda.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            celda.Style.Alignment.TextRotation = 90;

            // La columna que contiene hoy se marca en el encabezado: es la "linea de hoy" del
            // diagrama de pantalla, que en una cuadricula no se puede dibujar entre columnas.
            if (hoy >= periodos[i].Inicio && hoy <= periodos[i].Fin)
            {
                celda.Style.Fill.BackgroundColor = XLColor.FromArgb(0xD3, 0x2F, 0x2F);
                celda.Style.Font.FontColor = XLColor.White;
            }

            hoja.Column(columna).Width = AnchoColumnaEje;
        }
    }

    private static void DibujarBarras(
        IXLWorksheet hoja, int primeraColumna, List<Periodo> periodos, IReadOnlyList<BarraGanttExcel> barras)
    {
        var hoy = DateTime.Now;

        for (var fila = 0; fila < barras.Count; fila++)
        {
            var barra = barras[fila];
            // Sin fecha de fin la actividad sigue viva: la barra llega hasta hoy, no hasta el
            // final del rango, que fingiria trabajo que todavia no ocurre (mismo criterio que
            // el diagrama de pantalla).
            var abierta = barra.Fin is null;
            var fin = barra.Fin ?? hoy;
            if (fin < barra.Inicio) fin = barra.Inicio;

            var color = ColorEstatus(barra.IdEstatusWorkItem);

            for (var i = 0; i < periodos.Count; i++)
            {
                if (barra.Inicio > periodos[i].Fin || fin < periodos[i].Inicio)
                {
                    continue;
                }

                var celda = hoja.Cell(fila + 2, primeraColumna + i);
                celda.Style.Fill.BackgroundColor = color;

                // Las actividades abiertas se rayan en diagonal: en blanco y negro, que es como
                // se imprime un reporte de direccion, el color solo no distingue "cerrada" de
                // "sigue corriendo".
                if (abierta)
                {
                    celda.Style.Fill.PatternType = XLFillPatternValues.LightUp;
                    celda.Style.Fill.PatternColor = XLColor.White;
                }
            }
        }
    }

    private static void DibujarLeyenda(IXLWorksheet hoja, int fila)
    {
        var titulo = hoja.Cell(fila, 1);
        titulo.Value = "Colores del diagrama";
        titulo.Style.Font.Bold = true;

        var entradas = new (int Estatus, string Nombre)[]
        {
            (2, "En proceso"), (3, "En pruebas"), (4, "Correccion"), (0, "Pendiente / Terminado"),
        };

        for (var i = 0; i < entradas.Length; i++)
        {
            var muestra = hoja.Cell(fila + 1 + i, 1);
            muestra.Style.Fill.BackgroundColor = ColorEstatus(entradas[i].Estatus);
            hoja.Cell(fila + 1 + i, 2).Value = entradas[i].Nombre;
        }

        hoja.Cell(fila + 1 + entradas.Length, 2).Value =
            "Celda rayada = actividad sin fecha de fin (sigue en curso; la barra llega hasta hoy).";
        hoja.Cell(fila + 2 + entradas.Length, 2).Value =
            "Encabezado en rojo = periodo que contiene la fecha de hoy.";
    }

    /// <summary>
    /// Mismo mapa de estatus que los chips de las bandejas y las barras de la pantalla
    /// (colorEstatus en shared/api/workitems.ts). Los tonos son los de la paleta de MUI para
    /// que el Excel se vea como lo que el director ya vio en la aplicacion. El estatus
    /// Cancelado no aparece: el R16 lo excluye de la consulta.
    /// </summary>
    private static XLColor ColorEstatus(int idEstatusWorkItem) => idEstatusWorkItem switch
    {
        2 => XLColor.FromArgb(0x2E, 0x7D, 0x32),   // En Proceso  - success.main
        3 => XLColor.FromArgb(0x02, 0x88, 0xD1),   // En Pruebas  - info.main
        4 => XLColor.FromArgb(0xED, 0x6C, 0x02),   // Correccion  - warning.main
        _ => XLColor.FromArgb(0x9E, 0x9E, 0x9E),   // resto       - gris neutro
    };

    private static void EstablecerValor(IXLCell celda, object? valor)
    {
        switch (valor)
        {
            case null:
                celda.Value = string.Empty;
                break;
            case string texto:
                celda.Value = EscaparPosibleFormula(texto);
                break;
            case bool booleano:
                celda.Value = booleano;
                break;
            case int entero:
                celda.Value = entero;
                break;
            case long largo:
                celda.Value = largo;
                break;
            case decimal decimalValor:
                celda.Value = decimalValor;
                break;
            case double flotante:
                celda.Value = flotante;
                break;
            case DateTime fecha:
                celda.Value = fecha;
                break;
            case DateOnly soloFecha:
                celda.Value = soloFecha.ToDateTime(TimeOnly.MinValue);
                break;
            default:
                celda.Value = valor.ToString();
                break;
        }
    }

    /// <summary>
    /// Antepone un apostrofe cuando el texto empieza con un caracter que Excel puede interpretar
    /// como inicio de formula (=, +, -, @) o de un comando DDE (tab, retorno de carro). Mitigacion
    /// estandar de "CSV/Excel formula injection" para columnas con texto libre de usuario
    /// (titulo, descripcion, etc.) que se exportan tal cual.
    /// </summary>
    private static string EscaparPosibleFormula(string texto)
    {
        if (texto.Length == 0)
        {
            return texto;
        }

        return texto[0] is '=' or '+' or '-' or '@' or '\t' or '\r'
            ? "'" + texto
            : texto;
    }
}
