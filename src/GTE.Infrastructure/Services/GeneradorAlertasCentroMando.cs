using GTE.Application.Common;
using GTE.Application.Interfaces;
using GTE.Domain.CentroMando;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Genera las alertas gerenciales de un periodo a partir de las evaluaciones ya calculadas.
///
/// Corre DESPUES del motor porque lee su resultado, no los datos crudos: asi la alerta y el
/// dashboard nunca se contradicen.
///
/// DEDUPLICACION POR CLAVE: cada hallazgo tiene una clave estable
/// (tipo + equipo + periodo + indicador). Re-ejecutar el periodo no reemite lo mismo, y una
/// alerta ya atendida por el gerente no vuelve a aparecer como nueva.
///
/// TRES REGLAS, tomadas del modelo:
///   1. Umbral cruzado          -> Critica. El indicador esta peor que su umbral de alerta.
///   2. Deterioro sin cruzar    -> Atencion. Se movio mas de 4 puntos contra el mes anterior.
///   3. Causa externa detectada -> Atencion + RequiereGerencia. El diagnostico apunta a
///      Dependencia, Recursos o Prioridad: el responsable evaluado NO puede resolverlo solo,
///      asi que la alerta es para la gerencia, no para el.
/// Y una cuarta positiva: mejora sostenida, para que el sistema tambien sirva para reconocer.
/// </summary>
public class GeneradorAlertasCentroMando(
    FabricaContexto fabrica,
    IServicioNotificaciones notificaciones,
    AuditContext auditoria,
    ILogger<GeneradorAlertasCentroMando> logger) : IGeneradorAlertasCentroMando
{
    /// <summary>Puntos porcentuales de deterioro mensual que disparan una alerta de atencion.</summary>
    private const decimal DeterioroMinimo = 4m;

    /// <summary>Causas cuyo origen esta fuera del alcance del responsable evaluado.</summary>
    private static readonly string[] CausasExternas =
        [CausaRaiz.Dependencia, CausaRaiz.Recursos, CausaRaiz.Prioridad];

    public async Task<int> GenerarAsync(int anio, int mes, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var usuario = auditoria.TieneIdentidad ? auditoria.Usuario : "motor-centro-mando";

        var evaluaciones = await contexto.TblEvaluacionEquipo.AsNoTracking()
            .Where(e => e.Activo && e.Anio == anio && e.Mes == mes)
            .Select(e => new
            {
                e.IdEvaluacionEquipo,
                e.IdEquipo,
                Equipo = e.IdEquipoNavigation.Nombre,
                e.IdResponsable,
                e.ScoreGeneral,
                e.Semaforo,
            })
            .ToListAsync(cancellationToken);

        if (evaluaciones.Count == 0)
        {
            return 0;
        }

        var clavesExistentes = (await contexto.TblAlertaGestion.AsNoTracking()
            .Where(a => a.Anio == anio && a.Mes == mes)
            .Select(a => a.Clave)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var nuevas = new List<TblAlertaGestion>();

        foreach (var evaluacion in evaluaciones)
        {
            var detalle = await contexto.TblEvaluacionEquipoDetalle.AsNoTracking()
                .Where(d => d.IdEvaluacionEquipo == evaluacion.IdEvaluacionEquipo && !d.SinDatos)
                .Select(d => new
                {
                    d.IdIndicadorGestion,
                    Indicador = d.IdIndicadorGestionNavigation.Nombre,
                    d.IdIndicadorGestionNavigation.Direccion,
                    d.IdIndicadorGestionNavigation.UmbralAlerta,
                    d.IdIndicadorGestionNavigation.Unidad,
                    d.IdIndicadorGestionNavigation.AccionSugerida,
                    d.Valor,
                    d.Semaforo,
                    d.ValorPeriodoAnterior,
                })
                .ToListAsync(cancellationToken);

            foreach (var indicador in detalle)
            {
                // ---- Regla 1: umbral cruzado ----
                if (indicador.Semaforo == SemaforoCentroMando.Rojo)
                {
                    var clave = $"umbral:{evaluacion.IdEquipo}:{indicador.IdIndicadorGestion}:{anio}-{mes:00}";
                    if (clavesExistentes.Add(clave))
                    {
                        nuevas.Add(new TblAlertaGestion
                        {
                            Clave = clave,
                            Severidad = SeveridadAlerta.Critica,
                            IdEquipo = evaluacion.IdEquipo,
                            IdIndicadorGestion = indicador.IdIndicadorGestion,
                            Anio = (short)anio,
                            Mes = (byte)mes,
                            Titulo = $"{evaluacion.Equipo}: {indicador.Indicador} fuera de umbral",
                            Mensaje = $"Valor {Formatear(indicador.Valor, indicador.Unidad)} contra un umbral de "
                                + $"{Formatear(indicador.UmbralAlerta, indicador.Unidad)}. "
                                + (indicador.AccionSugerida ?? string.Empty),
                            RequiereGerencia = false,
                            UsuarioRegistro = usuario,
                            Activo = true,
                        });
                    }

                    continue;
                }

                // ---- Regla 2: deterioro sin cruzar umbral ----
                if (indicador.Valor is { } actual && indicador.ValorPeriodoAnterior is { } anterior && anterior != 0)
                {
                    var variacion = (actual - anterior) / Math.Abs(anterior) * 100m;
                    var empeoro = indicador.Direccion == DireccionIndicador.Bajar ? variacion > 0 : variacion < 0;

                    if (empeoro && Math.Abs(variacion) >= DeterioroMinimo)
                    {
                        var clave = $"deterioro:{evaluacion.IdEquipo}:{indicador.IdIndicadorGestion}:{anio}-{mes:00}";
                        if (clavesExistentes.Add(clave))
                        {
                            nuevas.Add(new TblAlertaGestion
                            {
                                Clave = clave,
                                Severidad = SeveridadAlerta.Atencion,
                                IdEquipo = evaluacion.IdEquipo,
                                IdIndicadorGestion = indicador.IdIndicadorGestion,
                                Anio = (short)anio,
                                Mes = (byte)mes,
                                Titulo = $"{evaluacion.Equipo}: {indicador.Indicador} empeorando",
                                Mensaje = $"Paso de {Formatear(anterior, indicador.Unidad)} a "
                                    + $"{Formatear(actual, indicador.Unidad)} en un mes "
                                    + $"({Math.Abs(variacion):0.#} puntos). Todavia dentro de umbral.",
                                RequiereGerencia = false,
                                UsuarioRegistro = usuario,
                                Activo = true,
                            });
                        }
                    }
                }
            }

            // ---- Regla 3: la causa esta fuera del alcance del responsable ----
            var causas = await contexto.TblDiagnosticoCausa.AsNoTracking()
                .Where(d => d.IdEvaluacionEquipo == evaluacion.IdEvaluacionEquipo
                    && CausasExternas.Contains(d.Causa))
                .Select(d => new { d.Causa, d.IndiceClave, d.Evidencia })
                .ToListAsync(cancellationToken);

            foreach (var causa in causas)
            {
                var clave = $"causa:{evaluacion.IdEquipo}:{causa.IndiceClave}:{anio}-{mes:00}";
                if (!clavesExistentes.Add(clave))
                {
                    continue;
                }

                nuevas.Add(new TblAlertaGestion
                {
                    Clave = clave,
                    Severidad = SeveridadAlerta.Atencion,
                    IdEquipo = evaluacion.IdEquipo,
                    Anio = (short)anio,
                    Mes = (byte)mes,
                    Titulo = $"{evaluacion.Equipo}: causa {causa.Causa.ToLowerInvariant()} fuera del area",
                    Mensaje = causa.Evidencia,
                    // Lo importante de esta alerta: el responsable evaluado no puede resolverlo
                    // por su cuenta, hace falta una decision de gerencia.
                    RequiereGerencia = true,
                    UsuarioRegistro = usuario,
                    Activo = true,
                });
            }

            // ---- Regla 4: mejora sostenida (reconocer, no solo senalar) ----
            var (anioAnt, mesAnt) = mes == 1 ? (anio - 1, 12) : (anio, mes - 1);
            var scoreAnterior = await contexto.TblEvaluacionEquipo.AsNoTracking()
                .Where(e => e.IdEquipo == evaluacion.IdEquipo && e.Anio == anioAnt && e.Mes == mesAnt)
                .Select(e => e.ScoreGeneral)
                .FirstOrDefaultAsync(cancellationToken);

            if (evaluacion.ScoreGeneral is { } score && scoreAnterior is { } previo
                && score - previo >= DeterioroMinimo)
            {
                var clave = $"mejora:{evaluacion.IdEquipo}:{anio}-{mes:00}";
                if (clavesExistentes.Add(clave))
                {
                    nuevas.Add(new TblAlertaGestion
                    {
                        Clave = clave,
                        Severidad = SeveridadAlerta.Positiva,
                        IdEquipo = evaluacion.IdEquipo,
                        Anio = (short)anio,
                        Mes = (byte)mes,
                        Titulo = $"{evaluacion.Equipo}: mejora de {score - previo:0.#} puntos",
                        Mensaje = $"El score paso de {previo:0.#} a {score:0.#} respecto al mes anterior.",
                        RequiereGerencia = false,
                        UsuarioRegistro = usuario,
                        Activo = true,
                    });
                }
            }
        }

        if (nuevas.Count == 0)
        {
            return 0;
        }

        contexto.TblAlertaGestion.AddRange(nuevas);
        await contexto.SaveChangesAsync(cancellationToken);

        await NotificarAsync(contexto, nuevas, evaluaciones.Select(e => e.IdResponsable), cancellationToken);

        logger.LogInformation(
            "Centro de Mando: {Total} alerta(s) generada(s) para {Anio}-{Mes:00}.", nuevas.Count, anio, mes);

        return nuevas.Count;
    }

    /// <summary>
    /// Avisa por el mecanismo generico que ya existe (tblNotificacion + SignalR + correo).
    /// Solo se notifican las criticas y las que requieren gerencia: mandar las 30 alertas de
    /// un mes entrenaria a la gente a ignorarlas.
    /// </summary>
    private async Task NotificarAsync(
        DbContextGTE contexto, List<TblAlertaGestion> alertas,
        IEnumerable<int?> responsables, CancellationToken ct)
    {
        var relevantes = alertas
            .Where(a => a.Severidad == SeveridadAlerta.Critica || a.RequiereGerencia)
            .ToList();

        if (relevantes.Count == 0)
        {
            return;
        }

        // Quien debe enterarse: los responsables evaluados y quien tenga GES.Ver (la gerencia).
        var conPermiso = await (
            from u in contexto.TblUsuario.AsNoTracking()
            where u.Activo && contexto.TblUsuarioRol.Any(ur =>
                ur.Activo && ur.IdUsuario == u.IdUsuario &&
                contexto.TblRolPermiso.Any(rp => rp.IdRol == ur.IdRol &&
                    contexto.TblPermiso.Any(p => p.IdPermiso == rp.IdPermiso
                        && p.Clave == PermisosCentroMando.Ver)))
            select u.IdUsuario)
            .ToListAsync(ct);

        var destinatarios = conPermiso
            .Concat(responsables.Where(r => r.HasValue).Select(r => r!.Value))
            .Distinct()
            .ToList();

        if (destinatarios.Count == 0)
        {
            return;
        }

        var criticas = relevantes.Count(a => a.Severidad == SeveridadAlerta.Critica);
        var gerencia = relevantes.Count(a => a.RequiereGerencia);

        var mensaje = criticas > 0 && gerencia > 0
            ? $"{criticas} alerta(s) critica(s) y {gerencia} que requieren decision de gerencia."
            : criticas > 0
                ? $"{criticas} alerta(s) critica(s) en el periodo."
                : $"{gerencia} alerta(s) requieren decision de gerencia.";

        await notificaciones.NotificarAsync(
            destinatarios,
            "Centro de Mando TI",
            mensaje,
            "AlertaGestion",
            null,
            "/centro-mando",
            ct);
    }

    private static string Formatear(decimal? valor, string unidad)
    {
        if (valor is null)
        {
            return "sin dato";
        }

        return unidad switch
        {
            "Porcentaje" => $"{valor:0.#}%",
            "Horas" => $"{valor:0.#} h",
            "Dias" => $"{valor:0.#} d",
            _ => $"{valor:0.##}",
        };
    }
}
