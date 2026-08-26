namespace GTE.Domain.ReglasNegocio;

/// <summary>Claves de permisos del modulo Catalogo de reglas de negocio (dbo.tblPermiso, script 43).</summary>
public static class PermisosReglasNegocio
{
    /// <summary>
    /// Consultar el catalogo. El alcance por proyecto lo resuelve el QueryService contra
    /// tblUsuarioRol: las reglas de negocio de un sistema son informacion sensible del
    /// cliente interno, asi que no se listan las de proyectos donde el usuario no tiene rol.
    /// </summary>
    public const string Ver = "RGN.Ver";

    /// <summary>Crear, editar, derogar reglas y administrar ambitos e impactos.</summary>
    public const string Administrar = "RGN.Administrar";
}

/// <summary>
/// IDs de dbo.tblTipoAmbitoRegla. Son CONTRATO (los siembra el script 43 con ID fijo).
/// Una regla se ubica en UN flujo O en UNA caracteristica, nunca en ambos: si aplica a
/// los dos casos, se dan de alta dos reglas (decision del equipo 2026-08-24).
/// </summary>
public static class TiposAmbitoRegla
{
    public const int FlujoDeOperacion = 1;
    public const int CaracteristicaDelSistema = 2;
}

/// <summary>IDs de dbo.tblEstadoReglaNegocio. Son CONTRATO (script 43).</summary>
public static class EstadosReglaNegocio
{
    public const int Vigente = 1;
    public const int ImplementadaParcialmente = 2;
    public const int DocumentadaSinImplementar = 3;
    public const int Derogada = 4;
}

/// <summary>IDs de dbo.tblTipoRelacionRegla. Son CONTRATO (script 43).</summary>
public static class TiposRelacionRegla
{
    public const int ComplementaA = 1;
    public const int DependeDe = 2;
    public const int EnConflictoCon = 3;
    public const int SustituyeA = 4;
}

/// <summary>Contratos del modulo Catalogo de reglas de negocio.</summary>
public static class ConstantesReglasNegocio
{
    public const int LongitudMaximaClave = 30;
    public const int LongitudMaximaNombre = 300;
    public const int LongitudMaximaMensajeError = 500;
    public const int LongitudMaximaUbicacionCodigo = 400;
    public const int LongitudMaximaNombreAmbito = 200;
    public const int LongitudMaximaDescripcionImpacto = 1000;

    /// <summary>Tope de resultados por pagina del listado.</summary>
    public const int TamanoPaginaMaximo = 100;

    /// <summary>Clave del proyecto que representa al propio GTE en tblProyecto (script 44).</summary>
    public const string ClaveProyectoGte = "GTE";
}
