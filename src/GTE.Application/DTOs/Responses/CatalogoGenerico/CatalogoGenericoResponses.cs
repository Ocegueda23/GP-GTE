namespace GTE.Application.DTOs.Responses.CatalogoGenerico;

public class CatalogoResumenResponse
{
    public int IdCatalogo { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string NombreTabla { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
}

public class ColumnaEsquemaResponse
{
    public string NombreColumna { get; set; } = string.Empty;
    public string TipoSql { get; set; } = string.Empty;
    public bool EsNulable { get; set; }
    public int? LongitudMaxima { get; set; }
    public bool EsPk { get; set; }
    public bool EsIdentity { get; set; }
}

/// <summary>Columna reconciliada: esquema real + configuracion de presentacion guardada.</summary>
public class ColumnaConfigResponse
{
    public string NombreColumna { get; set; } = string.Empty;
    public string TipoSql { get; set; } = string.Empty;
    public bool EsNulable { get; set; }
    public int? LongitudMaxima { get; set; }
    public bool EsPk { get; set; }
    public bool EsIdentity { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool EsVisible { get; set; }
    public bool EsSoloLectura { get; set; }
    public bool EsRequerido { get; set; }
    public int OrdinalPos { get; set; }
    public string? TablaFk { get; set; }
    public string? ColumnaClaveFk { get; set; }
    public string? ColumnaMostrarFk { get; set; }
    public bool EsCifrado { get; set; }
    public bool AutoFechaAlta { get; set; }
    public bool AutoFechaEdicion { get; set; }
    public bool AutoUsuarioAlta { get; set; }
    public bool AutoUsuarioEdicion { get; set; }
}

public class OpcionFkResponse
{
    public string Valor { get; set; } = string.Empty;
    public string Etiqueta { get; set; } = string.Empty;
}

public class ConfiguracionCatalogoResponse
{
    public int IdCatalogo { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string NombreTabla { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public List<ColumnaConfigResponse> Columnas { get; set; } = [];
}
