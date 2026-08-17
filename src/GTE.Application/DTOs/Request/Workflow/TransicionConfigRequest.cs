namespace GTE.Application.DTOs.Request.Workflow;

public class TransicionConfigRequest
{
    public int IdEstatusOrigen { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string EtiquetaBoton { get; set; } = string.Empty;
    public string? RequierePermiso { get; set; }
    public bool RequiereMotivo { get; set; }
    public bool EsAccionPrincipal { get; set; }
    public int Orden { get; set; }
}
