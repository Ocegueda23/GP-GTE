namespace GTE.Application.Interfaces;

/// <summary>
/// Folios propios de bdsGTE (dbo.spGenerarFolio, seguro ante concurrencia).
/// La serie tipica es la clave del proyecto (GTE) o serie-anio (SOL-2026).
/// </summary>
public interface IGeneradorFolios
{
    Task<string> GenerarAsync(string serie, int digitos = 4, CancellationToken cancellationToken = default);
}
