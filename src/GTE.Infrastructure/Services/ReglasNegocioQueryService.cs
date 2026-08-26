using System.Linq.Expressions;
using GTE.Application.DTOs.Request.ReglasNegocio;
using GTE.Application.DTOs.Responses.ReglasNegocio;
using GTE.Application.Interfaces;
using GTE.Domain.ReglasNegocio;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Lectura del Catalogo de reglas de negocio.
///
/// Sigue el patron de PlaneacionQueryService por la TRAMPA EF de CLAUDE.md: se filtra y se
/// ordena SIEMPRE sobre columnas reales de las entidades (o de sus navegaciones), nunca
/// sobre proyecciones intermedias tipo record; la proyeccion al DTO se hace al final con
/// una Expression reutilizable.
/// </summary>
public class ReglasNegocioQueryService(FabricaContexto fabrica) : IReglasNegocioQueryService
{
    /// <summary>
    /// Proyeccion de fila del listado para reglas PROPIAS del proyecto consultado.
    /// DescripcionImpacto va en null: solo tiene sentido en las heredadas.
    /// </summary>
    private static readonly Expression<Func<TblReglaNegocio, ReglaNegocioResumenResponse>> ProyeccionPropia =
        r => new ReglaNegocioResumenResponse(
            r.IdReglaNegocio,
            r.Clave,
            r.Nombre,
            r.IdProyecto,
            r.IdProyectoNavigation.Clave,
            r.IdProyectoNavigation.Nombre,
            OrigenRegla.Propia,
            r.IdAmbitoRegla,
            r.IdAmbitoReglaNavigation != null ? r.IdAmbitoReglaNavigation.Nombre : null,
            r.IdAmbitoReglaNavigation != null ? r.IdAmbitoReglaNavigation.IdTipoAmbitoRegla : (int?)null,
            r.IdAmbitoReglaNavigation != null ? r.IdAmbitoReglaNavigation.IdTipoAmbitoReglaNavigation.Nombre : null,
            r.IdEstadoReglaNegocio,
            r.IdEstadoReglaNegocioNavigation.Nombre,
            null,
            r.Activo);

    /// <summary>
    /// Proyeccion de fila para reglas HEREDADAS: se parte del impacto, no de la regla, para
    /// poder traer la descripcion de como le pega al proyecto consultado y para etiquetar
    /// el proyecto de origen (el dueno), que es lo que el usuario necesita ver para saber
    /// donde se edita.
    /// </summary>
    private static readonly Expression<Func<TblReglaNegocioImpacto, ReglaNegocioResumenResponse>> ProyeccionHeredada =
        i => new ReglaNegocioResumenResponse(
            i.IdReglaNegocioNavigation.IdReglaNegocio,
            i.IdReglaNegocioNavigation.Clave,
            i.IdReglaNegocioNavigation.Nombre,
            i.IdReglaNegocioNavigation.IdProyecto,
            i.IdReglaNegocioNavigation.IdProyectoNavigation.Clave,
            i.IdReglaNegocioNavigation.IdProyectoNavigation.Nombre,
            OrigenRegla.Heredada,
            i.IdReglaNegocioNavigation.IdAmbitoRegla,
            i.IdReglaNegocioNavigation.IdAmbitoReglaNavigation != null
                ? i.IdReglaNegocioNavigation.IdAmbitoReglaNavigation.Nombre : null,
            i.IdReglaNegocioNavigation.IdAmbitoReglaNavigation != null
                ? i.IdReglaNegocioNavigation.IdAmbitoReglaNavigation.IdTipoAmbitoRegla : (int?)null,
            i.IdReglaNegocioNavigation.IdAmbitoReglaNavigation != null
                ? i.IdReglaNegocioNavigation.IdAmbitoReglaNavigation.IdTipoAmbitoReglaNavigation.Nombre : null,
            i.IdReglaNegocioNavigation.IdEstadoReglaNegocio,
            i.IdReglaNegocioNavigation.IdEstadoReglaNegocioNavigation.Nombre,
            i.DescripcionImpacto,
            i.IdReglaNegocioNavigation.Activo);

