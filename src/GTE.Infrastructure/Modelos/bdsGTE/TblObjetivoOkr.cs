using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblObjetivoOkr
{
    public int IdObjetivoOkr { get; set; }

    public int? IdProyecto { get; set; }

    public int? IdEquipo { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public int Anio { get; set; }

    public byte Trimestre { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblEquipo? IdEquipoNavigation { get; set; }

    public virtual TblProyecto? IdProyectoNavigation { get; set; }

    public virtual ICollection<TblResultadoClave> TblResultadoClave { get; set; } = new List<TblResultadoClave>();
}
