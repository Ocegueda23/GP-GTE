using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;

namespace GTE.Application.WorkItems;

/// <summary>
/// Un elemento que ya entro a un release Aprobado o Liberado queda congelado: no se edita,
/// no cambia de estatus y no recibe tiempo, comentarios, archivos, pruebas ni revisiones,
/// ni siquiera con WI.ModificarTerminado. El release ya se firmo (o se desplego) con ese
/// contenido; cualquier cambio posterior va como un elemento nuevo en otro release. Si el
/// release se reabre (REABRIR lo regresa a En Preparacion) el candado se levanta solo.
/// </summary>
public static class CandadoRelease
{
    public static async Task ExigirNoCongeladoAsync(
        IWorkItemRepository repositorio, int idWorkItem, CancellationToken cancellationToken)
    {
        var release = await repositorio.ObtenerReleaseCongeladoAsync(idWorkItem, cancellationToken);
        if (release is null)
        {
            return;
        }

        var situacion = release.Liberado ? "ya se libero" : "ya esta aprobado";
        throw new BusinessException(
            $"El elemento pertenece al release {release.Release}, que {situacion}; ya no se "
            + "modifica. Registra el cambio como un elemento nuevo.");
    }
}
