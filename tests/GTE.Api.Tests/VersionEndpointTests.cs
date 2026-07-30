using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GTE.Api.Tests;

public class VersionEndpointTests(WebApplicationFactory<Program> fabrica)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Version_RespondeConEnvelopeExitoso()
    {
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/api/v1/version");

        respuesta.EnsureSuccessStatusCode();
        var json = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("\"success\":true", json);
        Assert.Contains("\"code\":\"OK\"", json);
        Assert.Contains("GTE", json);
    }

    [Fact]
    public async Task Health_Responde()
    {
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/health");

        respuesta.EnsureSuccessStatusCode();
    }
}
