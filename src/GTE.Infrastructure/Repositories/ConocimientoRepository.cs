using GTE.Application.Common;
using GTE.Domain.Conocimiento;
using GTE.Domain.Interfaces;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

public class ConocimientoRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), IConocimientoRepository
{
    public async Task<int> CrearAsync(ArticuloNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblArticuloConocimiento
        {
            Titulo = datos.Titulo,
            Contenido = datos.Contenido,
            VersionActual = 1,
            EsGlosario = datos.EsGlosario,
            // Trampa EF (CLAUDE.md): los DEFAULT de BD de las columnas bit no aplican de
            // forma confiable en los INSERT de EF, asi que se fijan siempre explicitamente.
            EsPublico = datos.EsPublico,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblArticuloConocimiento.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        // El historial guarda TODAS las versiones, incluida la vigente: VersionActual
        // apunta a la fila de tblArticuloVersion que corresponde al contenido actual.
        contexto.TblArticuloVersion.Add(new TblArticuloVersion
        {
            IdArticuloConocimiento = entidad.IdArticuloConocimiento,
            Version = 1,
            Contenido = datos.Contenido,
            UsuarioRegistro = Auditoria.Usuario
        });
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(
            ConstantesConocimiento.EntidadArchivo, entidad.IdArticuloConocimiento, "CREAR",
            datos.Titulo, cancellationToken);

        return entidad.IdArticuloConocimiento;
    }

    public async Task<EstadoArticulo?> ObtenerEstadoAsync(
        int idArticulo, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblArticuloConocimiento.AsNoTracking()
            .Where(a => a.IdArticuloConocimiento == idArticulo)
            .Select(a => new EstadoArticulo(
                a.IdArticuloConocimiento, a.Titulo, a.Contenido, a.VersionActual,
                a.EsGlosario, a.EsPublico, a.Activo))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task ActualizarAsync(
        int idArticulo, ArticuloActualizacion datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblArticuloConocimiento
            .FirstOrDefaultAsync(a => a.IdArticuloConocimiento == idArticulo, cancellationToken)
            ?? throw new InvalidOperationException($"Articulo {idArticulo} no existe.");

        // Solo un cambio real de contenido genera version nueva: renombrar el articulo o
        // mover los switches de glosario/publico no debe ensuciar el historial.
        var contenidoCambio = !string.Equals(entidad.Contenido, datos.Contenido, StringComparison.Ordinal);

        entidad.Titulo = datos.Titulo;
        entidad.Contenido = datos.Contenido;
        entidad.EsGlosario = datos.EsGlosario;
        entidad.EsPublico = datos.EsPublico;

        if (contenidoCambio)
        {
            entidad.VersionActual += 1;
            contexto.TblArticuloVersion.Add(new TblArticuloVersion
            {
                IdArticuloConocimiento = idArticulo,
                Version = entidad.VersionActual,
                Contenido = datos.Contenido,
                UsuarioRegistro = Auditoria.Usuario
            });
        }

        MarcarMovimiento(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(
            ConstantesConocimiento.EntidadArchivo, idArticulo, "ACTUALIZAR",
            contenidoCambio ? $"{datos.Titulo} (version {entidad.VersionActual})" : datos.Titulo,
            cancellationToken);
    }

    public async Task EliminarAsync(int idArticulo, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblArticuloConocimiento
            .FirstOrDefaultAsync(a => a.IdArticuloConocimiento == idArticulo, cancellationToken)
            ?? throw new InvalidOperationException($"Articulo {idArticulo} no existe.");

        entidad.Activo = false;
        // Un articulo eliminado deja de estar publicado en el mismo movimiento: si solo se
        // apagara Activo, un cambio futuro en el filtro publico podria volver a exponerlo.
        entidad.EsPublico = false;
        MarcarMovimiento(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(
            ConstantesConocimiento.EntidadArchivo, idArticulo, "ELIMINAR", entidad.Titulo, cancellationToken);
    }

    public async Task<bool> ExisteTituloAsync(
        string titulo, int? idArticuloExcluir = null, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblArticuloConocimiento.AsNoTracking()
            .AnyAsync(
                a => a.Titulo == titulo
                     && (idArticuloExcluir == null || a.IdArticuloConocimiento != idArticuloExcluir),
                cancellationToken);
    }

    private void MarcarMovimiento(TblArticuloConocimiento entidad)
    {
        entidad.UsuarioMovto = Auditoria.Usuario.Length > 50 ? Auditoria.Usuario[..50] : Auditoria.Usuario;
        entidad.FechaMovto = DateTime.Now;
    }
}
