using GTE.Application.Common;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Evaluacion RBAC contra tblUsuarioRol/tblRolPermiso/tblPermiso. El alcance de la
/// asignacion aplica: rol global (IdProyecto null) o acotado al proyecto consultado.
/// </summary>
public class VerificadorPermisos(FabricaContexto fabrica, AuditContext auditoria) : IVerificadorPermisos
{
    public async Task<bool> TienePermisoAsync(
        string clave, int? idProyecto = null, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        return await (
            from u in contexto.TblUsuario.AsNoTracking()
            join ur in contexto.TblUsuarioRol.AsNoTracking() on u.IdUsuario equals ur.IdUsuario
            join r in contexto.TblRol.AsNoTracking() on ur.IdRol equals r.IdRol
            join rp in contexto.TblRolPermiso.AsNoTracking() on r.IdRol equals rp.IdRol
            join p in contexto.TblPermiso.AsNoTracking() on rp.IdPermiso equals p.IdPermiso
            where u.Dominio == auditoria.Usuario && u.Activo
                  && ur.Activo && r.Activo && p.Activo
                  && p.Clave == clave
                  && (ur.IdProyecto == null || ur.IdProyecto == idProyecto)
            select p.IdPermiso
            ).AnyAsync(cancellationToken);
    }

    public async Task ExigirPermisoAsync(
        string clave, int? idProyecto = null, CancellationToken cancellationToken = default)
    {
        if (!await TienePermisoAsync(clave, idProyecto, cancellationToken))
        {
            throw new ForbiddenException($"La operacion requiere el permiso {clave}.");
        }
    }
}
