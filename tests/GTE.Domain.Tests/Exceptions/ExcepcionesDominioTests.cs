using GTE.Domain.Exceptions;
using Xunit;

namespace GTE.Domain.Tests.Exceptions;

public class ExcepcionesDominioTests
{
    [Fact]
    public void NotFound_IncluyeEntidadEIdentificadorEnElMensaje()
    {
        var excepcion = new NotFoundException("WorkItem", 482);

        Assert.Contains("WorkItem", excepcion.Message);
        Assert.Contains("482", excepcion.Message);
    }

    [Fact]
    public void Conflict_ConservaElDetalleEstructurado()
    {
        var detalle = new { revisionesPendientes = 2 };

        var excepcion = new ConflictException("No se puede terminar el elemento.", detalle);

        Assert.Same(detalle, excepcion.Detalle);
    }

    [Fact]
    public void Conflict_SinDetalle_ExponeNulo()
    {
        var excepcion = new ConflictException("Conflicto simple.");

        Assert.Null(excepcion.Detalle);
    }
}
