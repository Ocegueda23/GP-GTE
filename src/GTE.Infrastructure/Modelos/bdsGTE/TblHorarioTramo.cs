using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblHorarioTramo
{
    public int IdHorarioTramo { get; set; }

    public int IdHorario { get; set; }

    public byte DiaSemana { get; set; }

    public TimeOnly HoraInicio { get; set; }

    public TimeOnly HoraFin { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public virtual TblHorario IdHorarioNavigation { get; set; } = null!;
}
