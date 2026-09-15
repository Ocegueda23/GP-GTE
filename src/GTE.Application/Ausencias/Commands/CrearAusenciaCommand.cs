using FluentValidation;
using GTE.Application.DTOs.Request.Ausencias;
using GTE.Application.DTOs.Responses.Ausencias;
using GTE.Application.Interfaces;
using GTE.Domain.Ausencias;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Ausencias.Commands;

public record CrearAusenciaCommand(AusenciaCrearRequest Datos) : IRequest<AusenciaResponse>;

public class CrearAusenciaValidator : AbstractValidator<CrearAusenciaCommand>
{
    public CrearAusenciaValidator()
    {
        RuleFor(c => c.Datos.IdTipoAusencia).GreaterThan(0).WithMessage("El tipo de ausencia es obligatorio.");
        RuleFor(c => c.Datos.FechaInicio).NotEqual(default(DateOnly)).WithMessage("La fecha de inicio es obligatoria.");
        RuleFor(c => c.Datos.FechaFin).NotEqual(default(DateOnly)).WithMessage("La fecha de fin es obligatoria.");
        RuleFor(c => c.Datos)
            .Must(d => d.FechaFin >= d.FechaInicio)
            .WithMessage("La fecha de fin no puede ser anterior a la de inicio.");
        RuleFor(c => c.Datos.Motivo).MaximumLength(500);
    }
}

/// <summary>
/// Alta de ausencia: estatus inicial Solicitada (lo fija el backend, historial ALTA) y
/// aviso in-app a quienes pueden aprobar (A12). Registrar a nombre de otra persona exige
/// ADM.Ausencias. El traslape con otra ausencia vigente de la misma persona se rechaza
/// con 409 y el detalle de los periodos que chocan.
/// </summary>
public class CrearAusenciaHandler(
    IAusenciaRepository repositorio,
    IAusenciaQueryService consultas,
    IVerificadorPermisos permisos,
    IProveedorUsuarioActual proveedorUsuario,
    IServicioNotificaciones notificaciones) : IRequestHandler<CrearAusenciaCommand, AusenciaResponse>
{
    public async Task<AusenciaResponse> Handle(CrearAusenciaCommand command, CancellationToken cancellationToken)
    {
        var usuario = await proveedorUsuario.ObtenerAsync(cancellationToken)
            ?? throw new ForbiddenException("La identidad actual no esta registrada como usuario de GTE.");

        var idUsuario = usuario.IdUsuario;
        if (command.Datos.IdUsuario.HasValue && command.Datos.IdUsuario.Value != usuario.IdUsuario)
        {
            await permisos.ExigirPermisoAsync(PermisosAusencia.Gestionar, null, cancellationToken);
            idUsuario = command.Datos.IdUsuario.Value;
        }

        await ExigirSinTraslapeAsync(
            idUsuario, command.Datos.FechaInicio, command.Datos.FechaFin, null, cancellationToken);

        var motivo = string.IsNullOrWhiteSpace(command.Datos.Motivo) ? null : command.Datos.Motivo.Trim();

        var idAusencia = await repositorio.CrearAsync(new AusenciaNueva(
            idUsuario, command.Datos.IdTipoAusencia,
            command.Datos.FechaInicio, command.Datos.FechaFin, motivo), cancellationToken);

        var ausencia = await consultas.ObtenerPorIdAsync(idAusencia, cancellationToken)
            ?? throw new NotFoundException("Ausencia", idAusencia);

        await NotificarAprobadoresAsync(ausencia, cancellationToken);
        return ausencia;
    }

    private async Task ExigirSinTraslapeAsync(
        int idUsuario, DateOnly inicio, DateOnly fin, int? idExcluir, CancellationToken cancellationToken)
    {
        var traslapes = await repositorio.ObtenerTraslapesAsync(idUsuario, inicio, fin, idExcluir, cancellationToken);
        if (traslapes.Count > 0)
        {
            throw new ConflictException(
                "Ya existe una ausencia registrada que se cruza con ese periodo.",
                traslapes);
        }
    }

    /// <summary>A12: la ausencia solicitada avisa a quien tiene que resolverla.</summary>
    private async Task NotificarAprobadoresAsync(AusenciaResponse ausencia, CancellationToken cancellationToken)
    {
        var aprobadores = (await repositorio.ObtenerAprobadoresAsync(cancellationToken))
            .Where(id => id != ausencia.IdUsuario)
            .ToList();
        if (aprobadores.Count == 0)
        {
            return;
        }

        await notificaciones.NotificarAsync(
            aprobadores,
            $"{ausencia.Usuario} solicito una ausencia ({ausencia.Tipo})",
            $"Del {ausencia.FechaInicio:dd/MM/yyyy} al {ausencia.FechaFin:dd/MM/yyyy}.",
            "Ausencia", ausencia.IdAusencia, "/ausencias", cancellationToken);
    }
}
