using GTE.Application.Ayuda.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>
/// Documentos de ayuda que no pueden vivir en wwwroot porque exigen permiso. El Manual de
/// usuario si es estatico y publico dentro de la SPA (todos lo pueden leer); el Centro de
/// Mando TI describe como se evalua el desempeno de cada responsable de area, asi que se
/// sirve por aqui y el permiso se valida en el handler.
///
/// Devuelve HTML crudo en vez de ApiResponse&lt;T&gt; por la misma razon que la descarga de
/// adjuntos (ArchivosController): el consumidor es un iframe, no el cliente tipado.
/// </summary>
[ApiController]
[Route("api/v1/ayuda")]
public class AyudaController(IMediator mediator) : ControllerBase
{
    [HttpGet("centro-mando-ti")]
    public async Task<IActionResult> ObtenerCentroMando(CancellationToken cancellationToken)
    {
        var html = await mediator.Send(new ObtenerCentroMandoQuery(), cancellationToken);
        return Content(html, "text/html; charset=utf-8");
    }
}
