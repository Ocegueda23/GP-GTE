using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblFolio
{
    public int IdFolio { get; set; }

    public string Serie { get; set; } = null!;

    public int UltimoConsecutivo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }
}
