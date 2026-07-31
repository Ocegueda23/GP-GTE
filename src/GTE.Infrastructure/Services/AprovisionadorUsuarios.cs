using GTE.Application.Common;
using GTE.Application.Interfaces;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class AprovisionadorUsuarios(FabricaContexto fabrica, AuditContext auditoria) : IAprovisionadorUsuarios
{
    public async Task<int> ObtenerOCrearAsync(
        IdentidadToken identidad, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var existente = await contexto.TblUsuario
            .FirstOrDefaultAsync(u => u.Dominio == identidad.Dominio, cancellationToken);

        if (existente is not null)
        {
            // Un usuario dado de baja no se reactiva solo: eso es decision de administracion
            if (!existente.Activo)
            {
                throw new Domain.Exceptions.ForbiddenException(
                    "Tu usuario esta dado de baja en GTE. Solicita su reactivacion a administracion.");
            }

            // Se refresca el nombre y el correo que trae el proveedor de identidad
            var cambio = false;
            if (!string.IsNullOrWhiteSpace(identidad.Nombre) && existente.Nombre != identidad.Nombre)
            {
                existente.Nombre = identidad.Nombre;
                cambio = true;
            }
            if (!string.IsNullOrWhiteSpace(identidad.Correo) && existente.Correo != identidad.Correo)
            {
                existente.Correo = identidad.Correo;
                cambio = true;
            }
            if (cambio)
            {
                existente.UsuarioMovto = "aprovisionamiento";
                existente.FechaMovto = DateTime.Now;
                await contexto.SaveChangesAsync(cancellationToken);
            }

            return existente.IdUsuario;
        }

        var nuevo = new TblUsuario
        {
            Dominio = identidad.Dominio,
            Nombre = string.IsNullOrWhiteSpace(identidad.Nombre) ? identidad.Dominio : identidad.Nombre,
            Correo = identidad.Correo,
            FechaAlta = DateTime.Now,
            UsuarioRegistro = "aprovisionamiento-jit",
            Activo = true
        };
        contexto.TblUsuario.Add(nuevo);
        await contexto.SaveChangesAsync(cancellationToken);

        // El alta automatica queda en bitacora: nace sin roles, no puede operar todavia.
        // Durante el inicio de sesion aun no hay identidad en el contexto, asi que el
        // responsable del alta es el propio proceso de autenticacion.
        contexto.TblBitacora.Add(new TblBitacora
        {
            Usuario = auditoria.TieneIdentidad ? auditoria.Usuario : "sistema-autenticacion",
            Ip = auditoria.Ip,
            Endpoint = auditoria.Endpoint,
            Entidad = "Usuario",
            IdEntidad = nuevo.IdUsuario,
            Accion = "ALTA_JIT",
            Detalle = $"Primer inicio de sesion de {identidad.Dominio}; sin roles asignados",
            Fecha = DateTime.Now
        });
        await contexto.SaveChangesAsync(cancellationToken);

        return nuevo.IdUsuario;
    }
}
