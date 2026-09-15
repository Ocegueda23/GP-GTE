using GTE.Application.Common;
using GTE.Domain.Administracion;
using GTE.Domain.Exceptions;
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
            Administrado = datos.Administrado,
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
        entidad.Administrado = datos.Administrado;
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
            AmbitoCentroMando = datos.AmbitoCentroMando,
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
        entidad.AmbitoCentroMando = datos.AmbitoCentroMando;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Equipo", datos.IdEquipo, "EDITAR", datos.Nombre, cancellationToken);
    }

    public async Task<int> AgregarMiembroAsync(MiembroEquipoNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        // UQ_tblEquipoMiembro_EquipoUsuario es UNIQUE (IdEquipo, IdUsuario) sin importar Activo:
        // si la persona ya paso por este equipo y fue retirada (baja logica), insertar una fila
        // nueva revienta esa constraint y cae como error 500 generico. Hay que reactivar la fila
        // existente en vez de insertar.
        var existente = await contexto.TblEquipoMiembro
            .FirstOrDefaultAsync(m => m.IdEquipo == datos.IdEquipo && m.IdUsuario == datos.IdUsuario, cancellationToken);

        if (existente is not null)
        {
            if (existente.Activo)
            {
                throw new ConflictException("El usuario ya es miembro de este equipo.");
            }

            existente.Activo = true;
            existente.RolEquipo = datos.RolEquipo;
            existente.PorcentajeDedicacion = datos.PorcentajeDedicacion;
            existente.UsuarioMovto = Recortar(Auditoria.Usuario);
            existente.FechaMovto = DateTime.Now;
            await contexto.SaveChangesAsync(cancellationToken);

            await RegistrarBitacoraAsync(
                "Equipo", datos.IdEquipo, "AGREGAR_MIEMBRO", $"usuario {datos.IdUsuario}", cancellationToken);
            return existente.IdEquipoMiembro;
        }

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
    /// RN-GTE-001: sube la cadena de jefes desde idJefePropuesto (CTE recursivo parametrizado,
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

    /// <summary>
    /// Alta de una asignacion de rol (alcance global si IdProyecto es null, acotada al
    /// proyecto si trae valor). Si la misma persona ya tuvo ese rol con el mismo alcance y
    /// se le retiro, se REACTIVA la fila en vez de insertar una segunda: tblUsuarioRol no
    /// tiene UNIQUE que lo impida y dos filas activas iguales ensucian la auditoria sin
    /// cambiar los permisos efectivos.
    /// </summary>
    public async Task<int> AsignarRolAsync(RolAsignadoNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        // El caso global se consulta aparte a proposito: comparar una columna nullable
        // contra un parametro null depende de la compensacion de null semantics de EF, y
        // aqui un falso negativo insertaria un duplicado.
        var existente = datos.IdProyecto is null
            ? await contexto.TblUsuarioRol.FirstOrDefaultAsync(
                ur => ur.IdUsuario == datos.IdUsuario && ur.IdRol == datos.IdRol
                      && ur.IdProyecto == null && ur.IdEquipo == null, cancellationToken)
            : await contexto.TblUsuarioRol.FirstOrDefaultAsync(
                ur => ur.IdUsuario == datos.IdUsuario && ur.IdRol == datos.IdRol
                      && ur.IdProyecto == datos.IdProyecto && ur.IdEquipo == null, cancellationToken);

        var alcance = datos.IdProyecto is int idProyecto ? $"proyecto {idProyecto}" : "global";

        if (existente is not null)
        {
            if (existente.Activo)
            {
                return existente.IdUsuarioRol;
            }

            existente.Activo = true;
            existente.UsuarioMovto = Recortar(Auditoria.Usuario);
            existente.FechaMovto = DateTime.Now;
            await contexto.SaveChangesAsync(cancellationToken);

            await RegistrarBitacoraAsync("Usuario", datos.IdUsuario, "ASIGNAR_ROL",
                $"rol {datos.IdRol} ({alcance}, reactivado)", cancellationToken);
            return existente.IdUsuarioRol;
        }

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

        await RegistrarBitacoraAsync("Usuario", datos.IdUsuario, "ASIGNAR_ROL",
            $"rol {datos.IdRol} ({alcance})", cancellationToken);
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

        var alcance = entidad.IdProyecto is int idProyecto ? $"proyecto {idProyecto}" : "global";
        await RegistrarBitacoraAsync("Usuario", entidad.IdUsuario, "RETIRAR_ROL",
            $"rol {entidad.IdRol} ({alcance})", cancellationToken);
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

    /* ---------- Areas ---------- */

    public async Task<int> CrearAreaAsync(AreaNueva datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = new TblArea
        {
            Nombre = datos.Nombre,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblArea.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Area", entidad.IdArea, "CREAR", datos.Nombre, cancellationToken);
        return entidad.IdArea;
    }

    public async Task ActualizarAreaAsync(AreaEdicion datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblArea
            .FirstOrDefaultAsync(a => a.IdArea == datos.IdArea, cancellationToken)
            ?? throw new InvalidOperationException($"Area {datos.IdArea} no existe.");

        entidad.Nombre = datos.Nombre;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Area", datos.IdArea, "EDITAR", datos.Nombre, cancellationToken);
    }

    public async Task RetirarAreaAsync(int idArea, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblArea
            .FirstOrDefaultAsync(a => a.IdArea == idArea, cancellationToken)
            ?? throw new InvalidOperationException($"Area {idArea} no existe.");

        entidad.Activo = false;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Area", idArea, "RETIRAR", null, cancellationToken);
    }

    /* ---------- Puestos ---------- */

    public async Task<int> CrearPuestoAsync(PuestoNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = new TblPuesto
        {
            Nombre = datos.Nombre,
            IdArea = datos.IdArea,
            UsuarioRegistro = Auditoria.Usuario,
            Activo = true
        };
        contexto.TblPuesto.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Puesto", entidad.IdPuesto, "CREAR", datos.Nombre, cancellationToken);
        return entidad.IdPuesto;
    }

    public async Task ActualizarPuestoAsync(PuestoEdicion datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblPuesto
            .FirstOrDefaultAsync(p => p.IdPuesto == datos.IdPuesto, cancellationToken)
            ?? throw new InvalidOperationException($"Puesto {datos.IdPuesto} no existe.");

        entidad.Nombre = datos.Nombre;
        entidad.IdArea = datos.IdArea;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Puesto", datos.IdPuesto, "EDITAR", datos.Nombre, cancellationToken);
    }

    public async Task RetirarPuestoAsync(int idPuesto, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblPuesto
            .FirstOrDefaultAsync(p => p.IdPuesto == idPuesto, cancellationToken)
            ?? throw new InvalidOperationException($"Puesto {idPuesto} no existe.");

        entidad.Activo = false;
        entidad.UsuarioMovto = Recortar(Auditoria.Usuario);
        entidad.FechaMovto = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Puesto", idPuesto, "RETIRAR", null, cancellationToken);
    }

    private static string Recortar(string usuario) => usuario.Length > 50 ? usuario[..50] : usuario;
}
