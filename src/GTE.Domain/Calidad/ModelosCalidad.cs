namespace GTE.Domain.Calidad;

public record PasoCaso(int NumeroPaso, string Accion, string? ResultadoEsperado);

/// <summary>
/// Crea un caso nuevo. Si Reutilizable es false, el caso solo existe para esta
/// asignacion puntual y no aparece en el selector de "usar caso existente" de otros
/// WorkItems del proyecto.
/// </summary>
public record CasoPruebaNuevo(
    int IdProyecto,
    string? Folio,
    string Titulo,
    string? Precondiciones,
    string? ResultadoEsperado,
    int IdTipoPrueba,
    bool Reutilizable,
    IReadOnlyList<PasoCaso> Pasos);

public record CasoPruebaEdicion(
    int IdCasoPrueba,
    string Titulo,
    string? Precondiciones,
    string? ResultadoEsperado,
    int IdTipoPrueba,
    IReadOnlyList<PasoCaso> Pasos);

public record EstadoCaso(int IdCasoPrueba, int? IdProyecto, string Titulo, bool Activo);

/// <summary>Registra el resultado de correr un caso contra un WorkItem concreto.</summary>
public record EjecucionNueva(int IdCasoPrueba, int IdWorkItem, int IdEjecutor, int IdResultadoPrueba, string? Observaciones);

public record EstadoEjecucion(int IdEjecucionPrueba, int IdCasoPrueba, int IdWorkItem, int IdResultado, string TituloCaso);
