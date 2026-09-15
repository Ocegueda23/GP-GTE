using GTE.Application.Interfaces;
using GTE.Domain.Calendario;
using GTE.Domain.Exceptions;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Motor unico de tiempo laborable (ICalendarioLaboral).
/// - El calculo directo delega en dbo.fnMinutosLaborales (fuente unica que tambien
///   usa spCambiarEstatus para materializar el historial).
/// - El calculo inverso (fechas limite de SLA) usa CalculadoraCalendario (dominio),
///   cuyo contrato se verifica con los mismos vectores de prueba que la funcion SQL.
/// </summary>
public class CalendarioLaboral(FabricaContexto fabrica) : ICalendarioLaboral
{
    public async Task<int> CalcularMinutosLaboralesAsync(
        DateTime inicio,
        DateTime fin,
        int idHorario,
        CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.Database
            .SqlQuery<int>($"SELECT Minutos AS [Value] FROM dbo.fnMinutosLaborales({inicio}, {fin}, {idHorario})")
            .FirstAsync(cancellationToken);
    }

    public async Task<DateTime> SumarMinutosLaboralesAsync(
        DateTime inicio,
        int minutos,
        int idHorario,
        CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var (tramos, festivos) = await ObtenerCalendarioAsync(
            contexto, idHorario, DateOnly.FromDateTime(inicio), cancellationToken);

        return CalculadoraCalendario.SumarMinutosLaborales(inicio, minutos, tramos, festivos);
    }

    public async Task<bool> EsDiaLaborableAsync(
        DateOnly fecha,
        int idHorario,
        CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var (tramos, festivos) = await ObtenerCalendarioAsync(
            contexto, idHorario, fecha, cancellationToken);

        return CalculadoraCalendario.EsDiaLaborable(fecha, tramos, festivos);
    }

    private static async Task<(IReadOnlyList<TramoHorario> Tramos, IReadOnlySet<DateOnly> Festivos)>
        ObtenerCalendarioAsync(
            DbContextGTE contexto,
            int idHorario,
            DateOnly desde,
            CancellationToken cancellationToken)
    {
        var tramos = await contexto.TblHorarioTramo.AsNoTracking()
            .Where(t => t.IdHorario == idHorario)
            .Select(t => new TramoHorario(t.DiaSemana, t.HoraInicio, t.HoraFin))
            .ToListAsync(cancellationToken);

        if (tramos.Count == 0)
        {
            throw new NotFoundException("Horario con tramos", idHorario);
        }

        var hasta = desde.AddDays(400);
        var festivos = await contexto.TblDiaFestivo.AsNoTracking()
            .Where(f => f.Activo
                        && (f.IdHorario == null || f.IdHorario == idHorario)
                        && f.Fecha >= desde && f.Fecha <= hasta)
            .Select(f => f.Fecha)
            .ToListAsync(cancellationToken);

        return (tramos, festivos.ToHashSet());
    }

    /// <summary>
    /// Version por lotes: una sola conexion, y por cada horario distinto una sola carga de
    /// tramos y festivos. El calculo se hace con CalculadoraCalendario (dominio), cuyo contrato
    /// se verifica con los mismos vectores de prueba que dbo.fnMinutosLaborales.
    /// </summary>
    public async Task<IReadOnlyDictionary<int, int>> CalcularMinutosLaboralesLoteAsync(
        IReadOnlyList<TramoLaborableSolicitado> solicitudes,
        CancellationToken cancellationToken = default)
    {
        var resultado = new Dictionary<int, int>();
        if (solicitudes.Count == 0)
        {
            return resultado;
        }

        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        foreach (var grupo in solicitudes.GroupBy(s => s.IdHorario))
        {
            var tramos = await contexto.TblHorarioTramo.AsNoTracking()
                .Where(t => t.IdHorario == grupo.Key)
                .Select(t => new TramoHorario(t.DiaSemana, t.HoraInicio, t.HoraFin))
                .ToListAsync(cancellationToken);

            if (tramos.Count == 0)
            {
                continue;
            }

            var desde = DateOnly.FromDateTime(grupo.Min(s => s.Inicio));
            var hasta = DateOnly.FromDateTime(grupo.Max(s => s.Fin));

            var festivos = (await contexto.TblDiaFestivo.AsNoTracking()
                .Where(f => f.Activo
                            && (f.IdHorario == null || f.IdHorario == grupo.Key)
                            && f.Fecha >= desde && f.Fecha <= hasta)
                .Select(f => f.Fecha)
                .ToListAsync(cancellationToken))
                .ToHashSet();

            foreach (var solicitud in grupo)
            {
                resultado[solicitud.Clave] = CalculadoraCalendario.CalcularMinutosLaborales(
                    solicitud.Inicio, solicitud.Fin, tramos, festivos);
            }
        }

        return resultado;
    }
}
