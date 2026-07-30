using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblNotificacion
{
    public long IdNotificacion { get; set; }

    public int IdUsuario { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Mensaje { get; set; }

    public string? Entidad { get; set; }

    public int? IdEntidad { get; set; }

    public string? Url { get; set; }

    public bool Leida { get; set; }

    public DateTime? FechaLeida { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual TblUsuario IdUsuarioNavigation { get; set; } = null!;
}
