using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace GTE.WebApi.Seguridad;

/// <summary>
/// Registro de la autenticacion de GTE. Reglas duras:
/// - Produccion exige identidad externa configurada (Entra ID); si falta, la API
///   no arranca en vez de quedar abierta.
/// - El emisor local de desarrollo jamas se activa fuera de Development.
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

        var usarEmisorLocal = ambiente.IsDevelopment()
                              && opciones.Desarrollo.Habilitado
                              && !opciones.TieneIdentidadExterna;

        if (!opciones.TieneIdentidadExterna && !usarEmisorLocal)
        {
            throw new InvalidOperationException(
                "No hay forma de autenticar: configure Jwt:Authority con el tenant de Entra ID "
                + "(o habilite Jwt:Desarrollo:Habilitado unicamente en el ambiente Development). "
                + "La API no arranca sin autenticacion para no quedar abierta.");
        }

        servicios.Configure<OpcionesAutenticacion>(
            configuracion.GetSection(OpcionesAutenticacion.Seccion));

        if (usarEmisorLocal)
        {
            var clave = ObtenerClaveDesarrollo(opciones.Desarrollo);
            servicios.AddSingleton(new ClaveFirmaDesarrollo(clave, opciones.Desarrollo));

            servicios.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(config =>
                {
                    config.RequireHttpsMetadata = false;
                    config.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = opciones.Desarrollo.Issuer,
                        ValidateAudience = true,
                        ValidAudience = opciones.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = clave,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(2)
                    };
                });
        }
        else
        {
            servicios.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(config =>
                {
                    config.Authority = opciones.Authority;
                    config.Audience = opciones.Audience;
                    config.RequireHttpsMetadata = !ambiente.IsDevelopment();
                    config.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(2)
                    };
                });
        }

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

    private static SymmetricSecurityKey ObtenerClaveDesarrollo(EmisorDesarrollo desarrollo)
    {
        if (!string.IsNullOrWhiteSpace(desarrollo.ClaveFirma))
        {
            if (Encoding.UTF8.GetByteCount(desarrollo.ClaveFirma) < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:Desarrollo:ClaveFirma debe tener al menos 32 caracteres.");
            }
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(desarrollo.ClaveFirma));
        }

        // Sin clave configurada: se genera una efimera, distinta en cada arranque
        var aleatoria = RandomNumberGenerator.GetBytes(48);
        return new SymmetricSecurityKey(aleatoria);
    }
}

/// <summary>Clave de firma del emisor local, disponible solo cuando esta habilitado.</summary>
public record ClaveFirmaDesarrollo(SymmetricSecurityKey Clave, EmisorDesarrollo Opciones);
