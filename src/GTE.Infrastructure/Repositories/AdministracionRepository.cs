using GTE.Application.Common;
using GTE.Domain.Administracion;
using GTE.Domain.Interfaces;
using GTE.Domain.WorkItems;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

public class AdministracionRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), IAdministracionRepository
{
    /* ---------- Proyectos ---------- */

    public async Task<int> CrearProyectoAsync(ProyectoNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = new TblProyecto
        {
            Clave = datos.Clave,
            Nombre = datos.Nombre,
            IdPrograma = datos.IdPrograma,
            IdCategoriaProyecto = datos.IdCategoriaProyecto,
            IdEstatusProyecto = EstatusProyecto.Propuesto,
            IdResponsable = datos.IdResponsable,
            IdEquipo = datos.IdEquipo,
            FechaInicioPlan = datos.FechaInicioPlan,
            FechaFinPlan = datos.FechaFinPlan,
            EsMantenimiento = datos.EsMantenimiento,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblProyecto.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        contexto.TblHistorialEstatus.Add(new TblHistorialEstatus
        {
            Proceso = "Proyecto",
            IdRegistro = entidad.IdProyecto,
            IdEstatus = EstatusProyecto.Propuesto,
            Accion = "ALTA",
            Usuario = Auditoria.Usuario
        });
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Proyecto", entidad.IdProyecto, "CREAR", datos.Nombre, cancellationToken);
        return entidad.IdProyecto;
    }

    public async Task ActualizarProyectoAsync(ProyectoEdicion datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblProyecto
            .FirstOrDefaultAsync(p => p.IdProyecto == datos.IdProyecto, cancellationToken)
            ?? throw new InvalidOperationException($"Proyecto {datos.IdProyecto} no existe.");

        entidad.Nombre = datos.Nombre;
        entidad.IdCategoriaProyecto = datos.IdCategoriaProyecto;
        entidad.IdResponsable = datos.IdResponsable;
        entidad.IdEquipo = datos.IdEquipo;
        entidad.FechaInicioPlan = datos.FechaInicioPlan;
        entidad.FechaFinPlan = datos.FechaFinPlan;
        entidad.EsMantenimiento = datos.EsMantenimiento;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Proyecto", datos.IdProyecto, "EDITAR", datos.Nombre, cancellationToken);
    }

    public async Task<EstadoProyecto?> ObtenerEstadoProyectoAsync(int idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblProyecto.AsNoTracking()
            .Where(p => p.IdProyecto == idProyecto)
            .Select(p => new EstadoProyecto(p.IdProyecto, p.Folio, p.Clave, p.IdEstatusProyecto, p.Activo))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AsignarFolioProyectoAsync(int idProyecto, string folio, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblProyecto
            .FirstOrDefaultAsync(p => p.IdProyecto == idProyecto, cancellationToken)
            ?? throw new InvalidOperationException($"Proyecto {idProyecto} no existe.");

        entidad.Folio = folio;
        await contexto.SaveChangesAsync(cancellationToken);
        await RegistrarBitacoraAsync("Proyecto", idProyecto, "ASIGNAR_FOLIO", folio, cancellationToken);
    }

    /// <summary>
    /// Efectos propios de cada transicion: FechaInicioReal al INICIAR, FechaFinReal
    /// al CERRAR (ambas existen en tblProyecto justo para esto).
    /// </summary>
    public async Task AplicarEfectosTransicionProyectoAsync(
        int idProyecto, string accion, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblProyecto
            .FirstOrDefaultAsync(p => p.IdProyecto == idProyecto, cancellationToken)
            ?? throw new InvalidOperationException($"Proyecto {idProyecto} no existe.");

        if (accion == AccionesProyecto.Iniciar && entidad.FechaInicioReal is null)
        {
            entidad.FechaInicioReal = DateTime.Now;
        }
        if (accion == AccionesProyecto.Cerrar)
        {
            entidad.FechaFinReal = DateTime.Now;
        }

        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Proyecto", idProyecto, accion, null, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ObtenerFoliosWorkItemsAbiertosAsync(
        int idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblWorkItem.AsNoTracking()
            .Where(w => w.IdProyecto == idProyecto && w.Activo
                        && w.IdEstatusWorkItem != EstatusWorkItem.Terminado
                        && w.IdEstatusWorkItem != EstatusWorkItem.Cancelado)
            .OrderBy(w => w.IdWorkItem)
            .Select(w => w.Folio)
            .ToListAsync(cancellationToken);
    }

    /* ---------- Equipos ---------- */

    public async Task<int> CrearEquipoAsync(EquipoNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = new TblEquipo
        {
            Nombre = datos.Nombre,
            Descripcion = datos.Descripcion,
            IdLider = datos.IdLider,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblEquipo.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Equipo", entidad.IdEquipo, "CREAR", datos.Nombre, cancellationToken);
        return entidad.IdEquipo;
    }

    public async Task ActualizarEquipoAsync(EquipoEdicion datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblEquipo
            .FirstOrDefaultAsync(e => e.IdEquipo == datos.IdEquipo, cancellationToken)
            ?? throw new InvalidOperationException($"Equipo {datos.IdEquipo} no existe.");

        entidad.Nombre = datos.Nombre;
        entidad.Descripcion = datos.Descripcion;
        entidad.IdLider = datos.IdLider;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Equipo", datos.IdEquipo, "EDITAR", datos.Nombre, cancellationToken);
    }

    public async Task<int> AgregarMiembroAsync(MiembroEquipoNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = new TblEquipoMiembro
        {
            IdEquipo = datos.IdEquipo,
            IdUsuario = datos.IdUsuario,
            RolEquipo = datos.RolEquipo,
            PorcentajeDedicacion = datos.PorcentajeDedicacion,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblEquipoMiembro.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(
            "Equipo", datos.IdEquipo, "AGREGAR_MIEMBRO", $"usuario {datos.IdUsuario}", cancellationToken);
        return entidad.IdEquipoMiembro;
    }

    public async Task ActualizarMiembroAsync(MiembroEquipoEdicion datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblEquipoMiembro
            .FirstOrDefaultAsync(m => m.IdEquipoMiembro == datos.IdEquipoMiembro, cancellationToken)
            ?? throw new InvalidOperationException($"Miembro de equipo {datos.IdEquipoMiembro} no existe.");

        entidad.RolEquipo = datos.RolEquipo;
        entidad.PorcentajeDedicacion = datos.PorcentajeDedicacion;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(
            "Equipo", entidad.IdEquipo, "EDITAR_MIEMBRO", $"usuario {entidad.IdUsuario}", cancellationToken);
    }

    public async Task RetirarMiembroAsync(int idEquipoMiembro, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblEquipoMiembro
            .FirstOrDefaultAsync(m => m.IdEquipoMiembro == idEquipoMiembro, cancellationToken)
            ?? throw new InvalidOperationException($"Miembro de equipo {idEquipoMiembro} no existe.");

        entidad.Activo = false;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(
            "Equipo", entidad.IdEquipo, "RETIRAR_MIEMBRO", $"usuario {entidad.IdUsuario}", cancellationToken);
    }

    /* ---------- Usuarios ---------- */

    public async Task<int> CrearUsuarioAsync(UsuarioNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = new TblUsuario
        {
            Dominio = datos.Dominio,
            Nombre = datos.Nombre,
            Correo = datos.Correo,
            IdPuesto = datos.IdPuesto,
            IdNivel = datos.IdNivel,
            IdHorario = datos.IdHorario,
            IdJefe = datos.IdJefe,
            EsExterno = false,
            FechaAlta = DateTime.Now,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblUsuario.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Usuario", entidad.IdUsuario, "CREAR", datos.Nombre, cancellationToken);
        return entidad.IdUsuario;
    }

    public async Task ActualizarUsuarioAsync(UsuarioEdicion datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblUsuario
            .FirstOrDefaultAsync(u => u.IdUsuario == datos.IdUsuario, cancellationToken)
            ?? throw new InvalidOperationException($"Usuario {datos.IdUsuario} no existe.");

        entidad.Nombre = datos.Nombre;
        entidad.Correo = datos.Correo;
        entidad.IdPuesto = datos.IdPuesto;
        entidad.IdNivel = datos.IdNivel;
        entidad.IdHorario = datos.IdHorario;
        entidad.IdJefe = datos.IdJefe;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Usuario", datos.IdUsuario, "EDITAR", datos.Nombre, cancellationToken);
    }

    public async Task DarBajaUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblUsuario
            .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario, cancellationToken)
            ?? throw new InvalidOperationException($"Usuario {idUsuario} no existe.");

        entidad.Activo = false;
        entidad.FechaBaja = DateTime.Now;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Usuario", idUsuario, "BAJA", null, cancellationToken);
    }

    /// <summary>
    /// RN-ADM-01: sube la cadena de jefes desde idJefePropuesto (CTE recursivo parametrizado,
    /// sin SQL interpolado) y verifica si idUsuario aparece en ella; de ser asi, asignarlo
    /// formaria un ciclo. DbCommand crudo porque EF no expresa CTEs recursivos.
    /// </summary>
    public async Task<bool> FormariaCicloJerarquiaAsync(
        int idUsuario, int idJefePropuesto, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var conexion = contexto.Database.GetDbConnection();
        await contexto.Database.OpenConnectionAsync(cancellationToken);

        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            WITH Cadena AS (
                SELECT IdUsuario, IdJefe FROM dbo.tblUsuario WHERE IdUsuario = @IdJefePropuesto
                UNION ALL
                SELECT u.IdUsuario, u.IdJefe
                FROM dbo.tblUsuario u
                INNER JOIN Cadena c ON u.IdUsuario = c.IdJefe
            )
            SELECT COUNT(1) FROM Cadena WHERE IdUsuario = @IdUsuario
            """;
        comando.Parameters.Add(new SqlParameter("@IdJefePropuesto", idJefePropuesto));
        comando.Parameters.Add(new SqlParameter("@IdUsuario", idUsuario));

        var resultado = await comando.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(resultado) > 0;
    }

    /* ---------- Roles ---------- */

    public async Task<int> AsignarRolAsync(RolAsignadoNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = new TblUsuarioRol
        {
            IdUsuario = datos.IdUsuario,
            IdRol = datos.IdRol,
            IdProyecto = datos.IdProyecto,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblUsuarioRol.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Usuario", datos.IdUsuario, "ASIGNAR_ROL", $"rol {datos.IdRol}", cancellationToken);
        return entidad.IdUsuarioRol;
    }

    public async Task RetirarRolAsync(int idUsuarioRol, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblUsuarioRol
            .FirstOrDefaultAsync(ur => ur.IdUsuarioRol == idUsuarioRol, cancellationToken)
            ?? throw new InvalidOperationException($"Asignacion de rol {idUsuarioRol} no existe.");

        entidad.Activo = false;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Usuario", entidad.IdUsuario, "RETIRAR_ROL", $"rol {entidad.IdRol}", cancellationToken);
    }

    /// <summary>
    /// Reemplazo completo de tblRolPermiso para el rol en una sola llamada (guardado en
    /// lote, no un round-trip por fila): agrega lo nuevo, quita lo que ya no viene.
    /// Es una tabla de union pura (sin Activo): la baja es hard delete.
    /// </summary>
    public async Task GuardarMatrizPermisosAsync(
        int idRol, IReadOnlyList<int> idsPermiso, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var actuales = await contexto.TblRolPermiso
            .Where(rp => rp.IdRol == idRol)
            .ToListAsync(cancellationToken);

        var aQuitar = actuales.Where(rp => !idsPermiso.Contains(rp.IdPermiso)).ToList();
        contexto.TblRolPermiso.RemoveRange(aQuitar);

        var existentes = actuales.Select(rp => rp.IdPermiso).ToHashSet();
        var aAgregar = idsPermiso.Where(id => !existentes.Contains(id)).ToList();
        foreach (var idPermiso in aAgregar)
        {
            contexto.TblRolPermiso.Add(new TblRolPermiso
            {
                IdRol = idRol,
                IdPermiso = idPermiso,
                UsuarioRegistro = Auditoria.Usuario
            });
        }

        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Rol", idRol, "GUARDAR_MATRIZ_PERMISOS",
            $"{aAgregar.Count} agregado(s), {aQuitar.Count} quitado(s)", cancellationToken);
    }

    /* ---------- Horarios ---------- */

    public async Task<int> CrearHorarioAsync(HorarioNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = new TblHorario
        {
            Nombre = datos.Nombre,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblHorario.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Horario", entidad.IdHorario, "CREAR", datos.Nombre, cancellationToken);
        return entidad.IdHorario;
    }

    /// <summary>Reemplazo completo de los tramos del horario (guardado en lote).</summary>
    public async Task GuardarTramosHorarioAsync(
        int idHorario, IReadOnlyList<TramoHorario> tramos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var actuales = await contexto.TblHorarioTramo
            .Where(t => t.IdHorario == idHorario)
            .ToListAsync(cancellationToken);
        contexto.TblHorarioTramo.RemoveRange(actuales);

        foreach (var tramo in tramos)
        {
            contexto.TblHorarioTramo.Add(new TblHorarioTramo
            {
                IdHorario = idHorario,
                DiaSemana = tramo.DiaSemana,
                HoraInicio = tramo.HoraInicio,
                HoraFin = tramo.HoraFin,
                UsuarioRegistro = Auditoria.Usuario
            });
        }

        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Horario", idHorario, "GUARDAR_TRAMOS", $"{tramos.Count} tramo(s)", cancellationToken);
    }

    public async Task<int> CrearFestivoAsync(DiaFestivoNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = new TblDiaFestivo
        {
            Fecha = datos.Fecha,
            Descripcion = datos.Descripcion,
            IdHorario = datos.IdHorario,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblDiaFestivo.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Horario", datos.IdHorario, "CREAR_FESTIVO", datos.Descripcion, cancellationToken);
        return entidad.IdDiaFestivo;
    }

    public async Task RetirarFestivoAsync(int idDiaFestivo, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblDiaFestivo
            .FirstOrDefaultAsync(f => f.IdDiaFestivo == idDiaFestivo, cancellationToken)
            ?? throw new InvalidOperationException($"Dia festivo {idDiaFestivo} no existe.");

        entidad.Activo = false;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Horario", entidad.IdHorario, "RETIRAR_FESTIVO", entidad.Descripcion, cancellationToken);
    }

    /* ---------- Ambientes ---------- */

    public async Task<int> CrearAmbienteAsync(AmbienteNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = new TblAmbiente
        {
            IdProyecto = datos.IdProyecto,
            Nombre = datos.Nombre,
            Url = datos.Url,
            Servidor = datos.Servidor,
            BaseDatos = datos.BaseDatos,
            IdResponsable = datos.IdResponsable,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblAmbiente.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Ambiente", entidad.IdAmbiente, "CREAR", datos.Nombre, cancellationToken);
        return entidad.IdAmbiente;
    }

    public async Task ActualizarAmbienteAsync(AmbienteEdicion datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblAmbiente
            .FirstOrDefaultAsync(a => a.IdAmbiente == datos.IdAmbiente, cancellationToken)
            ?? throw new InvalidOperationException($"Ambiente {datos.IdAmbiente} no existe.");

        entidad.Nombre = datos.Nombre;
        entidad.Url = datos.Url;
        entidad.Servidor = datos.Servidor;
        entidad.BaseDatos = datos.BaseDatos;
        entidad.IdResponsable = datos.IdResponsable;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Ambiente", datos.IdAmbiente, "EDITAR", datos.Nombre, cancellationToken);
    }

    public async Task RetirarAmbienteAsync(int idAmbiente, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblAmbiente
            .FirstOrDefaultAsync(a => a.IdAmbiente == idAmbiente, cancellationToken)
            ?? throw new InvalidOperationException($"Ambiente {idAmbiente} no existe.");

        entidad.Activo = false;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Ambiente", idAmbiente, "RETIRAR", null, cancellationToken);
    }

    private static string Recortar(string usuario) => usuario.Length > 50 ? usuario[..50] : usuario;
}
