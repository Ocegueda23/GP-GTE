using GTE.Application.Common;
using GTE.Domain.Costeo;
using GTE.Domain.Interfaces;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

public class CosteoRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), ICosteoRepository
{
    public async Task<int> CrearTarifaNivelAsync(TarifaNivelNueva datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = new TblTarifaNivel
        {
            IdNivel = datos.IdNivel,
            CostoHora = datos.CostoHora,
            VigenciaDesde = datos.VigenciaDesde,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblTarifaNivel.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("TarifaNivel", entidad.IdTarifaNivel, "CREAR", null, cancellationToken);
        return entidad.IdTarifaNivel;
    }

    public async Task ActualizarTarifaNivelAsync(TarifaNivelEdicion datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblTarifaNivel
            .FirstOrDefaultAsync(t => t.IdTarifaNivel == datos.IdTarifaNivel, cancellationToken)
            ?? throw new InvalidOperationException($"TarifaNivel {datos.IdTarifaNivel} no existe.");

        entidad.CostoHora = datos.CostoHora;
        entidad.VigenciaDesde = datos.VigenciaDesde;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("TarifaNivel", datos.IdTarifaNivel, "EDITAR", null, cancellationToken);
    }

    public async Task RetirarTarifaNivelAsync(int idTarifaNivel, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblTarifaNivel
            .FirstOrDefaultAsync(t => t.IdTarifaNivel == idTarifaNivel, cancellationToken)
            ?? throw new InvalidOperationException($"TarifaNivel {idTarifaNivel} no existe.");

        entidad.Activo = false;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("TarifaNivel", idTarifaNivel, "RETIRAR", null, cancellationToken);
    }

    public async Task<int> CrearPresupuestoAsync(PresupuestoProyectoNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = new TblPresupuestoProyecto
        {
            IdProyecto = datos.IdProyecto,
            Anio = datos.Anio,
            MontoAutorizado = datos.MontoAutorizado,
            HorasAutorizadas = datos.HorasAutorizadas,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblPresupuestoProyecto.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("PresupuestoProyecto", entidad.IdPresupuestoProyecto, "CREAR", null, cancellationToken);
        return entidad.IdPresupuestoProyecto;
    }

    public async Task ActualizarPresupuestoAsync(PresupuestoProyectoEdicion datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblPresupuestoProyecto
            .FirstOrDefaultAsync(p => p.IdPresupuestoProyecto == datos.IdPresupuestoProyecto, cancellationToken)
            ?? throw new InvalidOperationException($"PresupuestoProyecto {datos.IdPresupuestoProyecto} no existe.");

        entidad.MontoAutorizado = datos.MontoAutorizado;
        entidad.HorasAutorizadas = datos.HorasAutorizadas;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("PresupuestoProyecto", datos.IdPresupuestoProyecto, "EDITAR", null, cancellationToken);
    }

    public async Task RetirarPresupuestoAsync(int idPresupuestoProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblPresupuestoProyecto
            .FirstOrDefaultAsync(p => p.IdPresupuestoProyecto == idPresupuestoProyecto, cancellationToken)
            ?? throw new InvalidOperationException($"PresupuestoProyecto {idPresupuestoProyecto} no existe.");

        entidad.Activo = false;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("PresupuestoProyecto", idPresupuestoProyecto, "RETIRAR", null, cancellationToken);
    }

    private static string Recortar(string usuario) => usuario.Length > 50 ? usuario[..50] : usuario;
}
