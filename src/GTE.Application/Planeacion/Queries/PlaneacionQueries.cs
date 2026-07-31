using GTE.Application.DTOs.Responses.Planeacion;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Domain.Interfaces;
using MediatR;

namespace GTE.Application.Planeacion.Queries;

public record ObtenerSprintsQuery(int? IdEquipo, bool SoloAbiertos) : IRequest<IReadOnlyList<SprintResponse>>;

public class ObtenerSprintsHandler(IPlaneacionQueryService consultas)
    : IRequestHandler<ObtenerSprintsQuery, IReadOnlyList<SprintResponse>>
{
    public async Task<IReadOnlyList<SprintResponse>> Handle(
        ObtenerSprintsQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerSprintsAsync(query.IdEquipo, query.SoloAbiertos, cancellationToken);
    }
}

public record ObtenerBacklogQuery(int IdProyecto) : IRequest<BacklogResponse>;

public class ObtenerBacklogHandler(IPlaneacionQueryService consultas)
    : IRequestHandler<ObtenerBacklogQuery, BacklogResponse>
{
    public async Task<BacklogResponse> Handle(ObtenerBacklogQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerBacklogAsync(query.IdProyecto, cancellationToken);
    }
}

public record ObtenerItemsSprintQuery(int IdSprint) : IRequest<BacklogResponse>;

public class ObtenerItemsSprintHandler(IPlaneacionQueryService consultas)
    : IRequestHandler<ObtenerItemsSprintQuery, BacklogResponse>
{
    public async Task<BacklogResponse> Handle(ObtenerItemsSprintQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerItemsDeSprintAsync(query.IdSprint, cancellationToken);
    }
}

public record ObtenerTableroQuery(int IdEquipo) : IRequest<TableroResponse>;

public class ObtenerTableroHandler(
    IPlaneacionQueryService consultas,
    IPlaneacionRepository repositorio) : IRequestHandler<ObtenerTableroQuery, TableroResponse>
{
    public async Task<TableroResponse> Handle(ObtenerTableroQuery query, CancellationToken cancellationToken)
    {
        // Garantiza que el equipo tenga tablero y columnas antes de leerlo
        await repositorio.ObtenerOCrearColumnasAsync(query.IdEquipo, cancellationToken);
        return await consultas.ObtenerTableroAsync(query.IdEquipo, cancellationToken);
    }
}

public record ObtenerBurndownQuery(int IdSprint) : IRequest<IReadOnlyList<PuntoBurndownResponse>>;

public class ObtenerBurndownHandler(IPlaneacionQueryService consultas)
    : IRequestHandler<ObtenerBurndownQuery, IReadOnlyList<PuntoBurndownResponse>>
{
    public async Task<IReadOnlyList<PuntoBurndownResponse>> Handle(
        ObtenerBurndownQuery query, CancellationToken cancellationToken)
    {
        return await consultas.ObtenerBurndownAsync(query.IdSprint, cancellationToken);
    }
}

public record ObtenerCapacidadSprintQuery(int IdSprint) : IRequest<CapacidadSprintResponse>;

/// <summary>
/// Capacidad real del sprint: por cada miembro del equipo cuenta los dias laborables
/// de SU horario (turnos partidos y festivos incluidos, via ICalendarioLaboral),
/// descuenta las ausencias aprobadas y aplica su porcentaje de dedicacion.
/// </summary>
public class ObtenerCapacidadSprintHandler(
    IPlaneacionRepository repositorio,
    IPlaneacionQueryService consultas,
    ICalendarioLaboral calendario) : IRequestHandler<ObtenerCapacidadSprintQuery, CapacidadSprintResponse>
{
    private const decimal HorasPorDiaDefault = 8m;

    public async Task<CapacidadSprintResponse> Handle(
        ObtenerCapacidadSprintQuery query, CancellationToken cancellationToken)
    {
        var sprint = await repositorio.ObtenerEstadoSprintAsync(query.IdSprint, cancellationToken)
            ?? throw new NotFoundException("Sprint", query.IdSprint);

        var miembros = await repositorio.ObtenerMiembrosEquipoAsync(sprint.IdEquipo, cancellationToken);
        var ausencias = await repositorio.ObtenerAusenciasAprobadasAsync(
            sprint.IdEquipo, sprint.FechaInicio, sprint.FechaFin, cancellationToken);

        var personas = new List<CapacidadPersonaResponse>();
        foreach (var miembro in miembros)
        {
            var diasLaborables = 0;
            var diasAusente = 0;

            for (var dia = sprint.FechaInicio; dia <= sprint.FechaFin; dia = dia.AddDays(1))
            {
                var esLaborable = miembro.IdHorario.HasValue
                    && await calendario.EsDiaLaborableAsync(dia, miembro.IdHorario.Value, cancellationToken);
                if (!esLaborable)
                {
                    continue;
                }

                diasLaborables++;
                if (ausencias.Any(a => a.IdUsuario == miembro.IdUsuario
                                       && a.FechaInicio <= dia && a.FechaFin >= dia))
                {
                    diasAusente++;
                }
            }

            var dedicacion = miembro.PorcentajeDedicacion / 100m;
            personas.Add(new CapacidadPersonaResponse
            {
                IdUsuario = miembro.IdUsuario,
                Nombre = miembro.Nombre,
                DiasLaborables = diasLaborables,
                DiasAusente = diasAusente,
                HorasPorDia = HorasPorDiaDefault,
                HorasCapacidad = Math.Round((diasLaborables - diasAusente) * HorasPorDiaDefault * dedicacion, 1)
            });
        }

        var items = await consultas.ObtenerItemsDeSprintAsync(query.IdSprint, cancellationToken);
        var minutosComprometidos = items.Items.Sum(i => i.MinutosPresupuesto ?? 0);

        return new CapacidadSprintResponse
        {
            IdSprint = query.IdSprint,
            HorasCapacidad = personas.Sum(p => p.HorasCapacidad),
            HorasComprometidas = Math.Round(minutosComprometidos / 60m, 1),
            Personas = personas
        };
    }
}
