namespace GTE.Application.DTOs.Responses.WorkItems;

/// <summary>
/// Vista previa del presupuesto que RN-GTE-015 congelaria con la complejidad elegida.
/// El presupuesto real se resuelve con la matriz Complejidad x Nivel del asignado, asi que
/// sin asignado (o con un asignado sin nivel capturado) Minutos/Puntos vienen en null y la
/// pantalla usa Niveles para mostrar lo que costaria segun quien lo tome.
/// </summary>
public class PresupuestoEstimadoResponse
{
    public int IdComplejidad { get; set; }
    public string Complejidad { get; set; } = string.Empty;
    public int? IdAsignado { get; set; }
    public int? IdNivel { get; set; }
    public string? Nivel { get; set; }
    public int? Minutos { get; set; }
    public decimal? Puntos { get; set; }

    /// <summary>Matriz de la complejidad por nivel, para referencia cuando no hay asignado.</summary>
    public List<PresupuestoNivelDTO> Niveles { get; set; } = [];
}

public class PresupuestoNivelDTO
{
    public int IdNivel { get; set; }
    public string Nivel { get; set; } = string.Empty;
    public int Minutos { get; set; }
    public decimal? Puntos { get; set; }
}
