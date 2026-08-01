namespace GTE.Application.Interfaces;

/// <summary>Hash de contraseñas con BCrypt (costo adaptativo).</summary>
public interface IHashPassword
{
    string Hash(string password);
    bool Verificar(string password, string hash);
}
