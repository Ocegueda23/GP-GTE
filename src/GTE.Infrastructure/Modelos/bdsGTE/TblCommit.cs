using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblCommit
{
    public int IdCommit { get; set; }

    public int IdRepositorio { get; set; }

    public string Sha { get; set; } = null!;

    public string Autor { get; set; } = null!;

    public DateTime FechaCommit { get; set; }

    public string? Mensaje { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public virtual TblRepositorio IdRepositorioNavigation { get; set; } = null!;

    public virtual ICollection<TblCommitWorkItem> TblCommitWorkItem { get; set; } = new List<TblCommitWorkItem>();
}
