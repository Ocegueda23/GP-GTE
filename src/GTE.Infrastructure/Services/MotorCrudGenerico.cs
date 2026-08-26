using System.Data.Common;
using System.Text.Json;
using GTE.Application.Common;
using GTE.Application.Interfaces;
using GTE.Domain.CatalogoGenerico;
using GTE.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

/// <summary>
/// CRUD generico contra la tabla de negocio de un catalogo, con SQL dinamico blindado
/// (mismo patron que MotorWorkflow): los identificadores de tabla/columna SOLO se leen
/// de ConfiguracionCatalogo.Columnas (ya validados contra INFORMATION_SCHEMA al guardar
/// la configuracion, ver CatalogoGenericoQueryService/MotorMetadatosEsquema) y siempre se
/// citan con SqlIdentificadores. Los valores de datos SIEMPRE van parametrizados, y se
/// leen del diccionario de la request buscando por el nombre de columna ya validado --
/// nunca se recorren las claves del diccionario para construir SQL.
/// </summary>
public class MotorCrudGenerico(FabricaContexto fabrica, IServicioCifradoColumna cifrado) : IMotorCrudGenerico
{
    public async Task<PagedResult<Dictionary<string, object?>>> ListarAsync(
        ConfiguracionCatalogo catalogo,
        FiltrosListadoCatalogo filtros,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var conexion = contexto.Database.GetDbConnection();
        await contexto.Database.OpenConnectionAsync(cancellationToken);

        var parametros = new List<SqlParameter>();
        SqlParameter NuevoParametro(object? valor)
        {
            var parametro = new SqlParameter($"@p{parametros.Count}", valor ?? DBNull.Value);
            parametros.Add(parametro);
            return parametro;
        }

        var condiciones = ConstruirCondicionesFiltro(catalogo, filtros, NuevoParametro);
        var whereSql = condiciones.Count > 0 ? "WHERE " + string.Join(" AND ", condiciones) : string.Empty;

        var columnaOrden = (!string.IsNullOrWhiteSpace(filtros.OrdenarPor)
                ? catalogo.Columnas.FirstOrDefault(c =>
                    string.Equals(c.NombreColumna, filtros.OrdenarPor, StringComparison.OrdinalIgnoreCase))
                : null)
            ?? catalogo.Columnas.FirstOrDefault(c => c.EsPk)
            ?? catalogo.Columnas[0];
        var direccionOrden = filtros.OrdenDescendente ? "DESC" : "ASC";

        var tablaSql = SqlIdentificadores.CitarTabla(catalogo.NombreTabla);
        var columnasSql = string.Join(", ", catalogo.Columnas.Select(c => SqlIdentificadores.Citar(c.NombreColumna)));

        var totalItems = await EjecutarConteoAsync(conexion, tablaSql, whereSql, parametros, cancellationToken);

        var paginaSegura = Math.Max(1, pagina);
        var tamanoSeguro = Math.Clamp(tamanoPagina, 1, 500);
        var offset = (paginaSegura - 1) * tamanoSeguro;

        var sqlPagina = $"SELECT {columnasSql} FROM {tablaSql} {whereSql} " +
                        $"ORDER BY {SqlIdentificadores.Citar(columnaOrden.NombreColumna)} {direccionOrden} " +
                        $"OFFSET @offset ROWS FETCH NEXT @tamanoPagina ROWS ONLY";

        await using var comando = conexion.CreateCommand();
        comando.CommandText = sqlPagina;
        comando.Parameters.AddRange(ClonarParametros(parametros));
        comando.Parameters.Add(new SqlParameter("@offset", offset));
        comando.Parameters.Add(new SqlParameter("@tamanoPagina", tamanoSeguro));

        var filas = new List<Dictionary<string, object?>>();
        await using (var lector = await comando.ExecuteReaderAsync(cancellationToken))
        {
            while (await lector.ReadAsync(cancellationToken))
            {
                filas.Add(LeerFila(lector));
            }
        }

        return new PagedResult<Dictionary<string, object?>>
        {
            Items = filas,
            Page = paginaSegura,
            PageSize = tamanoSeguro,
            TotalItems = totalItems
        };
    }

