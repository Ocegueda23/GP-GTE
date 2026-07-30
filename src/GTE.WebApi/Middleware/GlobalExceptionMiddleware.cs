using FluentValidation;
using GTE.Domain.Exceptions;
using GTE.WebApi.Models;

namespace GTE.WebApi.Middleware;

/// <summary>
/// Mapea excepciones de dominio a HTTP con el envelope ApiResponse:
/// NotFound 404, Validation/Business 400, Conflict 409 (con detalle estructurado),
/// Forbidden 403, resto 500 (mensaje generico al cliente, detalle al log).
/// </summary>
public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await next(contexto);
        }
        catch (NotFoundException ex)
        {
            await EscribirRespuestaAsync(contexto, StatusCodes.Status404NotFound,
                ApiResponse<object>.Falla(ApiResponseCodes.NotFound, ex.Message));
        }
        catch (ValidationException ex)
        {
            var errores = ex.Errors
                .Select(e => new { campo = e.PropertyName, error = e.ErrorMessage })
                .ToList();
            await EscribirRespuestaAsync(contexto, StatusCodes.Status400BadRequest,
                ApiResponse<object>.Falla(ApiResponseCodes.ValidationError,
                    "Los datos enviados no son validos.", ex.Message, new { errores }));
        }
        catch (BusinessException ex)
        {
            await EscribirRespuestaAsync(contexto, StatusCodes.Status400BadRequest,
                ApiResponse<object>.Falla(ApiResponseCodes.ValidationError, ex.Message));
        }
        catch (ConflictException ex)
        {
            await EscribirRespuestaAsync(contexto, StatusCodes.Status409Conflict,
                ApiResponse<object>.Falla(ApiResponseCodes.Conflict, ex.Message,
                    ex.GetType().Name, new { detalle = ex.Detalle }));
        }
        catch (ForbiddenException ex)
        {
            await EscribirRespuestaAsync(contexto, StatusCodes.Status403Forbidden,
                ApiResponse<object>.Falla(ApiResponseCodes.Forbidden, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error no controlado en {Ruta}", contexto.Request.Path);
            await EscribirRespuestaAsync(contexto, StatusCodes.Status500InternalServerError,
                ApiResponse<object>.Falla(ApiResponseCodes.InternalError,
                    "Ocurrio un error inesperado. El equipo tecnico ya fue notificado."));
        }
    }

    private static async Task EscribirRespuestaAsync(
        HttpContext contexto, int codigoHttp, ApiResponse<object> respuesta)
    {
        contexto.Response.StatusCode = codigoHttp;
        contexto.Response.ContentType = "application/json";
        await contexto.Response.WriteAsJsonAsync(respuesta);
    }
}
