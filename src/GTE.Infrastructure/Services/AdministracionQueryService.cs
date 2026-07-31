using GTE.Application.DTOs.Responses.Administracion;
using GTE.Application.Interfaces;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

public class AdministracionQueryService(FabricaContexto fabrica) : IAdministracionQueryService
{
    /* ---------- Proyectos ---------- */

    public async Task<IReadOnlyList<ProyectoResponse>> ObtenerProyectosAsync(
        bool soloActivos, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var proyectos = contexto.TblProyecto.AsNoTracking().AsQueryable();
        if (soloActivos)
        {
            proyectos = proyectos.Where(p => p.Activo);
        }

        return await ProyectarProyectos(proyectos, contexto)
            .OrderByDescending(p => p.IdProyecto)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProyectoResponse?> ObtenerProyectoAsync(int idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var proyectos = contexto.TblProyecto.AsNoTracking().Where(p => p.IdProyecto == idProyecto);
        return await ProyectarProyectos(proyectos, contexto).FirstOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<ProyectoResponse> ProyectarProyectos(IQueryable<TblProyecto> proyectos, DbContextGTE contexto)
    {
        return from p in proyectos
               join c in contexto.TblCategoriaProyecto.AsNoTracking() on p.IdCategoriaProyecto equals c.Id
               join e in contexto.TblEstatusProyecto.AsNoTracking() on p.IdEstatusProyecto equals e.Id
               join prog in contexto.TblPrograma.AsNoTracking() on p.IdPrograma equals prog.IdPrograma into progs
               from prog in progs.DefaultIfEmpty()
               join resp in contexto.TblUsuario.AsNoTracking() on p.IdResponsable equals resp.IdUsuario into resps
               from resp in resps.DefaultIfEmpty()
               join eq in contexto.TblEquipo.AsNoTracking() on p.IdEquipo equals eq.IdEquipo into eqs
               from eq in eqs.DefaultIfEmpty()
               select new ProyectoResponse
               {
                   IdProyecto = p.IdProyecto,
                   Folio = p.Folio,
                   Clave = p.Clave,
                   Nombre = p.Nombre,
                   IdPrograma = p.IdPrograma,
                   Programa = prog != null ? prog.Nombre : null,
                   IdCategoriaProyecto = p.IdCategoriaProyecto,
                   CategoriaProyecto = c.Nombre,
                   IdEstatus = p.IdEstatusProyecto,
                   Estatus = e.Descripcion,
                   IdResponsable = p.IdResponsable,
                   Responsable = resp != null ? resp.Nombre : null,
                   IdEquipo = p.IdEquipo,
                   Equipo = eq != null ? eq.Nombre : null,
                   FechaInicioPlan = p.FechaInicioPlan,
                   FechaFinPlan = p.FechaFinPlan,
                   FechaInicioReal = p.FechaInicioReal,
                   FechaFinReal = p.FechaFinReal,
                   EsMantenimiento = p.EsMantenimiento
               };
    }

    /* ---------- Equipos ---------- */

    public async Task<IReadOnlyList<EquipoResponse>> ObtenerEquiposAsync(CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await (
            from e in contexto.TblEquipo.AsNoTracking()
            where e.Activo
            join l in contexto.TblUsuario.AsNoTracking() on e.IdLider equals l.IdUsuario into lideres
            from l in lideres.DefaultIfEmpty()
            orderby e.Nombre
            select new EquipoResponse
            {
                IdEquipo = e.IdEquipo,
                Nombre = e.Nombre,
                Descripcion = e.Descripcion,
                IdLider = e.IdLider,
                Lider = l != null ? l.Nombre : null,
                TotalMiembros = contexto.TblEquipoMiembro.Count(m => m.IdEquipo == e.IdEquipo && m.Activo)
            }).ToListAsync(cancellationToken);
    }

    public async Task<EquipoDetalleResponse?> ObtenerEquipoAsync(int idEquipo, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var equipo = await (
            from e in contexto.TblEquipo.AsNoTracking()
            where e.IdEquipo == idEquipo
            join l in contexto.TblUsuario.AsNoTracking() on e.IdLider equals l.IdUsuario into lideres
            from l in lideres.DefaultIfEmpty()
            select new EquipoDetalleResponse
            {
                IdEquipo = e.IdEquipo,
                Nombre = e.Nombre,
                Descripcion = e.Descripcion,
                IdLider = e.IdLider,
                Lider = l != null ? l.Nombre : null
            }).FirstOrDefaultAsync(cancellationToken);

        if (equipo is null)
        {
            return null;
        }

        equipo.Miembros = await (
            from m in contexto.TblEquipoMiembro.AsNoTracking()
            join u in contexto.TblUsuario.AsNoTracking() on m.IdUsuario equals u.IdUsuario
            where m.IdEquipo == idEquipo && m.Activo
            orderby u.Nombre
            select new MiembroEquipoResponse
            {
                IdEquipoMiembro = m.IdEquipoMiembro,
                IdUsuario = m.IdUsuario,
                Usuario = u.Nombre,
                RolEquipo = m.RolEquipo,
                PorcentajeDedicacion = m.PorcentajeDedicacion
            }).ToListAsync(cancellationToken);

        return equipo;
    }

    /* ---------- Usuarios ---------- */

    public async Task<IReadOnlyList<UsuarioResponse>> ObtenerUsuariosAsync(
        string? texto, bool soloActivos, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var usuarios = contexto.TblUsuario.AsNoTracking().AsQueryable();
        if (soloActivos)
        {
            usuarios = usuarios.Where(u => u.Activo);
        }
        if (!string.IsNullOrWhiteSpace(texto))
        {
            usuarios = usuarios.Where(u => u.Nombre.Contains(texto) || u.Dominio.Contains(texto));
        }

        return await ProyectarUsuarios(usuarios, contexto)
            .OrderBy(u => u.Nombre)
            .ToListAsync(cancellationToken);
    }

    public async Task<UsuarioResponse?> ObtenerUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var usuarios = contexto.TblUsuario.AsNoTracking().Where(u => u.IdUsuario == idUsuario);
        return await ProyectarUsuarios(usuarios, contexto).FirstOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<UsuarioResponse> ProyectarUsuarios(IQueryable<TblUsuario> usuarios, DbContextGTE contexto)
    {
        return from u in usuarios
               join puesto in contexto.TblPuesto.AsNoTracking() on u.IdPuesto equals puesto.IdPuesto into puestos
               from puesto in puestos.DefaultIfEmpty()
               join nivel in contexto.TblNivel.AsNoTracking() on u.IdNivel equals nivel.IdNivel into niveles
               from nivel in niveles.DefaultIfEmpty()
               join horario in contexto.TblHorario.AsNoTracking() on u.IdHorario equals horario.IdHorario into horarios
               from horario in horarios.DefaultIfEmpty()
               join jefe in contexto.TblUsuario.AsNoTracking() on u.IdJefe equals jefe.IdUsuario into jefes
               from jefe in jefes.DefaultIfEmpty()
               select new UsuarioResponse
               {
                   IdUsuario = u.IdUsuario,
                   Dominio = u.Dominio,
                   Nombre = u.Nombre,
                   Correo = u.Correo,
                   IdPuesto = u.IdPuesto,
                   Puesto = puesto != null ? puesto.Nombre : null,
                   IdNivel = u.IdNivel,
                   Nivel = nivel != null ? nivel.Nombre : null,
                   IdHorario = u.IdHorario,
                   Horario = horario != null ? horario.Nombre : null,
                   IdJefe = u.IdJefe,
                   Jefe = jefe != null ? jefe.Nombre : null,
                   EsExterno = u.EsExterno,
                   FechaAlta = u.FechaAlta,
                   FechaBaja = u.FechaBaja,
                   Activo = u.Activo
               };
    }

    /* ---------- Roles ---------- */

    public async Task<IReadOnlyList<RolResponse>> ObtenerRolesAsync(CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblRol.AsNoTracking()
            .Where(r => r.Activo)
            .OrderBy(r => r.Nombre)
            .Select(r => new RolResponse
            {
                IdRol = r.IdRol,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion,
                EsSistema = r.EsSistema,
                TotalPermisos = contexto.TblRolPermiso.Count(rp => rp.IdRol == r.IdRol)
            }).ToListAsync(cancellationToken);
    }

    public async Task<MatrizPermisosResponse?> ObtenerMatrizPermisosAsync(int idRol, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var rol = await contexto.TblRol.AsNoTracking()
            .Where(r => r.IdRol == idRol)
            .Select(r => new { r.IdRol, r.Nombre })
            .FirstOrDefaultAsync(cancellationToken);
        if (rol is null)
        {
            return null;
        }

        var asignados = (await contexto.TblRolPermiso.AsNoTracking()
            .Where(rp => rp.IdRol == idRol)
            .Select(rp => rp.IdPermiso)
            .ToListAsync(cancellationToken)).ToHashSet();

        var permisos = await contexto.TblPermiso.AsNoTracking()
            .Where(p => p.Activo)
            .OrderBy(p => p.Modulo).ThenBy(p => p.Clave)
            .Select(p => new PermisoMatrizItemResponse
            {
                IdPermiso = p.IdPermiso,
                Clave = p.Clave,
                Modulo = p.Modulo,
                Descripcion = p.Descripcion,
                Asignado = asignados.Contains(p.IdPermiso)
            }).ToListAsync(cancellationToken);

        return new MatrizPermisosResponse
        {
            IdRol = rol.IdRol,
            Rol = rol.Nombre,
            Permisos = permisos
        };
    }

    public async Task<IReadOnlyList<RolUsuarioResponse>> ObtenerRolesUsuarioAsync(
        int idUsuario, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await (
            from ur in contexto.TblUsuarioRol.AsNoTracking()
            where ur.IdUsuario == idUsuario && ur.Activo
            join r in contexto.TblRol.AsNoTracking() on ur.IdRol equals r.IdRol
            join p in contexto.TblProyecto.AsNoTracking() on ur.IdProyecto equals p.IdProyecto into proyectos
            from p in proyectos.DefaultIfEmpty()
            join e in contexto.TblEquipo.AsNoTracking() on ur.IdEquipo equals e.IdEquipo into equipos
            from e in equipos.DefaultIfEmpty()
            orderby r.Nombre
            select new RolUsuarioResponse
            {
                IdUsuarioRol = ur.IdUsuarioRol,
                IdRol = ur.IdRol,
                Rol = r.Nombre,
                IdProyecto = ur.IdProyecto,
                Proyecto = p != null ? p.Nombre : null,
                IdEquipo = ur.IdEquipo,
                Equipo = e != null ? e.Nombre : null
            }).ToListAsync(cancellationToken);
    }

    /* ---------- Horarios ---------- */

    public async Task<IReadOnlyList<HorarioResponse>> ObtenerHorariosAsync(CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblHorario.AsNoTracking()
            .Where(h => h.Activo)
            .OrderBy(h => h.Nombre)
            .Select(h => new HorarioResponse { IdHorario = h.IdHorario, Nombre = h.Nombre })
            .ToListAsync(cancellationToken);
    }

    public async Task<HorarioDetalleResponse?> ObtenerHorarioAsync(int idHorario, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var horario = await contexto.TblHorario.AsNoTracking()
            .Where(h => h.IdHorario == idHorario)
            .Select(h => new HorarioDetalleResponse { IdHorario = h.IdHorario, Nombre = h.Nombre })
            .FirstOrDefaultAsync(cancellationToken);
        if (horario is null)
        {
            return null;
        }

        horario.Tramos = await contexto.TblHorarioTramo.AsNoTracking()
            .Where(t => t.IdHorario == idHorario)
            .OrderBy(t => t.DiaSemana).ThenBy(t => t.HoraInicio)
            .Select(t => new TramoHorarioResponse
            {
                IdHorarioTramo = t.IdHorarioTramo,
                DiaSemana = t.DiaSemana,
                HoraInicio = t.HoraInicio,
                HoraFin = t.HoraFin
            }).ToListAsync(cancellationToken);

        var festivos = contexto.TblDiaFestivo.AsNoTracking().Where(f => f.IdHorario == idHorario && f.Activo);
        horario.Festivos = await ProyectarFestivos(festivos, contexto)
            .OrderBy(f => f.Fecha)
            .ToListAsync(cancellationToken);

        return horario;
    }

    public async Task<IReadOnlyList<DiaFestivoResponse>> ObtenerFestivosAsync(
        int? idHorario, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var festivos = contexto.TblDiaFestivo.AsNoTracking().Where(f => f.Activo).AsQueryable();
        if (idHorario.HasValue)
        {
            festivos = festivos.Where(f => f.IdHorario == idHorario.Value);
        }

        return await ProyectarFestivos(festivos, contexto)
            .OrderBy(f => f.Fecha)
            .ToListAsync(cancellationToken);
    }

    public async Task<DiaFestivoResponse?> ObtenerFestivoAsync(int idDiaFestivo, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var festivos = contexto.TblDiaFestivo.AsNoTracking().Where(f => f.IdDiaFestivo == idDiaFestivo);
        return await ProyectarFestivos(festivos, contexto).FirstOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<DiaFestivoResponse> ProyectarFestivos(IQueryable<TblDiaFestivo> festivos, DbContextGTE contexto)
    {
        return from f in festivos
               join h in contexto.TblHorario.AsNoTracking() on f.IdHorario equals h.IdHorario into horarios
               from h in horarios.DefaultIfEmpty()
               select new DiaFestivoResponse
               {
                   IdDiaFestivo = f.IdDiaFestivo,
                   Fecha = f.Fecha,
                   Descripcion = f.Descripcion,
                   IdHorario = f.IdHorario,
                   Horario = h != null ? h.Nombre : null
               };
    }

    /* ---------- Ambientes ---------- */

    public async Task<IReadOnlyList<AmbienteResponse>> ObtenerAmbientesAsync(
        int? idProyecto, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var ambientes = contexto.TblAmbiente.AsNoTracking().Where(a => a.Activo).AsQueryable();
        if (idProyecto.HasValue)
        {
            ambientes = ambientes.Where(a => a.IdProyecto == idProyecto.Value);
        }

        return await ProyectarAmbientes(ambientes, contexto)
            .OrderBy(a => a.Nombre)
            .ToListAsync(cancellationToken);
    }

    public async Task<AmbienteResponse?> ObtenerAmbienteAsync(int idAmbiente, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var ambientes = contexto.TblAmbiente.AsNoTracking().Where(a => a.IdAmbiente == idAmbiente);
        return await ProyectarAmbientes(ambientes, contexto).FirstOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<AmbienteResponse> ProyectarAmbientes(IQueryable<TblAmbiente> ambientes, DbContextGTE contexto)
    {
        return from a in ambientes
               join p in contexto.TblProyecto.AsNoTracking() on a.IdProyecto equals p.IdProyecto into proyectos
               from p in proyectos.DefaultIfEmpty()
               join r in contexto.TblUsuario.AsNoTracking() on a.IdResponsable equals r.IdUsuario into responsables
               from r in responsables.DefaultIfEmpty()
               select new AmbienteResponse
               {
                   IdAmbiente = a.IdAmbiente,
                   IdProyecto = a.IdProyecto,
                   Proyecto = p != null ? p.Nombre : null,
                   Nombre = a.Nombre,
                   Url = a.Url,
                   Servidor = a.Servidor,
                   BaseDatos = a.BaseDatos,
                   IdResponsable = a.IdResponsable,
                   Responsable = r != null ? r.Nombre : null
               };
    }
}
