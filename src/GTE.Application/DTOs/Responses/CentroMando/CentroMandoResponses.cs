namespace GTE.Application.DTOs.Responses.CentroMando;

/// <summary>Una fila del catalogo de indicadores (pantalla de administracion).</summary>
public class IndicadorGestionResponse
{
    public int IdIndicadorGestion { get; set; }
    public string Clave { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public string Categoria { get; set; } = null!;
    public string Ambito { get; set; } = null!;
    public string Origen { get; set; } = null!;
    public string? Formula { get; set; }
    public string Unidad { get; set; } = null!;
    public decimal? Meta { get; set; }
    public decimal? UmbralAlerta { get; set; }
    public string Direccion { get; set; } = null!;
    public decimal Peso { get; set; }
    public bool PonderaEnScore { get; set; }
    public string Periodicidad { get; set; } = null!;
    public string? InterpretacionBuena { get; set; }
    public string? InterpretacionMala { get; set; }
    public string? AccionSugerida { get; set; }
    public bool Activo { get; set; }
}

/// <summary>Valor de un indicador dentro de la evaluacion de un responsable.</summary>
public class IndicadorEvaluadoResponse
{
    public int IdIndicadorGestion { get; set; }
    public string Clave { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Categoria { get; set; } = null!;
    public string Ambito { get; set; } = null!;
    public string Origen { get; set; } = null!;
    public string Unidad { get; set; } = null!;
    public decimal? Valor { get; set; }
    public decimal? ValorNormalizado { get; set; }
    public decimal? Meta { get; set; }
    public decimal? UmbralAlerta { get; set; }
    public string Direccion { get; set; } = null!;
    public decimal Peso { get; set; }
    public bool PonderaEnScore { get; set; }
    public string? Semaforo { get; set; }
    public bool SinDatos { get; set; }
    public decimal? ValorPeriodoAnterior { get; set; }

    /// <summary>Mejora / Empeora / Estable / null cuando no hay periodo anterior con dato.</summary>
    public string? Tendencia { get; set; }

    public string? AccionSugerida { get; set; }
    public string? InterpretacionMala { get; set; }
}

/// <summary>Una causa detectada por el modelo de diagnostico.</summary>
public class CausaDiagnosticoResponse
{
    public string Causa { get; set; } = null!;
    public string IndiceClave { get; set; } = null!;
    public decimal? Valor { get; set; }
    public decimal? Umbral { get; set; }
    public string Evidencia { get; set; } = null!;
}

/// <summary>Analisis de carga del equipo en el periodo.</summary>
public class CargaEquipoResponse
{
    public decimal HorasDisponibles { get; set; }
    public decimal HorasAsignadas { get; set; }
    public decimal HorasEjecutadas { get; set; }
    public decimal? IndiceCarga { get; set; }

    /// <summary>Subutilizado / Adecuado / SobrecargaModerada / SobrecargaCritica.</summary>
    public string? Situacion { get; set; }

    public int Integrantes { get; set; }
}

/// <summary>Evaluacion mensual completa de un responsable.</summary>
public class EvaluacionResponsableResponse
{
    public int IdEquipo { get; set; }
    public string Equipo { get; set; } = null!;
    public string? Ambito { get; set; }
    public int? IdResponsable { get; set; }
    public string? Responsable { get; set; }
    public string? UrlFoto { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal? ScoreGeneral { get; set; }
    public decimal? ScoreMesAnterior { get; set; }
    public string? Nivel { get; set; }
    public string? Semaforo { get; set; }
    public int IndicadoresConDato { get; set; }
    public int IndicadoresTotales { get; set; }
    public CargaEquipoResponse? Carga { get; set; }
    public IReadOnlyList<IndicadorEvaluadoResponse> Indicadores { get; set; } = [];
    public IReadOnlyList<CausaDiagnosticoResponse> Diagnostico { get; set; } = [];

    /// <summary>Conclusion automatica en una frase, lista para mostrarse bajo el score.</summary>
    public string? Conclusion { get; set; }
}

/// <summary>Una dimension transversal del IT Health Score.</summary>
public class DimensionSaludResponse
{
    public string Dimension { get; set; } = null!;
    public decimal Peso { get; set; }
    public decimal? Valor { get; set; }
    public string? Semaforo { get; set; }
}

public class AlertaGestionResponse
{
    public long IdAlertaGestion { get; set; }
    public string Clave { get; set; } = null!;
    public string Severidad { get; set; } = null!;
    public int? IdEquipo { get; set; }
    public string? Equipo { get; set; }
    public string? Indicador { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public string Titulo { get; set; } = null!;
    public string? Mensaje { get; set; }
    public bool RequiereGerencia { get; set; }
    public bool Atendida { get; set; }
    public DateTime FechaRegistro { get; set; }
}

/// <summary>Un punto de la tendencia mensual del score.</summary>
public class PuntoTendenciaCentroMandoResponse
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal? Valor { get; set; }
}

public class TendenciaCentroMandoResponse
{
    public string Serie { get; set; } = null!;
    public int? IdEquipo { get; set; }
    public IReadOnlyList<PuntoTendenciaCentroMandoResponse> Puntos { get; set; } = [];
}

/// <summary>Payload del dashboard ejecutivo: todo lo que se revisa en menos de dos minutos.</summary>
public class CentroMandoResponse
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal? SaludTi { get; set; }
    public string? SaludSemaforo { get; set; }
    public string? SaludNivel { get; set; }

    /// <summary>True cuando la regla de piso topo el score porque algo critico esta en rojo.</summary>
    public bool SaludTopada { get; set; }

    public string? RazonTope { get; set; }
    public IReadOnlyList<DimensionSaludResponse> Dimensiones { get; set; } = [];
    public IReadOnlyList<EvaluacionResponsableResponse> Responsables { get; set; } = [];
    public IReadOnlyList<AlertaGestionResponse> Alertas { get; set; } = [];
    public IReadOnlyList<TendenciaCentroMandoResponse> Tendencias { get; set; } = [];
    public DateTime? FechaUltimoCalculo { get; set; }
}
