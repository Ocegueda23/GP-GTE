using GTE.Application.Common;
using GTE.Domain.Interfaces;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

public class IndicadoresEjecutivosRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), IIndicadoresEjecutivosRepository
{
    public async Task GuardarLayoutAsync(int idUsuario, string layoutJson, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var existente = await contexto.TblDashboardLayoutUsuario
            .FirstOrDefaultAsync(l => l.IdUsuario == idUsuario, cancellationToken);

        if (existente is null)
        {
            contexto.TblDashboardLayoutUsuario.Add(new TblDashboardLayoutUsuario
            {
                IdUsuario = idUsuario,
                LayoutJson = layoutJson,
            });
        }
        else
        {
            existente.LayoutJson = layoutJson;
            existente.UsuarioMovto = Auditoria.Usuario;
            existente.FechaMovto = DateTime.Now;
        }

        await contexto.SaveChangesAsync(cancellationToken);
    }
}
