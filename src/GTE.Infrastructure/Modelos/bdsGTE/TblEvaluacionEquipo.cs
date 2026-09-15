using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblEvaluacionEquipo
{
    public int IdEvaluacionEquipo { get; set; }

    public int IdEquipo { get; set; }

    public int? IdResponsable { get; set; }

    public short Anio { get; set; }

    public byte Mes { get; set; }

    public decimal? ScoreGeneral { get; set; }

    public string? Nivel { get; set; }

    public string? Semaforo { get; set; }

    public decimal? IndiceCarga { get; set; }

    public short IndicadoresConDato { get; set; }

    public short IndicadoresTotales { get; set; }

    public DateTime FechaCalculo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblEquipo IdEquipoNavigation { get; set; } = null!;

    public virtual TblUsuario? IdResponsableNavigation { get; set; }

    public virtual ICollection<TblDiagnosticoCausa> TblDiagnosticoCausa { get; set; } = new List<TblDiagnosticoCausa>();

    public virtual ICollection<TblEvaluacionEquipoDetalle> TblEvaluacionEquipoDetalle { get; set; } = new List<TblEvaluacionEquipoDetalle>();
}
