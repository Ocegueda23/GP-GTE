using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblDespliegue
{
    public int IdDespliegue { get; set; }

    public int IdRelease { get; set; }

    public int IdAmbiente { get; set; }

    public int IdEstatusDespliegue { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public int? IdEjecutor { get; set; }

    public bool EsRollback { get; set; }

    public string? Bitacora { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public virtual TblAmbiente IdAmbienteNavigation { get; set; } = null!;

    public virtual TblUsuario? IdEjecutorNavigation { get; set; }

    public virtual TblEstatusDespliegue IdEstatusDespliegueNavigation { get; set; } = null!;

    public virtual TblRelease IdReleaseNavigation { get; set; } = null!;
}
