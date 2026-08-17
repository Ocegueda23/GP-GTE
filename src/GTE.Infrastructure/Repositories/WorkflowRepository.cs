using GTE.Application.Common;
using GTE.Domain.Interfaces;
using GTE.Domain.Workflow;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

public class WorkflowRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), IWorkflowRepository
{
    public async Task GuardarTransicionesConfigAsync(
        string proceso, IReadOnlyList<TransicionConfigEdicion> transiciones, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var existentes = await contexto.TblTransicionConfig
            .Where(c => c.Proceso == proceso)
            .ToListAsync(cancellationToken);
        var existentesPorClave = existentes.ToDictionary(c => (c.IdEstatusOrigen, c.Accion));

        foreach (var t in transiciones)
        {
            if (existentesPorClave.TryGetValue((t.IdEstatusOrigen, t.Accion), out var entidad))
            {
                entidad.EtiquetaBoton = t.EtiquetaBoton;
                entidad.RequierePermiso = t.RequierePermiso;
                entidad.RequiereMotivo = t.RequiereMotivo;
                entidad.EsAccionPrincipal = t.EsAccionPrincipal;
                entidad.Orden = t.Orden;
                entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
                entidad.FechaMovto = DateTime.Now;
            }
            else
            {
                contexto.TblTransicionConfig.Add(new TblTransicionConfig
                {
                    Proceso = proceso,
                    IdEstatusOrigen = t.IdEstatusOrigen,
                    Accion = t.Accion,
                    EtiquetaBoton = t.EtiquetaBoton,
                    RequierePermiso = t.RequierePermiso,
                    RequiereMotivo = t.RequiereMotivo,
                    EsAccionPrincipal = t.EsAccionPrincipal,
                    Orden = t.Orden,
                    UsuarioRegistro = Auditoria.Usuario,
                    Activo = true
                });
            }
        }

        await contexto.SaveChangesAsync(cancellationToken);
        await RegistrarBitacoraAsync("Workflow", null, "GUARDAR_CONFIG", proceso, cancellationToken);
    }

    private static string Recortar(string usuario) => usuario.Length > 50 ? usuario[..50] : usuario;
}
