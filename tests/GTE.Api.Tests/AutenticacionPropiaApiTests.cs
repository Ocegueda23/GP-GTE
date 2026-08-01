using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GTE.Domain.Autenticacion;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GTE.Api.Tests;

/// <summary>
/// E2E del login propio de GTE (sin proveedor externo): password con BCrypt, bloqueo
/// temporal tras intentos fallidos, rotacion de refresh token con deteccion de reuso,
/// logout y cambio/reset de password. Se omite si no hay LocalDB.
/// </summary>
public class AutenticacionPropiaApiTests(WebApplicationFactory<Program> fabricaApp)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string CadenaLocal =
        @"Server=(localdb)\MSSQLLocalDB;Database=bdsGTE;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5";

    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private sealed record Envelope<T>(string Code, bool Success, string UserMessage, T? Response);

    private static bool BaseDisponible() => FabricaApiAutenticada.BaseDisponible();

    private static FabricaContexto CrearFabricaDatos()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:bdsGTE"] = CadenaLocal })
            .Build();
        return new FabricaContexto(configuracion);
    }

    private async Task<(int IdUsuario, string Dominio, string PasswordTemporal)> CrearUsuarioDePruebaAsync(
        HttpClient clienteAdmin, string sufijo)
    {
        var dominio = $"e2eauth{sufijo}";
        var respuesta = await clienteAdmin.PostAsJsonAsync("/api/v1/usuarios", new
        {
            dominio,
            nombre = $"Usuario Auth E2E {sufijo}"
        });
        respuesta.EnsureSuccessStatusCode();
        var envelope = await respuesta.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
        var idUsuario = envelope!.Response.GetProperty("idUsuario").GetInt32();
        var passwordTemporal = envelope.Response.GetProperty("passwordTemporal").GetString()!;
        return (idUsuario, dominio, passwordTemporal);
    }

    private async Task LimpiarUsuarioAsync(int idUsuario)
    {
        var fabricaDatos = CrearFabricaDatos();
        await using var contexto = fabricaDatos.ConectarContexto<DbContextGTE>();
        contexto.TblRefreshToken.RemoveRange(contexto.TblRefreshToken.Where(t => t.IdUsuario == idUsuario));
        contexto.TblBitacora.RemoveRange(contexto.TblBitacora.Where(b => b.Entidad == "Usuario" && b.IdEntidad == idUsuario));
        await contexto.SaveChangesAsync();
        contexto.TblUsuario.RemoveRange(contexto.TblUsuario.Where(u => u.IdUsuario == idUsuario));
        await contexto.SaveChangesAsync();
    }

    [Fact]
    public async Task Login_ConCredencialesValidas_EmiteTokenYRequiereCambioPassword()
    {
        if (!BaseDisponible()) return;

        var clienteAdmin = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var (idUsuario, dominio, passwordTemporal) = await CrearUsuarioDePruebaAsync(clienteAdmin, sufijo);

        try
        {
            var clienteAnonimo = FabricaApiAutenticada.CrearClienteAnonimo(fabricaApp);
            var respuesta = await clienteAnonimo.PostAsJsonAsync(
                "/api/v1/auth/login", new { dominio, password = passwordTemporal });

            respuesta.EnsureSuccessStatusCode();
            var envelope = await respuesta.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
            Assert.True(envelope!.Success);
            Assert.False(string.IsNullOrEmpty(envelope.Response.GetProperty("token").GetString()));
            Assert.True(envelope.Response.GetProperty("requiereCambioPassword").GetBoolean());
            Assert.True(respuesta.Headers.TryGetValues("Set-Cookie", out var cookies));
            Assert.Contains(cookies!, c => c.StartsWith("gte.refresh=", StringComparison.Ordinal));
        }
        finally
        {
            await LimpiarUsuarioAsync(idUsuario);
        }
    }

    [Fact]
    public async Task Login_ConPasswordIncorrecta_MensajeGenericoIgualQueUsuarioInexistente()
    {
        if (!BaseDisponible()) return;

        var clienteAdmin = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var (idUsuario, dominio, _) = await CrearUsuarioDePruebaAsync(clienteAdmin, sufijo);

        try
        {
            var clienteAnonimo = FabricaApiAutenticada.CrearClienteAnonimo(fabricaApp);

            var respuestaPasswordMala = await clienteAnonimo.PostAsJsonAsync(
                "/api/v1/auth/login", new { dominio, password = "no-es-la-correcta" });
            Assert.Equal(HttpStatusCode.BadRequest, respuestaPasswordMala.StatusCode);
            var mensajePasswordMala = (await respuestaPasswordMala.Content
                .ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson))!.UserMessage;

            var respuestaUsuarioInexistente = await clienteAnonimo.PostAsJsonAsync(
                "/api/v1/auth/login", new { dominio = $"no-existe-{sufijo}", password = "cualquiera" });
            Assert.Equal(HttpStatusCode.BadRequest, respuestaUsuarioInexistente.StatusCode);
            var mensajeUsuarioInexistente = (await respuestaUsuarioInexistente.Content
                .ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson))!.UserMessage;

            Assert.Equal(mensajePasswordMala, mensajeUsuarioInexistente);
        }
        finally
        {
            await LimpiarUsuarioAsync(idUsuario);
        }
    }

    [Fact]
    public async Task Login_TrasIntentosMaximosFallidos_BloqueaTemporalmente()
    {
        if (!BaseDisponible()) return;

        var clienteAdmin = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var (idUsuario, dominio, passwordTemporal) = await CrearUsuarioDePruebaAsync(clienteAdmin, sufijo);

        try
        {
            var clienteAnonimo = FabricaApiAutenticada.CrearClienteAnonimo(fabricaApp);

            for (var i = 0; i < ConstantesAutenticacion.IntentosMaximos; i++)
            {
                var respuesta = await clienteAnonimo.PostAsJsonAsync(
                    "/api/v1/auth/login", new { dominio, password = "incorrecta" });
                Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            }

            // Ya bloqueado: ni con la password correcta procede
            var respuestaBloqueada = await clienteAnonimo.PostAsJsonAsync(
                "/api/v1/auth/login", new { dominio, password = passwordTemporal });
            Assert.Equal(HttpStatusCode.Forbidden, respuestaBloqueada.StatusCode);
        }
        finally
        {
            await LimpiarUsuarioAsync(idUsuario);
        }
    }

    /// <summary>
    /// Cliente con manejo de cookies desactivado: permite adjuntar a mano la cookie de
    /// refresh en cada solicitud (WebApplicationFactory con HandleCookies=true no deja
    /// fijar el encabezado Cookie manualmente, lanza InvalidOperationException).
    /// </summary>
    private HttpClient CrearClienteSinManejoDeCookies()
    {
        return FabricaApiAutenticada.Configurar(fabricaApp)
            .CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    }

    private static async Task<(string Token, string CookieRefresh)> LoginConCookieManualAsync(
        HttpClient cliente, string dominio, string password)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/v1/auth/login", new { dominio, password });
        respuesta.EnsureSuccessStatusCode();
        var token = (await respuesta.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson))!
            .Response.GetProperty("token").GetString()!;
        return (token, ExtraerCookieRefresh(respuesta));
    }

    private static async Task<HttpResponseMessage> RefrescarConCookieAsync(HttpClient cliente, string cookieRefresh)
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        solicitud.Headers.Add("Cookie", $"gte.refresh={cookieRefresh}");
        return await cliente.SendAsync(solicitud);
    }

    [Fact]
    public async Task Refresh_RotaElToken_YElReusoDeUnoViejoRevocaTodaLaCadena()
    {
        if (!BaseDisponible()) return;

        var clienteAdmin = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var (idUsuario, dominio, passwordTemporal) = await CrearUsuarioDePruebaAsync(clienteAdmin, sufijo);

        try
        {
            var cliente = CrearClienteSinManejoDeCookies();

            var (_, cookieOriginal) = await LoginConCookieManualAsync(cliente, dominio, passwordTemporal);

            // Rotacion legitima: la cookie original avanza a una nueva (el access token
            // puede coincidir si se emite en el mismo segundo con los mismos reclamos;
            // lo que debe cambiar siempre es el refresh token, aleatorio en cada emision)
            var primerRefresh = await RefrescarConCookieAsync(cliente, cookieOriginal);
            primerRefresh.EnsureSuccessStatusCode();
            var cookieRotada = ExtraerCookieRefresh(primerRefresh);
            Assert.NotEqual(cookieOriginal, cookieRotada);

            // Reuso de la cookie ORIGINAL (ya revocada por la rotacion): se rechaza y ademas
            // revoca toda la cadena, incluida la cookie rotada legitima
            var respuestaReuso = await RefrescarConCookieAsync(cliente, cookieOriginal);
            Assert.Equal(HttpStatusCode.Forbidden, respuestaReuso.StatusCode);

            var respuestaConLaRotadaLegitima = await RefrescarConCookieAsync(cliente, cookieRotada);
            Assert.Equal(HttpStatusCode.Forbidden, respuestaConLaRotadaLegitima.StatusCode);

            var refreshSinCookie = await FabricaApiAutenticada.CrearClienteAnonimo(fabricaApp)
                .PostAsync("/api/v1/auth/refresh", null);
            Assert.Equal(HttpStatusCode.Unauthorized, refreshSinCookie.StatusCode);
        }
        finally
        {
            await LimpiarUsuarioAsync(idUsuario);
        }
    }

    private static string ExtraerCookieRefresh(HttpResponseMessage respuesta)
    {
        var encabezado = respuesta.Headers.GetValues("Set-Cookie")
            .First(c => c.StartsWith("gte.refresh=", StringComparison.Ordinal));
        var valor = encabezado[(encabezado.IndexOf('=') + 1)..];
        return valor[..valor.IndexOf(';')];
    }

    [Fact]
    public async Task Logout_RevocaElRefreshToken_YaNoSePuedeRefrescar()
    {
        if (!BaseDisponible()) return;

        var clienteAdmin = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var (idUsuario, dominio, passwordTemporal) = await CrearUsuarioDePruebaAsync(clienteAdmin, sufijo);

        try
        {
            var cliente = FabricaApiAutenticada.CrearClienteAnonimo(fabricaApp);
            var login = await cliente.PostAsJsonAsync("/api/v1/auth/login", new { dominio, password = passwordTemporal });
            login.EnsureSuccessStatusCode();

            var logout = await cliente.PostAsync("/api/v1/auth/logout", null);
            logout.EnsureSuccessStatusCode();

            var refreshTrasLogout = await cliente.PostAsync("/api/v1/auth/refresh", null);
            Assert.Equal(HttpStatusCode.Unauthorized, refreshTrasLogout.StatusCode);
        }
        finally
        {
            await LimpiarUsuarioAsync(idUsuario);
        }
    }

    [Fact]
    public async Task CambiarPassword_ConPasswordActualCorrecta_PermiteLoginConLaNueva()
    {
        if (!BaseDisponible()) return;

        var clienteAdmin = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var (idUsuario, dominio, passwordTemporal) = await CrearUsuarioDePruebaAsync(clienteAdmin, sufijo);

        try
        {
            var cliente = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, dominio);

            var cambio = await cliente.PostAsJsonAsync("/api/v1/auth/cambiar-password", new
            {
                passwordActual = passwordTemporal,
                passwordNueva = "NuevaPassword123"
            });
            cambio.EnsureSuccessStatusCode();

            var clienteLogin = FabricaApiAutenticada.CrearClienteAnonimo(fabricaApp);

            var loginConVieja = await clienteLogin.PostAsJsonAsync(
                "/api/v1/auth/login", new { dominio, password = passwordTemporal });
            Assert.Equal(HttpStatusCode.BadRequest, loginConVieja.StatusCode);

            var loginConNueva = await clienteLogin.PostAsJsonAsync(
                "/api/v1/auth/login", new { dominio, password = "NuevaPassword123" });
            loginConNueva.EnsureSuccessStatusCode();
        }
        finally
        {
            await LimpiarUsuarioAsync(idUsuario);
        }
    }

    [Fact]
    public async Task EstablecerPasswordAdmin_InvalidaLaAnteriorYPermiteLoginConLaNueva()
    {
        if (!BaseDisponible()) return;

        var clienteAdmin = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var (idUsuario, dominio, passwordOriginal) = await CrearUsuarioDePruebaAsync(clienteAdmin, sufijo);

        try
        {
            var reset = await clienteAdmin.PutAsync($"/api/v1/usuarios/{idUsuario}/password", null);
            reset.EnsureSuccessStatusCode();
            var nuevaPassword = (await reset.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson))!
                .Response.GetProperty("passwordTemporal").GetString()!;

            var clienteLogin = FabricaApiAutenticada.CrearClienteAnonimo(fabricaApp);

            var loginConOriginal = await clienteLogin.PostAsJsonAsync(
                "/api/v1/auth/login", new { dominio, password = passwordOriginal });
            Assert.Equal(HttpStatusCode.BadRequest, loginConOriginal.StatusCode);

            var loginConNueva = await clienteLogin.PostAsJsonAsync(
                "/api/v1/auth/login", new { dominio, password = nuevaPassword });
            loginConNueva.EnsureSuccessStatusCode();
        }
        finally
        {
            await LimpiarUsuarioAsync(idUsuario);
        }
    }
}
