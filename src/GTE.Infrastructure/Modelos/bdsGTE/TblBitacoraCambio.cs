using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblBitacoraCambio
{
    public int IdBitacoraCambio { get; set; }

    public int IdAmbiente { get; set; }

    public string Descripcion { get; set; } = null!;

    public int? IdRelease { get; set; }

    public string Usuario { get; set; } = null!;

    public DateTime Fecha { get; set; }

    public virtual TblAmbiente IdAmbienteNavigation { get; set; } = null!;

    public virtual TblRelease? IdReleaseNavigation { get; set; }
}
