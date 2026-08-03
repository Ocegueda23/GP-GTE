using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblLocacion
{
    public int IdLocacion { get; set; }

    public string? Locacion { get; set; }

    public string? Descripcion { get; set; }

    public bool? Activo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public virtual ICollection<TblTicket> TblTicket { get; set; } = new List<TblTicket>();
}
