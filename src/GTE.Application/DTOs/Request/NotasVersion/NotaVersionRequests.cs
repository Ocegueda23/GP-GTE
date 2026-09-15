namespace GTE.Application.DTOs.Request.NotasVersion;

/// <summary>
/// Renglon de la nota. Id null = alta nueva; UiId es la llave temporal que pone el front y que
/// el back ecoa junto al Id real para rehidratar sin depender del orden (patron uiId).
/// </summary>
public class RenglonNotaVersionRequest
{
    public int? IdNotaVersionDetalle { get; set; }
    public string? UiId { get; set; }
    public int IdTipoCambioVersion { get; set; }
    public string? Modulo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int Orden { get; set; }
}

public class NotaVersionUpsertRequest
{
    public string Version { get; set; } = string.Empty;
    public DateOnly FechaLiberacion { get; set; }
    public string? Resumen { get; set; }
    public bool Publicada { get; set; }
    public IReadOnlyList<RenglonNotaVersionRequest> Renglones { get; set; } = [];
}
