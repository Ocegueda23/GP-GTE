using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblEstatusAusencia
{
    public int Id { get; set; }

    public string Descripcion { get; set; } = null!;

    public int Orden { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblAusencia> TblAusencia { get; set; } = new List<TblAusencia>();
}
