using GTE.Application.Interfaces;

namespace GTE.Infrastructure.Services;

public class HashPasswordBCrypt : IHashPassword
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verificar(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
