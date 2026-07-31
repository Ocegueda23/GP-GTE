namespace GTE.WebApi.Seguridad;

/// <summary>
/// Configuracion de autenticacion. En produccion se usa Entra ID (OIDC) llenando
/// Authority y Audience. El emisor local existe SOLO para desarrollo, cuando no
/// hay tenant disponible: nunca se habilita fuera del ambiente Development.
/// </summary>
public class OpcionesAutenticacion
{
    public const string Seccion = "Jwt";

    /// <summary>URL del tenant de Entra ID (o ADFS). Vacio = sin identidad externa.</summary>
    public string? Authority { get; set; }

    public string Audience { get; set; } = "gte-api";

    /// <summary>Emisor local de tokens para desarrollo. Ignorado fuera de Development.</summary>
    public EmisorDesarrollo Desarrollo { get; set; } = new();

    public bool TieneIdentidadExterna => !string.IsNullOrWhiteSpace(Authority);
}

public class EmisorDesarrollo
{
    /// <summary>Debe activarse explicitamente; solo surte efecto en Development.</summary>
    public bool Habilitado { get; set; }

    public string Issuer { get; set; } = "gte-desarrollo";

    /// <summary>
    /// Clave de firma. Si se deja vacia en desarrollo se genera una aleatoria en memoria,
    /// de modo que reiniciar la API invalida los tokens anteriores.
    /// </summary>
    public string? ClaveFirma { get; set; }

    public int MinutosVigencia { get; set; } = 480;
}
