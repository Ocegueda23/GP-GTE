namespace GTE.Application.DTOs.Request.Entregas;

public class ReleaseCrearRequest
{
    public int IdProyecto { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateOnly? FechaPlan { get; set; }
    public string? NotasVersion { get; set; }
}

public class CambiarEstatusReleaseRequest
{
    public string Accion { get; set; } = string.Empty;
    public string? Motivo { get; set; }
}

public class AgregarContenidoRequest
{
    public List<int> IdsWorkItem { get; set; } = [];
}

public class ArtefactoAgregarRequest
{
    public string Nombre { get; set; } = string.Empty;
    public int IdTipoArtefacto { get; set; }
    public string? HashSha256 { get; set; }
    public int? OrdenEjecucion { get; set; }

    /// <summary>Artefacto que revierte a este (obligatorio en scripts SQL, RN-REL-02).</summary>
    public int? IdArtefactoRollback { get; set; }

    /// <summary>Alternativa al rollback: explicar por que el cambio es irreversible.</summary>
    public string? JustificacionIrreversible { get; set; }
}

public class ResolverAprobacionRequest
{
    public bool Aprobada { get; set; }
    public string? Comentario { get; set; }
}

public class DespliegueRegistrarRequest
{
    public int IdAmbiente { get; set; }
    public bool EsRollback { get; set; }
    public string? Bitacora { get; set; }
    public bool Exitoso { get; set; } = true;
}
