namespace GTE.Domain.Common;

/// <summary>
/// Base para toda entidad de negocio: auditoria de alta, de movimiento y baja logica.
/// Los campos de alta nunca se modifican despues del INSERT.
/// </summary>
public abstract class EntidadAuditable
{
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public string UsuarioRegistro { get; set; } = string.Empty;
    public string? UsuarioMovto { get; set; }
    public DateTime? FechaMovto { get; set; }
    public bool Activo { get; set; } = true;
}
