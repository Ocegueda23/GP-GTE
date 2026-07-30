using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblEncuestaSatisfaccion
{
    public int IdEncuestaSatisfaccion { get; set; }

    public int IdTicket { get; set; }

    public byte Calificacion { get; set; }

    public string? Comentario { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public virtual TblTicket IdTicketNavigation { get; set; } = null!;
}
