using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblHorario
{
    public int IdHorario { get; set; }

    public string Nombre { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblDiaFestivo> TblDiaFestivo { get; set; } = new List<TblDiaFestivo>();

    public virtual ICollection<TblHorarioTramo> TblHorarioTramo { get; set; } = new List<TblHorarioTramo>();

    public virtual ICollection<TblSla> TblSla { get; set; } = new List<TblSla>();

    public virtual ICollection<TblUsuario> TblUsuario { get; set; } = new List<TblUsuario>();
}
