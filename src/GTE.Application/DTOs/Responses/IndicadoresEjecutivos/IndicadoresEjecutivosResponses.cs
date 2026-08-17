using GTE.Application.DTOs.Responses.Okr;

namespace GTE.Application.DTOs.Responses.IndicadoresEjecutivos;

public class LeadCycleTimeResponse
{
    public decimal LeadTimeHorasP50 { get; set; }
    public decimal LeadTimeHorasP85 { get; set; }
    public decimal CycleTimeHorasPromedio { get; set; }
    public int ItemsConsiderados { get; set; }
}

/// <summary>
/// DORA metrics (3.10 del Documento Maestro). LeadTimeCambiosSinDatos queda en true
/// mientras no exista integracion Git real (tblPullRequest/tblCommit sin consumidor,
/// ver Doctos/PENDIENTES.md "Resto de Fase 3") -- no se fabrica un numero sin esa fuente.
/// </summary>
public class DoraResponse
{
    public decimal DeploymentsPorSemana { get; set; }
    public decimal? LeadTimeCambiosHoras { get; set; }
    public bool LeadTimeCambiosSinDatos { get; set; } = true;
    public decimal ChangeFailureRatePorcentaje { get; set; }
    public decimal? MttrHoras { get; set; }
    public int DespliguesConsiderados { get; set; }
    public int IncidentesConsiderados { get; set; }
}

public class EntregaATiempoResponse
{
    public decimal Porcentaje { get; set; }
    public string Semaforo { get; set; } = string.Empty;
    public int TotalConCompromiso { get; set; }
}

/// <summary>
/// Retrabajo = tiempo invertido en items tipo Correccion sobre el tiempo total invertido.
/// ReaperturasSinDatos queda en true: no existe hoy un contador de "veces reabierto" por
/// WorkItem (se podria derivar contando transiciones de vuelta a En Proceso en
/// tblHistorialEstatus, pendiente por alcance -- mismo criterio de proxy debil documentado
/// ya en el dashboard de colaborador para Trabajo en equipo/Comunicacion).
/// </summary>
public class RetrabajoResponse
{
    public decimal Porcentaje { get; set; }
    public bool ReaperturasSinDatos { get; set; } = true;
}

public class ProductividadResponse
{
    public decimal PuntosPromedioPorPersona { get; set; }
    public int PersonasConsideradas { get; set; }
}

public class SlaEjecutivoResponse
{
    public decimal CumplimientoPorcentaje { get; set; }
    public int TicketsConsiderados { get; set; }
    public decimal? Csat { get; set; }
    public int EncuestasConsideradas { get; set; }
}

/// <summary>Semaforo de proyecto (entrega a tiempo) + costo real vs presupuesto autorizado (si existe).</summary>
public class SemaforoProyectoResponse
{
    public int IdProyecto { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Proyecto { get; set; } = string.Empty;
    public string Semaforo { get; set; } = string.Empty;
    public decimal EntregaATiempoPorcentaje { get; set; }
    public decimal? MontoAutorizado { get; set; }
    public decimal? CostoReal { get; set; }
}

public class PuntoSerieResponse
{
    public DateOnly Fecha { get; set; }
    public decimal Valor { get; set; }
}

/// <summary>Serie historica de un KPI personalizado (tblKpiDefinicion/tblKpiValor, snapshot nocturno via Hangfire).</summary>
public class KpiPersonalizadoSerieResponse
{
    public string Clave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal? Meta { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public IReadOnlyList<PuntoSerieResponse> Serie { get; set; } = [];
}

/// <summary>
/// Top riesgos (tblRiesgo). La tabla existe con workflow sembrado pero, mientras A5
/// (Portafolio: matriz de riesgos) no tenga CRUD/UI propia, normalmente vendra vacia --
/// ver Doctos/PENDIENTES.md seccion 3.2.
/// </summary>
public class RiesgoEjecutivoResponse
{
    public int IdRiesgo { get; set; }
    public int IdProyecto { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public byte Probabilidad { get; set; }
    public byte Impacto { get; set; }
    public byte Exposicion { get; set; }
    public string Estatus { get; set; } = string.Empty;
}

public class PuntoBurndownResponse
{
    public DateOnly Fecha { get; set; }
    public decimal RestanteIdeal { get; set; }
    public decimal RestanteReal { get; set; }
}

public class BurndownSprintResponse
{
    public int IdSprint { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int IdEquipo { get; set; }
    public string Equipo { get; set; } = string.Empty;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public decimal PuntosTotales { get; set; }
    public IReadOnlyList<PuntoBurndownResponse> Puntos { get; set; } = [];
}

/// <summary>
/// Payload agregado del Dashboard Ejecutivo P18 (Doctos/GTE-DocumentoMaestro.md 3.10/5.10) --
/// vista de equipo/proyecto (DORA, costo, rentabilidad, avance de OKR). Distinto del Dashboard
/// de colaborador individual (GET /api/v1/dashboard), un solo GET para toda la carga inicial.
/// </summary>
public class IndicadoresEjecutivosResponse
{
    public string Alcance { get; set; } = string.Empty; // "Global" | "Departamento"
    public int Anio { get; set; }
    public int Mes { get; set; }
    public LeadCycleTimeResponse LeadCycleTime { get; set; } = new();
    public DoraResponse Dora { get; set; } = new();
    public EntregaATiempoResponse EntregaATiempo { get; set; } = new();
    public decimal EficienciaPorcentaje { get; set; }
    public RetrabajoResponse Retrabajo { get; set; } = new();
    public ProductividadResponse Productividad { get; set; } = new();
    public SlaEjecutivoResponse Sla { get; set; } = new();
    public IReadOnlyList<SemaforoProyectoResponse> Proyectos { get; set; } = [];
    public IReadOnlyList<ObjetivoOkrResponse> Okr { get; set; } = [];
    public IReadOnlyList<KpiPersonalizadoSerieResponse> KpisPersonalizados { get; set; } = [];
    public IReadOnlyList<RiesgoEjecutivoResponse> TopRiesgos { get; set; } = [];
    public BurndownSprintResponse? BurndownSprintActivo { get; set; }
}

public class LayoutDashboardEjecutivoResponse
{
    public string? LayoutJson { get; set; }
}
