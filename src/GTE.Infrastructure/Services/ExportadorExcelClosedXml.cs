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

        // Sin cuadricula: es el cambio que mas hace por el aspecto de una hoja que se presenta.
        // Con ella, cualquier estilo compite con una retícula gris que llega hasta el infinito;
        // sin ella, lo unico que se ve son los bloques que si se dibujaron.
        hoja.ShowGridLines = false;

        // Preambulo ANTES del encabezado: con que filtros se corrio y que significan los
        // colores. Va arriba porque el archivo se abre y se imprime empezando por la primera
        // pagina; al pie del listado la leyenda no la ve quien solo mira la hoja 1.
        var filaEncabezado = EscribirPreambulo(hoja, gantt) + 1;

        var periodos = CalcularPeriodos(gantt.Desde, gantt.Hasta);
        var primeraColumnaEje = encabezados.Count + 1;
        var ultimaColumna = primeraColumnaEje + Math.Max(periodos.Count - 1, 0);

        // La banda de meses va en el renglon de arriba del encabezado; se reserva antes de
        // escribir nada para que el encabezado no se le encime.
        var filaBanda = filaEncabezado;
        filaEncabezado++;

        EscribirEncabezado(hoja, filaEncabezado, encabezados);

        var primeraFilaDatos = filaEncabezado + 1;
        for (var fila = 0; fila < filas.Count; fila++)
        {
            for (var columna = 0; columna < filas[fila].Count; columna++)
            {
                EstablecerValor(hoja.Cell(primeraFilaDatos + fila, columna + 1), filas[fila][columna]);
            }
        }

        var ultimaFila = primeraFilaDatos + Math.Max(filas.Count - 1, 0);
        DarFormatoATabla(hoja, encabezados.Count, filaEncabezado, primeraFilaDatos, filas.Count);
        AjustarColumnasDeDatos(hoja, encabezados.Count, filaEncabezado, ultimaFila);

        DibujarBandaDePeriodos(hoja, filaBanda, primeraColumnaEje, periodos);
        DibujarEje(hoja, filaEncabezado, primeraColumnaEje, periodos);
        DibujarFondoDelEje(hoja, primeraFilaDatos, ultimaFila, primeraColumnaEje, periodos, filas.Count);
        DibujarBarras(hoja, primeraFilaDatos, primeraColumnaEje, periodos, gantt.Barras);

        // Congelar hasta el encabezado y la primera columna: al recorrer el Gantt a la derecha
        // o hacia abajo se tiene que seguir viendo de que actividad es cada barra.
        hoja.SheetView.FreezeRows(filaEncabezado);
        hoja.SheetView.FreezeColumns(1);

        if (filas.Count > 0)
        {
            hoja.Range(filaEncabezado, 1, ultimaFila, encabezados.Count).SetAutoFilter();
        }

        PrepararImpresion(hoja, filaBanda, filaEncabezado, ultimaFila, ultimaColumna);

        using var flujo = new MemoryStream();
        libro.SaveAs(flujo);
        return flujo.ToArray();
    }

    /* ---------- Paleta ----------
       Un solo lugar donde vive el color del reporte. Los tonos de estatus son los de la
       paleta de MUI que el usuario ya vio en pantalla (ver ColorEstatus); el resto es la
       gama neutra que los acompana sin pelearse con ellos. */

    private static readonly XLColor Tinta = XLColor.FromArgb(0x1F, 0x3A, 0x5F);
    private static readonly XLColor TintaSuave = XLColor.FromArgb(0x5A, 0x64, 0x72);
    private static readonly XLColor Linea = XLColor.FromArgb(0xD0, 0xD7, 0xDE);
    private static readonly XLColor FilaAlterna = XLColor.FromArgb(0xF5, 0xF7, 0xFA);
    private static readonly XLColor FondoFinDeSemana = XLColor.FromArgb(0xEC, 0xEF, 0xF3);
    private static readonly XLColor FondoHoy = XLColor.FromArgb(0xFB, 0xE9, 0xE9);
    private static readonly XLColor ColorHoy = XLColor.FromArgb(0xD3, 0x2F, 0x2F);

    private static void EscribirEncabezado(
        IXLWorksheet hoja, int filaEncabezado, IReadOnlyList<string> encabezados)
    {
        for (var columna = 0; columna < encabezados.Count; columna++)
        {
            var celda = hoja.Cell(filaEncabezado, columna + 1);
            celda.Value = encabezados[columna];
            celda.Style.Font.Bold = true;
            celda.Style.Font.FontColor = XLColor.White;
            celda.Style.Fill.BackgroundColor = Tinta;
            celda.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            celda.Style.Alignment.WrapText = true;
        }

        hoja.Row(filaEncabezado).Height = 30;
    }

    /// <summary>
    /// Bandas alternas, bordes finos y formato por tipo de dato. Las bandas no son decorado:
    /// con veinte columnas de eje a la derecha, seguir un renglon con la vista de la actividad
    /// a su barra es justo donde se pierde quien lee esto.
    /// </summary>
    private static void DarFormatoATabla(
        IXLWorksheet hoja, int columnas, int filaEncabezado, int primeraFilaDatos, int totalFilas)
    {
        if (totalFilas == 0)
        {
            return;
        }

        var ultimaFila = primeraFilaDatos + totalFilas - 1;
        var rango = hoja.Range(filaEncabezado, 1, ultimaFila, columnas);
        rango.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        rango.Style.Border.InsideBorderColor = Linea;
        rango.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        rango.Style.Border.OutsideBorderColor = Linea;

        for (var i = 0; i < totalFilas; i++)
        {
            var fila = primeraFilaDatos + i;
            hoja.Row(fila).Height = 18;

            if (i % 2 == 1)
            {
                hoja.Range(fila, 1, fila, columnas).Style.Fill.BackgroundColor = FilaAlterna;
            }

            for (var columna = 1; columna <= columnas; columna++)
            {
                var celda = hoja.Cell(fila, columna);
                celda.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // El formato se decide por el tipo que quedo en la celda, no por el nombre de
                // la columna: el exportador es generico y no sabe cual es cual.
                if (celda.DataType == XLDataType.DateTime)
                {
                    celda.Style.DateFormat.Format = "dd/mm/yyyy";
                    celda.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                else if (celda.DataType == XLDataType.Number)
                {
                    celda.Style.NumberFormat.Format = "#,##0.00";
                    celda.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }
            }
        }
    }

    /// <summary>
    /// Banda de agrupacion arriba del eje: los dias se agrupan por mes y los meses por anio,
    /// con la celda combinada y centrada. Es lo que convierte una hilera de numeros sueltos en
    /// una linea de tiempo que se lee.
    /// </summary>
    private static void DibujarBandaDePeriodos(
        IXLWorksheet hoja, int fila, int primeraColumna, List<Periodo> periodos)
    {
        if (periodos.Count == 0)
        {
            return;
        }

        var inicioGrupo = 0;
        for (var i = 1; i <= periodos.Count; i++)
        {
            var mismoGrupo = i < periodos.Count
                && EtiquetaDeGrupo(periodos[i]) == EtiquetaDeGrupo(periodos[inicioGrupo]);

            if (mismoGrupo)
            {
                continue;
            }

            var desde = primeraColumna + inicioGrupo;
            var hasta = primeraColumna + i - 1;
            var rango = hoja.Range(fila, desde, fila, hasta);
            if (desde != hasta) rango.Merge();

            var celda = hoja.Cell(fila, desde);
            celda.Value = EtiquetaDeGrupo(periodos[inicioGrupo]);
            celda.Style.Font.Bold = true;
            celda.Style.Font.FontSize = 9;
            celda.Style.Font.FontColor = XLColor.White;
            celda.Style.Fill.BackgroundColor = TintaSuave;
            celda.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            celda.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            rango.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rango.Style.Border.OutsideBorderColor = XLColor.White;

            inicioGrupo = i;
        }

        hoja.Row(fila).Height = 16;
    }

    /// <summary>Mes y anio para la escala diaria y semanal; solo el anio para la mensual.</summary>
    private static string EtiquetaDeGrupo(Periodo periodo)
        => periodo.EsMensual ? $"{periodo.Inicio:yyyy}" : $"{periodo.Inicio:MMMM yyyy}";

    /// <summary>
    /// Fondo del area del diagrama: fines de semana en gris y la columna de hoy en rojo palido,
    /// de arriba a abajo. Se pinta ANTES que las barras, que lo tapan donde hay actividad.
    /// </summary>
    private static void DibujarFondoDelEje(
        IXLWorksheet hoja, int primeraFilaDatos, int ultimaFila, int primeraColumna,
        List<Periodo> periodos, int totalFilas)
    {
        if (totalFilas == 0)
        {
            return;
        }

        var hoy = DateTime.Now;

        for (var i = 0; i < periodos.Count; i++)
        {
            var periodo = periodos[i];
            var columna = primeraColumna + i;

            XLColor? fondo = null;
            if (hoy >= periodo.Inicio && hoy <= periodo.Fin)
            {
                fondo = FondoHoy;
            }
            else if (periodo.EsDiario
                && periodo.Inicio.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                fondo = FondoFinDeSemana;
            }

            if (fondo is not null)
            {
                hoja.Range(primeraFilaDatos, columna, ultimaFila, columna)
                    .Style.Fill.BackgroundColor = fondo;
            }
        }

        var area = hoja.Range(primeraFilaDatos, primeraColumna, ultimaFila, primeraColumna + periodos.Count - 1);
        area.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        area.Style.Border.InsideBorderColor = Linea;
        area.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        area.Style.Border.OutsideBorderColor = Linea;
    }

    /// <summary>
    /// Deja la hoja lista para imprimirse sin tocar nada: horizontal, ajustada al ancho de una
    /// pagina y repitiendo el encabezado en cada hoja. Esto se lleva impreso a una junta, y sin
    /// el ajuste al ancho un Gantt de treinta columnas sale partido en cuatro paginas sueltas.
    /// </summary>
    private static void PrepararImpresion(
        IXLWorksheet hoja, int filaBanda, int filaEncabezado, int ultimaFila, int ultimaColumna)
    {
        hoja.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        hoja.PageSetup.PaperSize = XLPaperSize.LetterPaper;
        hoja.PageSetup.FitToPages(1, 0);
        hoja.PageSetup.Margins.Top = 0.5;
        hoja.PageSetup.Margins.Bottom = 0.5;
        hoja.PageSetup.Margins.Left = 0.4;
        hoja.PageSetup.Margins.Right = 0.4;
        hoja.PageSetup.SetRowsToRepeatAtTop(filaBanda, filaEncabezado);
        hoja.PageSetup.CenterHorizontally = true;

        hoja.PageSetup.Footer.Left.AddText("GTE - Gantt de actividades");
        hoja.PageSetup.Footer.Right.AddText("Pagina ");
        hoja.PageSetup.Footer.Right.AddText(XLHFPredefinedText.PageNumber);
        hoja.PageSetup.Footer.Right.AddText(" de ");
        hoja.PageSetup.Footer.Right.AddText(XLHFPredefinedText.NumberOfPages);

        if (ultimaFila >= filaEncabezado && ultimaColumna >= 1)
        {
            hoja.PageSetup.PrintAreas.Add(1, 1, ultimaFila, ultimaColumna);
        }
    }

    /// <summary>
    /// Escribe filtros y leyenda arriba de todo y devuelve la ultima fila que ocupo.
    ///
    /// Todo el texto va en la MISMA columna (la B) y la A queda para las muestras de color:
    /// si el titulo de una seccion se pusiera en A y sus renglones en B, el bloque se lee
    /// escalonado; y si el texto se metiera en A, la muestra de color se lo comeria y el ancho
    /// de esa columna lo cortaria.
    /// </summary>
    private static int EscribirPreambulo(IXLWorksheet hoja, GanttExcel gantt)
    {
        var fila = 1;

        var titulo = hoja.Cell(fila, ColumnaTexto);
        titulo.Value = "Gantt de actividades";
        titulo.Style.Font.Bold = true;
        titulo.Style.Font.FontSize = 18;
        titulo.Style.Font.FontColor = Tinta;
        hoja.Row(fila).Height = 26;
        fila++;

        // Regla de color debajo del titulo: se dibuja pintando el fondo de un renglon bajito,
        // que es la unica forma que hay en una hoja de calculo de trazar una linea gruesa.
        // Arranca donde el titulo y mide lo mismo que los chips, para que el bloque quede
        // alineado por la izquierda y por la derecha.
        hoja.Range(fila, ColumnaTexto, fila, ColumnaTexto + CeldasPorChip - 1)
            .Style.Fill.BackgroundColor = Tinta;
        hoja.Row(fila).Height = 3;
        fila += 2;

        foreach (var filtro in gantt.Filtros)
        {
            // Texto enriquecido para que la etiqueta vaya en negritas y el valor no, SIN sacar
            // el valor a otra columna: todo el bloque tiene que quedar en una sola.
            var celda = hoja.Cell(fila, ColumnaTexto);
            var corte = filtro.IndexOf(':');
            if (corte > 0)
            {
                celda.GetRichText().AddText(filtro[..(corte + 1)]).SetBold().SetFontColor(TintaSuave);
                celda.GetRichText().AddText(filtro[(corte + 1)..]).SetFontColor(TintaSuave);
            }
            else
            {
                celda.Value = filtro;
                celda.Style.Font.FontColor = TintaSuave;
            }

            hoja.Row(fila).Height = 14;
            fila++;
        }

        fila++;   // renglon en blanco entre los filtros y la leyenda

        var tituloLeyenda = hoja.Cell(fila, ColumnaTexto);
        tituloLeyenda.Value = "Colores del diagrama";
        tituloLeyenda.Style.Font.Bold = true;
        tituloLeyenda.Style.Font.FontColor = Tinta;
        fila++;

        // Cada entrada es un "chip": el texto va DENTRO del color, no junto a una muestra
        // aparte. Por eso las etiquetas son cortas -- una frase larga sobre un fondo de color
        // no se puede leer, y ademas una celda con relleno ya no deja que el texto se derrame
        // a la de al lado, asi que se cortaria a media palabra.
        var entradas = new (XLColor Fondo, bool Rayada, string Texto)[]
        {
            (ColorEstatus(2), false, "En proceso"),
            (ColorEstatus(3), false, "En pruebas"),
            (ColorEstatus(4), false, "Correccion"),
            (ColorEstatus(0), false, "Pendiente / Terminado"),
            (ColorEstatus(2), true, "Sin fecha de fin (en curso)"),
            (FondoHoy, false, "Hoy"),
            (FondoFinDeSemana, false, "Sabado y domingo"),
        };

        foreach (var (fondo, rayada, texto) in entradas)
        {
            EscribirChip(hoja, fila, fondo, rayada, texto);
            fila++;
        }

        // Lo que no cabe en un chip va como nota suelta, SIN relleno: sin fondo el texto se
        // derrama sobre las celdas vacias de al lado y se lee completo.
        fila++;
        var nota = hoja.Cell(fila, ColumnaTexto);
        nota.Value = "La barra rayada llega hasta hoy porque la actividad no tiene fecha de fin.";
        nota.Style.Font.FontSize = 9;
        nota.Style.Font.Italic = true;
        nota.Style.Font.FontColor = TintaSuave;

        return fila + 1;   // un renglon en blanco antes del encabezado de la tabla
    }

    /// <summary>
    /// Entrada de la leyenda con el texto dentro del color. Se combinan varias celdas para que
    /// el chip tenga ancho propio: la columna del texto se dimensiona contra la tabla, no
    /// contra la leyenda, y sola se quedaria corta.
    /// </summary>
    private static void EscribirChip(
        IXLWorksheet hoja, int fila, XLColor fondo, bool rayada, string texto)
    {
        var rango = hoja.Range(fila, ColumnaTexto, fila, ColumnaTexto + CeldasPorChip - 1);
        rango.Merge();

        var celda = hoja.Cell(fila, ColumnaTexto);
        celda.Value = texto;
        celda.Style.Fill.BackgroundColor = fondo;
        celda.Style.Font.Bold = true;
        celda.Style.Font.FontSize = 10;
        celda.Style.Font.FontColor = ContrasteSobre(fondo);
        celda.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        celda.Style.Alignment.Indent = 1;
        rango.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        rango.Style.Border.OutsideBorderColor = XLColor.White;

        if (rayada)
        {
            celda.Style.Fill.PatternType = XLFillPatternValues.LightUp;
            celda.Style.Fill.PatternColor = XLColor.White;
        }

        hoja.Row(fila).Height = 16;
    }

    /// <summary>
    /// Cuantas celdas ocupa un chip. Es un numero fijo y no calculado porque los anchos de las
    /// columnas se fijan despues de escribir la leyenda; con siete alcanza para la etiqueta mas
    /// larga sin invadir la zona del eje en un reporte de pocas columnas.
    /// </summary>
    private const int CeldasPorChip = 7;

    /// <summary>
    /// Blanco o tinta segun que tan oscuro sea el fondo, con la luminancia percibida (la vista
    /// pesa mucho mas el verde que el azul). Sin esto, el chip gris claro de "Hoy" saldria con
    /// texto blanco sobre rosa palido.
    /// </summary>
    private static XLColor ContrasteSobre(XLColor fondo)
    {
        var color = fondo.Color;
        var luminancia = ((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) / 255;
        return luminancia > 0.6 ? Tinta : XLColor.White;
    }

    /// <summary>Columna unica donde cae TODO el texto del preambulo.</summary>
    private const int ColumnaTexto = 2;

    /// <summary>
    /// Ancho automatico SOLO de las columnas de datos y midiendo SOLO el encabezado y los
    /// renglones: si se midiera la hoja entera, los textos largos del preambulo estirarian la
    /// columna B a lo ancho de la pantalla y el diagrama quedaria fuera de vista. El tope
    /// evita lo mismo con una descripcion kilometrica.
    /// </summary>
    private static void AjustarColumnasDeDatos(
        IXLWorksheet hoja, int columnas, int filaInicio, int filaFin)
    {
        for (var columna = 1; columna <= columnas; columna++)
        {
            var ancho = hoja.Column(columna).AdjustToContents(filaInicio, filaFin).Width;
            if (ancho > AnchoMaximoColumnaDatos)
            {
                hoja.Column(columna).Width = AnchoMaximoColumnaDatos;
            }
        }
    }

    private const double AnchoMaximoColumnaDatos = 45;


    /// <summary>
    /// Una columna del eje de tiempo: el tramo que cubre, la etiqueta de su encabezado y en que
    /// escala se genero. La escala la necesitan la banda de agrupacion (agrupa por mes o por
    /// anio segun el caso) y el sombreado de fin de semana (que solo tiene sentido si la
    /// columna es un dia).
    /// </summary>
    private sealed record Periodo(DateTime Inicio, DateTime Fin, string Etiqueta, EscalaEje Escala)
    {
        public bool EsDiario => Escala == EscalaEje.Diaria;

        public bool EsMensual => Escala == EscalaEje.Mensual;
    }

    private enum EscalaEje { Diaria, Semanal, Mensual }

    /// <summary>
    /// Tope de columnas del eje. Un rango de varios anios en escala diaria haria una hoja
    /// ilegible (y lenta de abrir) mucho antes de topar con el limite real de Excel.
    /// </summary>
    private const int TopeColumnasEje = 400;

    private const int AnchoColumnaDia = 3;

    private const int AnchoColumnaPeriodo = 6;

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
        var escala = dias <= 31 ? EscalaEje.Diaria
            : dias <= 220 ? EscalaEje.Semanal
            : EscalaEje.Mensual;

        var periodos = new List<Periodo>();
        var cursor = desde;

        while (cursor <= hasta && periodos.Count < TopeColumnasEje)
        {
            DateOnly finPeriodo;
            string etiqueta;

            if (escala == EscalaEje.Diaria)
            {
                finPeriodo = cursor;
                etiqueta = cursor.Day.ToString();
            }
            else if (escala == EscalaEje.Semanal)
            {
                finPeriodo = cursor.AddDays(6);
                etiqueta = $"{cursor.Day}/{cursor.Month}";
            }
            else
            {
                finPeriodo = new DateOnly(cursor.Year, cursor.Month, 1).AddMonths(1).AddDays(-1);
                etiqueta = $"{cursor:MMM}";
            }

            if (finPeriodo > hasta) finPeriodo = hasta;

            periodos.Add(new Periodo(
                cursor.ToDateTime(TimeOnly.MinValue),
                finPeriodo.ToDateTime(TimeOnly.MaxValue),
                etiqueta,
                escala));

            cursor = finPeriodo.AddDays(1);
        }

        return periodos;
    }

    private static void DibujarEje(
        IXLWorksheet hoja, int filaEncabezado, int primeraColumna, List<Periodo> periodos)
    {
        var hoy = DateTime.Now;

        for (var i = 0; i < periodos.Count; i++)
        {
            var periodo = periodos[i];
            var columna = primeraColumna + i;
            var celda = hoja.Cell(filaEncabezado, columna);

            celda.Value = periodo.Etiqueta;
            celda.Style.Font.Bold = true;
            celda.Style.Font.FontSize = 8;
            celda.Style.Font.FontColor = XLColor.White;
            celda.Style.Fill.BackgroundColor = Tinta;
            celda.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            celda.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            celda.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            celda.Style.Border.OutsideBorderColor = XLColor.White;

            // La columna que contiene hoy se marca de rojo en el encabezado y el sombreado baja
            // por toda la tabla (DibujarFondoDelEje): es la "linea de hoy" del diagrama de
            // pantalla, que en una cuadricula no se puede trazar entre dos columnas.
            if (hoy >= periodo.Inicio && hoy <= periodo.Fin)
            {
                celda.Style.Fill.BackgroundColor = ColorHoy;
            }
            else if (periodo.EsDiario && periodo.Inicio.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                celda.Style.Fill.BackgroundColor = TintaSuave;
            }

            // La escala diaria cabe en una columna angosta con el numero de pie; la semanal y la
            // mensual llevan texto y necesitan aire, si no Excel lo corta.
            hoja.Column(columna).Width = periodo.EsDiario ? AnchoColumnaDia : AnchoColumnaPeriodo;
        }
    }

    private static void DibujarBarras(
        IXLWorksheet hoja, int primeraFilaDatos, int primeraColumna, List<Periodo> periodos,
        IReadOnlyList<BarraGanttExcel> barras)
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

                var celda = hoja.Cell(primeraFilaDatos + fila, primeraColumna + i);
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
