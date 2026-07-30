namespace GTE.Application.Interfaces;

/// <summary>Usuario de GTE que corresponde a la identidad del token.</summary>
public record UsuarioActual(int IdUsuario, string Dominio, string Nombre, int? IdNivel, int? IdHorario);

/// <summary>
/// Resuelve el usuario actual (AuditContext.Usuario -> tblUsuario.Dominio).
/// Devuelve null si la identidad no esta registrada en GTE.
/// </summary>
public interface IProveedorUsuarioActual
{
    Task<UsuarioActual?> ObtenerAsync(CancellationToken cancellationToken = default);
}
