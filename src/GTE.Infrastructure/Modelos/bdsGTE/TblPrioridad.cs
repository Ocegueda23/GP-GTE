using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblPrioridad
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblSla> TblSla { get; set; } = new List<TblSla>();

    public virtual ICollection<TblSolicitud> TblSolicitud { get; set; } = new List<TblSolicitud>();

    public virtual ICollection<TblTicket> TblTicket { get; set; } = new List<TblTicket>();

    public virtual ICollection<TblWorkItem> TblWorkItem { get; set; } = new List<TblWorkItem>();
}
