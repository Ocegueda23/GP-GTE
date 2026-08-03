namespace GTE.Application.DTOs.Responses.WorkItems;

/// <summary>
/// Fila de la lista de subtareas (WorkItems hijos) de un WorkItem padre.
/// MinutosRegistrados es la suma directa de tblRegistroTiempo, no
/// MinutosInvertidos (ese sale de tblHistorialEstatus, que la migracion del
/// GT solo llena para los WorkItems raiz, nunca para los hijos).
/// </summary>
public class WorkItemHijoResponse
{
    public int IdWorkItem { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public int IdEstatus { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public string? Asignado { get; set; }
    public int MinutosRegistrados { get; set; }
}
