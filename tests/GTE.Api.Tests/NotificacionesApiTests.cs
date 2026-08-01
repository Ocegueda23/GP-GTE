using System.Net.Http.Json;
using System.Text.Json;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GTE.Api.Tests;

/// <summary>
/// E2E de los disparadores de notificaciones (A3): Solicitud rechazada notifica al
/// solicitante, y @mencion en un comentario notifica al mencionado. Se omite si no hay
/// LocalDB.
/// </summary>
public class NotificacionesApiTests(WebApplicationFactory<Program> fabricaApp)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private sealed record Envelope<T>(string Code, bool Success, string UserMessage, T? Response);

    private static bool BaseDisponible() => FabricaApiAutenticada.BaseDisponible();

    private static FabricaContexto CrearFabricaDatos()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:bdsGTE"] = FabricaApiAutenticada.CadenaLocal
            })
            .Build();
        return new FabricaContexto(configuracion);
    }

    [Fact]
    public async Task RechazarSolicitud_NotificaAlSolicitante()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var fabricaDatos = CrearFabricaDatos();
        var cliente = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");

        int idSolicitud;
        int idSolicitante;
        await using (var contexto = fabricaDatos.ConectarContexto<DbContextGTE>())
        {
            var usuario = await contexto.TblUsuario.FirstAsync(u => u.Dominio == "aviramontes");
            idSolicitante = usuario.IdUsuario;

            var solicitud = new TblSolicitud
            {
                IdSolicitante = idSolicitante,
                Titulo = "Solicitud de prueba E2E notificaciones",
                IdTipoSolicitud = 1,
                IdPrioridad = 1,
                IdEstatusSolicitud = 3, // En Analisis: unico estatus desde el que RECHAZAR es valido
                UsuarioRegistro = "e2e",
                Activo = true
            };
            contexto.TblSolicitud.Add(solicitud);
            await contexto.SaveChangesAsync();
            idSolicitud = solicitud.IdSolicitud;
        }

        try
        {
            var respuesta = await cliente.PutAsJsonAsync($"/api/v1/solicitudes/{idSolicitud}/estatus", new
            {
                accion = "RECHAZAR",
                motivo = "Falta informacion para evaluarla"
            });
            respuesta.EnsureSuccessStatusCode();

            await using var verificacion = fabricaDatos.ConectarContexto<DbContextGTE>();
            var notificacion = await verificacion.TblNotificacion.AsNoTracking()
                .Where(n => n.IdUsuario == idSolicitante && n.Entidad == "Solicitud" && n.IdEntidad == idSolicitud)
                .FirstOrDefaultAsync();

            Assert.NotNull(notificacion);
            Assert.False(notificacion.Leida);
            Assert.Equal("Falta informacion para evaluarla", notificacion.Mensaje);
        }
        finally
        {
            await using var limpieza = fabricaDatos.ConectarContexto<DbContextGTE>();
            limpieza.TblNotificacion.RemoveRange(
                limpieza.TblNotificacion.Where(n => n.Entidad == "Solicitud" && n.IdEntidad == idSolicitud));
            limpieza.TblHistorialEstatus.RemoveRange(
                limpieza.TblHistorialEstatus.Where(h => h.Proceso == "Solicitud" && h.IdRegistro == idSolicitud));
            limpieza.TblSolicitud.RemoveRange(limpieza.TblSolicitud.Where(s => s.IdSolicitud == idSolicitud));
            await limpieza.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task MencionarEnComentario_NotificaAlMencionado()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var fabricaDatos = CrearFabricaDatos();
        var cliente = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");

        var sufijo = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var clave = $"NOT{sufijo}";
        int idProyecto;
        int idMencionado;
        await using (var contexto = fabricaDatos.ConectarContexto<DbContextGTE>())
        {
            var proyecto = new TblProyecto
            {
                Clave = clave,
                Nombre = $"Proyecto Notificaciones {sufijo}",
                IdCategoriaProyecto = 1,
                IdEstatusProyecto = 3,
                UsuarioRegistro = "e2e",
                Activo = true
            };
            contexto.TblProyecto.Add(proyecto);

            var mencionado = new TblUsuario
            {
                Dominio = $"mencion.{sufijo}",
                Nombre = $"Usuario Mencionado {sufijo}",
                UsuarioRegistro = "e2e",
                Activo = true
            };
            contexto.TblUsuario.Add(mencionado);

            await contexto.SaveChangesAsync();
            idProyecto = proyecto.IdProyecto;
            idMencionado = mencionado.IdUsuario;
        }

        var idWorkItem = 0;
        try
        {
            var respuestaItem = await cliente.PostAsJsonAsync("/api/v1/workitems", new
            {
                idProyecto,
                idTipoWorkItem = 3,
                titulo = "Item para mencion",
                idPrioridad = 3,
                fechaCompromiso = DateTime.Today.AddDays(5)
            });
            respuestaItem.EnsureSuccessStatusCode();
            var itemCreado = await respuestaItem.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
            idWorkItem = itemCreado!.Response.GetProperty("idWorkItem").GetInt32();

            var respuestaComentario = await cliente.PostAsJsonAsync(
                $"/api/v1/workitems/{idWorkItem}/comentarios",
                new
                {
                    contenido = $"<p>Hola <span class=\"mencion\" data-type=\"mention\" "
                        + $"data-id=\"{idMencionado}\" data-label=\"prueba\">@prueba</span></p>"
                });
            respuestaComentario.EnsureSuccessStatusCode();

            await using var verificacion = fabricaDatos.ConectarContexto<DbContextGTE>();
            var notificacion = await verificacion.TblNotificacion.AsNoTracking()
                .Where(n => n.IdUsuario == idMencionado && n.Entidad == "WorkItem" && n.IdEntidad == idWorkItem)
                .FirstOrDefaultAsync();

            Assert.NotNull(notificacion);
            Assert.Contains("te mencion", notificacion.Titulo, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await using var limpieza = fabricaDatos.ConectarContexto<DbContextGTE>();
            limpieza.TblNotificacion.RemoveRange(
                limpieza.TblNotificacion.Where(n => n.Entidad == "WorkItem" && n.IdEntidad == idWorkItem));
            limpieza.TblComentario.RemoveRange(
                limpieza.TblComentario.Where(c => c.Entidad == "WorkItem" && c.IdEntidad == idWorkItem));
            limpieza.TblHistorialEstatus.RemoveRange(
                limpieza.TblHistorialEstatus.Where(h => h.Proceso == "WorkItem" && h.IdRegistro == idWorkItem));
            limpieza.TblBitacora.RemoveRange(limpieza.TblBitacora.Where(b => b.IdEntidad == idWorkItem));
            await limpieza.SaveChangesAsync();

            limpieza.TblWorkItem.RemoveRange(limpieza.TblWorkItem.Where(w => w.IdWorkItem == idWorkItem));
            await limpieza.SaveChangesAsync();

            limpieza.TblFolio.RemoveRange(limpieza.TblFolio.Where(f => f.Serie == clave));
            limpieza.TblUsuario.RemoveRange(limpieza.TblUsuario.Where(u => u.IdUsuario == idMencionado));
            limpieza.TblProyecto.RemoveRange(limpieza.TblProyecto.Where(p => p.IdProyecto == idProyecto));
            await limpieza.SaveChangesAsync();
        }
    }
}
