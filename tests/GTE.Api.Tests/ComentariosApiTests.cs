using System.Net;
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
/// E2E del modulo Comentarios: alta, hilo (padre+hijo), sanitizacion de HTML peligroso
/// y la regla de autoria (solo quien escribio puede borrar). Se omite si no hay LocalDB.
/// </summary>
public class ComentariosApiTests(WebApplicationFactory<Program> fabricaApp)
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

    private static async Task<(int IdProyecto, string Clave)> CrearProyectoAsync(FabricaContexto fabricaDatos)
    {
        await using var contexto = fabricaDatos.ConectarContexto<DbContextGTE>();
        var sufijo = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var clave = $"COM{sufijo}";
        var proyecto = new TblProyecto
        {
            Clave = clave,
            Nombre = $"Proyecto Comentarios {sufijo}",
            IdCategoriaProyecto = 1,
            IdEstatusProyecto = 3,
            UsuarioRegistro = "e2e",
            Activo = true
        };
        contexto.TblProyecto.Add(proyecto);
        await contexto.SaveChangesAsync();
        return (proyecto.IdProyecto, clave);
    }

    private static async Task<int> CrearWorkItemAsync(HttpClient cliente, int idProyecto)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/v1/workitems", new
        {
            idProyecto,
            idTipoWorkItem = 3,
            titulo = "Item para comentarios",
            idPrioridad = 3,
            fechaCompromiso = DateTime.Today.AddDays(5)
        });
        respuesta.EnsureSuccessStatusCode();
        var envelope = await respuesta.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
        Assert.NotNull(envelope);
        return envelope.Response.GetProperty("idWorkItem").GetInt32();
    }

    private static async Task LimpiarAsync(FabricaContexto fabricaDatos, string clave, int idProyecto, int idWorkItem)
    {
        await using var contexto = fabricaDatos.ConectarContexto<DbContextGTE>();
        contexto.TblComentario.RemoveRange(
            contexto.TblComentario.Where(c => c.Entidad == "WorkItem" && c.IdEntidad == idWorkItem));
        contexto.TblHistorialEstatus.RemoveRange(
            contexto.TblHistorialEstatus.Where(h => h.Proceso == "WorkItem" && h.IdRegistro == idWorkItem));
        contexto.TblBitacora.RemoveRange(
            contexto.TblBitacora.Where(b => b.IdEntidad == idWorkItem));
        await contexto.SaveChangesAsync();

        contexto.TblWorkItem.RemoveRange(contexto.TblWorkItem.Where(w => w.IdWorkItem == idWorkItem));
        await contexto.SaveChangesAsync();

        contexto.TblFolio.RemoveRange(contexto.TblFolio.Where(f => f.Serie == clave));
        contexto.TblProyecto.RemoveRange(contexto.TblProyecto.Where(p => p.IdProyecto == idProyecto));
        await contexto.SaveChangesAsync();
    }

    [Fact]
    public async Task Vertical_ComentarHiloSanitizarYBorrarPropio()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var fabricaDatos = CrearFabricaDatos();
        var cliente = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var (idProyecto, clave) = await CrearProyectoAsync(fabricaDatos);
        var idWorkItem = await CrearWorkItemAsync(cliente, idProyecto);

        try
        {
            // 1. Comentario con HTML peligroso: el script debe desaparecer, el formato basico se conserva
            var respuestaAlta = await cliente.PostAsJsonAsync(
                $"/api/v1/workitems/{idWorkItem}/comentarios",
                new { contenido = "<p><strong>Hola</strong> mundo</p><script>alert(1)</script>" });
            respuestaAlta.EnsureSuccessStatusCode();
            var comentario = await respuestaAlta.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
            Assert.NotNull(comentario);
            var contenidoGuardado = comentario.Response.GetProperty("contenido").GetString();
            Assert.Contains("<strong>Hola</strong>", contenidoGuardado);
            Assert.DoesNotContain("script", contenidoGuardado, StringComparison.OrdinalIgnoreCase);
            var idComentarioPadre = comentario.Response.GetProperty("idComentario").GetInt32();

            // 2. Respuesta en el hilo
            var respuestaHijo = await cliente.PostAsJsonAsync(
                $"/api/v1/workitems/{idWorkItem}/comentarios",
                new { contenido = "<p>Respuesta</p>", idComentarioPadre });
            respuestaHijo.EnsureSuccessStatusCode();

            // 3. El listado trae ambos, en orden cronologico, con el hilo marcado
            var listado = await cliente.GetFromJsonAsync<Envelope<JsonElement[]>>(
                $"/api/v1/workitems/{idWorkItem}/comentarios", OpcionesJson);
            Assert.NotNull(listado);
            Assert.Equal(2, listado.Response!.Length);
            Assert.Equal(idComentarioPadre, listado.Response[1].GetProperty("idComentarioPadre").GetInt32());

            // 4. Otra identidad no puede borrar el comentario ajeno
            var clienteAjeno = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "lgarcia");
            var respuestaAjena = await clienteAjeno.DeleteAsync($"/api/v1/comentarios/{idComentarioPadre}");
            Assert.Equal(HttpStatusCode.Forbidden, respuestaAjena.StatusCode);

            // 5. El autor si puede borrar el propio
            var respuestaBorrar = await cliente.DeleteAsync($"/api/v1/comentarios/{idComentarioPadre}");
            respuestaBorrar.EnsureSuccessStatusCode();

            var listadoFinal = await cliente.GetFromJsonAsync<Envelope<JsonElement[]>>(
                $"/api/v1/workitems/{idWorkItem}/comentarios", OpcionesJson);
            Assert.Single(listadoFinal!.Response!);
        }
        finally
        {
            await LimpiarAsync(fabricaDatos, clave, idProyecto, idWorkItem);
        }
    }
}
