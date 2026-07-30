namespace GTE.Application.Interfaces;

/// <summary>
/// Evaluacion RBAC del usuario actual (del token): permisos por clave con alcance
/// global o por proyecto. El rol Administrador recibe todos los permisos por seed,
/// sin cortocircuitos en codigo (RN-ADM-02).
/// </summary>
public interface IVerificadorPermisos
{
    Task<bool> TienePermisoAsync(string clave, int? idProyecto = null, CancellationToken cancellationToken = default);

    /// <summary>Lanza ForbiddenException si el usuario actual no tiene el permiso.</summary>
    Task ExigirPermisoAsync(string clave, int? idProyecto = null, CancellationToken cancellationToken = default);
}
