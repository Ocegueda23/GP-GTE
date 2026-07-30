namespace GTE.Application.DTOs.Request.WorkItems;

public class RegistrarTiempoRequest
{
    public DateOnly Fecha { get; set; }
    public int Minutos { get; set; }
    public string? Descripcion { get; set; }
}
