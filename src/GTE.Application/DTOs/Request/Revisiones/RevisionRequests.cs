namespace GTE.Application.DTOs.Request.Revisiones;

public class RevisionCrearRequest
{
    public string Comentarios { get; set; } = string.Empty;
}

public class RevisionCorregirRequest
{
    /// <summary>true = marcar corregido; false = reabrir (exige permiso REV.Reabrir).</summary>
    public bool Corregido { get; set; } = true;

    /// <summary>Motivo obligatorio al reabrir.</summary>
    public string? Motivo { get; set; }
}
