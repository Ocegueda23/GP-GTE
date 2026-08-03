using GTE.Application.Common;
using GTE.Domain.Interfaces;
using GTE.Domain.Soporte;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

public class TicketRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), ITicketRepository
{
    public async Task<int> CrearAsync(TicketNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblTicket
        {
            Folio = datos.Folio,
            IdSolicitante = datos.IdSolicitante,
            Titulo = datos.Titulo,
            Descripcion = datos.Descripcion,
            IdCategoriaTicket = datos.IdCategoriaTicket,
            IdPrioridad = datos.IdPrioridad,
            IdEstatusTicket = EstatusTicket.Nuevo,   // el estatus inicial lo fija el backend
            IdSla = datos.IdSla,
            FechaLimiteRespuesta = datos.FechaLimiteRespuesta,
            FechaLimiteResolucion = datos.FechaLimiteResolucion,
            IdUsuarioSolicitante = datos.IdUsuarioSolicitante,
            IdLocacion = datos.IdLocacion,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblTicket.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        contexto.TblHistorialEstatus.Add(new TblHistorialEstatus
        {
            Proceso = "Ticket",
            IdRegistro = entidad.IdTicket,
            IdEstatus = EstatusTicket.Nuevo,
            Accion = "ALTA",
            Usuario = Auditoria.Usuario
        });
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Ticket", entidad.IdTicket, "CREAR", datos.Folio, cancellationToken);
        return entidad.IdTicket;
    }

    public async Task<EstadoTicket?> ObtenerEstadoAsync(int idTicket, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblTicket.AsNoTracking()
            .Where(t => t.IdTicket == idTicket)
            .Select(t => new EstadoTicket(
                t.IdTicket, t.Folio, t.IdEstatusTicket, t.IdSolicitante, t.IdAsignado,
                t.IdWorkItemDerivado, t.Titulo, t.Descripcion, t.IdPrioridad, t.Activo))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SlaVigente?> ObtenerSlaVigenteAsync(int idPrioridad, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblSla.AsNoTracking()
            .Where(s => s.IdPrioridad == idPrioridad && s.Activo)
            .Select(s => new SlaVigente(s.IdSla, s.MinutosRespuesta, s.MinutosResolucion, s.IdHorario))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AsignarAsync(int idTicket, int idAsignado, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblTicket
            .FirstOrDefaultAsync(t => t.IdTicket == idTicket, cancellationToken)
            ?? throw new InvalidOperationException($"Ticket {idTicket} no existe.");
        entidad.IdAsignado = idAsignado;
        MarcarMovimiento(entidad);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task AplicarEfectosTransicionAsync(
        int idTicket, string accion, string? solucion = null, int? minutosSolucion = null,
        CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblTicket
            .FirstOrDefaultAsync(t => t.IdTicket == idTicket, cancellationToken)
            ?? throw new InvalidOperationException($"Ticket {idTicket} no existe.");

        if (accion == AccionesTicket.IniciarAtencion && entidad.FechaPrimeraRespuesta is null)
        {
            entidad.FechaPrimeraRespuesta = DateTime.Now;
        }
        else if (accion == AccionesTicket.Resolver)
        {
            entidad.FechaResolucion = DateTime.Now;
            entidad.Solucion = solucion;
            entidad.MinutosSolucion = minutosSolucion;
        }
        else if (accion == AccionesTicket.Reabrir)
        {
            entidad.FechaResolucion = null;
        }

        MarcarMovimiento(entidad);
        await contexto.SaveChangesAsync(cancellationToken);
        await RegistrarBitacoraAsync("Ticket", idTicket, accion, null, cancellationToken);
    }

    public async Task EscalarAsync(int idTicket, int idWorkItem, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblTicket
            .FirstOrDefaultAsync(t => t.IdTicket == idTicket, cancellationToken)
            ?? throw new InvalidOperationException($"Ticket {idTicket} no existe.");
        entidad.IdWorkItemDerivado = idWorkItem;
        MarcarMovimiento(entidad);
        await contexto.SaveChangesAsync(cancellationToken);
        await RegistrarBitacoraAsync("Ticket", idTicket, "ESCALAR", null, cancellationToken);
    }

    public async Task RegistrarEncuestaAsync(int idTicket, int calificacion, string? comentario, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        contexto.TblEncuestaSatisfaccion.Add(new TblEncuestaSatisfaccion
        {
            IdTicket = idTicket,
            Calificacion = (byte)calificacion,
            Comentario = comentario,
            UsuarioRegistro = Auditoria.Usuario
        });
        await contexto.SaveChangesAsync(cancellationToken);
    }

    private void MarcarMovimiento(TblTicket entidad)
    {
        entidad.UsuarioMovto = Auditoria.Usuario.Length > 50 ? Auditoria.Usuario[..50] : Auditoria.Usuario;
        entidad.FechaMovto = DateTime.Now;
    }
}
