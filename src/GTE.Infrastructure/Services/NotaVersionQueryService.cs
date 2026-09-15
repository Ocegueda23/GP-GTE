using GTE.Application.DTOs.Responses.NotasVersion;
using GTE.Application.Interfaces;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class NotaVersionQueryService(FabricaContexto fabrica) : INotaVersionQueryService
{
    public async Task<IReadOnlyList<NotaVersionResponse>> ObtenerPublicadasAsync(
        CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        return await contexto.TblNotaVersion.AsNoTracking()
            .Where(n => n.Activo && n.Publicada)
            .OrderByDescending(n => n.FechaLiberacion)
            .ThenByDescending(n => n.IdNotaVersion)
            .Select(ProyeccionNota)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotaVersionListaResponse>> ObtenerTodasAsync(
        CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        return await contexto.TblNotaVersion.AsNoTracking()
            .Where(n => n.Activo)
            .OrderByDescending(n => n.FechaLiberacion)
            .ThenByDescending(n => n.IdNotaVersion)
            .Select(n => new NotaVersionListaResponse
            {
                IdNotaVersion = n.IdNotaVersion,
                Version = n.Version,
                FechaLiberacion = n.FechaLiberacion,
                Resumen = n.Resumen,
                Publicada = n.Publicada,
                TotalRenglones = n.TblNotaVersionDetalle.Count(d => d.Activo),
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<NotaVersionResponse?> ObtenerPorIdAsync(
        int idNotaVersion, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        return await contexto.TblNotaVersion.AsNoTracking()
            .Where(n => n.Activo && n.IdNotaVersion == idNotaVersion)
            .Select(ProyeccionNota)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TipoCambioVersionResponse>> ObtenerTiposCambioAsync(
        CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        return await contexto.TblTipoCambioVersion.AsNoTracking()
            .Where(t => t.Activo)
            .OrderBy(t => t.Orden)
            .Select(t => new TipoCambioVersionResponse
            {
                IdTipoCambioVersion = t.IdTipoCambioVersion,
                Nombre = t.Nombre,
                Orden = t.Orden,
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// TRAMPA EF (§7.8): .Select() no traduce metodos estaticos, por eso la proyeccion del arbol
    /// va como Expression y se reutiliza en vez de llamar a un helper desde el Select.
    /// </summary>
    private static readonly System.Linq.Expressions.Expression<
        Func<Modelos.bdsGTE.TblNotaVersion, NotaVersionResponse>> ProyeccionNota =
        n => new NotaVersionResponse
        {
            IdNotaVersion = n.IdNotaVersion,
            Version = n.Version,
            FechaLiberacion = n.FechaLiberacion,
            Resumen = n.Resumen,
            Publicada = n.Publicada,
            Renglones = n.TblNotaVersionDetalle
                .Where(d => d.Activo)
                .OrderBy(d => d.IdTipoCambioVersionNavigation.Orden)
                .ThenBy(d => d.Orden)
                .ThenBy(d => d.IdNotaVersionDetalle)
                .Select(d => new RenglonNotaVersionResponse
                {
                    IdNotaVersionDetalle = d.IdNotaVersionDetalle,
                    IdTipoCambioVersion = d.IdTipoCambioVersion,
                    TipoCambio = d.IdTipoCambioVersionNavigation.Nombre,
                    Modulo = d.Modulo,
                    Descripcion = d.Descripcion,
                    Orden = d.Orden,
                })
                .ToList(),
        };
}
