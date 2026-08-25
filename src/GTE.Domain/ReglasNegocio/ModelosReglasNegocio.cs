namespace GTE.Domain.ReglasNegocio;

/// <summary>Alta de una regla. El proyecto dueno es obligatorio: toda regla nace en un proyecto.</summary>
public record ReglaNegocioNueva(
    int IdProyecto,
    string Clave,
    string Nombre,
    string Enunciado,
    string? Justificacion,
    int? IdAmbitoRegla,
    int IdEstadoReglaNegocio,
    string? MensajeError,
    string? PermisoBypass,
    string? UbicacionCodigo,
    DateOnly? FechaVigenciaDesde,
    DateOnly? FechaVigenciaHasta);

/// <summary>
/// Edicion de una regla. El proyecto dueno NO se puede cambiar: mover una regla de
/// sistema cambiaria su identidad y dejaria huerfanos sus impactos.
/// </summary>
public record ReglaNegocioActualizacion(
    string Nombre,
    string Enunciado,
    string? Justificacion,
    int? IdAmbitoRegla,
    int IdEstadoReglaNegocio,
    string? MensajeError,
    string? PermisoBypass,
    string? UbicacionCodigo,
    DateOnly? FechaVigenciaDesde,
    DateOnly? FechaVigenciaHasta,
    string? MotivoCambio);

/// <summary>Estado minimo de una regla, para los gates de los handlers.</summary>
public record EstadoReglaNegocio(
    int IdReglaNegocio,
    int IdProyecto,
    string Clave,
    int IdEstado,
    int VersionActual,
    bool Activo);

/// <summary>Alta de un proyecto afectado (secundario) por una regla.</summary>
public record ImpactoReglaNuevo(
    int IdReglaNegocio,
    int IdProyectoAfectado,
    string? DescripcionImpacto,
    int? IdAmbitoRegla);

/// <summary>Alta o edicion de un flujo de operacion / caracteristica del sistema.</summary>
public record AmbitoReglaNuevo(
    int IdProyecto,
    int IdTipoAmbitoRegla,
    string Nombre,
    string? Descripcion);
