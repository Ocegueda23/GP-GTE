using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblCasoPrueba
{
    public int IdCasoPrueba { get; set; }

    public string? Folio { get; set; }

    public int IdPlanPrueba { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Precondiciones { get; set; }

    public string? ResultadoEsperado { get; set; }

    public int IdTipoPrueba { get; set; }

    public int? IdWorkItem { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblPlanPrueba IdPlanPruebaNavigation { get; set; } = null!;

    public virtual TblTipoPrueba IdTipoPruebaNavigation { get; set; } = null!;

    public virtual TblWorkItem? IdWorkItemNavigation { get; set; }

    public virtual ICollection<TblCasoPruebaPaso> TblCasoPruebaPaso { get; set; } = new List<TblCasoPruebaPaso>();

    public virtual ICollection<TblEjecucionPrueba> TblEjecucionPrueba { get; set; } = new List<TblEjecucionPrueba>();
}
