using GTE.Application.DTOs.Responses.Seguridad;
using GTE.Application.Interfaces;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class SesionQueryService(FabricaContexto fabrica) : ISesionQueryService
{
    public async Task<SesionResponse?> ObtenerSesionAsync(
        int idUsuario, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var sesion = await (
            from u in contexto.TblUsuario.AsNoTracking()
            join p in contexto.TblPuesto.AsNoTracking() on u.IdPuesto equals p.IdPuesto into puestos
            from p in puestos.DefaultIfEmpty()
            join n in contexto.TblNivel.AsNoTracking() on u.IdNivel equals n.IdNivel into niveles
            from n in niveles.DefaultIfEmpty()
            where u.IdUsuario == idUsuario && u.Activo
            select new SesionResponse
            {
                IdUsuario = u.IdUsuario,
                Dominio = u.Dominio,
                Nombre = u.Nombre,
                Correo = u.Correo,
                Puesto = p != null ? p.Nombre : null,
                Nivel = n != null ? n.Nombre : null
            }).FirstOrDefaultAsync(cancellationToken);

        if (sesion is null)
        {
            return null;
        }

        sesion.Roles = await (
            from ur in contexto.TblUsuarioRol.AsNoTracking()
            join r in contexto.TblRol.AsNoTracking() on ur.IdRol equals r.IdRol
            where ur.IdUsuario == idUsuario && ur.Activo && r.Activo
            select r.Nombre
            ).Distinct().ToListAsync(cancellationToken);

        sesion.Permisos = await (
            from ur in contexto.TblUsuarioRol.AsNoTracking()
            join rp in contexto.TblRolPermiso.AsNoTracking() on ur.IdRol equals rp.IdRol
            join pe in contexto.TblPermiso.AsNoTracking() on rp.IdPermiso equals pe.IdPermiso
            where ur.IdUsuario == idUsuario && ur.Activo && pe.Activo
            select pe.Clave
            ).Distinct().ToListAsync(cancellationToken);

        sesion.Equipos = await contexto.TblEquipoMiembro.AsNoTracking()
            .Where(em => em.IdUsuario == idUsuario && em.Activo)
            .Select(em => em.IdEquipo)
            .ToListAsync(cancellationToken);

        return sesion;
    }
}
