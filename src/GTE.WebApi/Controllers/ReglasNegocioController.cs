using GTE.Application.DTOs.Request.ReglasNegocio;
using GTE.Application.DTOs.Responses.ReglasNegocio;
using GTE.Application.ReglasNegocio.Commands;
using GTE.Application.ReglasNegocio.Queries;
using GTE.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTE.WebApi.Controllers;

/// <summary>
/// Catalogo de reglas de negocio por proyecto.
///
/// Toda regla pertenece a un proyecto dueno y puede declararse como afectando a otros
/// proyectos (lista explicita). Al consultar un proyecto se devuelven sus reglas propias
/// y las heredadas de otros proyectos, etiquetadas con su origen; las heredadas solo se
/// editan en su proyecto dueno.
///
/// Todo exige autenticacion (FallbackPolicy) y cada handler valida RGN.Ver o
/// RGN.Administrar con alcance por proyecto.
/// </summary>
[ApiController]
[Route("api/v1/reglas-negocio")]
public class ReglasNegocioController(IMediator mediator) : ControllerBase
{
    /// <summary>Proyectos con reglas, acotado al alcance del usuario (selector de la UI).</summary>
    [HttpGet("proyectos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProyectoConReglasResponse>>>> ObtenerProyectos(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerProyectosConReglasQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ProyectoConReglasResponse>>.Exito(resultado));
    }

    /// <summary>Catalogos de apoyo de los selects (tipos de ambito, estados, tipos de relacion).</summary>
    [HttpGet("catalogos")]
    public async Task<ActionResult<ApiResponse<CatalogosReglasNegocioResponse>>> ObtenerCatalogos(
        CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerCatalogosReglasQuery(), cancellationToken);
        return Ok(ApiResponse<CatalogosReglasNegocioResponse>.Exito(resultado));
    }

    /// <summary>Reglas de un proyecto: propias mas las que otros proyectos declararon que lo afectan.</summary>
    [HttpGet("proyectos/{idProyecto:int}")]
    public async Task<ActionResult<ApiResponse<CatalogoReglasProyectoResponse>>> ObtenerCatalogoProyecto(
        int idProyecto,
        [FromQuery] int? idAmbitoRegla = null,
        [FromQuery] int? idEstadoReglaNegocio = null,
        [FromQuery] int? idTipoAmbitoRegla = null,
        [FromQuery] string? busqueda = null,
        [FromQuery] bool incluirHeredadas = true,
        [FromQuery] bool incluirDerogadas = false,
        CancellationToken cancellationToken = default)
    {
        var filtro = new ReglasNegocioFiltroRequest(
            idProyecto, idAmbitoRegla, idEstadoReglaNegocio, idTipoAmbitoRegla, busqueda,
            incluirHeredadas, incluirDerogadas);

        var resultado = await mediator.Send(
            new ObtenerCatalogoProyectoQuery(idProyecto, filtro), cancellationToken);
        return Ok(ApiResponse<CatalogoReglasProyectoResponse>.Exito(resultado));
    }

    /// <summary>Flujos de operacion y caracteristicas del sistema de un proyecto.</summary>
    [HttpGet("proyectos/{idProyecto:int}/ambitos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AmbitoReglaResponse>>>> ObtenerAmbitos(
        int idProyecto, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerAmbitosProyectoQuery(idProyecto), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AmbitoReglaResponse>>.Exito(resultado));
    }

    /// <summary>Busqueda que cruza proyectos ("en que proyecto estaba esa regla").</summary>
    [HttpGet("buscar")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReglaNegocioResumenResponse>>>> Buscar(
        [FromQuery] string? busqueda = null,
        [FromQuery] int? idProyecto = null,
        [FromQuery] int? idEstadoReglaNegocio = null,
        [FromQuery] int? idTipoAmbitoRegla = null,
        [FromQuery] bool incluirDerogadas = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var filtro = new ReglasNegocioFiltroRequest(
            idProyecto, null, idEstadoReglaNegocio, idTipoAmbitoRegla, busqueda,
            IncluirHeredadas: false, incluirDerogadas, page, pageSize);

        var resultado = await mediator.Send(new BuscarReglasNegocioQuery(filtro), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ReglaNegocioResumenResponse>>.Exito(resultado));
    }

    /// <summary>Ficha de la regla, con el panel de a que proyectos afecta.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ReglaNegocioResponse>>> ObtenerPorId(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerReglaNegocioQuery(id), cancellationToken);
        return Ok(ApiResponse<ReglaNegocioResponse>.Exito(resultado));
    }

