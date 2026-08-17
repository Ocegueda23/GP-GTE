namespace GTE.Application.DTOs.Responses.Workflow;

public class ProcesoWorkflowResponse
{
    public int IdProceso { get; set; }
    public string Proceso { get; set; } = string.Empty;
}

public class EstatusWorkflowResponse
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

public class TransicionWorkflowResponse
{
    public int IdEstatusOrigen { get; set; }
    public string EstatusOrigen { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public int IdEstatusDestino { get; set; }
    public string EstatusDestino { get; set; } = string.Empty;
    public string EtiquetaBoton { get; set; } = string.Empty;
    public string? RequierePermiso { get; set; }
    public bool RequiereMotivo { get; set; }
    public bool EsAccionPrincipal { get; set; }
    public int Orden { get; set; }
}

public class DefinicionWorkflowResponse
{
    public string Proceso { get; set; } = string.Empty;
    public List<EstatusWorkflowResponse> Estatus { get; set; } = [];
    public List<TransicionWorkflowResponse> Transiciones { get; set; } = [];
}

public class PermisoWorkflowResponse
{
    public string Clave { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
