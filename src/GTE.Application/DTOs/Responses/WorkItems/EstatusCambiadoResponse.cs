namespace GTE.Application.DTOs.Responses.WorkItems;

public class EstatusCambiadoResponse
{
    public int IdEstatusAnterior { get; set; }
    public int IdEstatusNuevo { get; set; }
    public string Estatus { get; set; } = string.Empty;
}

public class AccionDisponibleResponse
{
    public string Accion { get; set; } = string.Empty;
    public string Etiqueta { get; set; } = string.Empty;
    public bool RequiereMotivo { get; set; }
    public bool EsAccionPrincipal { get; set; }
}

public class RegistroTiempoResponse
{
    public int IdRegistroTiempo { get; set; }
    public DateOnly Fecha { get; set; }
    public int Minutos { get; set; }
    public string? Descripcion { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
}
