using GTE.Application.Common;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.ReglasNegocio;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

/// <summary>
/// Escritura del Catalogo de reglas de negocio. El proyecto dueno de una regla no se
/// modifica nunca despues del alta: cambiarlo dejaria sus impactos apuntando a un dueno
/// que ya no es (la FK compuesta de tblReglaNegocioImpacto lo impediria de todos modos).
/// </summary>
public class ReglasNegocioRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), IReglasNegocioRepository
{
    private const string Entidad = "ReglaNegocio";
    private const string EntidadAmbito = "AmbitoRegla";

    public async Task<int> CrearAsync(ReglaNegocioNueva datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var entidad = new TblReglaNegocio
        {
            IdProyecto = datos.IdProyecto,
            Clave = datos.Clave,
            Nombre = datos.Nombre,
            Enunciado = datos.Enunciado,
            Justificacion = datos.Justificacion,
            IdAmbitoRegla = datos.IdAmbitoRegla,
            IdEstadoReglaNegocio = datos.IdEstadoReglaNegocio,
            MensajeError = datos.MensajeError,
            PermisoBypass = datos.PermisoBypass,
            UbicacionCodigo = datos.UbicacionCodigo,
            FechaVigenciaDesde = datos.FechaVigenciaDesde,
            FechaVigenciaHasta = datos.FechaVigenciaHasta,
            VersionActual = 1,
            UsuarioRegistro = Auditoria.Usuario,
            // Trampa EF (CLAUDE.md): el DEFAULT de BD de las columnas bit no aplica de
            // forma confiable en los INSERT de EF, asi que se fija explicitamente.
            Activo = true
        };
        contexto.TblReglaNegocio.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        contexto.TblReglaNegocioVersion.Add(new TblReglaNegocioVersion
        {
            IdReglaNegocio = entidad.IdReglaNegocio,
            NumeroVersion = 1,
            Enunciado = datos.Enunciado,
            Justificacion = datos.Justificacion,
            MotivoCambio = "Alta de la regla",
            UsuarioRegistro = Auditoria.Usuario
        });
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(
            Entidad, entidad.IdReglaNegocio, "CREAR", $"{datos.Clave} - {datos.Nombre}", cancellationToken);

        return entidad.IdReglaNegocio;
    }

