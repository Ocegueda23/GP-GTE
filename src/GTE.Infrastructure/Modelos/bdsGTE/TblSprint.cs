using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblSprint
{
    public int IdSprint { get; set; }

    public int IdEquipo { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Objetivo { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }

    public int IdEstatusSprint { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblEquipo IdEquipoNavigation { get; set; } = null!;

    public virtual TblEstatusSprint IdEstatusSprintNavigation { get; set; } = null!;

    public virtual ICollection<TblCapacidadSprint> TblCapacidadSprint { get; set; } = new List<TblCapacidadSprint>();

    public virtual ICollection<TblWorkItem> TblWorkItem { get; set; } = new List<TblWorkItem>();
}
