using GTE.Application.Common;
using GTE.Application.Interfaces;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Resuelve el usuario GTE de la identidad del token (una vez por request; scoped).
/// </summary>
public class ProveedorUsuarioActual(FabricaContexto fabrica, AuditContext auditoria) : IProveedorUsuarioActual
{
    private bool _resuelto;
    private UsuarioActual? _usuario;

    public async Task<UsuarioActual?> ObtenerAsync(CancellationToken cancellationToken = default)
    {
        if (_resuelto)
        {
            return _usuario;
        }

        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        _usuario = await contexto.TblUsuario.AsNoTracking()
            .Where(u => u.Dominio == auditoria.Usuario && u.Activo)
            .Select(u => new UsuarioActual(u.IdUsuario, u.Dominio, u.Nombre, u.IdNivel, u.IdHorario))
            .FirstOrDefaultAsync(cancellationToken);
        _resuelto = true;
        return _usuario;
    }
}
