using System.Net;
using System.Text.RegularExpressions;
using GTE.Application.Common;
using GTE.Application.DTOs.Responses.Conocimiento;
using GTE.Application.Interfaces;
using GTE.Domain.Conocimiento;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public partial class ConocimientoQueryService(FabricaContexto fabrica) : IConocimientoQueryService
{
    /// <summary>
    /// Cuanto contenido crudo se baja para armar el fragmento del listado. Se recorta en
    /// SQL (SUBSTRING) para no traer articulos completos en un listado de 100 filas; el
    /// texto plano se obtiene despues en memoria.
    /// </summary>
    private const int CaracteresParaFragmento = 600;

    private const int LongitudFragmento = 180;

    /// <summary>
    /// Marca de una imagen incrustada por el editor enriquecido: el nodo ImagenProtegida
    /// guarda solo el GUID del adjunto (img data-guid), nunca una URL.
    /// </summary>
    private const string MarcaImagen = "data-guid";

    public async Task<PagedResult<ArticuloListaResponse>> ObtenerListaAsync(
        FiltroArticulos filtro, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        // Trampa EF (CLAUDE.md): se filtra y ordena por columnas REALES sobre las
        // entidades sin proyectar; la proyeccion va al final.
        var consulta = contexto.TblArticuloConocimiento.AsNoTracking().Where(a => a.Activo);
        consulta = AplicarFiltros(consulta, filtro);

        var total = await consulta.CountAsync(cancellationToken);
        var page = Math.Max(1, filtro.Page);
        var pageSize = Math.Clamp(filtro.PageSize, 1, ConstantesConocimiento.TamanoPaginaMaximo);

        var filas = await consulta
            .OrderBy(a => a.Titulo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.IdArticuloConocimiento,
                a.Titulo,
                Recorte = a.Contenido.Substring(0, CaracteresParaFragmento),
                TieneImagen = a.Contenido.Contains(MarcaImagen),
                a.EsGlosario,
                a.EsPublico,
                a.FechaRegistro,
                a.FechaMovto,
                Autor = contexto.TblUsuario
                    .Where(u => u.Dominio == (a.UsuarioMovto ?? a.UsuarioRegistro))
                    .Select(u => u.Nombre)
                    .FirstOrDefault(),
                DominioAutor = a.UsuarioMovto ?? a.UsuarioRegistro
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ArticuloListaResponse>
        {
            Items = filas.Select(f => new ArticuloListaResponse
            {
                IdArticuloConocimiento = f.IdArticuloConocimiento,
                Titulo = f.Titulo,
                Fragmento = ExtraerFragmento(f.Recorte),
                EsGlosario = f.EsGlosario,
                EsPublico = f.EsPublico,
                TieneImagen = f.TieneImagen,
                FechaRegistro = f.FechaRegistro,
                FechaMovto = f.FechaMovto,
                UltimoAutor = f.Autor ?? f.DominioAutor
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<ArticuloResponse?> ObtenerPorIdAsync(
        int idArticulo, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        return await (
            from a in contexto.TblArticuloConocimiento.AsNoTracking()
            join u in contexto.TblUsuario.AsNoTracking()
                on (a.UsuarioMovto ?? a.UsuarioRegistro) equals u.Dominio into usuarios
            from u in usuarios.DefaultIfEmpty()
            where a.IdArticuloConocimiento == idArticulo && a.Activo
            select new ArticuloResponse
            {
                IdArticuloConocimiento = a.IdArticuloConocimiento,
                Titulo = a.Titulo,
                Contenido = a.Contenido,
                VersionActual = a.VersionActual,
                EsGlosario = a.EsGlosario,
                EsPublico = a.EsPublico,
                FechaRegistro = a.FechaRegistro,
                FechaMovto = a.FechaMovto,
                UltimoAutor = u != null ? u.Nombre : (a.UsuarioMovto ?? a.UsuarioRegistro)
            }).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ArticuloVersionResponse>> ObtenerVersionesAsync(
        int idArticulo, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        return await (
            from v in contexto.TblArticuloVersion.AsNoTracking()
            join a in contexto.TblArticuloConocimiento.AsNoTracking()
                on v.IdArticuloConocimiento equals a.IdArticuloConocimiento
            join u in contexto.TblUsuario.AsNoTracking() on v.UsuarioRegistro equals u.Dominio into usuarios
            from u in usuarios.DefaultIfEmpty()
            where v.IdArticuloConocimiento == idArticulo
            orderby v.Version descending
            select new ArticuloVersionResponse
            {
                IdArticuloVersion = v.IdArticuloVersion,
                Version = v.Version,
                EsVersionActual = v.Version == a.VersionActual,
                FechaRegistro = v.FechaRegistro,
                Autor = u != null ? u.Nombre : v.UsuarioRegistro
            }).ToListAsync(cancellationToken);
    }

    public async Task<ArticuloVersionContenidoResponse?> ObtenerVersionAsync(
        int idArticulo, int version, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        return await (
            from v in contexto.TblArticuloVersion.AsNoTracking()
            join u in contexto.TblUsuario.AsNoTracking() on v.UsuarioRegistro equals u.Dominio into usuarios
            from u in usuarios.DefaultIfEmpty()
            where v.IdArticuloConocimiento == idArticulo && v.Version == version
            select new ArticuloVersionContenidoResponse
            {
                Version = v.Version,
                Contenido = v.Contenido,
                FechaRegistro = v.FechaRegistro,
                Autor = u != null ? u.Nombre : v.UsuarioRegistro
            }).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<ArticuloPublicoListaResponse>> ObtenerListaPublicaAsync(
        FiltroArticulos filtro, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        // El filtro de publicidad vive aqui, en la consulta anonima, no en un parametro
        // opcional compartido con el listado interno.
        var consulta = contexto.TblArticuloConocimiento.AsNoTracking()
            .Where(a => a.Activo && a.EsPublico);
        consulta = AplicarFiltros(consulta, filtro);

        var total = await consulta.CountAsync(cancellationToken);
        var page = Math.Max(1, filtro.Page);
        var pageSize = Math.Clamp(filtro.PageSize, 1, ConstantesConocimiento.TamanoPaginaMaximo);

        var filas = await consulta
            .OrderBy(a => a.Titulo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.IdArticuloConocimiento,
                a.Titulo,
                Recorte = a.Contenido.Substring(0, CaracteresParaFragmento),
                TieneImagen = a.Contenido.Contains(MarcaImagen),
                a.EsGlosario
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ArticuloPublicoListaResponse>
        {
            Items = filas.Select(f => new ArticuloPublicoListaResponse
            {
                IdArticuloConocimiento = f.IdArticuloConocimiento,
                Titulo = f.Titulo,
                Fragmento = ExtraerFragmento(f.Recorte),
                EsGlosario = f.EsGlosario,
                TieneImagen = f.TieneImagen
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<ArticuloPublicoResponse?> ObtenerPublicoPorIdAsync(
        int idArticulo, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        return await contexto.TblArticuloConocimiento.AsNoTracking()
            .Where(a => a.IdArticuloConocimiento == idArticulo && a.Activo && a.EsPublico)
            .Select(a => new ArticuloPublicoResponse
            {
                IdArticuloConocimiento = a.IdArticuloConocimiento,
                Titulo = a.Titulo,
                Contenido = a.Contenido,
                EsGlosario = a.EsGlosario,
                FechaActualizacion = a.FechaMovto ?? a.FechaRegistro
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> EsImagenDeArticuloPublicoAsync(
        Guid guidArchivo, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        // El GUID se serializa en minusculas con guiones tanto en la respuesta de la API
        // como en el atributo data-guid que guarda el editor, asi que el texto buscado
        // coincide con lo que quedo dentro del HTML. Va como parametro de la consulta
        // (Contains -> LIKE parametrizado): nunca SQL interpolado.
        var aguja = guidArchivo.ToString();

        return await (
            from v in contexto.TblArchivoVinculo.AsNoTracking()
            join arch in contexto.TblArchivo.AsNoTracking() on v.IdArchivo equals arch.IdArchivo
            join a in contexto.TblArticuloConocimiento.AsNoTracking()
                on v.IdEntidad equals a.IdArticuloConocimiento
            where v.Entidad == ConstantesConocimiento.EntidadArchivo
                  && v.Activo
                  && arch.GuidArchivo == guidArchivo
                  && arch.Activo
                  && a.Activo
                  && a.EsPublico
                  // Condicion clave: el archivo tiene que estar INCRUSTADO en el texto del
                  // articulo. Un adjunto suelto (PDF, archivo interno) nunca se sirve sin
                  // sesion, aunque alguien adivine su GUID.
                  && a.Contenido.Contains(aguja)
            select v.IdArchivoVinculo
            ).AnyAsync(cancellationToken);
    }

    private static IQueryable<TblArticuloConocimiento> AplicarFiltros(
        IQueryable<TblArticuloConocimiento> consulta, FiltroArticulos filtro)
    {
        if (filtro.EsGlosario.HasValue)
        {
            var esGlosario = filtro.EsGlosario.Value;
            consulta = consulta.Where(a => a.EsGlosario == esGlosario);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();
            consulta = consulta.Where(a => a.Titulo.Contains(texto) || a.Contenido.Contains(texto));
        }

        return consulta;
    }

    /// <summary>
    /// Convierte el recorte de HTML en texto plano para el fragmento del listado. Se hace
    /// en memoria (SQL Server no tiene forma razonable de quitar etiquetas) sobre el recorte
    /// que ya limito la consulta, no sobre el articulo completo.
    /// </summary>
    private static string ExtraerFragmento(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var sinEtiquetas = ExpresionEtiquetas().Replace(html, " ");
        var texto = WebUtility.HtmlDecode(sinEtiquetas);
        texto = ExpresionEspacios().Replace(texto, " ").Trim();

        return texto.Length <= LongitudFragmento
            ? texto
            : texto[..LongitudFragmento].TrimEnd() + "...";
    }

    [GeneratedRegex("<[^>]*>", RegexOptions.None, matchTimeoutMilliseconds: 500)]
    private static partial Regex ExpresionEtiquetas();

    [GeneratedRegex(@"\s+", RegexOptions.None, matchTimeoutMilliseconds: 500)]
    private static partial Regex ExpresionEspacios();
}
