namespace GTE.Application.DTOs.Request.Revisiones;

public class RevisionCrearRequest
{
    public string Comentarios { get; set; } = string.Empty;

    /// <summary>Decide si el hallazgo bloquea: S1/S2 bloquean, S3/S4 solo quedan registrados.</summary>
    public int IdSeveridad { get; set; }

    /// <summary>Solo la fija internamente RegistrarEjecucionCommand; no se expone en el formulario manual.</summary>
    public int? IdEjecucionPrueba { get; set; }
}

public class RevisionCorregirRequest
{
    /// <summary>true = marcar corregido; false = reabrir (exige permiso REV.Reabrir).</summary>
    public bool Corregido { get; set; } = true;

    /// <summary>Motivo obligatorio al reabrir y al descartar.</summary>
    public string? Motivo { get; set; }

    /// <summary>
    /// true = el hallazgo se cierra como "No es un error" en vez de como corregido.
    /// Exige REV.Descartar y motivo capturado. Solo aplica junto con Corregido = true.
    /// </summary>
    public bool EsFalsoPositivo { get; set; }
}
