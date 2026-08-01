namespace GTE.WebApi.Seguridad;

/// <summary>
/// Configuracion de autenticacion propia de GTE (sin proveedor externo): un solo JWT
/// HMAC compartido por el atajo de desarrollo y por el login real. La clave de firma
/// es obligatoria fuera de Development (falla al arrancar en vez de quedar abierta).
/// </summary>
public class OpcionesAutenticacion
{
    public const string Seccion = "Jwt";

    public string Issuer { get; set; } = "gte-api";

    public string Audience { get; set; } = "gte-api";

    /// <summary>
    /// Clave de firma HMAC. Obligatoria fuera de Development. En Development, si se deja
    /// vacia se genera una efimera en memoria (los tokens se invalidan al reiniciar).
    /// </summary>
    public string? ClaveFirma { get; set; }

    public int MinutosVigenciaAcceso { get; set; } = 15;

    /// <summary>Atajo de desarrollo (sin contraseña, por nombre de dominio). Solo Development.</summary>
    public EmisorDesarrollo Desarrollo { get; set; } = new();
}

public class EmisorDesarrollo
{
    /// <summary>Debe activarse explicitamente; solo surte efecto en Development.</summary>
    public bool Habilitado { get; set; }
}
