using System.Data;
using GTE.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Job nocturno de Hangfire (recurrente, registrado en Program.cs) que materializa el
/// snapshot diario de KPIs personalizados en tblKpiValor via dbo.spSnapshotKpi -- la pieza
/// de infraestructura que el Dashboard Ejecutivo P18 necesitaba (Doctos/PENDIENTES.md,
/// tblKpiDefinicion/tblKpiValor sin consumidor hasta ahora). Clase plana sin dependencia de
/// Hangfire: la programacion recurrente vive en Program.cs via RecurringJob.AddOrUpdate.
/// </summary>
public class SnapshotKpiJob(FabricaContexto fabrica, ILogger<SnapshotKpiJob> logger)
{
    public async Task EjecutarAsync(CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var conexion = contexto.Database.GetDbConnection();
        await contexto.Database.OpenConnectionAsync(cancellationToken);

        await using var comando = conexion.CreateCommand();
        comando.CommandType = CommandType.StoredProcedure;
        comando.CommandText = "dbo.spSnapshotKpi";
        var parametroMensaje = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 4000)
        {
            Direction = ParameterDirection.Output
        };
        comando.Parameters.Add(parametroMensaje);

        await comando.ExecuteNonQueryAsync(cancellationToken);

        logger.LogInformation("spSnapshotKpi ejecutado: {Mensaje}", parametroMensaje.Value);
    }
}
