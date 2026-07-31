using System.Reflection;
using GTE.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>Endpoint de diagnostico: version desplegada y ambiente. Sirve de smoke test.</summary>
[ApiController]
[Route("api/v1/version")]
public class VersionController(IWebHostEnvironment ambiente) : ControllerBase
{
    /// <summary>Anonimo a proposito: es el smoke test de despliegue y no expone datos.</summary>
    [HttpGet]
    [AllowAnonymous]
    public ActionResult<ApiResponse<object>> Obtener()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "desconocida";

        return Ok(ApiResponse<object>.Exito(new
        {
            sistema = "GTE",
            version,
            ambiente = ambiente.EnvironmentName,
            fechaServidor = DateTime.Now
        }));
    }
}
