namespace GTE.Domain.Interfaces;

/// <summary>Persistencia del layout de widgets del Dashboard Ejecutivo P18 (por usuario).</summary>
public interface IIndicadoresEjecutivosRepository
{
    /// <summary>Upsert de tblDashboardLayoutUsuario: una fila por usuario.</summary>
    Task GuardarLayoutAsync(int idUsuario, string layoutJson, CancellationToken cancellationToken = default);
}
