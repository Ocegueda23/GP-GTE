using FluentValidation;
using MediatR;

namespace GTE.Application.Common.Behaviors;

/// <summary>
/// Pipeline de MediatR: ejecuta los validadores de FluentValidation del request
/// antes del handler. Las fallas se lanzan como ValidationException y el
/// GlobalExceptionMiddleware las convierte en HTTP 400 con detalle por campo.
/// </summary>
public sealed class ComportamientoValidacion<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validadores) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (validadores.Any())
        {
            var contexto = new ValidationContext<TRequest>(request);
            var resultados = await Task.WhenAll(
                validadores.Select(v => v.ValidateAsync(contexto, cancellationToken)));
            var fallas = resultados.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

            if (fallas.Count > 0)
            {
                throw new ValidationException(fallas);
            }
        }

        return await next();
    }
}
