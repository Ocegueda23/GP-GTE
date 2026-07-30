using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblAprobacion
{
    public int IdAprobacion { get; set; }

    public string Entidad { get; set; } = null!;

    public int IdEntidad { get; set; }

    public int IdAprobador { get; set; }

    public string RolAprobacion { get; set; } = null!;

    public int IdEstatusAprobacion { get; set; }

    public DateTime? FechaResolucion { get; set; }

    public string? Comentario { get; set; }

    public string? FirmaHash { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public bool Activo { get; set; }

    public virtual TblUsuario IdAprobadorNavigation { get; set; } = null!;

    public virtual TblEstatusAprobacion IdEstatusAprobacionNavigation { get; set; } = null!;
}
