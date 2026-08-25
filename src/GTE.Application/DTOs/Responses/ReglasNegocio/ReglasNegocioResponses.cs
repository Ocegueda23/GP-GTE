namespace GTE.Application.DTOs.Responses.ReglasNegocio;

/// <summary>
/// De donde le viene la regla al proyecto que se esta consultando. "Propia" = el proyecto
/// es el dueno y ahi se edita; "Heredada" = otro proyecto la declaro y este solo la lee.
/// </summary>
public static class OrigenRegla
{
    public const string Propia = "Propia";
    public const string Heredada = "Heredada";
}

/// <summary>Fila del listado de reglas de un proyecto.</summary>
public record ReglaNegocioResumenResponse(
    int IdReglaNegocio,
    string Clave,
    string Nombre,
    int IdProyectoDueno,
    string ClaveProyectoDueno,
    string NombreProyectoDueno,
    // Propia o Heredada respecto del proyecto consultado.
    string Origen,
    int? IdAmbitoRegla,
    string? NombreAmbito,
    int? IdTipoAmbitoRegla,
    string? NombreTipoAmbito,
    int IdEstadoReglaNegocio,
    string NombreEstado,
    // Solo en heredadas: como le pega esta regla al proyecto consultado.
    string? DescripcionImpacto,
    bool Activo);

/// <summary>Un proyecto afectado por la regla (panel inverso de la ficha).</summary>
public record ImpactoReglaResponse(
    int IdReglaNegocioImpacto,
    int IdProyectoAfectado,
    string ClaveProyectoAfectado,
    string NombreProyectoAfectado,
    string? DescripcionImpacto,
    int? IdAmbitoRegla,
    string? NombreAmbito);

public record RelacionReglaResponse(
    int IdReglaNegocioRelacion,
    int IdReglaNegocioRelacionada,
    string ClaveRelacionada,
    string NombreRelacionada,
    int IdProyectoRelacionada,
    int IdTipoRelacionRegla,
    string NombreTipoRelacion,
    string? Nota);

public record ReglaVersionResponse(
    int IdReglaNegocioVersion,
    int NumeroVersion,
    string Enunciado,
    string? Justificacion,
    string? MotivoCambio,
    DateTime FechaRegistro,
    string UsuarioRegistro);

/// <summary>Ficha completa de la regla.</summary>
public record ReglaNegocioResponse(
    int IdReglaNegocio,
    int IdProyecto,
    string ClaveProyecto,
    string NombreProyecto,
    string Clave,
    string Nombre,
    string Enunciado,
    string? Justificacion,
    int? IdAmbitoRegla,
    string? NombreAmbito,
    int? IdTipoAmbitoRegla,
    string? NombreTipoAmbito,
    int IdEstadoReglaNegocio,
    string NombreEstado,
    string? MensajeError,
    string? PermisoBypass,
    string? UbicacionCodigo,
    DateOnly? FechaVigenciaDesde,
    DateOnly? FechaVigenciaHasta,
    int VersionActual,
    IReadOnlyList<ImpactoReglaResponse> Impactos,
    IReadOnlyList<RelacionReglaResponse> Relaciones,
    DateTime FechaRegistro,
    string UsuarioRegistro,
    DateTime? FechaMovto,
    string? UsuarioMovto,
    bool Activo);

/// <summary>
/// Lo que ve un proyecto al consultar sus reglas: las propias (editables) y las que otros
/// proyectos declararon que lo afectan (solo lectura, con su proyecto de origen).
/// </summary>
public record CatalogoReglasProyectoResponse(
    int IdProyecto,
    string ClaveProyecto,
    string NombreProyecto,
    IReadOnlyList<ReglaNegocioResumenResponse> Propias,
    IReadOnlyList<ReglaNegocioResumenResponse> Heredadas,
    int TotalPropias,
    int TotalHeredadas);

public record AmbitoReglaResponse(
    int IdAmbitoRegla,
    int IdProyecto,
    int IdTipoAmbitoRegla,
    string NombreTipoAmbito,
    string Nombre,
    string? Descripcion,
    int TotalReglas,
    bool Activo);

public record OpcionCatalogoResponse(int Id, string Nombre);

/// <summary>Catalogos que alimentan los selects del formulario de reglas.</summary>
public record CatalogosReglasNegocioResponse(
    IReadOnlyList<OpcionCatalogoResponse> TiposAmbito,
    IReadOnlyList<OpcionCatalogoResponse> Estados,
    IReadOnlyList<OpcionCatalogoResponse> TiposRelacion);
