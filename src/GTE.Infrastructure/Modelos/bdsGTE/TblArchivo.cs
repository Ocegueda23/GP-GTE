using System;
using System.Collections.Generic;

namespace GTE.Infrastructure.Modelos.bdsGTE;

public partial class TblArchivo
{
    public int IdArchivo { get; set; }

    public Guid GuidArchivo { get; set; }

    public string NombreArchivo { get; set; } = null!;

    public string? Extension { get; set; }

    public long TamanoBytes { get; set; }

    public string RutaRelativa { get; set; } = null!;

    public string? HashSha256 { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string UsuarioRegistro { get; set; } = null!;

    public string? UsuarioMovto { get; set; }

    public DateTime? FechaMovto { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblArchivoVinculo> TblArchivoVinculo { get; set; } = new List<TblArchivoVinculo>();

    public virtual ICollection<TblArtefacto> TblArtefacto { get; set; } = new List<TblArtefacto>();
}
