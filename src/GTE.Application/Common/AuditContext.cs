namespace GTE.Application.Common;

/// <summary>
/// Contexto de auditoria por request. Lo llena el AuditMiddleware desde el JWT:
/// la auditoria siempre sale del token, nunca del payload.
/// Registrado como scoped en el contenedor de DI.
/// </summary>
public class AuditContext
{
    /// <summary>
    /// Identidad del token (cuenta de dominio); es la que se audita.
    /// Vacia cuando la peticion no trae identidad. No se usa un centinela con texto
    /// (como "anonimo") porque podria coincidir con una cuenta real y confundir la auditoria.
    /// </summary>
    public string Usuario { get; set; } = string.Empty;

    public bool TieneIdentidad => !string.IsNullOrWhiteSpace(Usuario);

    /// <summary>Nombre para mostrar que trae el proveedor de identidad.</summary>
    public string? NombreCompleto { get; set; }

    public string? Correo { get; set; }
    public string Ip { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string IdSistema { get; set; } = "GTE";
}
