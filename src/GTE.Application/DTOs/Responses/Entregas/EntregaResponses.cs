namespace GTE.Application.DTOs.Responses.Entregas;

public class ReleaseResponse
{
    public int IdRelease { get; set; }
    public int IdProyecto { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public string ClaveProyecto { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Folio { get; set; }
    public string? NotasVersion { get; set; }
    public int IdEstatus { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public DateOnly? FechaPlan { get; set; }
    public DateTime? FechaLiberacion { get; set; }
    public int TotalItems { get; set; }
    public int TotalArtefactos { get; set; }
    public int AprobacionesPendientes { get; set; }
}

public class ItemReleaseResponse
{
    public int IdWorkItem { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Estatus { get; set; } = string.Empty;
}

public class ArtefactoResponse
{
    public int IdArtefacto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public int IdTipoArtefacto { get; set; }
    public string? HashSha256 { get; set; }
    public int? OrdenEjecucion { get; set; }
    public int? IdArtefactoRollback { get; set; }
    public string? NombreRollback { get; set; }
    public string? JustificacionIrreversible { get; set; }
    public bool RequiereRollback { get; set; }
    public bool CumpleRollback { get; set; }
}

public class AprobacionResponse
{
    public int IdAprobacion { get; set; }
    public string RolAprobacion { get; set; } = string.Empty;
    public int IdEstatus { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public string? Aprobador { get; set; }
    public string? Comentario { get; set; }
    public DateTime? FechaResolucion { get; set; }
    public string? FirmaHash { get; set; }
}

public class DespliegueResponse
{
    public int IdDespliegue { get; set; }
    public string Ambiente { get; set; } = string.Empty;
    public string Estatus { get; set; } = string.Empty;
    public bool EsRollback { get; set; }
    public string? Ejecutor { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string? Bitacora { get; set; }
}

/// <summary>Detalle completo del release: contenido, artefactos, firmas y despliegues.</summary>
public class ReleaseDetalleResponse : ReleaseResponse
{
    public IReadOnlyList<ItemReleaseResponse> Items { get; set; } = [];
    public IReadOnlyList<ArtefactoResponse> Artefactos { get; set; } = [];
    public IReadOnlyList<AprobacionResponse> Aprobaciones { get; set; } = [];
    public IReadOnlyList<DespliegueResponse> Despliegues { get; set; } = [];
}

/// <summary>Version viva en cada ambiente por proyecto.</summary>
public class MatrizAmbienteResponse
{
    public int IdAmbiente { get; set; }
    public string Ambiente { get; set; } = string.Empty;
    public string? ClaveProyecto { get; set; }
    public string? VersionDesplegada { get; set; }
    public DateTime? FechaDespliegue { get; set; }
}
