namespace GTE.Application.DTOs.Request.Administracion;

public class HorarioCrearRequest
{
    public string Nombre { get; set; } = string.Empty;
}

public class TramoHorarioRequest
{
    public byte DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
}

public class GuardarTramosHorarioRequest
{
    public List<TramoHorarioRequest> Tramos { get; set; } = [];
}

public class DiaFestivoCrearRequest
{
    public DateOnly Fecha { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int? IdHorario { get; set; }
}