    public async Task<CatalogoReglasProyectoResponse?> ObtenerCatalogoProyectoAsync(
        int idProyecto, ReglasNegocioFiltroRequest filtro, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var proyecto = await contexto.TblProyecto.AsNoTracking()
            .Where(p => p.IdProyecto == idProyecto)
            .Select(p => new { p.IdProyecto, p.Clave, p.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

        if (proyecto is null)
        {
            return null;
        }

        var busqueda = string.IsNullOrWhiteSpace(filtro.Busqueda) ? null : filtro.Busqueda.Trim();

        var consultaPropias = contexto.TblReglaNegocio.AsNoTracking()
            .Where(r => r.IdProyecto == idProyecto);

        if (!filtro.IncluirDerogadas)
        {
            consultaPropias = consultaPropias.Where(r => r.Activo);
        }

        if (filtro.IdAmbitoRegla is not null)
        {
            consultaPropias = consultaPropias.Where(r => r.IdAmbitoRegla == filtro.IdAmbitoRegla);
        }

        if (filtro.IdEstadoReglaNegocio is not null)
        {
            consultaPropias = consultaPropias.Where(r => r.IdEstadoReglaNegocio == filtro.IdEstadoReglaNegocio);
        }

        if (filtro.IdTipoAmbitoRegla is not null)
        {
            consultaPropias = consultaPropias.Where(r =>
                r.IdAmbitoReglaNavigation != null
                && r.IdAmbitoReglaNavigation.IdTipoAmbitoRegla == filtro.IdTipoAmbitoRegla);
        }

        if (busqueda is not null)
        {
            consultaPropias = consultaPropias.Where(r =>
                r.Clave.Contains(busqueda) || r.Nombre.Contains(busqueda) || r.Enunciado.Contains(busqueda));
        }

        var propias = await consultaPropias
            .OrderBy(r => r.Clave)
            .Select(ProyeccionPropia)
            .ToListAsync(cancellationToken);

        var heredadas = new List<ReglaNegocioResumenResponse>();
        if (filtro.IncluirHeredadas)
        {
            var consultaHeredadas = contexto.TblReglaNegocioImpacto.AsNoTracking()
                .Where(i => i.IdProyectoAfectado == idProyecto && i.Activo);

            if (!filtro.IncluirDerogadas)
            {
                consultaHeredadas = consultaHeredadas.Where(i => i.IdReglaNegocioNavigation.Activo);
            }

            if (filtro.IdEstadoReglaNegocio is not null)
            {
                consultaHeredadas = consultaHeredadas.Where(i =>
                    i.IdReglaNegocioNavigation.IdEstadoReglaNegocio == filtro.IdEstadoReglaNegocio);
            }

            if (busqueda is not null)
            {
                consultaHeredadas = consultaHeredadas.Where(i =>
                    i.IdReglaNegocioNavigation.Clave.Contains(busqueda)
                    || i.IdReglaNegocioNavigation.Nombre.Contains(busqueda)
                    || i.IdReglaNegocioNavigation.Enunciado.Contains(busqueda));
            }

            heredadas = await consultaHeredadas
                .OrderBy(i => i.IdReglaNegocioNavigation.IdProyectoNavigation.Clave)
                .ThenBy(i => i.IdReglaNegocioNavigation.Clave)
                .Select(ProyeccionHeredada)
                .ToListAsync(cancellationToken);
        }

        return new CatalogoReglasProyectoResponse(
            proyecto.IdProyecto, proyecto.Clave, proyecto.Nombre,
            propias, heredadas, propias.Count, heredadas.Count);
    }

    public async Task<ReglaNegocioResponse?> ObtenerPorIdAsync(
        int idRegla, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var regla = await contexto.TblReglaNegocio.AsNoTracking()
            .Where(r => r.IdReglaNegocio == idRegla)
            .Select(r => new
            {
                r.IdReglaNegocio,
                r.IdProyecto,
                ClaveProyecto = r.IdProyectoNavigation.Clave,
                NombreProyecto = r.IdProyectoNavigation.Nombre,
                r.Clave,
                r.Nombre,
                r.Enunciado,
                r.Justificacion,
                r.IdAmbitoRegla,
                NombreAmbito = r.IdAmbitoReglaNavigation != null ? r.IdAmbitoReglaNavigation.Nombre : null,
                IdTipoAmbito = r.IdAmbitoReglaNavigation != null
                    ? r.IdAmbitoReglaNavigation.IdTipoAmbitoRegla : (int?)null,
                NombreTipoAmbito = r.IdAmbitoReglaNavigation != null
                    ? r.IdAmbitoReglaNavigation.IdTipoAmbitoReglaNavigation.Nombre : null,
                r.IdEstadoReglaNegocio,
                NombreEstado = r.IdEstadoReglaNegocioNavigation.Nombre,
                r.MensajeError,
                r.PermisoBypass,
                r.UbicacionCodigo,
                r.FechaVigenciaDesde,
                r.FechaVigenciaHasta,
                r.VersionActual,
                r.FechaRegistro,
                r.UsuarioRegistro,
                r.FechaMovto,
                r.UsuarioMovto,
                r.Activo
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (regla is null)
        {
            return null;
        }

        // Panel de impacto: a que proyectos afecta esta regla. Es la mitad que hace util
        // el modelo -- deja ver a quien le pega un cambio ANTES de tocar el enunciado.
        var impactos = await contexto.TblReglaNegocioImpacto.AsNoTracking()
            .Where(i => i.IdReglaNegocio == idRegla && i.Activo)
            .OrderBy(i => i.IdProyectoAfectadoNavigation.Clave)
            .Select(i => new ImpactoReglaResponse(
                i.IdReglaNegocioImpacto,
                i.IdProyectoAfectado,
                i.IdProyectoAfectadoNavigation.Clave,
                i.IdProyectoAfectadoNavigation.Nombre,
                i.DescripcionImpacto,
                i.IdAmbitoRegla,
                i.IdAmbitoReglaNavigation != null ? i.IdAmbitoReglaNavigation.Nombre : null))
            .ToListAsync(cancellationToken);

        var relaciones = await contexto.TblReglaNegocioRelacion.AsNoTracking()
            .Where(x => x.IdReglaNegocio == idRegla && x.Activo)
            .OrderBy(x => x.IdReglaNegocioRelacionadaNavigation.Clave)
            .Select(x => new RelacionReglaResponse(
                x.IdReglaNegocioRelacion,
                x.IdReglaNegocioRelacionada,
                x.IdReglaNegocioRelacionadaNavigation.Clave,
                x.IdReglaNegocioRelacionadaNavigation.Nombre,
                x.IdReglaNegocioRelacionadaNavigation.IdProyecto,
                x.IdTipoRelacionRegla,
                x.IdTipoRelacionReglaNavigation.Nombre,
                x.Nota))
            .ToListAsync(cancellationToken);

        return new ReglaNegocioResponse(
            regla.IdReglaNegocio, regla.IdProyecto, regla.ClaveProyecto, regla.NombreProyecto,
            regla.Clave, regla.Nombre, regla.Enunciado, regla.Justificacion,
            regla.IdAmbitoRegla, regla.NombreAmbito, regla.IdTipoAmbito, regla.NombreTipoAmbito,
            regla.IdEstadoReglaNegocio, regla.NombreEstado,
            regla.MensajeError, regla.PermisoBypass, regla.UbicacionCodigo,
            regla.FechaVigenciaDesde, regla.FechaVigenciaHasta, regla.VersionActual,
            impactos, relaciones,
            regla.FechaRegistro, regla.UsuarioRegistro, regla.FechaMovto, regla.UsuarioMovto, regla.Activo);
    }

    public async Task<IReadOnlyList<ReglaVersionResponse>> ObtenerVersionesAsync(
        int idRegla, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblReglaNegocioVersion.AsNoTracking()
            .Where(v => v.IdReglaNegocio == idRegla)
            .OrderByDescending(v => v.NumeroVersion)
            .Select(v => new ReglaVersionResponse(
                v.IdReglaNegocioVersion, v.NumeroVersion, v.Enunciado, v.Justificacion,
                v.MotivoCambio, v.FechaRegistro, v.UsuarioRegistro))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReglaNegocioResumenResponse>> BuscarAsync(
        ReglasNegocioFiltroRequest filtro, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var consulta = contexto.TblReglaNegocio.AsNoTracking().AsQueryable();

        if (!filtro.IncluirDerogadas)
        {
            consulta = consulta.Where(r => r.Activo);
        }

        if (filtro.IdProyecto is not null)
        {
            consulta = consulta.Where(r => r.IdProyecto == filtro.IdProyecto);
        }

        if (filtro.IdEstadoReglaNegocio is not null)
        {
            consulta = consulta.Where(r => r.IdEstadoReglaNegocio == filtro.IdEstadoReglaNegocio);
        }

        if (filtro.IdTipoAmbitoRegla is not null)
        {
            consulta = consulta.Where(r =>
                r.IdAmbitoReglaNavigation != null
                && r.IdAmbitoReglaNavigation.IdTipoAmbitoRegla == filtro.IdTipoAmbitoRegla);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
        {
            var busqueda = filtro.Busqueda.Trim();
            consulta = consulta.Where(r =>
                r.Clave.Contains(busqueda) || r.Nombre.Contains(busqueda) || r.Enunciado.Contains(busqueda));
        }

        var tamano = Math.Clamp(filtro.TamanoPagina, 1, ConstantesReglasNegocio.TamanoPaginaMaximo);
        var pagina = Math.Max(filtro.Pagina, 1);

        return await consulta
            .OrderBy(r => r.IdProyectoNavigation.Clave)
            .ThenBy(r => r.Clave)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(ProyeccionPropia)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AmbitoReglaResponse>> ObtenerAmbitosAsync(
        int idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblAmbitoRegla.AsNoTracking()
            .Where(a => a.IdProyecto == idProyecto && a.Activo)
            .OrderBy(a => a.IdTipoAmbitoRegla)
            .ThenBy(a => a.Nombre)
            .Select(a => new AmbitoReglaResponse(
                a.IdAmbitoRegla,
                a.IdProyecto,
                a.IdTipoAmbitoRegla,
                a.IdTipoAmbitoReglaNavigation.Nombre,
                a.Nombre,
                a.Descripcion,
                a.TblReglaNegocio.Count(r => r.Activo),
                a.Activo))
            .ToListAsync(cancellationToken);
    }

    public async Task<CatalogosReglasNegocioResponse> ObtenerCatalogosAsync(
        CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var tipos = await contexto.TblTipoAmbitoRegla.AsNoTracking()
            .Where(t => t.Activo).OrderBy(t => t.Id)
            .Select(t => new OpcionCatalogoResponse(t.Id, t.Nombre))
            .ToListAsync(cancellationToken);

        var estados = await contexto.TblEstadoReglaNegocio.AsNoTracking()
            .Where(e => e.Activo).OrderBy(e => e.Id)
            .Select(e => new OpcionCatalogoResponse(e.Id, e.Nombre))
            .ToListAsync(cancellationToken);

        var relaciones = await contexto.TblTipoRelacionRegla.AsNoTracking()
            .Where(t => t.Activo).OrderBy(t => t.Id)
            .Select(t => new OpcionCatalogoResponse(t.Id, t.Nombre))
            .ToListAsync(cancellationToken);

        return new CatalogosReglasNegocioResponse(tipos, estados, relaciones);
    }

    public async Task<IReadOnlyList<(int IdProyecto, string Clave, string Nombre, int TotalReglas)>>
        ObtenerProyectosConReglasAsync(CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        // Un proyecto entra al selector si tiene reglas propias O si alguna regla de otro
        // proyecto lo declara afectado: las heredadas tambien son parte de su catalogo.
        var filas = await contexto.TblProyecto.AsNoTracking()
            .Where(p => p.Activo)
            .Select(p => new
            {
                p.IdProyecto,
                p.Clave,
                p.Nombre,
                Propias = contexto.TblReglaNegocio.Count(r => r.IdProyecto == p.IdProyecto && r.Activo),
                Heredadas = contexto.TblReglaNegocioImpacto.Count(i =>
                    i.IdProyectoAfectado == p.IdProyecto && i.Activo && i.IdReglaNegocioNavigation.Activo)
            })
            .Where(p => p.Propias > 0 || p.Heredadas > 0)
            .OrderBy(p => p.Clave)
            .ToListAsync(cancellationToken);

        return filas
            .Select(p => (p.IdProyecto, p.Clave, p.Nombre, p.Propias + p.Heredadas))
            .ToList();
    }
}
