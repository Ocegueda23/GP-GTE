using System.Security.Cryptography;
using System.Text;
using GTE.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace GTE.WebApi.Seguridad;

/// <summary>
/// Registro de la autenticacion propia de GTE. Regla dura: fuera de Development,
/// Jwt:ClaveFirma es obligatoria -- la API no arranca sin ella en vez de quedar abierta
/// o de generar una clave efimera en un ambiente real.
/// </summary>
public static class ConfiguracionAutenticacion
{
    public static IServiceCollection AgregarAutenticacionGte(
        this IServiceCollection servicios,
        IConfiguration configuracion,
        IWebHostEnvironment ambiente)
    {
        var opciones = configuracion.GetSection(OpcionesAutenticacion.Seccion)
            .Get<OpcionesAutenticacion>() ?? new OpcionesAutenticacion();

        servicios.Configure<OpcionesAutenticacion>(
            configuracion.GetSection(OpcionesAutenticacion.Seccion));

        var claveBytes = ObtenerClaveFirma(opciones, ambiente);
        servicios.AddSingleton(new ClaveFirmaGte(
            claveBytes, opciones.Issuer, opciones.Audience, opciones.MinutosVigenciaAcceso));

        servicios.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(config =>
            {
                config.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = opciones.Issuer,
                    ValidateAudience = true,
                    ValidAudience = opciones.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(claveBytes),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };
            });

        // Toda la API exige identidad; los permisos finos los evalua cada caso de uso
        servicios.AddAuthorization(opcionesAutorizacion =>
        {
            opcionesAutorizacion.FallbackPolicy = new Microsoft.AspNetCore.Authorization
                .AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return servicios;
    }

    private static byte[] ObtenerClaveFirma(OpcionesAutenticacion opciones, IWebHostEnvironment ambiente)
    {
        if (!string.IsNullOrWhiteSpace(opciones.ClaveFirma))
        {
            if (Encoding.UTF8.GetByteCount(opciones.ClaveFirma) < 32)
            {
                throw new InvalidOperationException("Jwt:ClaveFirma debe tener al menos 32 caracteres.");
            }
            return Encoding.UTF8.GetBytes(opciones.ClaveFirma);
        }

        if (!ambiente.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Falta Jwt:ClaveFirma. La API no arranca sin una clave de firma real fuera de Development.");
        }

        // Development sin clave configurada: efimera, valida solo mientras el proceso vive.
        return RandomNumberGenerator.GetBytes(48);
    }
}
