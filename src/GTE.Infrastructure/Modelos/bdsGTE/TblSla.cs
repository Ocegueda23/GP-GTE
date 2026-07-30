using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblSla
{
    public int IdSla { get; set; }

    public string Nombre { get; set; } = null!;

    public int IdPrioridad { get; set; }

    public int MinutosRespuesta { get; set; }

    public int MinutosResolucion { get; set; }

    public int IdHorario { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblHorario IdHorarioNavigation { get; set; } = null!;

    public virtual TblPrioridad IdPrioridadNavigation { get; set; } = null!;

    public virtual ICollection<TblTicket> TblTicket { get; set; } = new List<TblTicket>();
}
