using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblProceso
{
    public int IdProceso { get; set; }

    public string Proceso { get; set; } = null!;

    public string TablaEstatus { get; set; } = null!;

    public string TablaTransaccional { get; set; } = null!;

    public string ColumnaEstatus { get; set; } = null!;

    public string ColumnaPk { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblTransicion> TblTransicion { get; set; } = new List<TblTransicion>();
}
