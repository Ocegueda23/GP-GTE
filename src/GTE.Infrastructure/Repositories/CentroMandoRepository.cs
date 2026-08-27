using GTE.Application.Common;
using GTE.Application.Interfaces;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

/// <summary>
/// Escritura del Centro de Mando TI: calibracion del catalogo y cierre de alertas. El
/// calculo de las evaluaciones NO pasa por aqui -- lo persiste
/// <see cref="GTE.Infrastructure.Services.MotorEvaluacionCentroMando"/>, que es quien tiene
/// el contexto completo del periodo.
/// </summary>
public class CentroMandoRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), ICentroMandoRepository
{
    public async Task<bool> ExisteIndicadorAsync(int idIndicadorGestion, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblIndicadorGestion
            .AnyAsync(i => i.IdIndicadorGestion == idIndicadorGestion, cancellationToken);
    }

    public async Task ActualizarIndicadorAsync(
        int idIndicadorGestion, decimal? meta, decimal? umbralAlerta, decimal peso,
        bool ponderaEnScore, string? accionSugerida, bool activo, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var indicador = await contexto.TblIndicadorGestion
            .FirstAsync(i => i.IdIndicadorGestion == idIndicadorGestion, cancellationToken);

        var anterior = $"Meta={indicador.Meta}; Umbral={indicador.UmbralAlerta}; Peso={indicador.Peso}; "
            + $"Pondera={indicador.PonderaEnScore}; Activo={indicador.Activo}";

        // Clave, Nombre, Ambito, Categoria, Direccion y Formula NO se tocan: los gobierna el
        // script de despliegue para que el modelo siga siendo comparable entre periodos.
        indicador.Meta = meta;
        indicador.UmbralAlerta = umbralAlerta;
        indicador.Peso = peso;
        indicador.PonderaEnScore = ponderaEnScore;
        indicador.AccionSugerida = accionSugerida;
        indicador.Activo = activo;
        indicador.UsuarioMovto = Auditoria.Usuario;
        indicador.FechaMovto = DateTime.Now;

        await contexto.SaveChangesAsync(cancellationToken);

        // Cambiar una meta o un peso reinterpreta TODO el historico del indicador, asi que
        // queda en bitacora con el valor anterior.
        await RegistrarBitacoraAsync(
            "IndicadorGestion", idIndicadorGestion, "Calibrar",
            $"{indicador.Clave}: {anterior} -> Meta={meta}; Umbral={umbralAlerta}; Peso={peso}; "
            + $"Pondera={ponderaEnScore}; Activo={activo}",
            cancellationToken);
    }

    public async Task<bool> ExisteAlertaAsync(long idAlertaGestion, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblAlertaGestion
            .AnyAsync(a => a.IdAlertaGestion == idAlertaGestion, cancellationToken);
    }

    public async Task MarcarAlertaAtendidaAsync(long idAlertaGestion, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();

        var alerta = await contexto.TblAlertaGestion
            .FirstAsync(a => a.IdAlertaGestion == idAlertaGestion, cancellationToken);

        // La alerta no se borra: el historial de que se detecto y quien la atendio es la
        // evidencia de que el problema se vio, no ruido a limpiar.
        alerta.Atendida = true;
        alerta.AtendidaPor = Auditoria.Usuario;
        alerta.FechaAtendida = DateTime.Now;
        alerta.UsuarioMovto = Auditoria.Usuario;
        alerta.FechaMovto = DateTime.Now;

        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync(
            "AlertaGestion", (int)idAlertaGestion, "Atender", alerta.Titulo, cancellationToken);
    }
}
