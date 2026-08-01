using GTE.Application.DTOs.Responses.Seguridad;

namespace GTE.Application.DTOs.Responses.Autenticacion;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expira { get; set; }
    public SesionResponse Sesion { get; set; } = new();
    public bool RequiereCambioPassword { get; set; }
}
