namespace GTE.WebApi.Models;

/// <summary>Codigos estandar del envelope de respuesta.</summary>
public static class ApiResponseCodes
{
    public const string Ok = "OK";
    public const string NotFound = "NOT_FOUND";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string Conflict = "CONFLICT";
    public const string Forbidden = "FORBIDDEN";
    public const string InternalError = "INTERNAL_ERROR";
}

/// <summary>
/// Envelope estandar de TODA respuesta de la API (patron del ecosistema Interflo).
/// El frontend lee response para el dato y code/success para el flujo.
/// </summary>
public class ApiResponse<T>
{
    public string Code { get; set; } = ApiResponseCodes.Ok;
    public bool Success { get; set; } = true;
    public string UserMessage { get; set; } = string.Empty;
    public string? Message { get; set; }
    public T? Response { get; set; }

    public static ApiResponse<T> Exito(T response, string userMessage = "Operacion realizada correctamente")
    {
        return new ApiResponse<T>
        {
            Code = ApiResponseCodes.Ok,
            Success = true,
            UserMessage = userMessage,
            Response = response
        };
    }

    public static ApiResponse<T> Falla(string code, string userMessage, string? message = null, T? response = default)
    {
        return new ApiResponse<T>
        {
            Code = code,
            Success = false,
            UserMessage = userMessage,
            Message = message,
            Response = response
        };
    }
}
