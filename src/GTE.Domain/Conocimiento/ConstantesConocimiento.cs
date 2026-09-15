namespace GTE.Domain.Conocimiento;

/// <summary>Claves de permisos del modulo Base de conocimiento (dbo.tblPermiso, script 42).</summary>
public static class PermisosConocimiento
{
    /// <summary>
    /// Crear, editar y dar de baja articulos. La LECTURA interna no exige permiso:
    /// P23 esta marcada como "Todos" en el Documento Maestro (seccion 5.1).
    /// </summary>
    public const string Administrar = "CON.Administrar";

    /// <summary>
    /// Dar de baja articulos. Separado de Administrar a proposito: un articulo es memoria
    /// acumulada del equipo y su baja no se deshace desde la interfaz, asi que redactar
    /// no deberia alcanzar para borrar lo que escribieron los demas.
    /// </summary>
    public const string Eliminar = "CON.Eliminar";
}

/// <summary>Reglas y contratos del modulo Base de conocimiento.</summary>
public static class ConstantesConocimiento
{
    /// <summary>
    /// Discriminador de dbo.tblArchivoVinculo.Entidad para los adjuntos de un articulo.
    /// La tabla de vinculos es generica (Entidad + IdEntidad), asi que el modulo no
    /// necesita esquema propio de adjuntos.
    /// </summary>
    public const string EntidadArchivo = "ArticuloConocimiento";

    public const int LongitudMaximaTitulo = 200;

    /// <summary>Tope de resultados por pagina del listado (interno y publico).</summary>
    public const int TamanoPaginaMaximo = 100;
}
