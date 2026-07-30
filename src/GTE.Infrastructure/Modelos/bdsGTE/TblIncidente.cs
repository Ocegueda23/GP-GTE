using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblIncidente
{
    public int IdIncidente { get; set; }

    public string? Folio { get; set; }

    public int IdProyecto { get; set; }

    public int IdSeveridad { get; set; }

    public int IdEstatusIncidente { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateTime FechaOcurrencia { get; set; }

    public DateTime? FechaDeteccion { get; set; }

    public DateTime? FechaResolucion { get; set; }

    public int? MinutosIndisponibilidad { get; set; }

    public string? CausaRaiz { get; set; }

    public int? IdWorkItemCorrectivo { get; set; }

    public int? IdReleaseCausante { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblEstatusIncidente IdEstatusIncidenteNavigation { get; set; } = null!;

    public virtual TblProyecto IdProyectoNavigation { get; set; } = null!;

    public virtual TblRelease? IdReleaseCausanteNavigation { get; set; }

    public virtual TblSeveridad IdSeveridadNavigation { get; set; } = null!;

    public virtual TblWorkItem? IdWorkItemCorrectivoNavigation { get; set; }
}
