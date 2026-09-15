namespace GTE.Domain.Ayuda;

/// <summary>Claves de permisos del modulo Ayuda (dbo.tblPermiso, script 03_Scripts/01).</summary>
public static class PermisosAyuda
{
    /// <summary>
    /// Leer el Centro de Mando TI (modelo de indicadores y evaluacion de los responsables
    /// de area). El Manual de usuario NO exige permiso -- es para todos; este documento si,
    /// porque describe como se evalua el desempeno de cada responsable.
    /// </summary>
    public const string VerCentroMando = "AYU.CentroMando";
}

/// <summary>Contratos del modulo Ayuda.</summary>
public static class ConstantesAyuda
{
    /// <summary>
    /// Documentos servidos por el endpoint autenticado de ayuda. La clave es parte de la
    /// ruta publica y del nombre del archivo en disco, asi que se valida contra esta lista
    /// blanca antes de tocar el sistema de archivos (nunca se concatena lo que mande el
    /// cliente a una ruta).
    /// </summary>
    public const string DocumentoCentroMando = "centro-mando-ti";

    /// <summary>Carpeta (relativa al directorio de la aplicacion) donde viven los documentos.</summary>
    public const string CarpetaDocumentos = "Contenido/Ayuda";
}
