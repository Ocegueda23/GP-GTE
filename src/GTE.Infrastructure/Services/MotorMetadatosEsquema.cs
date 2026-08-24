using GTE.Application.Interfaces;
using GTE.Domain.CatalogoGenerico;
using GTE.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Introspeccion de esquema de bdsGTE (dbo) via INFORMATION_SCHEMA. Los nombres de tabla
/// se pasan siempre como parametro de VALOR (@p0) a las consultas al catalogo del motor
/// de BD -- nunca se interpolan como identificador aqui, por eso este servicio si puede
/// operar sobre nombres que todavia no fueron validados (es el propio validador).
/// </summary>
public class MotorMetadatosEsquema(FabricaContexto fabrica) : IMotorMetadatosEsquema
{
    private static readonly string[] TablasSistemaExcluidas =
    [
        "tblCatalogoGenerico",
        "tblCatalogoGenericoColumna"
    ];

    public async Task<IReadOnlyList<string>> ObtenerTablasDisponiblesAsync(
        CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        const string sql = """
            SELECT TABLE_NAME AS [Value]
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = 'dbo'
            ORDER BY TABLE_NAME
            """;

        var tablas = await contexto.Database.SqlQueryRaw<string>(sql).ToListAsync(cancellationToken);

        return tablas.Where(t => !TablasSistemaExcluidas.Contains(t)).ToList();
    }

    public async Task<IReadOnlyList<ColumnaEsquema>> ObtenerColumnasAsync(
        string nombreTabla, CancellationToken cancellationToken = default)
    {
        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();

        const string sql = """
            SELECT
                c.COLUMN_NAME              AS NombreColumna,
                c.DATA_TYPE                AS TipoSql,
                c.IS_NULLABLE              AS EsNulableTexto,
                c.CHARACTER_MAXIMUM_LENGTH AS LongitudMaxima,
                c.ORDINAL_POSITION         AS OrdinalEsquema,
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS EsPkNumero,
                ISNULL(COLUMNPROPERTY(OBJECT_ID('dbo.' + @p0), c.COLUMN_NAME, 'IsIdentity'), 0) AS EsIdentityNumero
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN (
                SELECT ku.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                    ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                    AND tc.TABLE_NAME = ku.TABLE_NAME
                    AND tc.TABLE_SCHEMA = ku.TABLE_SCHEMA
                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY' AND tc.TABLE_NAME = @p0 AND tc.TABLE_SCHEMA = 'dbo'
            ) pk ON pk.COLUMN_NAME = c.COLUMN_NAME
            WHERE c.TABLE_SCHEMA = 'dbo' AND c.TABLE_NAME = @p0
            ORDER BY c.ORDINAL_POSITION
            """;

        var filas = await contexto.Database
            .SqlQueryRaw<FilaColumnaEsquema>(sql, new SqlParameter("@p0", nombreTabla))
            .ToListAsync(cancellationToken);

        return filas.Select(f => new ColumnaEsquema(
            f.NombreColumna,
            f.TipoSql,
            string.Equals(f.EsNulableTexto, "YES", StringComparison.OrdinalIgnoreCase),
            f.LongitudMaxima,
            f.EsPkNumero != 0,
            f.EsIdentityNumero != 0,
            f.OrdinalEsquema)).ToList();
    }

    /// <summary>Fila cruda de la consulta de metadatos; se mapea a ColumnaEsquema (tipos correctos).</summary>
    private sealed class FilaColumnaEsquema
    {
        public string NombreColumna { get; set; } = null!;
        public string TipoSql { get; set; } = null!;
        public string EsNulableTexto { get; set; } = null!;
        public int? LongitudMaxima { get; set; }
        public int OrdinalEsquema { get; set; }
        public int EsPkNumero { get; set; }
        public int EsIdentityNumero { get; set; }
    }
}
