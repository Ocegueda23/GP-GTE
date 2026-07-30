using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblRiesgo
{
    public int IdRiesgo { get; set; }

    public int IdProyecto { get; set; }

    public string Descripcion { get; set; } = null!;

    public byte Probabilidad { get; set; }

    public byte Impacto { get; set; }

    public byte? Exposicion { get; set; }

    public string? PlanMitigacion { get; set; }

    public int? IdResponsable { get; set; }

    public int IdEstatusRiesgo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblEstatusRiesgo IdEstatusRiesgoNavigation { get; set; } = null!;

    public virtual TblProyecto IdProyectoNavigation { get; set; } = null!;

    public virtual TblUsuario? IdResponsableNavigation { get; set; }
}
