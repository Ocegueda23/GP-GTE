using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblWorkItemCasoPrueba
{
    public int IdWorkItemCasoPrueba { get; set; }

    public int IdWorkItem { get; set; }

    public int IdCasoPrueba { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblCasoPrueba IdCasoPruebaNavigation { get; set; } = null!;

    public virtual TblWorkItem IdWorkItemNavigation { get; set; } = null!;
}