    [HttpGet("{id:int}/versiones")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReglaVersionResponse>>>> ObtenerVersiones(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ObtenerVersionesReglaQuery(id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ReglaVersionResponse>>.Exito(resultado));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReglaNegocioResponse>>> Crear(
        [FromBody] ReglaNegocioCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new CrearReglaNegocioCommand(request), cancellationToken);
        return Ok(ApiResponse<ReglaNegocioResponse>.Exito(resultado, "Regla creada."));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ReglaNegocioResponse>>> Actualizar(
        int id, [FromBody] ReglaNegocioActualizarRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new ActualizarReglaNegocioCommand(id, request), cancellationToken);
        return Ok(ApiResponse<ReglaNegocioResponse>.Exito(resultado, "Regla actualizada."));
    }

    /// <summary>Derogacion: baja logica mas estado Derogada. Nunca borrado fisico.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Derogar(
        int id, [FromQuery] DateOnly? fechaHasta = null, CancellationToken cancellationToken = default)
    {
        await mediator.Send(new DerogarReglaNegocioCommand(id, fechaHasta), cancellationToken);
        return Ok(ApiResponse<bool>.Exito(true, "Regla derogada."));
    }

    [HttpPost("{id:int}/reactivar")]
    public async Task<ActionResult<ApiResponse<bool>>> Reactivar(int id, CancellationToken cancellationToken)
    {
        await mediator.Send(new ReactivarReglaNegocioCommand(id), cancellationToken);
        return Ok(ApiResponse<bool>.Exito(true, "Regla reactivada."));
    }

    /// <summary>Declara que la regla afecta ademas a otro proyecto.</summary>
    [HttpPost("{id:int}/impactos")]
    public async Task<ActionResult<ApiResponse<ReglaNegocioResponse>>> AgregarImpacto(
        int id, [FromBody] ImpactoReglaCrearRequest request, CancellationToken cancellationToken)
    {
        var resultado = await mediator.Send(new AgregarImpactoReglaCommand(id, request), cancellationToken);
        return Ok(ApiResponse<ReglaNegocioResponse>.Exito(resultado, "Proyecto afectado agregado."));
    }

    [HttpDelete("impactos/{idImpacto:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> QuitarImpacto(
        int idImpacto, CancellationToken cancellationToken)
    {
        await mediator.Send(new QuitarImpactoReglaCommand(idImpacto), cancellationToken);
        return Ok(ApiResponse<bool>.Exito(true, "Proyecto afectado quitado."));
    }

    [HttpPost("ambitos")]
    public async Task<ActionResult<ApiResponse<int>>> CrearAmbito(
        [FromBody] AmbitoReglaCrearRequest request, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(new CrearAmbitoReglaCommand(request), cancellationToken);
        return Ok(ApiResponse<int>.Exito(id, "Elemento creado."));
    }

    [HttpPut("ambitos/{idAmbito:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> ActualizarAmbito(
        int idAmbito, [FromBody] AmbitoReglaActualizarRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new ActualizarAmbitoReglaCommand(idAmbito, request), cancellationToken);
        return Ok(ApiResponse<bool>.Exito(true, "Elemento actualizado."));
    }

    [HttpDelete("ambitos/{idAmbito:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> EliminarAmbito(
        int idAmbito, CancellationToken cancellationToken)
    {
        await mediator.Send(new EliminarAmbitoReglaCommand(idAmbito), cancellationToken);
        return Ok(ApiResponse<bool>.Exito(true, "Elemento dado de baja."));
    }
}
