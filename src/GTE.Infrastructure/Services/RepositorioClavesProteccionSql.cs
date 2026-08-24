using System.Xml.Linq;
using GTE.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Persiste el key ring de Data Protection en dbo.tblSysDataProtectionKeys (bdsGTE, ADR-03:
/// base unica) en vez del filesystem por defecto. Tabla de sistema FUERA del scaffold de
/// EF -- igual que el schema propio de Hangfire dentro de bdsGTE -- accedida solo con
/// ADO.NET crudo, nunca por LINQ/DbSet. IXmlRepository es sincrono (sin variante async en
/// el framework), asi que aqui tambien lo es.
/// </summary>
public class RepositorioClavesProteccionSql(FabricaContexto fabrica) : IXmlRepository
{
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var conexion = contexto.Database.GetDbConnection();
        conexion.Open();

        using var comando = conexion.CreateCommand();
        comando.CommandText = "SELECT Xml FROM dbo.tblSysDataProtectionKeys ORDER BY Id";

        var elementos = new List<XElement>();
        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            elementos.Add(XElement.Parse(lector.GetString(0)));
        }
        return elementos;
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var conexion = contexto.Database.GetDbConnection();
        conexion.Open();

        using var comando = conexion.CreateCommand();
        comando.CommandText = "INSERT INTO dbo.tblSysDataProtectionKeys (FriendlyName, Xml) VALUES (@FriendlyName, @Xml)";
        comando.Parameters.Add(new SqlParameter("@FriendlyName", (object?)friendlyName ?? DBNull.Value));
        comando.Parameters.Add(new SqlParameter("@Xml", element.ToString(SaveOptions.DisableFormatting)));
        comando.ExecuteNonQuery();
    }
}
