using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblEstatusRiesgo
{
    public int Id { get; set; }

    public string Descripcion { get; set; } = null!;

    public int Orden { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblRiesgo> TblRiesgo { get; set; } = new List<TblRiesgo>();
}
