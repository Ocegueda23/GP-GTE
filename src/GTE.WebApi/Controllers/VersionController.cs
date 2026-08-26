using System.Reflection;
using GTE.Application.Interfaces;
using GTE.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>Endpoint de diagnostico: version desplegada y ambiente. Sirve de smoke test.</summary>
[ApiController]
[Route("api/v1/version")]
public class VersionController(IWebHostEnvironment ambiente) : ControllerBase
{
    /// <summary>
    /// Version desplegada, en el esquema Proyecto.Mejora.Defecto.Reenvio de Interflo
    /// (CLAUDE.md, seccion "Versionado"). La fija Directory.Build.props y la estampa
    /// publicar.bat. Se lee de InformationalVersion y no de GetName().Version porque esta
    /// ultima normaliza cada digito a entero y perderia los ceros a la izquierda.
    /// Sin estampar (build local desde el IDE) cae a "desarrollo".
    /// </summary>
    private static readonly string Version = ResolverVersion();

    /// <summary>Anonimo a proposito: es el smoke test de despliegue y no expone datos.</summary>
    [HttpGet]
    [AllowAnonymous]
    public ActionResult<ApiResponse<object>> Obtener()
    {
        return Ok(ApiResponse<object>.Exito(new
        {
            sistema = "GTE",
            version = Version,
            ambiente = ambiente.EnvironmentName,
            fechaServidor = DateTime.Now
        }));
    }

    /// <summary>
    /// Diagnostico del almacen de archivos: donde quedo la ruta resuelta y si se puede
    /// escribir ahi. Existe porque una ruta mal configurada en el servidor rompe TODA subida
    /// de archivos, y averiguarlo obligaba a entrar a la maquina a leer los logs. Requiere
    /// identidad (no lleva [AllowAnonymous]): la ruta del share no es dato publico.
    /// </summary>
    [HttpGet("almacen")]
    public ActionResult<ApiResponse<object>> ObtenerAlmacen(
        [FromServices] IAlmacenArchivos almacen)
    {
        var sonda = almacen.Verificar();
        var respuesta = new
        {
            ruta = sonda.Raiz,
            existe = sonda.Existe,
            sePuedeEscribir = sonda.SePuedeEscribir,
            error = sonda.Error
        };

        return sonda.SePuedeEscribir
            ? Ok(ApiResponse<object>.Exito(respuesta, "El almacen de archivos acepta escritura."))
            : Ok(ApiResponse<object>.Falla(ApiResponseCodes.InternalError,
                "El almacen de archivos NO acepta escritura: ninguna subida va a funcionar.",
                sonda.Error, respuesta));
    }

    private static string ResolverVersion()
    {
        var informativa = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrWhiteSpace(informativa))
        {
            return "desarrollo";
        }

        // El SDK anexa "+<hash del commit>" al InformationalVersion; la version es lo de antes.
        return informativa.Split('+')[0];
    }
}
