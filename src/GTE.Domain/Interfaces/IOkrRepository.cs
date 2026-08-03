using GTE.Domain.Okr;

namespace GTE.Domain.Interfaces;

public interface IOkrRepository
{
    /* ---------- Objetivos ---------- */
    Task<int> CrearObjetivoAsync(ObjetivoOkrNuevo datos, CancellationToken cancellationToken = default);
    Task ActualizarObjetivoAsync(ObjetivoOkrEdicion datos, CancellationToken cancellationToken = default);
    Task RetirarObjetivoAsync(int idObjetivoOkr, CancellationToken cancellationToken = default);

    /* ---------- Resultados clave ---------- */
    Task<int> CrearResultadoClaveAsync(ResultadoClaveNuevo datos, CancellationToken cancellationToken = default);
    Task ActualizarResultadoClaveAsync(ResultadoClaveEdicion datos, CancellationToken cancellationToken = default);
    Task RetirarResultadoClaveAsync(int idResultadoClave, CancellationToken cancellationToken = default);
}
