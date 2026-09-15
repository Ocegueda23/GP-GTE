using GTE.Application.Common;
using GTE.Domain.Interfaces;
using GTE.Domain.NotasVersion;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

public class NotaVersionRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), INotaVersionRepository
{
    public async Task<(int IdNotaVersion, IReadOnlyList<RenglonPersistido> Renglones)> CrearAsync(
        NotaVersionNueva datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblNotaVersion
        {
            Version = datos.Version,
            FechaLiberacion = datos.FechaLiberacion,
            Resumen = datos.Resumen,
            Publicada = datos.Publicada,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true   // TRAMPA EF: el DEFAULT 1 de la BD no aplica en INSERT de EF
        };
        contexto.TblNotaVersion.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        var nuevos = datos.Renglones
            .Select(r => (r.UiId, Entidad: NuevoRenglon(entidad.IdNotaVersion, r)))
            .ToList();

        contexto.TblNotaVersionDetalle.AddRange(nuevos.Select(n => n.Entidad));
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(
            "NotaVersion", entidad.IdNotaVersion, "CREAR",
            $"{datos.Version} ({datos.Renglones.Count} renglones)", cancellationToken);

        var persistidos = nuevos
            .Select(n => new RenglonPersistido(n.UiId, n.Entidad.IdNotaVersionDetalle))
            .ToList();

        return (entidad.IdNotaVersion, persistidos);
    }

    public async Task<IReadOnlyList<RenglonPersistido>> ActualizarAsync(
        NotaVersionEdicion datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = await contexto.TblNotaVersion
            .FirstOrDefaultAsync(n => n.IdNotaVersion == datos.IdNotaVersion, cancellationToken)
            ?? throw new InvalidOperationException($"Nota de version {datos.IdNotaVersion} no existe.");

        entidad.Version = datos.Version;
        entidad.FechaLiberacion = datos.FechaLiberacion;
        entidad.Resumen = datos.Resumen;
        entidad.Publicada = datos.Publicada;
        MarcarMovimiento(entidad);

        var existentes = await contexto.TblNotaVersionDetalle
            .Where(d => d.IdNotaVersion == datos.IdNotaVersion)
            .ToListAsync(cancellationToken);

        // Sync pattern: con Id = UPDATE, sin Id = INSERT, ausente = se borra.
        var idsQueSiguen = datos.Renglones
            .Where(r => r.IdNotaVersionDetalle.HasValue)
            .Select(r => r.IdNotaVersionDetalle!.Value)
            .ToHashSet();

        contexto.TblNotaVersionDetalle.RemoveRange(
            existentes.Where(e => !idsQueSiguen.Contains(e.IdNotaVersionDetalle)));

        var persistidos = new List<RenglonPersistido>();
        var nuevos = new List<(string? UiId, TblNotaVersionDetalle Entidad)>();

        foreach (var renglon in datos.Renglones)
        {
            if (renglon.IdNotaVersionDetalle is { } id)
            {
                var existente = existentes.FirstOrDefault(e => e.IdNotaVersionDetalle == id)
                    ?? throw new InvalidOperationException(
                        $"El renglon {id} no pertenece a la nota {datos.IdNotaVersion}.");

                existente.IdTipoCambioVersion = renglon.IdTipoCambioVersion;
                existente.Modulo = renglon.Modulo;
                existente.Descripcion = renglon.Descripcion;
                existente.Orden = renglon.Orden;
                MarcarMovimiento(existente);

                persistidos.Add(new RenglonPersistido(renglon.UiId, id));
            }
            else
            {
                var nueva = NuevoRenglon(datos.IdNotaVersion, renglon);
                nuevos.Add((renglon.UiId, nueva));
                contexto.TblNotaVersionDetalle.Add(nueva);
            }
        }

        await contexto.SaveChangesAsync(cancellationToken);
        persistidos.AddRange(nuevos.Select(n => new RenglonPersistido(n.UiId, n.Entidad.IdNotaVersionDetalle)));

        await RegistrarBitacoraAsync(
            "NotaVersion", datos.IdNotaVersion, "EDITAR",
            $"{datos.Version} ({datos.Renglones.Count} renglones)", cancellationToken);

        return persistidos;
    }

    public async Task<EstadoNotaVersion?> ObtenerEstadoAsync(
        int idNotaVersion, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblNotaVersion.AsNoTracking()
            .Where(n => n.IdNotaVersion == idNotaVersion && n.Activo)
            .Select(n => new EstadoNotaVersion(n.IdNotaVersion, n.Version, n.Publicada))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task EliminarAsync(int idNotaVersion, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblNotaVersion
            .FirstOrDefaultAsync(n => n.IdNotaVersion == idNotaVersion, cancellationToken)
            ?? throw new InvalidOperationException($"Nota de version {idNotaVersion} no existe.");

        entidad.Activo = false;
        MarcarMovimiento(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(
            "NotaVersion", idNotaVersion, "ELIMINAR", entidad.Version, cancellationToken);
    }

    public async Task<bool> ExisteVersionAsync(
        string version, int? idNotaVersionExcluir = null, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        // Sin filtro de Activo a proposito: el UNIQUE de la BD tampoco distingue las bajas logicas.
        return await contexto.TblNotaVersion.AsNoTracking()
            .AnyAsync(n => n.Version == version
                && (idNotaVersionExcluir == null || n.IdNotaVersion != idNotaVersionExcluir),
                cancellationToken);
    }

    private TblNotaVersionDetalle NuevoRenglon(int idNotaVersion, RenglonNotaVersion datos) => new()
    {
        IdNotaVersion = idNotaVersion,
        IdTipoCambioVersion = datos.IdTipoCambioVersion,
        Modulo = datos.Modulo,
        Descripcion = datos.Descripcion,
        Orden = datos.Orden,
        UsuarioRegistro = Auditoria.Usuario,
        Activo = true
    };

    private void MarcarMovimiento(TblNotaVersion entidad)
    {
        entidad.UsuarioMovto = RecortarUsuario();
        entidad.FechaMovto = DateTime.Now;
    }

    private void MarcarMovimiento(TblNotaVersionDetalle entidad)
    {
        entidad.UsuarioMovto = RecortarUsuario();
        entidad.FechaMovto = DateTime.Now;
    }

    private string RecortarUsuario()
        => Auditoria.Usuario.Length > 50 ? Auditoria.Usuario[..50] : Auditoria.Usuario;
}
