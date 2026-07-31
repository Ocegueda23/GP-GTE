namespace GTE.Domain.Administracion;

public record ProyectoNuevo(
    string Clave,
    string Nombre,
    int? IdPrograma,
    int IdCategoriaProyecto,
    int? IdResponsable,
    int? IdEquipo,
    DateTime? FechaInicioPlan,
    DateTime? FechaFinPlan,
    bool EsMantenimiento);

public record ProyectoEdicion(
    int IdProyecto,
    string Nombre,
    int IdCategoriaProyecto,
    int? IdResponsable,
    int? IdEquipo,
    DateTime? FechaInicioPlan,
    DateTime? FechaFinPlan,
    bool EsMantenimiento);

/// <summary>Estado minimo necesario para validar y aplicar una transicion de estatus.</summary>
public record EstadoProyecto(int IdProyecto, string? Folio, string Clave, int IdEstatus, bool Activo);

public record EquipoNuevo(string Nombre, string? Descripcion, int? IdLider);

public record EquipoEdicion(int IdEquipo, string Nombre, string? Descripcion, int? IdLider);

public record MiembroEquipoNuevo(int IdEquipo, int IdUsuario, string? RolEquipo, decimal PorcentajeDedicacion);

public record MiembroEquipoEdicion(int IdEquipoMiembro, string? RolEquipo, decimal PorcentajeDedicacion);

public record UsuarioNuevo(
    string Dominio,
    string Nombre,
    string? Correo,
    int? IdPuesto,
    int? IdNivel,
    int? IdHorario,
    int? IdJefe);

public record UsuarioEdicion(
    int IdUsuario,
    string Nombre,
    string? Correo,
    int? IdPuesto,
    int? IdNivel,
    int? IdHorario,
    int? IdJefe);

public record HorarioNuevo(string Nombre);

public record TramoHorario(byte DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin);

public record DiaFestivoNuevo(DateOnly Fecha, string Descripcion, int? IdHorario);

public record AmbienteNuevo(
    int? IdProyecto,
    string Nombre,
    string? Url,
    string? Servidor,
    string? BaseDatos,
    int? IdResponsable);

public record AmbienteEdicion(
    int IdAmbiente,
    string Nombre,
    string? Url,
    string? Servidor,
    string? BaseDatos,
    int? IdResponsable);

public record RolAsignadoNuevo(int IdUsuario, int IdRol, int? IdProyecto);
