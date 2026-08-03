using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblEquipo
{
    public int IdEquipo { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public int? IdLider { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblUsuario? IdLiderNavigation { get; set; }

    public virtual ICollection<TblEquipoMiembro> TblEquipoMiembro { get; set; } = new List<TblEquipoMiembro>();

    public virtual ICollection<TblObjetivoOkr> TblObjetivoOkr { get; set; } = new List<TblObjetivoOkr>();

    public virtual ICollection<TblProyecto> TblProyecto { get; set; } = new List<TblProyecto>();

    public virtual ICollection<TblSprint> TblSprint { get; set; } = new List<TblSprint>();

    public virtual ICollection<TblTablero> TblTablero { get; set; } = new List<TblTablero>();

    public virtual ICollection<TblUsuarioRol> TblUsuarioRol { get; set; } = new List<TblUsuarioRol>();

    public virtual ICollection<TblWorkItem> TblWorkItem { get; set; } = new List<TblWorkItem>();
}
