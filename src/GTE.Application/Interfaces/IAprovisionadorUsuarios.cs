namespace GTE.Application.Interfaces;

/// <summary>Datos de identidad que llegan en el token.</summary>
public record IdentidadToken(string Dominio, string? Nombre, string? Correo);

/// <summary>
/// Aprovisionamiento JIT (RN-ADM-01): al primer inicio de sesion de una identidad
/// valida de Entra ID se crea su usuario en GTE. Nace SIN roles, asi que no puede
/// hacer nada hasta que un administrador se los asigne.
/// </summary>
public interface IAprovisionadorUsuarios
{
    Task<int> ObtenerOCrearAsync(IdentidadToken identidad, CancellationToken cancellationToken = default);
}