    public async Task<IReadOnlyList<string>> ObtenerValoresDistintosAsync(
        ConfiguracionCatalogo catalogo, string nombreColumna, CancellationToken cancellationToken = default)
    {
        var columna = catalogo.Columnas.FirstOrDefault(c =>
            string.Equals(c.NombreColumna, nombreColumna, StringComparison.OrdinalIgnoreCase)
            && c.EsVisible && !c.EsCifrado)
            ?? throw new InvalidOperationException($"Columna no valida para filtro: {nombreColumna}.");

        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var conexion = contexto.Database.GetDbConnection();
        await contexto.Database.OpenConnectionAsync(cancellationToken);

        var columnaCitada = SqlIdentificadores.Citar(columna.NombreColumna);
        var sql = $"SELECT DISTINCT TOP (500) CAST({columnaCitada} AS NVARCHAR(400)) AS Valor " +
                  $"FROM {SqlIdentificadores.CitarTabla(catalogo.NombreTabla)} " +
                  $"WHERE {columnaCitada} IS NOT NULL ORDER BY 1";

        await using var comando = conexion.CreateCommand();
        comando.CommandText = sql;

        var valores = new List<string>();
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            valores.Add(lector.GetString(0));
        }
        return valores;
    }

    public async Task<IReadOnlyList<OpcionFk>> ObtenerOpcionesFkAsync(
        string tabla, string columnaClave, string columnaMostrar, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var conexion = contexto.Database.GetDbConnection();
        await contexto.Database.OpenConnectionAsync(cancellationToken);

        var claveCitada = SqlIdentificadores.Citar(columnaClave);
        var mostrarCitada = SqlIdentificadores.Citar(columnaMostrar);
        var sql = $"SELECT DISTINCT TOP (500) CAST({claveCitada} AS NVARCHAR(400)) AS Valor, " +
                  $"CAST({mostrarCitada} AS NVARCHAR(400)) AS Etiqueta " +
                  $"FROM {SqlIdentificadores.CitarTabla(tabla)} " +
                  $"WHERE {claveCitada} IS NOT NULL ORDER BY 2";

        await using var comando = conexion.CreateCommand();
        comando.CommandText = sql;

        var opciones = new List<OpcionFk>();
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            opciones.Add(new OpcionFk(lector.GetString(0), lector.IsDBNull(1) ? lector.GetString(0) : lector.GetString(1)));
        }
        return opciones;
    }

    public async Task<Dictionary<string, object?>> CrearAsync(
        ConfiguracionCatalogo catalogo, Dictionary<string, object?> valores, string usuario,
        CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var conexion = contexto.Database.GetDbConnection();
        await contexto.Database.OpenConnectionAsync(cancellationToken);

        var parametros = new List<SqlParameter>();
        SqlParameter NuevoParametro(object? valor)
        {
            var parametro = new SqlParameter($"@p{parametros.Count}", valor ?? DBNull.Value);
            parametros.Add(parametro);
            return parametro;
        }

        var columnasInsertables = catalogo.Columnas.Where(c => !c.EsIdentity).ToList();
        var nombresColumnas = new List<string>();
        var expresionesValores = new List<string>();

        foreach (var columna in columnasInsertables)
        {
            string expresion;
            if (columna.AutoFechaAlta)
            {
                expresion = "SYSDATETIME()";
            }
            else if (columna.AutoUsuarioAlta)
            {
                expresion = NuevoParametro(usuario).ParameterName;
            }
            else if (valores.TryGetValue(columna.NombreColumna, out var valorCrudo))
            {
                expresion = NuevoParametro(ProtegerSiAplica(columna, ConvertirValor(valorCrudo, columna))).ParameterName;
            }
            else
            {
                // No vino en el request: se omite del INSERT (no se manda NULL explicito) para
                // que aplique el DEFAULT de la columna en BD -- la misma trampa EF de columnas
                // bit/fecha con DEFAULT que ya se cuida en el resto del proyecto.
                continue;
            }
            nombresColumnas.Add(SqlIdentificadores.Citar(columna.NombreColumna));
            expresionesValores.Add(expresion);
        }

        var columnasSalida = string.Join(", ", catalogo.Columnas.Select(c => "INSERTED." + SqlIdentificadores.Citar(c.NombreColumna)));
        var sql = $"INSERT INTO {SqlIdentificadores.CitarTabla(catalogo.NombreTabla)} " +
                  $"({string.Join(", ", nombresColumnas)}) OUTPUT {columnasSalida} " +
                  $"VALUES ({string.Join(", ", expresionesValores)})";

        await using var comando = conexion.CreateCommand();
        comando.CommandText = sql;
        comando.Parameters.AddRange(parametros.ToArray());

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        if (!await lector.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("El INSERT no devolvio la fila creada.");
        }
        return LeerFila(lector);
    }

    public async Task ActualizarAsync(
        ConfiguracionCatalogo catalogo, Dictionary<string, object?> clavesPk, Dictionary<string, object?> valores,
        string usuario, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var conexion = contexto.Database.GetDbConnection();
        await contexto.Database.OpenConnectionAsync(cancellationToken);

        var columnasPk = ColumnasPk(catalogo);
        var parametros = new List<SqlParameter>();
        SqlParameter NuevoParametro(object? valor)
        {
            var parametro = new SqlParameter($"@p{parametros.Count}", valor ?? DBNull.Value);
            parametros.Add(parametro);
            return parametro;
        }

        var asignaciones = new List<string>();
        foreach (var columna in catalogo.Columnas.Where(c => !c.EsPk && !c.EsIdentity && !c.EsSoloLectura))
        {
            string expresion;
            if (columna.AutoFechaEdicion)
            {
                expresion = "SYSDATETIME()";
            }
            else if (columna.AutoUsuarioEdicion)
            {
                expresion = NuevoParametro(usuario).ParameterName;
            }
            else
            {
                if (!valores.TryGetValue(columna.NombreColumna, out var valorCrudo))
                {
                    continue;
                }
                expresion = NuevoParametro(ProtegerSiAplica(columna, ConvertirValor(valorCrudo, columna))).ParameterName;
            }
            asignaciones.Add($"{SqlIdentificadores.Citar(columna.NombreColumna)} = {expresion}");
        }

        if (asignaciones.Count == 0)
        {
            return;
        }

        var whereSql = ConstruirClausulaPk(columnasPk, clavesPk, NuevoParametro);
        var sql = $"UPDATE {SqlIdentificadores.CitarTabla(catalogo.NombreTabla)} " +
                  $"SET {string.Join(", ", asignaciones)} WHERE {whereSql}";

        await using var comando = conexion.CreateCommand();
        comando.CommandText = sql;
        comando.Parameters.AddRange(parametros.ToArray());
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task EliminarAsync(
        ConfiguracionCatalogo catalogo, Dictionary<string, object?> clavesPk, string usuario,
        CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var conexion = contexto.Database.GetDbConnection();
        await contexto.Database.OpenConnectionAsync(cancellationToken);

        var columnasPk = ColumnasPk(catalogo);
        var parametros = new List<SqlParameter>();
        SqlParameter NuevoParametro(object? valor)
        {
            var parametro = new SqlParameter($"@p{parametros.Count}", valor ?? DBNull.Value);
            parametros.Add(parametro);
            return parametro;
        }

        var columnaActivo = catalogo.Columnas.FirstOrDefault(c =>
            string.Equals(c.NombreColumna, "Activo", StringComparison.OrdinalIgnoreCase)
            && string.Equals(c.TipoSql, "bit", StringComparison.OrdinalIgnoreCase));

        string sql;
        if (columnaActivo is not null)
        {
            var asignaciones = new List<string> { $"{SqlIdentificadores.Citar(columnaActivo.NombreColumna)} = 0" };

            var columnaUsuarioMovto = catalogo.Columnas.FirstOrDefault(c =>
                string.Equals(c.NombreColumna, "UsuarioMovto", StringComparison.OrdinalIgnoreCase));
            if (columnaUsuarioMovto is not null)
            {
                asignaciones.Add($"{SqlIdentificadores.Citar(columnaUsuarioMovto.NombreColumna)} = {NuevoParametro(usuario).ParameterName}");
            }

            var columnaFechaMovto = catalogo.Columnas.FirstOrDefault(c =>
                string.Equals(c.NombreColumna, "FechaMovto", StringComparison.OrdinalIgnoreCase));
            if (columnaFechaMovto is not null)
            {
                asignaciones.Add($"{SqlIdentificadores.Citar(columnaFechaMovto.NombreColumna)} = SYSDATETIME()");
            }

            var whereSql = ConstruirClausulaPk(columnasPk, clavesPk, NuevoParametro);
            sql = $"UPDATE {SqlIdentificadores.CitarTabla(catalogo.NombreTabla)} SET {string.Join(", ", asignaciones)} WHERE {whereSql}";
        }
        else
        {
            var whereSql = ConstruirClausulaPk(columnasPk, clavesPk, NuevoParametro);
            sql = $"DELETE FROM {SqlIdentificadores.CitarTabla(catalogo.NombreTabla)} WHERE {whereSql}";
        }

        await using var comando = conexion.CreateCommand();
        comando.CommandText = sql;
        comando.Parameters.AddRange(parametros.ToArray());
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Dictionary<string, object?>?> ObtenerPorPkAsync(
        ConfiguracionCatalogo catalogo, Dictionary<string, object?> clavesPk, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var conexion = contexto.Database.GetDbConnection();
        await contexto.Database.OpenConnectionAsync(cancellationToken);

        var columnasPk = ColumnasPk(catalogo);
        var parametros = new List<SqlParameter>();
        SqlParameter NuevoParametro(object? valor)
        {
            var parametro = new SqlParameter($"@p{parametros.Count}", valor ?? DBNull.Value);
            parametros.Add(parametro);
            return parametro;
        }

        var whereSql = ConstruirClausulaPk(columnasPk, clavesPk, NuevoParametro);
        var columnasSql = string.Join(", ", catalogo.Columnas.Select(c => SqlIdentificadores.Citar(c.NombreColumna)));
        var sql = $"SELECT {columnasSql} FROM {SqlIdentificadores.CitarTabla(catalogo.NombreTabla)} WHERE {whereSql}";

        await using var comando = conexion.CreateCommand();
        comando.CommandText = sql;
        comando.Parameters.AddRange(parametros.ToArray());

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        return await lector.ReadAsync(cancellationToken) ? LeerFila(lector) : null;
    }

    /// <summary>Cifra el valor si la columna esta marcada EsCifrado y hay un valor real que proteger.</summary>
    private object ProtegerSiAplica(ColumnaReconciliada columna, object valorConvertido)
    {
        if (columna.EsCifrado && valorConvertido is string textoPlano)
        {
            return cifrado.Proteger(columna.NombreColumna, textoPlano);
        }
        return valorConvertido;
    }

    private static List<ColumnaReconciliada> ColumnasPk(ConfiguracionCatalogo catalogo)
    {
        var columnasPk = catalogo.Columnas.Where(c => c.EsPk).ToList();
        if (columnasPk.Count == 0)
        {
            throw new InvalidOperationException(
                $"La tabla {catalogo.NombreTabla} no tiene llave primaria definida; el motor generico la requiere.");
        }
        return columnasPk;
    }

    private static string ConstruirClausulaPk(
        IReadOnlyList<ColumnaReconciliada> columnasPk, Dictionary<string, object?> clavesPk,
        Func<object?, SqlParameter> nuevoParametro)
    {
        var condiciones = new List<string>();
        foreach (var columna in columnasPk)
        {
            if (!clavesPk.TryGetValue(columna.NombreColumna, out var valor))
            {
                throw new InvalidOperationException($"Falta el valor de la llave primaria {columna.NombreColumna}.");
            }
            var parametro = nuevoParametro(ConvertirValor(valor, columna));
            condiciones.Add($"{SqlIdentificadores.Citar(columna.NombreColumna)} = {parametro.ParameterName}");
        }
        return string.Join(" AND ", condiciones);
    }

    private static List<string> ConstruirCondicionesFiltro(
        ConfiguracionCatalogo catalogo, FiltrosListadoCatalogo filtros, Func<object?, SqlParameter> nuevoParametro)
    {
        var condiciones = new List<string>();
        var columnasVisibles = catalogo.Columnas.Where(c => c.EsVisible).ToList();

        if (!string.IsNullOrWhiteSpace(filtros.Texto))
        {
            var columnasTexto = columnasVisibles.Where(c => !c.EsCifrado && EsTipoTexto(c.TipoSql)).ToList();
            if (columnasTexto.Count > 0)
            {
                var parametroTexto = nuevoParametro("%" + filtros.Texto + "%");
                var comparaciones = columnasTexto.Select(c =>
                    $"{SqlIdentificadores.Citar(c.NombreColumna)} LIKE {parametroTexto.ParameterName}");
                condiciones.Add("(" + string.Join(" OR ", comparaciones) + ")");
            }
        }

        foreach (var filtroColumna in filtros.FiltrosColumna)
        {
            var columna = catalogo.Columnas.FirstOrDefault(c =>
                string.Equals(c.NombreColumna, filtroColumna.NombreColumna, StringComparison.OrdinalIgnoreCase));
            if (columna is null || filtroColumna.Valores.Count == 0)
            {
                continue;
            }
            var parametrosValores = filtroColumna.Valores.Select(v => nuevoParametro(v)).ToList();
            condiciones.Add(
                $"{SqlIdentificadores.Citar(columna.NombreColumna)} IN ({string.Join(", ", parametrosValores.Select(p => p.ParameterName))})");
        }

        if (filtros.Fecha is not null)
        {
            var columna = catalogo.Columnas.FirstOrDefault(c =>
                string.Equals(c.NombreColumna, filtros.Fecha.NombreColumna, StringComparison.OrdinalIgnoreCase));
            if (columna is not null)
            {
                if (filtros.Fecha.Desde.HasValue)
                {
                    var parametro = nuevoParametro(filtros.Fecha.Desde.Value.ToDateTime(TimeOnly.MinValue));
                    condiciones.Add($"{SqlIdentificadores.Citar(columna.NombreColumna)} >= {parametro.ParameterName}");
                }
                if (filtros.Fecha.Hasta.HasValue)
                {
                    var parametro = nuevoParametro(filtros.Fecha.Hasta.Value.ToDateTime(TimeOnly.MaxValue));
                    condiciones.Add($"{SqlIdentificadores.Citar(columna.NombreColumna)} <= {parametro.ParameterName}");
                }
            }
        }

        return condiciones;
    }

    private static async Task<int> EjecutarConteoAsync(
        DbConnection conexion, string tablaSql, string whereSql, List<SqlParameter> parametros,
        CancellationToken cancellationToken)
    {
        await using var comando = conexion.CreateCommand();
        comando.CommandText = $"SELECT COUNT(*) FROM {tablaSql} {whereSql}";
        comando.Parameters.AddRange(ClonarParametros(parametros));
        var resultado = await comando.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(resultado);
    }

    private static SqlParameter[] ClonarParametros(List<SqlParameter> parametros) =>
        parametros.Select(p => (SqlParameter)((ICloneable)p).Clone()).ToArray();

    private static bool EsTipoTexto(string tipoSql) => tipoSql.ToLowerInvariant() switch
    {
        "varchar" or "nvarchar" or "char" or "nchar" or "text" or "ntext" => true,
        _ => false
    };

    private static Dictionary<string, object?> LeerFila(DbDataReader lector)
    {
        var fila = new Dictionary<string, object?>();
        for (var i = 0; i < lector.FieldCount; i++)
        {
            var valor = lector.GetValue(i);
            fila[lector.GetName(i)] = valor is DBNull ? null : valor;
        }
        return fila;
    }

    /// <summary>
    /// Los valores de datos llegan del request como JsonElement (Dictionary&lt;string,object?&gt;
    /// deserializado por System.Text.Json); se convierten al tipo CLR correcto segun el tipo
    /// SQL real de la columna antes de usarse como parametro.
    /// </summary>
    private static object ConvertirValor(object? valorCrudo, ColumnaReconciliada columna)
    {
        if (valorCrudo is null)
        {
            return DBNull.Value;
        }
        if (valorCrudo is not JsonElement elemento)
        {
            return valorCrudo;
        }
        if (elemento.ValueKind == JsonValueKind.Null)
        {
            return DBNull.Value;
        }

        return columna.TipoSql.ToLowerInvariant() switch
        {
            "bit" => elemento.GetBoolean(),
            "int" or "smallint" or "tinyint" => elemento.GetInt32(),
            "bigint" => elemento.GetInt64(),
            "decimal" or "numeric" or "money" or "smallmoney" => elemento.GetDecimal(),
            "float" or "real" => elemento.GetDouble(),
            "date" => DateOnly.Parse(elemento.GetString()!),
            "datetime" or "datetime2" or "smalldatetime" => DateTime.Parse(elemento.GetString()!),
            _ => elemento.GetString() ?? (object)DBNull.Value
        };
    }
}
