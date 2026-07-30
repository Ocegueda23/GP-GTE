using System.Data;
using GTE.Application.Common;
using GTE.Application.Interfaces;
using GTE.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

/// <summary>Folios propios de bdsGTE via dbo.spGenerarFolio (ROWLOCK/UPDLOCK/HOLDLOCK).</summary>
public class GeneradorFolios(FabricaContexto fabrica, AuditContext auditoria) : IGeneradorFolios
{
    public async Task<string> GenerarAsync(string serie, int digitos = 4, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var conexion = contexto.Database.GetDbConnection();
        await contexto.Database.OpenConnectionAsync(cancellationToken);

        await using var comando = conexion.CreateCommand();
        comando.CommandType = CommandType.StoredProcedure;
        comando.CommandText = "dbo.spGenerarFolio";
        comando.Parameters.Add(new SqlParameter("@Serie", serie));
        comando.Parameters.Add(new SqlParameter("@Digitos", digitos));
        comando.Parameters.Add(new SqlParameter("@Usuario", auditoria.Usuario));
        var parametroFolio = new SqlParameter("@Folio", SqlDbType.NVarChar, 50)
        {
            Direction = ParameterDirection.Output
        };
        comando.Parameters.Add(parametroFolio);
        var parametroMensaje = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 4000)
        {
            Direction = ParameterDirection.Output
        };
        comando.Parameters.Add(parametroMensaje);

        await comando.ExecuteNonQueryAsync(cancellationToken);

        return parametroFolio.Value as string
            ?? throw new InvalidOperationException(
                $"spGenerarFolio no devolvio folio para la serie {serie}: {parametroMensaje.Value}");
    }
}
