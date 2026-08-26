using GTE.Application.Interfaces;
using GTE.Domain.CatalogoGenerico;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Lectura del motor de catalogos genericos: reconcilia el esquema real (IMotorMetadatosEsquema,
/// fuente de verdad de tipo/nulabilidad/PK/orden) con la configuracion de presentacion guardada
/// (tblCatalogoGenericoColumna, fuente de verdad de como se ve/edita).
/// </summary>
public class CatalogoGenericoQueryService(FabricaContexto fabrica, IMotorMetadatosEsquema motorMetadatos)
    : ICatalogoGenericoQueryService
{
    public async Task<IReadOnlyList<CatalogoResumen>> ObtenerCatalogosAsync(
        CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        return await contexto.TblCatalogoGenerico
            .AsNoTracking()
            .Where(c => c.Activo)
            .OrderBy(c => c.Titulo)
            .Select(c => new CatalogoResumen(c.IdCatalogo, c.Clave, c.NombreTabla, c.Titulo))
            .ToListAsync(cancellationToken);
    }

    public async Task<CatalogoResumen?> ObtenerPorClaveAsync(
        string clave, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        return await contexto.TblCatalogoGenerico
            .AsNoTracking()
            .Where(c => c.Clave == clave && c.Activo)
            .Select(c => new CatalogoResumen(c.IdCatalogo, c.Clave, c.NombreTabla, c.Titulo))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ConfiguracionCatalogo?> ObtenerConfigCatalogoAsync(
        string clave, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var catalogo = await contexto.TblCatalogoGenerico
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Clave == clave && c.Activo, cancellationToken);
        if (catalogo is null)
        {
            return null;
        }

        var columnasEsquema = await motorMetadatos.ObtenerColumnasAsync(catalogo.NombreTabla, cancellationToken);

        var configPorColumna = await contexto.TblCatalogoGenericoColumna
            .AsNoTracking()
            .Where(col => col.IdCatalogo == catalogo.IdCatalogo && col.Activo)
            .ToDictionaryAsync(col => col.NombreColumna, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var columnas = columnasEsquema
            .Select(esquema =>
            {
                configPorColumna.TryGetValue(esquema.NombreColumna, out var config);
                return new ColumnaReconciliada(
                    esquema.NombreColumna,
                    esquema.TipoSql,
                    esquema.EsNulable,
                    esquema.LongitudMaxima,
                    esquema.EsPk,
                    esquema.EsIdentity,
                    config?.DisplayName ?? esquema.NombreColumna,
                    config?.EsVisible ?? true,
                    config?.EsSoloLectura ?? false,
                    config?.EsRequerido ?? (!esquema.EsNulable && !esquema.EsIdentity && !esquema.EsPk),
                    config?.OrdinalPos ?? esquema.OrdinalEsquema,
                    config?.TablaFk,
                    config?.ColumnaClaveFk,
                    config?.ColumnaMostrarFk,
                    config?.EsCifrado ?? false,
                    config?.AutoFechaAlta ?? false,
                    config?.AutoFechaEdicion ?? false,
                    config?.AutoUsuarioAlta ?? false,
                    config?.AutoUsuarioEdicion ?? false);
            })
            .OrderBy(c => c.OrdinalPos)
            .ToList();

        return new ConfiguracionCatalogo(catalogo.IdCatalogo, catalogo.Clave, catalogo.NombreTabla, catalogo.Titulo, columnas);
    }
}
