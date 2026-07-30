using GTE.Application.DTOs.Responses.WorkItems;

namespace GTE.Application.DTOs.Responses.MiDia;

/// <summary>
/// Item de la vista personal con la accion que lo pone En Proceso desde su
/// estatus actual, resuelta por el MOTOR (INICIAR o REANUDAR segun el grafo).
/// El front pinta y manda lo que recibe: nunca deduce la transicion.
/// </summary>
public class MiDiaItemResponse : BandejaItemResponse
{
    public string? AccionInicio { get; set; }
    public string? EtiquetaAccionInicio { get; set; }
}

/// <summary>
/// Vista personal del dia: todo lo que una persona necesita para decidir
/// que hacer, en una sola llamada.
/// </summary>
public class MiDiaResponse
{
    public string Usuario { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }

    /// <summary>El unico item En Proceso del usuario (RN-REQ-01); null si no esta trabajando en nada.</summary>
    public MiDiaItemResponse? EnProceso { get; set; }

    public IReadOnlyList<MiDiaItemResponse> Vencidas { get; set; } = [];
    public IReadOnlyList<MiDiaItemResponse> ParaHoy { get; set; } = [];
    public IReadOnlyList<MiDiaItemResponse> Proximas { get; set; } = [];

    /// <summary>Minutos registrados por el usuario el dia de hoy.</summary>
    public int MinutosHoy { get; set; }

    /// <summary>Total de elementos abiertos asignados al usuario.</summary>
    public int TotalAbiertos { get; set; }
}
