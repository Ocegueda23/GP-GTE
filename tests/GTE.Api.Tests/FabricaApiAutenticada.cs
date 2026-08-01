using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;

namespace GTE.Api.Tests;

/// <summary>
/// Arranca la API de pruebas con el emisor local de tokens habilitado y entrega
/// clientes ya autenticados. Toda la API exige identidad, asi que las pruebas de
/// integracion deben iniciar sesion como lo haria la interfaz.
/// </summary>
public static class FabricaApiAutenticada
{
    public const string CadenaLocal =
        @"Server=(localdb)\MSSQLLocalDB;Database=bdsGTE;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5";

    private const string ClaveFirmaPruebas = "clave-de-firma-solo-para-pruebas-integracion-32";

    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    public static bool BaseDisponible()
    {
        try
        {
            using var conexion = new SqlConnection(CadenaLocal);
            conexion.Open();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static WebApplicationFactory<Program> Configurar(WebApplicationFactory<Program> fabrica)
    {
        return fabrica.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.UseSetting("ConnectionStrings:bdsGTE", CadenaLocal);
            builder.UseSetting("Jwt:Issuer", "gte-api");
            builder.UseSetting("Jwt:Audience", "gte-api");
            builder.UseSetting("Jwt:ClaveFirma", ClaveFirmaPruebas);
            builder.UseSetting("Jwt:Desarrollo:Habilitado", "true");
        });
    }

    /// <summary>Cliente con el token de la cuenta indicada ya puesto en el encabezado.</summary>
    public static async Task<HttpClient> CrearClienteAsync(
        WebApplicationFactory<Program> fabrica, string dominio)
    {
        var cliente = Configurar(fabrica).CreateClient();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/auth/desarrollo/token", new { dominio });
        respuesta.EnsureSuccessStatusCode();

        var contenido = await respuesta.Content.ReadFromJsonAsync<JsonElement>(OpcionesJson);
        var token = contenido.GetProperty("response").GetProperty("token").GetString()
            ?? throw new InvalidOperationException("El emisor de desarrollo no devolvio token.");

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    /// <summary>Cliente sin credenciales, para comprobar que la API rechaza el anonimato.</summary>
    public static HttpClient CrearClienteAnonimo(WebApplicationFactory<Program> fabrica)
    {
        return Configurar(fabrica).CreateClient();
    }
}
