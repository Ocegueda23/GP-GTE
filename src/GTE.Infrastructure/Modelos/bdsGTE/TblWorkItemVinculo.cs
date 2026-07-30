using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblWorkItemVinculo
{
    public int IdWorkItemVinculo { get; set; }

    public int IdWorkItemOrigen { get; set; }

    public int IdWorkItemDestino { get; set; }

    public int IdTipoVinculo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public virtual TblTipoVinculo IdTipoVinculoNavigation { get; set; } = null!;

    public virtual TblWorkItem IdWorkItemDestinoNavigation { get; set; } = null!;

    public virtual TblWorkItem IdWorkItemOrigenNavigation { get; set; } = null!;
}
