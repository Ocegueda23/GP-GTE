using FluentValidation;
using GTE.Application.DTOs.Request.WorkItems;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.WorkItems;
using MediatR;

namespace GTE.Application.WorkItems.Commands;

public record RegistrarTiempoCommand(int IdWorkItem, RegistrarTiempoRequest Datos) : IRequest<int>;

public class RegistrarTiempoValidator : AbstractValidator<RegistrarTiempoCommand>
{
    public RegistrarTiempoValidator()
    {
        RuleFor(c => c.IdWorkItem).GreaterThan(0);
        RuleFor(c => c.Datos.Minutos).InclusiveBetween(1, 1440)
            .WithMessage("Los minutos deben estar entre 1 y 1440 (24 horas).");
        RuleFor(c => c.Datos.Fecha).Must(f => f <= DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("No se puede registrar tiempo en el futuro.");
        RuleFor(c => c.Datos.Descripcion).MaximumLength(500);
    }
}

public class RegistrarTiempoHandler(
    IWorkItemRepository repositorio,
    IProveedorUsuarioActual proveedorUsuario) : IRequestHandler<RegistrarTiempoCommand, int>
{
    public async Task<int> Handle(RegistrarTiempoCommand command, CancellationToken cancellationToken)
    {
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");

        var estado = await repositorio.ObtenerEstadoAsync(command.IdWorkItem, cancellationToken)
            ?? throw new NotFoundException("WorkItem", command.IdWorkItem);

        if (!estado.Activo || estado.IdEstatus == EstatusWorkItem.Cancelado)
        {
            throw new BusinessException("No se puede registrar tiempo en un elemento cancelado o eliminado.");
        }

        return await repositorio.RegistrarTiempoAsync(
            command.IdWorkItem, usuario.IdUsuario, command.Datos.Fecha,
            command.Datos.Minutos, command.Datos.Descripcion, cancellationToken);
    }
}
