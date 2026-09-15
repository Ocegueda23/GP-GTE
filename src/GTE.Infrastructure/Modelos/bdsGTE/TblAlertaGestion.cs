using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblAlertaGestion
{
    public long IdAlertaGestion { get; set; }

    public string Clave { get; set; } = null!;

    public string Severidad { get; set; } = null!;

    public int? IdEquipo { get; set; }

    public int? IdIndicadorGestion { get; set; }

    public short Anio { get; set; }

    public byte Mes { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Mensaje { get; set; }

    public bool RequiereGerencia { get; set; }

    public bool Atendida { get; set; }

    public string? AtendidaPor { get; set; }

    public DateTime? FechaAtendida { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual TblEquipo? IdEquipoNavigation { get; set; }

    public virtual TblIndicadorGestion? IdIndicadorGestionNavigation { get; set; }
}
