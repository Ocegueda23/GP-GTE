using System.Security.Cryptography;

namespace GTE.Domain.Autenticacion;

/// <summary>
/// Password temporal legible para alta/reset de usuario (alfabeto sin caracteres
/// ambiguos: sin 0/O, 1/l/I). Usado tanto por el alta de usuario como por el reset
/// de administrador para no duplicar la logica.
/// </summary>
public static class GeneradorPasswordTemporal
{
    private const string Alfabeto = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";

    public static string Generar(int longitud = 10)
    {
        return string.Create(longitud, Alfabeto, (destino, alfabeto) =>
        {
            for (var i = 0; i < destino.Length; i++)
            {
                destino[i] = alfabeto[RandomNumberGenerator.GetInt32(alfabeto.Length)];
            }
        });
    }
}
