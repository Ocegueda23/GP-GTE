using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblTipoRespaldo
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public int Orden { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblReleaseRespaldo> TblReleaseRespaldo { get; set; } = new List<TblReleaseRespaldo>();
}
