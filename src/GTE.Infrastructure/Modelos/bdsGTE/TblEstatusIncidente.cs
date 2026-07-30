using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblEstatusIncidente
{
    public int Id { get; set; }

    public string Descripcion { get; set; } = null!;

    public int Orden { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblIncidente> TblIncidente { get; set; } = new List<TblIncidente>();
}
