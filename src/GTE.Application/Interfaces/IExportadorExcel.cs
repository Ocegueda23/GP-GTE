namespace GTE.Application.Interfaces;

/// <summary>
/// Exportacion generica de una tabla a un libro de Excel (.xlsx), usada por el catalogo de
/// reportes R01-R14 (Doctos/GTE-DocumentoMaestro.md seccion 13: "exportacion Excel via back").
/// </summary>
public interface IExportadorExcel
{
    /// <summary>Genera un libro de una sola hoja con encabezados en negrita y columnas autoajustadas.</summary>
    byte[] GenerarLibro(string tituloHoja, IReadOnlyList<string> encabezados, IReadOnlyList<IReadOnlyList<object?>> filas);

    /// <summary>
    /// Igual que <see cref="GenerarLibro"/> pero agregando a la derecha de los datos el
    /// diagrama de Gantt: una columna por periodo del eje y, en cada renglon, las celdas del
    /// tramo de la actividad pintadas de su color de estatus. Es lo que se lleva el director:
    /// el Excel plano de fechas no le dice nada, la barra si.
    /// </summary>
    byte[] GenerarLibroGantt(
        string tituloHoja,
        IReadOnlyList<string> encabezados,
        IReadOnlyList<IReadOnlyList<object?>> filas,
        GanttExcel gantt);
}

/// <summary>Datos que necesita el exportador para dibujar las barras; uno por renglon de <c>filas</c>.</summary>
/// <param name="Desde">Extremo izquierdo del eje (el mismo del filtro).</param>
/// <param name="Hasta">Extremo derecho del eje (el mismo del filtro).</param>
/// <param name="Barras">
/// Una por renglon y EN EL MISMO ORDEN: el exportador las empareja por indice con
/// <c>filas</c>, no por folio.
/// </param>
/// <param name="Filtros">
/// Con que se corrio el reporte, ya redactado ("Proyecto: Todos"). Se imprime arriba del
/// encabezado: el archivo circula por correo fuera de la aplicacion y sin esto nadie puede
/// saber si esta viendo un mes o un ano, ni un proyecto o todos.
/// </param>
public record GanttExcel(
    DateOnly Desde,
    DateOnly Hasta,
    IReadOnlyList<BarraGanttExcel> Barras,
    IReadOnlyList<string> Filtros);

/// <summary>
/// Tramo a pintar de un renglon. <paramref name="Fin"/> nulo = actividad todavia abierta: la
/// barra corre hasta hoy y se marca aparte, para no inventar una fecha de cierre.
/// </summary>
public record BarraGanttExcel(DateTime Inicio, DateTime? Fin, int IdEstatusWorkItem);
