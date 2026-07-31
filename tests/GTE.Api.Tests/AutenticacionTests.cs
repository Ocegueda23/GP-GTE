using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GTE.Api.Tests;

/// <summary>
/// Contrato de seguridad de la API: sin identidad no se entra, y con identidad
/// pero sin roles no se opera. Se omite si no hay base local disponible.
/// </summary>
public class AutenticacionTests(WebApplicationFactory<Program> fabrica)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task SinToken_LosEndpointsDeNegocioResponden401()
    {
        if (!FabricaApiAutenticada.BaseDisponible()) return;

        var cliente = FabricaApiAutenticada.CrearClienteAnonimo(fabrica);

        foreach (var ruta in new[] { "/api/v1/workitems", "/api/v1/auth/sesion", "/api/v1/sprints" })
        {
            var respuesta = await cliente.GetAsync(ruta);
            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        }
    }

    [Fact]
    public async Task SinToken_ElHealthYLaVersionSiguenAbiertos()
    {
        if (!FabricaApiAutenticada.BaseDisponible()) return;

        var cliente = FabricaApiAutenticada.CrearClienteAnonimo(fabrica);

        (await cliente.GetAsync("/health")).EnsureSuccessStatusCode();
        (await cliente.GetAsync("/api/v1/version")).EnsureSuccessStatusCode();
        (await cliente.GetAsync("/api/v1/auth/configuracion")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task TokenAlterado_Responde401()
    {
        if (!FabricaApiAutenticada.BaseDisponible()) return;

        var cliente = await FabricaApiAutenticada.CrearClienteAsync(fabrica, "aviramontes");
        var original = cliente.DefaultRequestHeaders.Authorization!.Parameter!;
        cliente.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", original + "alterado");

        var respuesta = await cliente.GetAsync("/api/v1/workitems");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task ConToken_LaSesionTraeIdentidadYPermisos()
    {
        if (!FabricaApiAutenticada.BaseDisponible()) return;

        var cliente = await FabricaApiAutenticada.CrearClienteAsync(fabrica, "aviramontes");

        var envelope = await cliente.GetFromJsonAsync<JsonElement>("/api/v1/auth/sesion", OpcionesJson);
        var sesion = envelope.GetProperty("response");

        Assert.Equal("aviramontes", sesion.GetProperty("dominio").GetString());
        Assert.False(sesion.GetProperty("sinRoles").GetBoolean());
        Assert.NotEmpty(sesion.GetProperty("permisos").EnumerateArray());
    }

    [Fact]
    public async Task UsuarioSinRoles_LeePeroNoOpera()
    {
        if (!FabricaApiAutenticada.BaseDisponible()) return;

        var dominio = $"prueba{Guid.NewGuid():N}"[..14];
        var cliente = await FabricaApiAutenticada.CrearClienteAsync(fabrica, dominio);

        // Puede consultar
        (await cliente.GetAsync("/api/v1/workitems?pageSize=1")).EnsureSuccessStatusCode();

        // Pero no puede ejecutar acciones que exigen permiso
        var creacionSprint = await cliente.PostAsJsonAsync("/api/v1/sprints", new
        {
            idEquipo = 1,
            nombre = "Sprint sin permiso",
            fechaInicio = "2026-09-01",
            fechaFin = "2026-09-15"
        });
        Assert.Equal(HttpStatusCode.Forbidden, creacionSprint.StatusCode);

        var triage = await cliente.GetAsync("/api/v1/solicitudes");
        Assert.Equal(HttpStatusCode.Forbidden, triage.StatusCode);
    }
}
