namespace GTE.Domain.NotasVersion;

/// <summary>
/// Tipos de cambio de una nota de version. Enumerado de ID fijo (tblTipoCambioVersion): son el
/// mismo eje del estandar de versionado de Interflo (Proyecto.Mejora.Defecto), asi que el
/// detalle de la nota deja ver por que subio el digito que subio.
/// </summary>
public static class TipoCambioVersion
{
    public const int Proyecto = 1;
    public const int Mejora = 2;
    public const int Defecto = 3;
}

public static class PermisosNotasVersion
{
    /// <summary>
    /// Redactar y publicar notas. LEER las notas publicadas no lleva permiso: cualquier usuario
    /// autenticado debe poder ver que trae la version que esta usando.
    /// </summary>
    public const string Administrar = "ADM.NotasVersion";
}

/// <summary>Un renglon de la nota. IdNotaVersionDetalle null = alta; UiId lo ecoa el back (patron uiId).</summary>
public record RenglonNotaVersion(
    int? IdNotaVersionDetalle,
    string? UiId,
    int IdTipoCambioVersion,
    string? Modulo,
    string Descripcion,
    int Orden);

public record NotaVersionNueva(
    string Version,
    DateOnly FechaLiberacion,
    string? Resumen,
    bool Publicada,
    IReadOnlyList<RenglonNotaVersion> Renglones);

public record NotaVersionEdicion(
    int IdNotaVersion,
    string Version,
    DateOnly FechaLiberacion,
    string? Resumen,
    bool Publicada,
    IReadOnlyList<RenglonNotaVersion> Renglones);

/// <summary>Lo minimo para validar antes de escribir, sin cargar el arbol completo.</summary>
public record EstadoNotaVersion(int IdNotaVersion, string Version, bool Publicada);

/// <summary>Id real que el front reasocia con su UiId despues de guardar.</summary>
public record RenglonPersistido(string? UiId, int IdNotaVersionDetalle);
