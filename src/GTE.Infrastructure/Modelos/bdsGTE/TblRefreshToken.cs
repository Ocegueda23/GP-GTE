using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblRefreshToken
{
    public int IdRefreshToken { get; set; }

    public int IdUsuario { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime FechaExpiracion { get; set; }

    public DateTime? FechaRevocado { get; set; }

    public int? IdReemplazadoPor { get; set; }

    public string? IpOrigen { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public virtual TblRefreshToken? IdReemplazadoPorNavigation { get; set; }

    public virtual TblUsuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<TblRefreshToken> InverseIdReemplazadoPorNavigation { get; set; } = new List<TblRefreshToken>();
}
