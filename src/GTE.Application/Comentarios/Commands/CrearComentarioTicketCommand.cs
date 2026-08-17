using System.Text.RegularExpressions;
using FluentValidation;
using GTE.Application.DTOs.Request.Comentarios;
using GTE.Application.DTOs.Responses.Comentarios;
using GTE.Application.Interfaces;
using GTE.Domain.Comentarios;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using GTE.Domain.Soporte;
using MediatR;

namespace GTE.Application.Comentarios.Commands;

public record CrearComentarioTicketCommand(int IdTicket, ComentarioCrearRequest Datos) : IRequest<ComentarioResponse>;

public class CrearComentarioTicketValidator : AbstractValidator<CrearComentarioTicketCommand>
{
    public CrearComentarioTicketValidator()
    {
        RuleFor(c => c.IdTicket).GreaterThan(0);
        RuleFor(c => c.Datos.Contenido).NotEmpty()
            .WithMessage("Escribe algo antes de comentar.");
    }
}

/// <summary>
/// Comenta sobre un Ticket: el solicitante siempre puede (es su ticket), cualquier otro
/// usuario necesita TKT.Atender. Mismo mecanismo generico (tblComentario, Entidad="Ticket")
/// que ya usa WorkItem -- ver CrearComentarioCommand.
/// </summary>
public class CrearComentarioTicketHandler(
    IComentarioRepository repositorio,
    IComentarioQueryService consultas,
    ITicketRepository tickets,
    IVerificadorPermisos permisos,
    ISanitizadorHtml sanitizador,
    IProveedorUsuarioActual proveedorUsuario,
    IServicioNotificaciones notificaciones) : IRequestHandler<CrearComentarioTicketCommand, ComentarioResponse>
{
    private static readonly Regex ExpresionMencion = new(
        "class=\"mencion\"[^>]*data-id=\"(\\d+)\"", RegexOptions.Compiled);

    public async Task<ComentarioResponse> Handle(CrearComentarioTicketCommand command, CancellationToken cancellationToken)
    {
        var estadoTicket = await tickets.ObtenerEstadoAsync(command.IdTicket, cancellationToken)
            ?? throw new NotFoundException("Ticket", command.IdTicket);

        var usuarioActual = await proveedorUsuario.ObtenerAsync(cancellationToken);
        var esSolicitante = usuarioActual?.IdUsuario == estadoTicket.IdSolicitante;
        if (!esSolicitante)
        {
            await permisos.ExigirPermisoAsync(PermisosTicket.Atender, null, cancellationToken);
        }

        if (command.Datos.IdComentarioPadre.HasValue)
        {
            var padre = await repositorio.ObtenerEstadoAsync(
                command.Datos.IdComentarioPadre.Value, cancellationToken);
            if (padre is null || !padre.Activo)
            {
                throw new NotFoundException("Comentario", command.Datos.IdComentarioPadre.Value);
            }
        }

        var html = sanitizador.Sanitizar(command.Datos.Contenido);
        if (string.IsNullOrWhiteSpace(html))
        {
            throw new BusinessException("El comentario quedo vacio despues de limpiar el formato.");
        }

        var idComentario = await repositorio.CrearAsync(
            new ComentarioNuevo("Ticket", command.IdTicket, html, command.Datos.IdComentarioPadre),
            cancellationToken);

        await NotificarMencionesAsync(html, estadoTicket, usuarioActual, cancellationToken);

        return await consultas.ObtenerPorIdAsync(idComentario, cancellationToken)
            ?? throw new NotFoundException("Comentario", idComentario);
    }

    /// <summary>Notifica a los usuarios mencionados (span.mencion[data-id]), sin auto-notificarse.</summary>
    private async Task NotificarMencionesAsync(
        string html, EstadoTicket estadoTicket, UsuarioActual? usuarioActual, CancellationToken cancellationToken)
    {
        var idsMencionados = ExpresionMencion.Matches(html)
            .Select(m => int.Parse(m.Groups[1].Value))
            .Distinct()
            .Where(id => id != usuarioActual?.IdUsuario)
            .ToList();

        if (idsMencionados.Count == 0)
        {
            return;
        }

        var quien = usuarioActual?.Nombre ?? "Alguien";
        await notificaciones.NotificarAsync(
            idsMencionados, $"{quien} te menciono en un comentario", null,
            "Ticket", estadoTicket.IdTicket, $"/tickets/{estadoTicket.Folio}", cancellationToken);
    }
}
