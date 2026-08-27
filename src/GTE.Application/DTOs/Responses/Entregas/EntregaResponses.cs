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

    /// <summary>HTML enriquecido (formato, tablas e imagenes por GUID) con el instructivo de
    /// despliegue del release; alimenta la Solicitud de despliegue imprimible.</summary>
    public string? InstruccionesImplementacion { get; set; }

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

/// <summary>Elemento que puede entrar al release (ver ObtenerCandidatosContenidoAsync).</summary>
public class CandidatoContenidoResponse
{
    public int IdWorkItem { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Hallazgos de revision sin corregir: mayor que cero lo bloquea (RN-GTE-031).</summary>
    public int HallazgosPendientes { get; set; }

    /// <summary>Sprint donde se trabajo, para poder filtrar el selector por sprint. Nulo si
    /// el elemento se termino fuera de un sprint.</summary>
    public int? IdSprint { get; set; }

    public string? Sprint { get; set; }
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

    /// <summary>Instructivo propio del artefacto, en HTML enriquecido (ver ReleaseResponse).</summary>
    public string? InstruccionesImplementacion { get; set; }

    public bool RequiereRollback { get; set; }
    public bool CumpleRollback { get; set; }
}

/// <summary>Respaldo previo al despliegue (apartado propio de la Solicitud de despliegue).</summary>
public class RespaldoResponse
{
    public int IdReleaseRespaldo { get; set; }
    public int IdTipoRespaldo { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
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
    public IReadOnlyList<RespaldoResponse> Respaldos { get; set; } = [];
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

public class ItemCoberturaResponse
{
    public int IdWorkItem { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
}

/// <summary>Un proyecto tocado por el sprint, con lo que le falta enviar a un release.</summary>
public class ProyectoCoberturaResponse
{
    public int IdProyecto { get; set; }
    public string ClaveProyecto { get; set; } = string.Empty;
    public string Proyecto { get; set; } = string.Empty;

    /// <summary>Terminados, sin hallazgos pendientes y sin release todavia: lo que se puede enviar ya.</summary>
    public IReadOnlyList<ItemCoberturaResponse> Disponibles { get; set; } = [];

    /// <summary>Terminados sin release pero con hallazgos de revision pendientes: no pueden entrar (RN-GTE-031).</summary>
    public IReadOnlyList<ItemCoberturaResponse> Bloqueados { get; set; } = [];

    /// <summary>Release En Preparacion de este proyecto, si ya existe uno (normalmente solo hay uno a la vez).</summary>
    public int? IdReleaseEnPreparacion { get; set; }
    public string? VersionEnPreparacion { get; set; }
    public string? FolioReleaseEnPreparacion { get; set; }
}

/// <summary>
/// Cobertura de release de un sprint (paso aparte tras cerrarlo, RN-GTE-018 complementaria):
/// por cada proyecto que el sprint toco, que le falta mandar a un release. Vacio = nada
/// pendiente, ya sea porque no hubo terminados o porque ya todos tienen release.
/// </summary>
public class CoberturaReleaseSprintResponse
{
    public int IdSprint { get; set; }
    public string Sprint { get; set; } = string.Empty;
    public IReadOnlyList<ProyectoCoberturaResponse> Proyectos { get; set; } = [];
}
