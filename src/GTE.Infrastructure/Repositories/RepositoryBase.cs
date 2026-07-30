using GTE.Application.Common;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;

namespace GTE.Infrastructure.Repositories;

/// <summary>
/// Base de todo repositorio de escritura: expone la fabrica de contextos,
/// el AuditContext (como propiedad, nunca campo sin asignar) y el registro de bitacora.
/// </summary>
public abstract class RepositoryBase(FabricaContexto fabrica, AuditContext auditoria)
{
    protected FabricaContexto Fabrica { get; } = fabrica;
    protected AuditContext Auditoria { get; } = auditoria;

    /// <summary>
    /// Escribe bitacora con contexto propio de vida corta: el registro persiste
    /// aunque la transaccion de negocio haga rollback.
    /// </summary>
    protected async Task RegistrarBitacoraAsync(
        string entidad,
        int? idEntidad,
        string accion,
        string? detalle = null,
        CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        contexto.TblBitacora.Add(new TblBitacora
        {
            Usuario = Auditoria.Usuario,
            Ip = Auditoria.Ip,
            Endpoint = Auditoria.Endpoint,
            Entidad = entidad,
            IdEntidad = idEntidad,
            Accion = accion,
            Detalle = detalle,
            Fecha = DateTime.Now
        });
        await contexto.SaveChangesAsync(cancellationToken);
    }
}
