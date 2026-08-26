using GTE.Application.Common;
using GTE.Domain.CatalogoGenerico;
using GTE.Domain.Interfaces;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

/// <summary>Escritura de las tablas de sistema del motor de catalogos genericos (EF, tipado).</summary>
public class CatalogoGenericoRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), ICatalogoGenericoRepository
{
    public async Task<int> CrearCatalogoAsync(CatalogoNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblCatalogoGenerico
        {
            Clave = datos.Clave,
            NombreTabla = datos.NombreTabla,
            Titulo = datos.Titulo,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblCatalogoGenerico.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        return entidad.IdCatalogo;
    }

    public async Task ActualizarConfigColumnasAsync(
        int idCatalogo, IReadOnlyList<ColumnaConfigEdicion> columnas, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var existentes = await contexto.TblCatalogoGenericoColumna
            .Where(c => c.IdCatalogo == idCatalogo && c.Activo)
            .ToListAsync(cancellationToken);
        var existentesPorNombre = existentes.ToDictionary(c => c.NombreColumna, StringComparer.OrdinalIgnoreCase);
        var nombresNuevos = columnas.Select(c => c.NombreColumna).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var columna in columnas)
        {
            if (existentesPorNombre.TryGetValue(columna.NombreColumna, out var entidad))
            {
                AplicarConfig(entidad, columna);
                entidad.UsuarioMovto = Auditoria.Usuario;
                entidad.FechaMovto = DateTime.Now;
            }
            else
            {
                entidad = new TblCatalogoGenericoColumna
                {
                    IdCatalogo = idCatalogo,
                    NombreColumna = columna.NombreColumna,
                    UsuarioRegistro = Auditoria.Usuario,
                    Activo = true
                };
                AplicarConfig(entidad, columna);
                contexto.TblCatalogoGenericoColumna.Add(entidad);
            }
        }

        // Columna que ya no vino en la config nueva (p.ej. se quito de la tabla real): baja logica.
        foreach (var sobrante in existentes.Where(e => !nombresNuevos.Contains(e.NombreColumna)))
        {
            sobrante.Activo = false;
            sobrante.UsuarioMovto = Auditoria.Usuario;
            sobrante.FechaMovto = DateTime.Now;
        }

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task SembrarPermisosAsync(
        string clave, string titulo, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var clavesDeseadas = PermisosCatalogoGenerico.ClavesPorCatalogo(clave, titulo);

        foreach (var (claveP, descripcion) in clavesDeseadas)
        {
            var existe = await contexto.TblPermiso.AnyAsync(p => p.Clave == claveP, cancellationToken);
            if (!existe)
            {
                contexto.TblPermiso.Add(new TblPermiso
                {
                    Clave = claveP,
                    Modulo = "Catalogos",
                    Descripcion = descripcion,
                    UsuarioRegistro = Auditoria.Usuario,
                    Activo = true
                });
            }
        }
        await contexto.SaveChangesAsync(cancellationToken);

        var administrador = await contexto.TblRol
            .FirstOrDefaultAsync(r => r.Nombre == "Administrador", cancellationToken);
        if (administrador is null)
        {
            return;
        }

        var nombresClave = clavesDeseadas.Select(c => c.Clave).ToList();
        var permisos = await contexto.TblPermiso
            .Where(p => nombresClave.Contains(p.Clave))
            .ToListAsync(cancellationToken);
        var yaAsignados = await contexto.TblRolPermiso
            .Where(rp => rp.IdRol == administrador.IdRol)
            .Select(rp => rp.IdPermiso)
            .ToListAsync(cancellationToken);

        foreach (var permiso in permisos.Where(p => !yaAsignados.Contains(p.IdPermiso)))
        {
            contexto.TblRolPermiso.Add(new TblRolPermiso
            {
                IdRol = administrador.IdRol,
                IdPermiso = permiso.IdPermiso,
                UsuarioRegistro = Auditoria.Usuario
            });
        }

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public Task RegistrarDescifradoAsync(
        string clave, string nombreColumna, CancellationToken cancellationToken = default)
    {
        return RegistrarBitacoraAsync(
            "CatalogoGenerico.Descifrado", null, "DESCIFRAR",
            $"Catalogo {clave}, columna {nombreColumna}", cancellationToken);
    }

    private static void AplicarConfig(TblCatalogoGenericoColumna entidad, ColumnaConfigEdicion columna)
    {
        entidad.DisplayName = columna.DisplayName;
        entidad.EsVisible = columna.EsVisible;
        entidad.EsSoloLectura = columna.EsSoloLectura;
        entidad.EsRequerido = columna.EsRequerido;
        entidad.OrdinalPos = columna.OrdinalPos;
        entidad.TablaFk = columna.TablaFk;
        entidad.ColumnaClaveFk = columna.ColumnaClaveFk;
        entidad.ColumnaMostrarFk = columna.ColumnaMostrarFk;
        entidad.EsCifrado = columna.EsCifrado;
        entidad.AutoFechaAlta = columna.AutoFechaAlta;
        entidad.AutoFechaEdicion = columna.AutoFechaEdicion;
        entidad.AutoUsuarioAlta = columna.AutoUsuarioAlta;
        entidad.AutoUsuarioEdicion = columna.AutoUsuarioEdicion;
    }
}
