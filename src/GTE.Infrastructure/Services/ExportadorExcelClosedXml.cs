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

    private static void EstablecerValor(IXLCell celda, object? valor)
    {
        switch (valor)
        {
            case null:
                celda.Value = string.Empty;
                break;
            case string texto:
                celda.Value = texto;
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
}
