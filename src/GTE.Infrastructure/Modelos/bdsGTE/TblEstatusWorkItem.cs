using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblEstatusWorkItem
{
    public int Id { get; set; }

    public string Descripcion { get; set; } = null!;

    public int Orden { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblTableroColumna> TblTableroColumna { get; set; } = new List<TblTableroColumna>();

    public virtual ICollection<TblWorkItem> TblWorkItem { get; set; } = new List<TblWorkItem>();
}
