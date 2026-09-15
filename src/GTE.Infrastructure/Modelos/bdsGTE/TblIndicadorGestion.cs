using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblIndicadorGestion
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

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblAlertaGestion> TblAlertaGestion { get; set; } = new List<TblAlertaGestion>();

    public virtual ICollection<TblEvaluacionEquipoDetalle> TblEvaluacionEquipoDetalle { get; set; } = new List<TblEvaluacionEquipoDetalle>();
}
