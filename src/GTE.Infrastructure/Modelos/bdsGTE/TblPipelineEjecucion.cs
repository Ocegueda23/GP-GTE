using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblPipelineEjecucion
{
    public int IdPipelineEjecucion { get; set; }

    public int IdRepositorio { get; set; }

    public int Numero { get; set; }

    public string Tipo { get; set; } = null!;

    public string Resultado { get; set; } = null!;

    public int? IdAmbiente { get; set; }

    public int? DuracionSegundos { get; set; }

    public string? Url { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public virtual TblAmbiente? IdAmbienteNavigation { get; set; }

    public virtual TblRepositorio IdRepositorioNavigation { get; set; } = null!;

    public virtual ICollection<TblArtefacto> TblArtefacto { get; set; } = new List<TblArtefacto>();
}
