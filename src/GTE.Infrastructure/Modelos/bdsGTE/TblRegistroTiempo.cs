using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblRegistroTiempo
{
    public int IdRegistroTiempo { get; set; }

    public int IdWorkItem { get; set; }

    public int IdUsuario { get; set; }

    public DateOnly Fecha { get; set; }

    public int Minutos { get; set; }

    public string? Descripcion { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblUsuario IdUsuarioNavigation { get; set; } = null!;

    public virtual TblWorkItem IdWorkItemNavigation { get; set; } = null!;
}
