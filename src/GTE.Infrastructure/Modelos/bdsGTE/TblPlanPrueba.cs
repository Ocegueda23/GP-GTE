using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblPlanPrueba
{
    public int IdPlanPrueba { get; set; }

    public int IdProyecto { get; set; }

    public int? IdRelease { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblProyecto IdProyectoNavigation { get; set; } = null!;

    public virtual TblRelease? IdReleaseNavigation { get; set; }

    public virtual ICollection<TblCasoPrueba> TblCasoPrueba { get; set; } = new List<TblCasoPrueba>();

    public virtual ICollection<TblCicloPrueba> TblCicloPrueba { get; set; } = new List<TblCicloPrueba>();
}
