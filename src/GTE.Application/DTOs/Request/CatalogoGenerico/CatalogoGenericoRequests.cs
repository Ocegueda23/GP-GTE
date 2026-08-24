namespace GTE.Application.DTOs.Request.CatalogoGenerico;

public class CatalogoCrearRequest
{
    public string Clave { get; set; } = string.Empty;
    public string NombreTabla { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
}

public class ColumnaConfigRequest
{
    public string NombreColumna { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool EsVisible { get; set; } = true;
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

public class ActualizarConfigColumnasRequest
{
    public List<ColumnaConfigRequest> Columnas { get; set; } = [];
}

public class FiltroColumnaDiscretoRequest
{
    public string NombreColumna { get; set; } = string.Empty;
    public List<string> Valores { get; set; } = [];
}

public class FiltroFechaRequest
{
    public string NombreColumna { get; set; } = string.Empty;
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
}

/// <summary>Body de la busqueda paginada (POST .../registros/consulta): filtros combinables + paginacion.</summary>
public class ListarRegistrosRequest
{
    public string? Texto { get; set; }
    public List<FiltroColumnaDiscretoRequest> FiltrosColumna { get; set; } = [];
    public FiltroFechaRequest? Fecha { get; set; }
    public string? OrdenarPor { get; set; }
    public bool OrdenDescendente { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 50;
}

public class CrearRegistroRequest
{
    public Dictionary<string, object?> Valores { get; set; } = [];
}

public class ActualizarRegistroRequest
{
    public Dictionary<string, object?> ClavesPk { get; set; } = [];
    public Dictionary<string, object?> Valores { get; set; } = [];
}

public class EliminarRegistroRequest
{
    public Dictionary<string, object?> ClavesPk { get; set; } = [];
}

public class DescifrarValorRequest
{
    public Dictionary<string, object?> ClavesPk { get; set; } = [];
    public string NombreColumna { get; set; } = string.Empty;
}
