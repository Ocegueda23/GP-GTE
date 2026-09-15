namespace GTE.Application.DTOs.Responses.NotasVersion;

public class RenglonNotaVersionResponse
{
    public int IdNotaVersionDetalle { get; set; }

    /// <summary>Eco del UiId que mando el front en el alta; null al releer una nota ya guardada.</summary>
    public string? UiId { get; set; }

    public int IdTipoCambioVersion { get; set; }

    /// <summary>Nombre resuelto por el back: el front pinta esto, no mantiene su propio mapa de ids.</summary>
    public string TipoCambio { get; set; } = string.Empty;

    public string? Modulo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int Orden { get; set; }
}

public class NotaVersionResponse
{
    public int IdNotaVersion { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateOnly FechaLiberacion { get; set; }
    public string? Resumen { get; set; }
    public bool Publicada { get; set; }
    public IReadOnlyList<RenglonNotaVersionResponse> Renglones { get; set; } = [];
}

/// <summary>Fila del listado de administracion: sin el arbol, solo el conteo de renglones.</summary>
public class NotaVersionListaResponse
{
    public int IdNotaVersion { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateOnly FechaLiberacion { get; set; }
    public string? Resumen { get; set; }
    public bool Publicada { get; set; }
    public int TotalRenglones { get; set; }
}

public class TipoCambioVersionResponse
{
    public int IdTipoCambioVersion { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }
}
