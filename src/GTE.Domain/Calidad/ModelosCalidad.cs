namespace GTE.Domain.Calidad;

public record PlanPruebaNuevo(int IdProyecto, int? IdRelease, string Nombre, string? Descripcion);

public record CasoPruebaNuevo(
    string? Folio,
    int IdPlanPrueba,
    string Titulo,
    string? Precondiciones,
    string? ResultadoEsperado,
    int IdTipoPrueba,
    int? IdWorkItem,
    IReadOnlyList<PasoCaso> Pasos);

public record PasoCaso(int NumeroPaso, string Accion, string? ResultadoEsperado);

public record CicloPruebaNuevo(int IdPlanPrueba, string Nombre, DateOnly? FechaInicio, DateOnly? FechaFin);

public record EjecucionNueva(
    int IdCasoPrueba,
    int IdCicloPrueba,
    int IdEjecutor,
    int IdResultadoPrueba,
    string? Observaciones);

public record EstadoPlan(int IdPlanPrueba, int IdProyecto, int? IdRelease, string Nombre, bool Activo);

public record EstadoCaso(int IdCasoPrueba, int IdPlanPrueba, int IdProyecto, string Titulo, int? IdWorkItem, bool Activo);

public record EstadoEjecucion(
    int IdEjecucionPrueba,
    int IdCasoPrueba,
    int IdCicloPrueba,
    int IdResultado,
    int IdProyecto,
    string TituloCaso,
    string? Observaciones);
