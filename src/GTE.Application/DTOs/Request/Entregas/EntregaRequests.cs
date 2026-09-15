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

public class ConfigurarCadenaAprobacionRequest
{
    /// <summary>Vacia para volver al default fijo (QA, Lider, Negocio).</summary>
    public List<string> Roles { get; set; } = [];
}

/// <summary>
/// Envio de lo terminado de un sprint a un release: o entra a uno ya En Preparacion
/// del proyecto (IdReleaseExistente), o se crea uno nuevo con VersionNueva. Exactamente
/// uno de los dos aplica.
/// </summary>
public class EnviarSprintAReleaseRequest
{
    public int IdProyecto { get; set; }
    public int? IdReleaseExistente { get; set; }
    public string? VersionNueva { get; set; }
}

public class ArtefactoAgregarRequest
{
    public string Nombre { get; set; } = string.Empty;
    public int IdTipoArtefacto { get; set; }
    public string? HashSha256 { get; set; }
    public int? OrdenEjecucion { get; set; }

    /// <summary>Artefacto que revierte a este (obligatorio en scripts SQL, RN-GTE-032).</summary>
    public int? IdArtefactoRollback { get; set; }

    /// <summary>Alternativa al rollback: explicar por que el cambio es irreversible.</summary>
    public string? JustificacionIrreversible { get; set; }

    /// <summary>HTML enriquecido con el instructivo propio de este artefacto.</summary>
    public string? InstruccionesImplementacion { get; set; }

    /// <summary>
    /// Version que se libera de este artefacto en este release. Texto libre porque conviven
    /// los 4 digitos de aplicaciones e instaladores y los 3 de procedimientos almacenados.
    /// </summary>
    public string? VersionArtefacto { get; set; }
}

/// <summary>
/// Cambios a un artefacto ya registrado. Mismo payload que el alta: la pantalla manda el
/// artefacto completo, no un parche. Solo aplica mientras el release esta En Preparacion.
/// </summary>
public class ArtefactoEditarRequest : ArtefactoAgregarRequest;

/// <summary>Cambios a un respaldo ya registrado; igual que artefactos, solo En Preparacion.</summary>
public class RespaldoEditarRequest : RespaldoAgregarRequest;

/// <summary>Lider responsable de la entrega. Nulo lo desasigna.</summary>
public class AsignarLiderRequest
{
    public int? IdLiderAsignado { get; set; }
}

/// <summary>Respaldo que se debe tomar antes de desplegar (apartado de la Solicitud).</summary>
public class RespaldoAgregarRequest
{
    public int IdTipoRespaldo { get; set; }

    /// <summary>Nombre o ubicacion exacta: la base, el servicio, el sitio o la ruta.</summary>
    public string Descripcion { get; set; } = string.Empty;
}

/// <summary>Instructivo de despliegue del release, en HTML enriquecido. Vacio lo limpia.</summary>
public class ActualizarInstruccionesRequest
{
    public string? InstruccionesImplementacion { get; set; }
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
