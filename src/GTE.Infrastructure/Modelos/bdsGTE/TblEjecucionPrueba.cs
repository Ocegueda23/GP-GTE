using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblEjecucionPrueba
{
    public int IdEjecucionPrueba { get; set; }

    public int IdCasoPrueba { get; set; }

    public int? IdWorkItem { get; set; }

    public int IdEjecutor { get; set; }

    public int IdResultadoPrueba { get; set; }

    public DateTime FechaEjecucion { get; set; }

    public string? Observaciones { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public virtual TblCasoPrueba IdCasoPruebaNavigation { get; set; } = null!;

    public virtual TblUsuario IdEjecutorNavigation { get; set; } = null!;

    public virtual TblResultadoPrueba IdResultadoPruebaNavigation { get; set; } = null!;

    public virtual TblWorkItem? IdWorkItemNavigation { get; set; }

    public virtual ICollection<TblRevision> TblRevision { get; set; } = new List<TblRevision>();
}
