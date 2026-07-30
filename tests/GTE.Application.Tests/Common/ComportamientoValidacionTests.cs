using FluentValidation;
using GTE.Application.Common.Behaviors;
using MediatR;
using Xunit;

namespace GTE.Application.Tests.Common;

public record ComandoPrueba(string Titulo) : IRequest<string>;

public class ValidadorComandoPrueba : AbstractValidator<ComandoPrueba>
{
    public ValidadorComandoPrueba()
    {
        RuleFor(c => c.Titulo).NotEmpty().WithMessage("El titulo es obligatorio.");
    }
}

public class ComportamientoValidacionTests
{
    private static ComportamientoValidacion<ComandoPrueba, string> CrearComportamiento()
    {
        return new ComportamientoValidacion<ComandoPrueba, string>([new ValidadorComandoPrueba()]);
    }

    [Fact]
    public async Task LanzaValidationException_CuandoElRequestEsInvalido()
    {
        var comportamiento = CrearComportamiento();

        var excepcion = await Assert.ThrowsAsync<ValidationException>(() =>
            comportamiento.Handle(
                new ComandoPrueba(string.Empty),
                _ => Task.FromResult("ok"),
                CancellationToken.None));

        Assert.Contains(excepcion.Errors, e => e.ErrorMessage == "El titulo es obligatorio.");
    }

    [Fact]
    public async Task ContinuaAlHandler_CuandoElRequestEsValido()
    {
        var comportamiento = CrearComportamiento();

        var resultado = await comportamiento.Handle(
            new ComandoPrueba("Titulo valido"),
            _ => Task.FromResult("ok"),
            CancellationToken.None);

        Assert.Equal("ok", resultado);
    }
}
