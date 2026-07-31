namespace GTE.Application.DTOs.Responses.Administracion;

public class HorarioResponse
{
    public int IdHorario { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class TramoHorarioResponse
{
    public int IdHorarioTramo { get; set; }
    public byte DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
}

public class DiaFestivoResponse
{
    public int IdDiaFestivo { get; set; }
    public DateOnly Fecha { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int? IdHorario { get; set; }
    public string? Horario { get; set; }
}

public class HorarioDetalleResponse
{
    public int IdHorario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public List<TramoHorarioResponse> Tramos { get; set; } = [];
    public List<DiaFestivoResponse> Festivos { get; set; } = [];
}
