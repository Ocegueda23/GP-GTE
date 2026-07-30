using System.Data;
using GTE.Application.Common;
using GTE.Application.Interfaces;
using GTE.Domain.Exceptions;
using GTE.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Implementacion del motor de workflow: unica puerta de cambio de estatus.
/// Invoca dbo.spCambiarEstatus (todo en bdsGTE - independencia total, ADR-03)
/// via DbCommand con ParameterDirection.ReturnValue (los SPs con valor de
/// retorno no funcionan con ExecuteSqlRaw - trampa conocida del ecosistema).
/// </summary>
public class MotorWorkflow(FabricaContexto fabrica, AuditContext auditoria) : IMotorWorkflow
{
    public async Task<ResultadoTransicion> EjecutarAccionAsync(
        string proceso,
        int idRegistro,
        string accion,
        string? motivo = null,
        int? idHorario = null,
        CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var configuracion = await ObtenerProcesoAsync(contexto, proceso, cancellationToken);
        var estatusActual = await LeerEstatusActualAsync(contexto, configuracion, idRegistro, cancellationToken);

        var transicion = await contexto.TblTransicion.AsNoTracking()
            .FirstOrDefaultAsync(t => t.IdProceso == configuracion.IdProceso
                                      && t.IdEstatusOrigen == estatusActual
                                      && t.Accion == accion
                                      && t.Activo, cancellationToken)
            ?? throw new BusinessException(
                $"La accion {accion} no esta permitida desde el estatus actual.");

        var conexion = contexto.Database.GetDbConnection();
        await contexto.Database.OpenConnectionAsync(cancellationToken);

        await using var comando = conexion.CreateCommand();
        comando.CommandType = CommandType.StoredProcedure;
        comando.CommandText = "dbo.spCambiarEstatus";
        comando.Parameters.Add(new SqlParameter("@Proceso", proceso));
        comando.Parameters.Add(new SqlParameter("@IdRegistro", idRegistro));
        comando.Parameters.Add(new SqlParameter("@IdEstatusActual", estatusActual));
        comando.Parameters.Add(new SqlParameter("@Accion", accion));
        comando.Parameters.Add(new SqlParameter("@Usuario", auditoria.Usuario));
        comando.Parameters.Add(new SqlParameter("@Motivo", (object?)motivo ?? DBNull.Value));
        comando.Parameters.Add(new SqlParameter("@IdHorario", (object?)idHorario ?? DBNull.Value));
        var parametroMensaje = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 4000)
        {
            Direction = ParameterDirection.Output
        };
        comando.Parameters.Add(parametroMensaje);
        var parametroRetorno = new SqlParameter("@ReturnValue", SqlDbType.Int)
        {
            Direction = ParameterDirection.ReturnValue
        };
        comando.Parameters.Add(parametroRetorno);

        await comando.ExecuteNonQueryAsync(cancellationToken);

        var retorno = (int)(parametroRetorno.Value ?? -1);
        var mensaje = parametroMensaje.Value as string ?? "Sin mensaje del motor de estatus.";

        switch (retorno)
        {
            case 0:
                break;
            case 52:
                throw new ConflictException(mensaje);
            case 53:
                throw new BusinessException(mensaje);
            default:
                // 50/51 son errores de configuracion: no accionables por el usuario
                throw new InvalidOperationException(
                    $"Motor de estatus ({retorno}): {mensaje}");
        }

        var descripcionDestino = await LeerDescripcionEstatusAsync(
            contexto, configuracion.TablaEstatus, transicion.IdEstatusDestino, cancellationToken);

        return new ResultadoTransicion(estatusActual, transicion.IdEstatusDestino, descripcionDestino);
    }

    public async Task<IReadOnlyList<AccionDisponible>> ObtenerAccionesAsync(
        string proceso,
        int idRegistro,
        CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        var configuracion = await ObtenerProcesoAsync(contexto, proceso, cancellationToken);
        var estatusActual = await LeerEstatusActualAsync(contexto, configuracion, idRegistro, cancellationToken);

        var acciones = await (
            from t in contexto.TblTransicion.AsNoTracking()
            where t.IdProceso == configuracion.IdProceso
                  && t.IdEstatusOrigen == estatusActual
                  && t.Activo
            join c in contexto.TblTransicionConfig.AsNoTracking()
                  .Where(c => c.Proceso == proceso && c.Activo)
                on new { t.IdEstatusOrigen, t.Accion } equals new { c.IdEstatusOrigen, c.Accion }
                into configs
            from c in configs.DefaultIfEmpty()
            select new AccionDisponible(
                t.Accion,
                c != null ? c.EtiquetaBoton : t.Accion,
                c != null && c.RequiereMotivo,
                c != null && c.EsAccionPrincipal,
                c != null ? c.Orden : 0,
                c != null ? c.RequierePermiso : null)
            ).ToListAsync(cancellationToken);

        return acciones.OrderBy(a => a.Orden).ThenBy(a => a.Accion).ToList();
    }

    private static async Task<Modelos.bdsGTE.TblProceso> ObtenerProcesoAsync(
        DbContextGTE contexto, string proceso, CancellationToken cancellationToken)
    {
        return await contexto.TblProceso.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Proceso == proceso && p.Activo, cancellationToken)
            ?? throw new NotFoundException("Proceso de workflow", proceso);
    }

    /// <summary>
    /// Lee el estatus vigente del registro con SQL dinamico blindado: los identificadores
    /// vienen del catalogo dbo.tblProceso (confiable) y aun asi se citan con corchetes.
    /// </summary>
    private static async Task<int> LeerEstatusActualAsync(
        DbContextGTE contexto,
        Modelos.bdsGTE.TblProceso configuracion,
        int idRegistro,
        CancellationToken cancellationToken)
    {
        var sql = $"SELECT {Citar(configuracion.ColumnaEstatus)} AS [Value] " +
                  $"FROM {CitarTabla(configuracion.TablaTransaccional)} " +
                  $"WHERE {Citar(configuracion.ColumnaPk)} = @p0";

        var estatus = await contexto.Database
            .SqlQueryRaw<int?>(sql, new SqlParameter("@p0", idRegistro))
            .FirstOrDefaultAsync(cancellationToken);

        return estatus ?? throw new NotFoundException(configuracion.Proceso, idRegistro);
    }

    private static async Task<string> LeerDescripcionEstatusAsync(
        DbContextGTE contexto,
        string tablaEstatus,
        int idEstatus,
        CancellationToken cancellationToken)
    {
        var sql = $"SELECT Descripcion AS [Value] FROM {CitarTabla(tablaEstatus)} WHERE Id = @p0";
        var descripcion = await contexto.Database
            .SqlQueryRaw<string>(sql, new SqlParameter("@p0", idEstatus))
            .FirstOrDefaultAsync(cancellationToken);

        return descripcion ?? idEstatus.ToString();
    }

    private static string Citar(string identificador)
    {
        return "[" + identificador.Replace("]", "]]") + "]";
    }

    private static string CitarTabla(string tabla)
    {
        var partes = tabla.Split('.');
        return partes.Length switch
        {
            1 => "[dbo]." + Citar(partes[0]),
            2 => Citar(partes[0]) + "." + Citar(partes[1]),
            _ => throw new InvalidOperationException($"Nombre de tabla invalido en tblProceso: {tabla}")
        };
    }
}
