using System.Security.Cryptography;
using System.Text;

namespace GTE.Domain.Entregas;

/// <summary>
/// Firma electronica de aprobaciones: hash SHA-256 de usuario, fecha UTC, entidad,
/// rol y decision. Es verificable y no repudiable dentro del alcance interno, y
/// queda respaldada por la bitacora.
/// </summary>
public static class FirmaElectronica
{
    public static string Calcular(string usuario, string folio, string rol, bool aprobada)
    {
        return Calcular(usuario, folio, rol, aprobada, DateTime.UtcNow);
    }

    /// <summary>Sobrecarga con fecha explicita: permite verificar una firma existente.</summary>
    public static string Calcular(string usuario, string folio, string rol, bool aprobada, DateTime fechaUtc)
    {
        var contenido = $"{usuario}|{fechaUtc:O}|{folio}|{rol}|{(aprobada ? "APROBADA" : "RECHAZADA")}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contenido)));
    }
}
