using GTE.Application.Common;
using GTE.Domain.Exceptions;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using GTE.Infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GTE.Api.Tests;

/// <summary>
/// Pruebas de integracion contra una bdsGTE real (LocalDB). Si la base no esta
/// disponible, las pruebas se omiten silenciosamente: la verificacion completa
/// corre en las maquinas de desarrollo donde la tanda de scripts fue desplegada.
/// </summary>
public class MotorWorkflowIntegracionTests
{
    private const string CadenaLocal =
        @"Server=(localdb)\MSSQLLocalDB;Database=bdsGTE;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5";

    private static FabricaContexto? CrearFabrica()
    {
        try
        {
            using var conexion = new SqlConnection(CadenaLocal);
            conexion.Open();
        }
        catch (Exception)
        {
            return null; // sin BD local: la prueba se omite
        }

        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:bdsGTE"] = CadenaLocal
            })
            .Build();
        return new FabricaContexto(configuracion);
    }

    [Fact]
    public async Task CicloCompleto_IniciarConGuardYAcciones()
    {
        var fabrica = CrearFabrica();
        if (fabrica is null)
        {
            return;
        }

        var auditoria = new AuditContext { Usuario = "prueba-integracion" };
        var motor = new MotorWorkflow(fabrica, auditoria);
        var sufijo = Guid.NewGuid().ToString("N")[..8];

        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var proyecto = new TblProyecto
        {
            Clave = $"IT{sufijo[..6]}",
            Nombre = $"Proyecto integracion {sufijo}",
            IdCategoriaProyecto = 1,
            IdEstatusProyecto = 3,
            UsuarioRegistro = auditoria.Usuario,
            Activo = true
        };
        contexto.TblProyecto.Add(proyecto);
        await contexto.SaveChangesAsync();

        var item = new TblWorkItem
        {
            Folio = $"ITG-{sufijo}",
            IdTipoWorkItem = 3,
            IdProyecto = proyecto.IdProyecto,
            Titulo = "Item de integracion",
            IdEstatusWorkItem = 1,
            IdPrioridad = 3,
            FechaCompromiso = DateTime.Now.AddDays(5),
            UsuarioRegistro = auditoria.Usuario,
            Activo = true
        };
        contexto.TblWorkItem.Add(item);
        await contexto.SaveChangesAsync();

        contexto.TblHistorialEstatus.Add(new TblHistorialEstatus
        {
            Proceso = "WorkItem",
            IdRegistro = item.IdWorkItem,
            IdEstatus = 1,
            Accion = "ALTA",
            Usuario = auditoria.Usuario
        });
        await contexto.SaveChangesAsync();

        try
        {
            // Acciones disponibles desde Pendiente: INICIAR y CANCELAR
            var acciones = await motor.ObtenerAccionesAsync("WorkItem", item.IdWorkItem);
            Assert.Contains(acciones, a => a.Accion == "INICIAR");
            Assert.Contains(acciones, a => a.Accion == "CANCELAR");

            // INICIAR (1 -> 2) con retorno del motor
            var resultado = await motor.EjecutarAccionAsync("WorkItem", item.IdWorkItem, "INICIAR");
            Assert.Equal(1, resultado.IdEstatusAnterior);
            Assert.Equal(2, resultado.IdEstatusNuevo);
            Assert.Equal("En Proceso", resultado.DescripcionEstatusNuevo);

            // Accion invalida desde En Proceso -> BusinessException (pre-validacion)
            await Assert.ThrowsAsync<BusinessException>(() =>
                motor.EjecutarAccionAsync("WorkItem", item.IdWorkItem, "APROBAR"));

            // CalendarioLaboral contra la funcion SQL real (paridad con la calculadora C#)
            var calendario = new CalendarioLaboral(fabrica);
            await using var contextoLectura = fabrica.ConectarContexto<DbContextGTE>();
            var idBansi = contextoLectura.TblHorario.Single(h => h.Nombre == "BANSI").IdHorario;
            var minutos = await calendario.CalcularMinutosLaboralesAsync(
                new DateTime(2026, 7, 27, 0, 0, 0), new DateTime(2026, 7, 27, 23, 59, 0), idBansi);
            Assert.Equal(510, minutos);
        }
        finally
        {
            await using var limpieza = fabrica.ConectarContexto<DbContextGTE>();
            limpieza.TblHistorialEstatus.RemoveRange(
                limpieza.TblHistorialEstatus.Where(h => h.Proceso == "WorkItem" && h.IdRegistro == item.IdWorkItem));
            limpieza.TblHistorialCampo.RemoveRange(
                limpieza.TblHistorialCampo.Where(h => h.Entidad == "WorkItem" && h.IdEntidad == item.IdWorkItem));
            await limpieza.SaveChangesAsync();
            limpieza.TblWorkItem.RemoveRange(limpieza.TblWorkItem.Where(w => w.IdWorkItem == item.IdWorkItem));
            await limpieza.SaveChangesAsync();
            limpieza.TblProyecto.RemoveRange(limpieza.TblProyecto.Where(p => p.IdProyecto == proyecto.IdProyecto));
            await limpieza.SaveChangesAsync();
        }
    }
}
