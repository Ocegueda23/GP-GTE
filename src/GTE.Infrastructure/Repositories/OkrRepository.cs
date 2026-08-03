using GTE.Application.Common;
using GTE.Domain.Interfaces;
using GTE.Domain.Okr;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

public class OkrRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), IOkrRepository
{
    public async Task<int> CrearObjetivoAsync(ObjetivoOkrNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = new TblObjetivoOkr
        {
            IdProyecto = datos.IdProyecto,
            IdEquipo = datos.IdEquipo,
            Nombre = datos.Nombre,
            Descripcion = datos.Descripcion,
            Anio = datos.Anio,
            Trimestre = datos.Trimestre,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblObjetivoOkr.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("ObjetivoOkr", entidad.IdObjetivoOkr, "CREAR", datos.Nombre, cancellationToken);
        return entidad.IdObjetivoOkr;
    }

    public async Task ActualizarObjetivoAsync(ObjetivoOkrEdicion datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblObjetivoOkr
            .FirstOrDefaultAsync(o => o.IdObjetivoOkr == datos.IdObjetivoOkr, cancellationToken)
            ?? throw new InvalidOperationException($"ObjetivoOkr {datos.IdObjetivoOkr} no existe.");

        entidad.Nombre = datos.Nombre;
        entidad.Descripcion = datos.Descripcion;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("ObjetivoOkr", datos.IdObjetivoOkr, "EDITAR", datos.Nombre, cancellationToken);
    }

    public async Task RetirarObjetivoAsync(int idObjetivoOkr, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblObjetivoOkr
            .FirstOrDefaultAsync(o => o.IdObjetivoOkr == idObjetivoOkr, cancellationToken)
            ?? throw new InvalidOperationException($"ObjetivoOkr {idObjetivoOkr} no existe.");

        entidad.Activo = false;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("ObjetivoOkr", idObjetivoOkr, "RETIRAR", null, cancellationToken);
    }

    public async Task<int> CrearResultadoClaveAsync(ResultadoClaveNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = new TblResultadoClave
        {
            IdObjetivoOkr = datos.IdObjetivoOkr,
            Nombre = datos.Nombre,
            ValorMeta = datos.ValorMeta,
            ValorActual = 0,
            ClaveKpi = datos.ClaveKpi,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblResultadoClave.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("ResultadoClave", entidad.IdResultadoClave, "CREAR", datos.Nombre, cancellationToken);
        return entidad.IdResultadoClave;
    }

    public async Task ActualizarResultadoClaveAsync(ResultadoClaveEdicion datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblResultadoClave
            .FirstOrDefaultAsync(r => r.IdResultadoClave == datos.IdResultadoClave, cancellationToken)
            ?? throw new InvalidOperationException($"ResultadoClave {datos.IdResultadoClave} no existe.");

        entidad.Nombre = datos.Nombre;
        entidad.ValorMeta = datos.ValorMeta;
        entidad.ValorActual = datos.ValorActual;
        entidad.ClaveKpi = datos.ClaveKpi;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("ResultadoClave", datos.IdResultadoClave, "EDITAR", datos.Nombre, cancellationToken);
    }

    public async Task RetirarResultadoClaveAsync(int idResultadoClave, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblResultadoClave
            .FirstOrDefaultAsync(r => r.IdResultadoClave == idResultadoClave, cancellationToken)
            ?? throw new InvalidOperationException($"ResultadoClave {idResultadoClave} no existe.");

        entidad.Activo = false;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("ResultadoClave", idResultadoClave, "RETIRAR", null, cancellationToken);
    }

    private static string Recortar(string usuario) => usuario.Length > 50 ? usuario[..50] : usuario;
}
