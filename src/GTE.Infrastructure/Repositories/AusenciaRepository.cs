using GTE.Application.Common;
using GTE.Domain.Ausencias;
using GTE.Domain.Interfaces;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

public class AusenciaRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), IAusenciaRepository
{
    public async Task<int> CrearAsync(AusenciaNueva datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblAusencia
        {
            IdUsuario = datos.IdUsuario,
            IdTipoAusencia = datos.IdTipoAusencia,
            IdEstatusAusencia = EstatusAusencia.Solicitada,   // el estatus inicial lo fija el backend
            FechaInicio = datos.FechaInicio,
            FechaFin = datos.FechaFin,
            Motivo = datos.Motivo,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblAusencia.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        contexto.TblHistorialEstatus.Add(new TblHistorialEstatus
        {
            Proceso = "Ausencia",
            IdRegistro = entidad.IdAusencia,
            IdEstatus = EstatusAusencia.Solicitada,
            Accion = "ALTA",
            Usuario = Auditoria.Usuario
        });
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(
            "Ausencia", entidad.IdAusencia, "CREAR",
            $"{datos.FechaInicio:yyyy-MM-dd} a {datos.FechaFin:yyyy-MM-dd}", cancellationToken);
        return entidad.IdAusencia;
    }

    public async Task ActualizarAsync(AusenciaEdicion datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblAusencia
            .FirstOrDefaultAsync(a => a.IdAusencia == datos.IdAusencia, cancellationToken)
            ?? throw new InvalidOperationException($"Ausencia {datos.IdAusencia} no existe.");

        entidad.IdTipoAusencia = datos.IdTipoAusencia;
        entidad.FechaInicio = datos.FechaInicio;
        entidad.FechaFin = datos.FechaFin;
        entidad.Motivo = datos.Motivo;
        MarcarMovimiento(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(
            "Ausencia", datos.IdAusencia, "EDITAR",
            $"{datos.FechaInicio:yyyy-MM-dd} a {datos.FechaFin:yyyy-MM-dd}", cancellationToken);
    }

    public async Task<EstadoAusencia?> ObtenerEstadoAsync(
        int idAusencia, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblAusencia.AsNoTracking()
            .Where(a => a.IdAusencia == idAusencia)
            .Select(a => new EstadoAusencia(
                a.IdAusencia, a.IdUsuario, a.IdEstatusAusencia, a.FechaInicio, a.FechaFin, a.Activo))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TraslapeAusencia>> ObtenerTraslapesAsync(
        int idUsuario,
        DateOnly fechaInicio,
        DateOnly fechaFin,
        int? idAusenciaExcluir = null,
        CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var vigentes = EstatusAusencia.Vigentes.ToArray();

        // Dos periodos se cruzan si cada uno empieza antes de que el otro termine.
        var consulta = contexto.TblAusencia.AsNoTracking()
            .Where(a => a.Activo
                && a.IdUsuario == idUsuario
                && vigentes.Contains(a.IdEstatusAusencia)
                && a.FechaInicio <= fechaFin
                && a.FechaFin >= fechaInicio);

        if (idAusenciaExcluir.HasValue)
        {
            consulta = consulta.Where(a => a.IdAusencia != idAusenciaExcluir.Value);
        }

        return await consulta
            .OrderBy(a => a.FechaInicio)
            .Select(a => new TraslapeAusencia(
                a.IdAusencia,
                a.IdTipoAusenciaNavigation.Nombre,
                a.IdEstatusAusenciaNavigation.Descripcion,
                a.FechaInicio,
                a.FechaFin))
            .ToListAsync(cancellationToken);
    }

    public async Task AplicarEfectosTransicionAsync(
        int idAusencia, string accion, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblAusencia
            .FirstOrDefaultAsync(a => a.IdAusencia == idAusencia, cancellationToken)
            ?? throw new InvalidOperationException($"Ausencia {idAusencia} no existe.");
        MarcarMovimiento(entidad);
        await contexto.SaveChangesAsync(cancellationToken);
        await RegistrarBitacoraAsync("Ausencia", idAusencia, accion, null, cancellationToken);
    }

    public async Task<IReadOnlyList<int>> ObtenerAprobadoresAsync(CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        return await (
            from ur in contexto.TblUsuarioRol.AsNoTracking()
            join rp in contexto.TblRolPermiso.AsNoTracking() on ur.IdRol equals rp.IdRol
            join p in contexto.TblPermiso.AsNoTracking() on rp.IdPermiso equals p.IdPermiso
            join u in contexto.TblUsuario.AsNoTracking() on ur.IdUsuario equals u.IdUsuario
            where ur.Activo && u.Activo && p.Clave == PermisosAusencia.Gestionar
            select ur.IdUsuario)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private void MarcarMovimiento(TblAusencia entidad)
    {
        entidad.UsuarioMovto = Auditoria.Usuario.Length > 50 ? Auditoria.Usuario[..50] : Auditoria.Usuario;
        entidad.FechaMovto = DateTime.Now;
    }
}
