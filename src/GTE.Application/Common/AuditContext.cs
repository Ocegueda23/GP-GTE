namespace GTE.Application.Common;

/// <summary>
/// Contexto de auditoria por request. Lo llena el AuditMiddleware desde el JWT:
/// la auditoria siempre sale del token, nunca del payload.
/// Registrado como scoped en el contenedor de DI.
/// </summary>
public class AuditContext
{
    public string Usuario { get; set; } = "anonimo";
    public string Ip { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string IdSistema { get; set; } = "GTE";
}
