using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GTE.Api.Tests;

/// <summary>
/// E2E del modulo Archivos: subida, listado, descarga por streaming (bytes identicos),
/// rechazo por extension no permitida y la regla de autoria al borrar. Se omite si no
/// hay LocalDB.
/// </summary>
public class ArchivosApiTests(WebApplicationFactory<Program> fabricaApp)
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
        var clave = $"ARC{sufijo}";
        var proyecto = new TblProyecto
        {
            Clave = clave,
            Nombre = $"Proyecto Archivos {sufijo}",
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
            titulo = "Item para archivos",
            idPrioridad = 3,
            fechaCompromiso = DateTime.Today.AddDays(5)
        });
        respuesta.EnsureSuccessStatusCode();
        var envelope = await respuesta.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
        Assert.NotNull(envelope);
        return envelope.Response.GetProperty("idWorkItem").GetInt32();
    }

    private static async Task<HttpResponseMessage> SubirAsync(
        HttpClient cliente, int idWorkItem, string nombreArchivo, byte[] contenido)
    {
        using var formulario = new MultipartFormDataContent();
        using var archivoContenido = new ByteArrayContent(contenido);
        formulario.Add(archivoContenido, "archivo", nombreArchivo);
        return await cliente.PostAsync($"/api/v1/workitems/{idWorkItem}/archivos", formulario);
    }

    private static async Task LimpiarAsync(FabricaContexto fabricaDatos, string clave, int idProyecto, int idWorkItem)
    {
        await using var contexto = fabricaDatos.ConectarContexto<DbContextGTE>();
        var vinculos = await contexto.TblArchivoVinculo
            .Where(v => v.Entidad == "WorkItem" && v.IdEntidad == idWorkItem)
            .ToListAsync();
        var idsArchivo = vinculos.Select(v => v.IdArchivo).ToArray();
        contexto.TblArchivoVinculo.RemoveRange(vinculos);
        await contexto.SaveChangesAsync();

        contexto.TblArchivo.RemoveRange(contexto.TblArchivo.Where(a => idsArchivo.Contains(a.IdArchivo)));
        contexto.TblHistorialEstatus.RemoveRange(
            contexto.TblHistorialEstatus.Where(h => h.Proceso == "WorkItem" && h.IdRegistro == idWorkItem));
        contexto.TblBitacora.RemoveRange(contexto.TblBitacora.Where(b => b.IdEntidad == idWorkItem));
        await contexto.SaveChangesAsync();

        contexto.TblWorkItem.RemoveRange(contexto.TblWorkItem.Where(w => w.IdWorkItem == idWorkItem));
        await contexto.SaveChangesAsync();

        contexto.TblFolio.RemoveRange(contexto.TblFolio.Where(f => f.Serie == clave));
        contexto.TblProyecto.RemoveRange(contexto.TblProyecto.Where(p => p.IdProyecto == idProyecto));
        await contexto.SaveChangesAsync();
    }

    [Fact]
    public async Task Vertical_SubirDescargarRechazarYBorrarPropio()
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
            // 1. Extension no permitida se rechaza (400) antes de tocar disco
            var respuestaRechazada = await SubirAsync(
                cliente, idWorkItem, "instalador.exe", Encoding.UTF8.GetBytes("contenido"));
            Assert.Equal(HttpStatusCode.BadRequest, respuestaRechazada.StatusCode);

            // 2. Subida valida
            var contenidoOriginal = Encoding.UTF8.GetBytes("contenido de prueba E2E");
            var respuestaSubida = await SubirAsync(cliente, idWorkItem, "prueba.txt", contenidoOriginal);
            respuestaSubida.EnsureSuccessStatusCode();
            var subido = await respuestaSubida.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
            Assert.NotNull(subido);
            var guidArchivo = subido.Response.GetProperty("guidArchivo").GetGuid();
            var idArchivoVinculo = subido.Response.GetProperty("idArchivoVinculo").GetInt32();

            // 3. Aparece en el listado del WorkItem
            var listado = await cliente.GetFromJsonAsync<Envelope<JsonElement[]>>(
                $"/api/v1/workitems/{idWorkItem}/archivos", OpcionesJson);
            Assert.NotNull(listado);
            Assert.Single(listado.Response!);

            // 4. La descarga devuelve exactamente los mismos bytes
            var respuestaDescarga = await cliente.GetAsync($"/api/v1/archivos/{guidArchivo}");
            respuestaDescarga.EnsureSuccessStatusCode();
            var bytesDescargados = await respuestaDescarga.Content.ReadAsByteArrayAsync();
            Assert.Equal(contenidoOriginal, bytesDescargados);

            // 5. Otra identidad no puede borrar el adjunto ajeno
            var clienteAjeno = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "lgarcia");
            var respuestaAjena = await clienteAjeno.DeleteAsync($"/api/v1/archivos-vinculo/{idArchivoVinculo}");
            Assert.Equal(HttpStatusCode.Forbidden, respuestaAjena.StatusCode);

            // 6. El autor si puede borrar el propio; tras la baja ya no aparece en el listado
            var respuestaBorrar = await cliente.DeleteAsync($"/api/v1/archivos-vinculo/{idArchivoVinculo}");
            respuestaBorrar.EnsureSuccessStatusCode();

            var listadoFinal = await cliente.GetFromJsonAsync<Envelope<JsonElement[]>>(
                $"/api/v1/workitems/{idWorkItem}/archivos", OpcionesJson);
            Assert.Empty(listadoFinal!.Response!);
        }
        finally
        {
            await LimpiarAsync(fabricaDatos, clave, idProyecto, idWorkItem);
        }
    }
}
