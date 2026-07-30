using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class VwBandejaTrabajo
{
    public int IdWorkItem { get; set; }

    public string Folio { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public string Titulo { get; set; } = null!;

    public string ClaveProyecto { get; set; } = null!;

    public string Proyecto { get; set; } = null!;

    public bool EsMantenimiento { get; set; }

    public int IdEstatusWorkItem { get; set; }

    public string Estatus { get; set; } = null!;

    public int IdPrioridad { get; set; }

    public string Prioridad { get; set; } = null!;

    public int? IdAsignado { get; set; }

    public string? Asignado { get; set; }

    public string? Solicitante { get; set; }

    public int? IdSprint { get; set; }

    public string? Sprint { get; set; }

    public decimal? PuntosHistoria { get; set; }

    public int? MinutosPresupuesto { get; set; }

    public int? MinutosInvertidos { get; set; }

    public DateTime? FechaCompromiso { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public DateTime FechaRegistro { get; set; }

    public bool? EsVencida { get; set; }

    public int? RevisionesPendientes { get; set; }
}