    public async Task<EstadoReglaNegocio?> ObtenerEstadoAsync(
        int idRegla, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblReglaNegocio.AsNoTracking()
            .Where(r => r.IdReglaNegocio == idRegla)
            .Select(r => new EstadoReglaNegocio(
                r.IdReglaNegocio, r.IdProyecto, r.Clave, r.IdEstadoReglaNegocio, r.VersionActual, r.Activo))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task ActualizarAsync(
        int idRegla, ReglaNegocioActualizacion datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblReglaNegocio
            .FirstOrDefaultAsync(r => r.IdReglaNegocio == idRegla, cancellationToken)
            ?? throw new NotFoundException(Entidad, idRegla);

        // Solo un cambio real del texto genera version nueva: renombrar la regla o moverla
        // de flujo no debe ensuciar el historial del enunciado.
        var textoCambio =
            !string.Equals(entidad.Enunciado, datos.Enunciado, StringComparison.Ordinal)
            || !string.Equals(entidad.Justificacion, datos.Justificacion, StringComparison.Ordinal);

        entidad.Nombre = datos.Nombre;
        entidad.Enunciado = datos.Enunciado;
        entidad.Justificacion = datos.Justificacion;
        entidad.IdAmbitoRegla = datos.IdAmbitoRegla;
        entidad.IdEstadoReglaNegocio = datos.IdEstadoReglaNegocio;
        entidad.MensajeError = datos.MensajeError;
        entidad.PermisoBypass = datos.PermisoBypass;
        entidad.UbicacionCodigo = datos.UbicacionCodigo;
        entidad.FechaVigenciaDesde = datos.FechaVigenciaDesde;
        entidad.FechaVigenciaHasta = datos.FechaVigenciaHasta;
        entidad.UsuarioMovto = Auditoria.Usuario;
        entidad.FechaMovto = DateTime.Now;

        if (textoCambio)
        {
            entidad.VersionActual += 1;
            contexto.TblReglaNegocioVersion.Add(new TblReglaNegocioVersion
            {
                IdReglaNegocio = idRegla,
                NumeroVersion = entidad.VersionActual,
                Enunciado = datos.Enunciado,
                Justificacion = datos.Justificacion,
                MotivoCambio = datos.MotivoCambio,
                UsuarioRegistro = Auditoria.Usuario
            });
        }

        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(
            Entidad, idRegla, "ACTUALIZAR",
            textoCambio ? $"{entidad.Clave} - version {entidad.VersionActual}" : entidad.Clave,
            cancellationToken);
    }

    public async Task DerogarAsync(
        int idRegla, DateOnly fechaHasta, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblReglaNegocio
            .FirstOrDefaultAsync(r => r.IdReglaNegocio == idRegla, cancellationToken)
            ?? throw new NotFoundException(Entidad, idRegla);

        entidad.Activo = false;
        entidad.IdEstadoReglaNegocio = EstadosReglaNegocio.Derogada;
        entidad.FechaVigenciaHasta = fechaHasta;
        entidad.UsuarioMovto = Auditoria.Usuario;
        entidad.FechaMovto = DateTime.Now;

        // Los impactos se apagan en el mismo movimiento: una regla derogada no puede
        // seguir apareciendo como vigente en la lista de los proyectos secundarios.
        var impactos = await contexto.TblReglaNegocioImpacto
            .Where(i => i.IdReglaNegocio == idRegla && i.Activo)
            .ToListAsync(cancellationToken);
        foreach (var impacto in impactos)
        {
            impacto.Activo = false;
            impacto.UsuarioMovto = Auditoria.Usuario;
            impacto.FechaMovto = DateTime.Now;
        }

        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(
            Entidad, idRegla, "DEROGAR",
            $"{entidad.Clave} (vigencia hasta {fechaHasta:yyyy-MM-dd}, {impactos.Count} impactos dados de baja)",
            cancellationToken);
    }

    public async Task ReactivarAsync(int idRegla, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblReglaNegocio
            .FirstOrDefaultAsync(r => r.IdReglaNegocio == idRegla, cancellationToken)
            ?? throw new NotFoundException(Entidad, idRegla);

        entidad.Activo = true;
        entidad.IdEstadoReglaNegocio = EstadosReglaNegocio.Vigente;
        entidad.FechaVigenciaHasta = null;
        entidad.UsuarioMovto = Auditoria.Usuario;
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        // Los impactos NO se reactivan solos: al derogar pudo cambiar el alcance real, y
        // reponerlo a ciegas reintroduciria la regla en proyectos que quiza ya no aplica.
        await RegistrarBitacoraAsync(Entidad, idRegla, "REACTIVAR", entidad.Clave, cancellationToken);
    }

    public async Task<bool> ExisteClaveAsync(
        int idProyecto, string clave, int? idReglaExcluir = null, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblReglaNegocio.AsNoTracking()
            .AnyAsync(r => r.IdProyecto == idProyecto
                           && r.Clave == clave
                           && (idReglaExcluir == null || r.IdReglaNegocio != idReglaExcluir),
                cancellationToken);
    }

    public async Task<string?> ObtenerClaveProyectoAsync(
        int idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblProyecto.AsNoTracking()
            .Where(p => p.IdProyecto == idProyecto)
            .Select(p => p.Clave)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /* ---------------------------------------------------------------
       Impactos
       --------------------------------------------------------------- */

    public async Task<int> AgregarImpactoAsync(
        ImpactoReglaNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var regla = await contexto.TblReglaNegocio.AsNoTracking()
            .Where(r => r.IdReglaNegocio == datos.IdReglaNegocio)
            .Select(r => new { r.IdProyecto, r.Clave })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Entidad, datos.IdReglaNegocio);

        var existente = await contexto.TblReglaNegocioImpacto
            .FirstOrDefaultAsync(i => i.IdReglaNegocio == datos.IdReglaNegocio
                                      && i.IdProyectoAfectado == datos.IdProyectoAfectado,
                cancellationToken);

        if (existente is not null)
        {
            if (existente.Activo)
            {
                throw new ConflictException("Ese proyecto ya estaba declarado como afectado por la regla.");
            }

            // Reusar la fila dada de baja en vez de insertar una nueva: la UNIQUE
            // (IdReglaNegocio, IdProyectoAfectado) no distingue activos de inactivos.
            existente.Activo = true;
            existente.DescripcionImpacto = datos.DescripcionImpacto;
            existente.IdAmbitoRegla = datos.IdAmbitoRegla;
            existente.UsuarioMovto = Auditoria.Usuario;
            existente.FechaMovto = DateTime.Now;
            await contexto.SaveChangesAsync(cancellationToken);

            await RegistrarBitacoraAsync(
                Entidad, datos.IdReglaNegocio, "AGREGAR_IMPACTO",
                $"{regla.Clave} -> proyecto {datos.IdProyectoAfectado}", cancellationToken);
            return existente.IdReglaNegocioImpacto;
        }

        var entidad = new TblReglaNegocioImpacto
        {
            IdReglaNegocio = datos.IdReglaNegocio,
            IdProyectoDueno = regla.IdProyecto,
            IdProyectoAfectado = datos.IdProyectoAfectado,
            DescripcionImpacto = datos.DescripcionImpacto,
            IdAmbitoRegla = datos.IdAmbitoRegla,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblReglaNegocioImpacto.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(
            Entidad, datos.IdReglaNegocio, "AGREGAR_IMPACTO",
            $"{regla.Clave} -> proyecto {datos.IdProyectoAfectado}", cancellationToken);

        return entidad.IdReglaNegocioImpacto;
    }

    public async Task QuitarImpactoAsync(int idImpacto, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblReglaNegocioImpacto
            .FirstOrDefaultAsync(i => i.IdReglaNegocioImpacto == idImpacto, cancellationToken)
            ?? throw new NotFoundException("ReglaNegocioImpacto", idImpacto);

        entidad.Activo = false;
        entidad.UsuarioMovto = Auditoria.Usuario;
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(
            Entidad, entidad.IdReglaNegocio, "QUITAR_IMPACTO",
            $"proyecto {entidad.IdProyectoAfectado}", cancellationToken);
    }

    public async Task<(int IdReglaNegocio, int IdProyectoDueno)?> ObtenerOrigenImpactoAsync(
        int idImpacto, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var fila = await contexto.TblReglaNegocioImpacto.AsNoTracking()
            .Where(i => i.IdReglaNegocioImpacto == idImpacto)
            .Select(i => new { i.IdReglaNegocio, i.IdProyectoDueno })
            .FirstOrDefaultAsync(cancellationToken);

        return fila is null ? null : (fila.IdReglaNegocio, fila.IdProyectoDueno);
    }

    /* ---------------------------------------------------------------
       Ambitos
       --------------------------------------------------------------- */

    public async Task<int> CrearAmbitoAsync(
        AmbitoReglaNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var existente = await contexto.TblAmbitoRegla
            .FirstOrDefaultAsync(a => a.IdProyecto == datos.IdProyecto
                                      && a.IdTipoAmbitoRegla == datos.IdTipoAmbitoRegla
                                      && a.Nombre == datos.Nombre,
                cancellationToken);

        if (existente is not null)
        {
            if (existente.Activo)
            {
                throw new ConflictException(
                    $"El proyecto ya tiene un elemento llamado \"{datos.Nombre}\" de ese tipo.");
            }

            existente.Activo = true;
            existente.Descripcion = datos.Descripcion;
            existente.UsuarioMovto = Auditoria.Usuario;
            existente.FechaMovto = DateTime.Now;
            await contexto.SaveChangesAsync(cancellationToken);
            return existente.IdAmbitoRegla;
        }

        var entidad = new TblAmbitoRegla
        {
            IdProyecto = datos.IdProyecto,
            IdTipoAmbitoRegla = datos.IdTipoAmbitoRegla,
            Nombre = datos.Nombre,
            Descripcion = datos.Descripcion,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblAmbitoRegla.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(
            EntidadAmbito, entidad.IdAmbitoRegla, "CREAR", datos.Nombre, cancellationToken);

        return entidad.IdAmbitoRegla;
    }

    public async Task ActualizarAmbitoAsync(
        int idAmbito, string nombre, string? descripcion, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblAmbitoRegla
            .FirstOrDefaultAsync(a => a.IdAmbitoRegla == idAmbito, cancellationToken)
            ?? throw new NotFoundException(EntidadAmbito, idAmbito);

        entidad.Nombre = nombre;
        entidad.Descripcion = descripcion;
        entidad.UsuarioMovto = Auditoria.Usuario;
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(EntidadAmbito, idAmbito, "ACTUALIZAR", nombre, cancellationToken);
    }

    public async Task EliminarAmbitoAsync(int idAmbito, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblAmbitoRegla
            .FirstOrDefaultAsync(a => a.IdAmbitoRegla == idAmbito, cancellationToken)
            ?? throw new NotFoundException(EntidadAmbito, idAmbito);

        entidad.Activo = false;
        entidad.UsuarioMovto = Auditoria.Usuario;
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(EntidadAmbito, idAmbito, "ELIMINAR", entidad.Nombre, cancellationToken);
    }

    public async Task<(int IdProyecto, int IdTipoAmbitoRegla)?> ObtenerProyectoAmbitoAsync(
        int idAmbito, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var fila = await contexto.TblAmbitoRegla.AsNoTracking()
            .Where(a => a.IdAmbitoRegla == idAmbito && a.Activo)
            .Select(a => new { a.IdProyecto, a.IdTipoAmbitoRegla })
            .FirstOrDefaultAsync(cancellationToken);

        return fila is null ? null : (fila.IdProyecto, fila.IdTipoAmbitoRegla);
    }

    public async Task<bool> AmbitoTieneReglasAsync(
        int idAmbito, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblReglaNegocio.AsNoTracking()
            .AnyAsync(r => r.IdAmbitoRegla == idAmbito && r.Activo, cancellationToken);
    }
}
