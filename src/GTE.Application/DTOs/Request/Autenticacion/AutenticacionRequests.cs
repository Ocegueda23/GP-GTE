namespace GTE.Application.DTOs.Request.Autenticacion;

public class LoginRequest
{
    public string Dominio { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class CambiarPasswordRequest
{
    public string PasswordActual { get; set; } = string.Empty;
    public string PasswordNueva { get; set; } = string.Empty;
}
