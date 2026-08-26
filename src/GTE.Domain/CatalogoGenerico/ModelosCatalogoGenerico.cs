namespace GTE.Domain.CatalogoGenerico;

/// <summary>
/// Columna real de una tabla de bdsGTE, leida en vivo de INFORMATION_SCHEMA/sys.*
/// (nunca duplicada en tblCatalogoGenericoColumna). "Que hay" -- fuente de verdad = BD.
/// </summary>
public record ColumnaEsquema(
    string NombreColumna,
    string TipoSql,
    bool EsNulable,
    int? LongitudMaxima,
    bool EsPk,
    bool EsIdentity,
    int OrdinalEsquema);

/// <summary>Datos minimos para registrar un catalogo nuevo (CrearCatalogoCommand).</summary>
public record CatalogoNuevo(string Clave, string NombreTabla, string Titulo);

/// <summary>Configuracion de presentacion de una columna, tal como la edita el administrador.</summary>
public record ColumnaConfigEdicion(
    string NombreColumna,
    string DisplayName,
    bool EsVisible,
    bool EsSoloLectura,
    bool EsRequerido,
    int OrdinalPos,
    string? TablaFk,
    string? ColumnaClaveFk,
    string? ColumnaMostrarFk,
    bool EsCifrado,
    bool AutoFechaAlta,
    bool AutoFechaEdicion,
    bool AutoUsuarioAlta,
    bool AutoUsuarioEdicion);

/// <summary>Catalogo ya registrado (fila de tblCatalogoGenerico).</summary>
public record CatalogoResumen(int IdCatalogo, string Clave, string NombreTabla, string Titulo);

/// <summary>
/// "Como se ve/edita": merge de ColumnaEsquema (manda tipo/nulabilidad/PK/identity) y
/// ColumnaConfigEdicion (manda presentacion). Es lo que consume el motor CRUD generico
/// y lo que arma el grid/formulario dinamico del frontend.
/// </summary>
public record ColumnaReconciliada(
    string NombreColumna,
    string TipoSql,
    bool EsNulable,
    int? LongitudMaxima,
    bool EsPk,
    bool EsIdentity,
    string DisplayName,
    bool EsVisible,
    bool EsSoloLectura,
    bool EsRequerido,
    int OrdinalPos,
    string? TablaFk,
    string? ColumnaClaveFk,
    string? ColumnaMostrarFk,
    bool EsCifrado,
    bool AutoFechaAlta,
    bool AutoFechaEdicion,
    bool AutoUsuarioAlta,
    bool AutoUsuarioEdicion);

/// <summary>Configuracion reconciliada completa de un catalogo: metadatos + sus columnas.</summary>
public record ConfiguracionCatalogo(
    int IdCatalogo,
    string Clave,
    string NombreTabla,
    string Titulo,
    IReadOnlyList<ColumnaReconciliada> Columnas);

/// <summary>Opcion de un combo de busqueda FK: valor real de la columna clave + texto a mostrar.</summary>
public record OpcionFk(string Valor, string Etiqueta);

/// <summary>Filtro discreto por columna, tipo "filtro de Excel": solo estos valores (como texto).</summary>
public record FiltroColumnaDiscreto(string NombreColumna, IReadOnlyList<string> Valores);

/// <summary>Filtro por rango de fecha sobre una columna elegida dinamicamente.</summary>
public record FiltroFecha(string NombreColumna, DateOnly? Desde, DateOnly? Hasta);

/// <summary>Todos los filtros combinables del listado, unidos por AND.</summary>
public record FiltrosListadoCatalogo(
    string? Texto,
    IReadOnlyList<FiltroColumnaDiscreto> FiltrosColumna,
    FiltroFecha? Fecha,
    string? OrdenarPor,
    bool OrdenDescendente);

/// <summary>Claves de permiso del motor de catalogos genericos (dbo.tblPermiso).</summary>
public static class PermisosCatalogoGenerico
{
    /// <summary>Dar de alta catalogos nuevos y editar su configuracion de columnas (sembrado en el script 37).</summary>
    public const string Configurar = "ADM.CatalogoGenerico";

    public static string Ver(string clave) => $"CAT.{clave}.Ver";
    public static string Crear(string clave) => $"CAT.{clave}.Crear";
    public static string Editar(string clave) => $"CAT.{clave}.Editar";
    public static string Eliminar(string clave) => $"CAT.{clave}.Eliminar";

    /// <summary>Descifrar puntualmente una columna cifrada (accion explicita y auditada, nunca automatica).</summary>
    public static string Descifrar(string clave) => $"CAT.{clave}.Descifrar";

    /// <summary>Las claves que se siembran en tblPermiso al crear (o refrescar) un catalogo.</summary>
    public static IReadOnlyList<(string Clave, string Descripcion)> ClavesPorCatalogo(string clave, string titulo) =>
    [
        (Ver(clave), $"Ver los registros del catalogo {titulo}"),
        (Crear(clave), $"Crear registros en el catalogo {titulo}"),
        (Editar(clave), $"Editar registros del catalogo {titulo}"),
        (Eliminar(clave), $"Eliminar registros del catalogo {titulo}"),
        (Descifrar(clave), $"Descifrar puntualmente columnas cifradas del catalogo {titulo}")
    ];
}
