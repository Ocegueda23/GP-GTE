namespace GTE.Application.DTOs.Request.ReglasNegocio;

/// <summary>
/// Alta de una regla. IdProyecto es el DUENO y no se puede cambiar despues.
/// IdAmbitoRegla es opcional (regla general al proyecto) y apunta a UN flujo de operacion
/// O a UNA caracteristica del sistema, nunca a ambos.
///
/// La CLAVE no se captura: la forma el backend como RN-{CLAVE DEL PROYECTO}-{CONSECUTIVO}
/// via dbo.spGenerarFolio. Por eso no aparece en este request.
/// </summary>
public record ReglaNegocioCrearRequest(
    int IdProyecto,
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

/// <summary>Edicion. El proyecto dueno y la clave no se editan.</summary>
public record ReglaNegocioActualizarRequest(
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

/// <summary>Declara que la regla afecta ademas a otro proyecto (secundario).</summary>
public record ImpactoReglaCrearRequest(
    int IdProyectoAfectado,
    string? DescripcionImpacto,
    int? IdAmbitoRegla);

public record AmbitoReglaCrearRequest(
    int IdProyecto,
    int IdTipoAmbitoRegla,
    string Nombre,
    string? Descripcion);

public record AmbitoReglaActualizarRequest(string Nombre, string? Descripcion);

/// <summary>Filtros del listado de reglas de un proyecto.</summary>
public record ReglasNegocioFiltroRequest(
    int? IdProyecto,
    int? IdAmbitoRegla,
    int? IdEstadoReglaNegocio,
    int? IdTipoAmbitoRegla,
    string? Busqueda,
    bool IncluirHeredadas = true,
    bool IncluirDerogadas = false,
    int Pagina = 1,
    int TamanoPagina = 50);
