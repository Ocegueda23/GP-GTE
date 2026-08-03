using FluentValidation;
using GTE.Application.DTOs.Request.Operacion;
using GTE.Application.DTOs.Responses.Operacion;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Operacion;
using MediatR;

namespace GTE.Application.Operacion.Commands;

public record VincularReleaseCausanteIncidenteCommand(int IdIncidente, VincularReleaseCausanteRequest Datos)
    : IRequest<IncidenteResponse>;

public class VincularReleaseCausanteIncidenteValidator : AbstractValidator<VincularReleaseCausanteIncidenteCommand>
{
    public VincularReleaseCausanteIncidenteValidator()
    {
        RuleFor(c => c.IdIncidente).GreaterThan(0);
        RuleFor(c => c.Datos.IdRelease).GreaterThan(0).WithMessage("El release causante es obligatorio.");
    }
}

/// <summary>
/// Vincula un release YA EXISTENTE (a diferencia del correctivo, aqui no se crea nada
/// nuevo) como causante del incidente -- insumo de DORA Change Failure Rate a futuro.
/// Se permite en cualquier estatus, incluido Cerrado (el postmortem puede identificar
/// la causa despues del cierre).
/// </summary>
public class VincularReleaseCausanteIncidenteHandler(
    IIncidenteRepository repositorio,
    IIncidenteQueryService consultas,
    IVerificadorPermisos permisos) : IRequestHandler<VincularReleaseCausanteIncidenteCommand, IncidenteResponse>
{
    public async Task<IncidenteResponse> Handle(VincularReleaseCausanteIncidenteCommand command, CancellationToken cancellationToken)
    {
        await permisos.ExigirPermisoAsync(PermisosIncidente.Gestionar, null, cancellationToken);

        var estado = await repositorio.ObtenerEstadoAsync(command.IdIncidente, cancellationToken)
            ?? throw new NotFoundException("Incidente", command.IdIncidente);

        var existe = await repositorio.ExisteReleaseEnProyectoAsync(
            command.Datos.IdRelease, estado.IdProyecto, cancellationToken);
        if (!existe)
        {
            throw new BusinessException("El release no existe o no pertenece al mismo proyecto del incidente.");
        }

        await repositorio.VincularReleaseCausanteAsync(command.IdIncidente, command.Datos.IdRelease, cancellationToken);

        return await consultas.ObtenerPorIdAsync(command.IdIncidente, cancellationToken)
            ?? throw new NotFoundException("Incidente", command.IdIncidente);
    }
}
