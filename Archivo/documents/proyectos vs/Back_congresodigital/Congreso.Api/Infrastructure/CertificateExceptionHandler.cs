using Congreso.Api.DTOs.Certificate;
using System.Text.Json;

namespace Congreso.Api.Infrastructure;

/// <summary>
/// Manejador global de excepciones para el módulo de certificados
/// </summary>
public class CertificateExceptionHandler
{
    private readonly ILogger<CertificateExceptionHandler> _logger;

    public CertificateExceptionHandler(ILogger<CertificateExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Manejar excepciones del módulo de certificados y retornar respuestas apropiadas
    /// </summary>
    public async Task<ExceptionResult> HandleExceptionAsync(Exception exception, string context = "")
    {
        _logger.LogError(exception, "Excepción en módulo de certificados: {Context}", context);

        return exception switch
        {
            UnauthorizedAccessException ex => HandleUnauthorizedAccess(ex),
            InvalidOperationException ex => HandleInvalidOperation(ex),
            KeyNotFoundException ex => HandleKeyNotFound(ex),
            ArgumentNullException ex => HandleArgumentNullException(ex),
            ArgumentException ex => HandleArgumentException(ex),
            TimeoutException ex => HandleTimeoutException(ex),
            System.IO.IOException ex => HandleIOException(ex),
            Npgsql.PostgresException ex => HandlePostgresException(ex),
            System.Net.Http.HttpRequestException ex => HandleHttpRequestException(ex),
            _ => HandleGenericException(exception)
        };
    }

    private ExceptionResult HandleUnauthorizedAccess(UnauthorizedAccessException ex)
    {
        return new ExceptionResult
        {
            StatusCode = 403,
            ErrorCode = "UNAUTHORIZED_ACCESS",
            Message = "Acceso no autorizado al recurso solicitado",
            Details = ex.Message,
            ShouldLog = false // No necesita log adicional, ya fue logueado
        };
    }

    private ExceptionResult HandleInvalidOperation(InvalidOperationException ex)
    {
        return new ExceptionResult
        {
            StatusCode = 400,
            ErrorCode = "INVALID_OPERATION",
            Message = "Operación no válida",
            Details = ex.Message,
            ShouldLog = false
        };
    }

    private ExceptionResult HandleKeyNotFound(KeyNotFoundException ex)
    {
        return new ExceptionResult
        {
            StatusCode = 404,
            ErrorCode = "RESOURCE_NOT_FOUND",
            Message = "Recurso no encontrado",
            Details = ex.Message,
            ShouldLog = false
        };
    }

    private ExceptionResult HandleArgumentException(ArgumentException ex)
    {
        return new ExceptionResult
        {
            StatusCode = 400,
            ErrorCode = "INVALID_ARGUMENT",
            Message = "Argumento inválido proporcionado",
            Details = ex.Message,
            ShouldLog = false
        };
    }

    private ExceptionResult HandleArgumentNullException(ArgumentNullException ex)
    {
        return new ExceptionResult
        {
            StatusCode = 400,
            ErrorCode = "NULL_ARGUMENT",
            Message = "Se requiere un argumento que no puede ser nulo",
            Details = ex.Message,
            ShouldLog = false
        };
    }

    private ExceptionResult HandleTimeoutException(TimeoutException ex)
    {
        return new ExceptionResult
        {
            StatusCode = 504,
            ErrorCode = "OPERATION_TIMEOUT",
            Message = "La operación excedió el tiempo límite",
            Details = "Por favor, intente nuevamente más tarde",
            ShouldLog = true // Necesita log adicional para monitoreo
        };
    }

    private ExceptionResult HandleIOException(System.IO.IOException ex)
    {
        return new ExceptionResult
        {
            StatusCode = 500,
            ErrorCode = "IO_ERROR",
            Message = "Error al procesar archivos o datos",
            Details = "Error interno al procesar el certificado",
            ShouldLog = true
        };
    }

    private ExceptionResult HandlePostgresException(Npgsql.PostgresException ex)
    {
        var errorCode = MapPostgresErrorCode(ex.SqlState);
        var message = GetPostgresErrorMessage(ex);
        
        return new ExceptionResult
        {
            StatusCode = GetPostgresStatusCode(ex.SqlState),
            ErrorCode = errorCode,
            Message = message,
            Details = "Error de base de datos al procesar el certificado",
            ShouldLog = true
        };
    }

    private ExceptionResult HandleHttpRequestException(System.Net.Http.HttpRequestException ex)
    {
        return new ExceptionResult
        {
            StatusCode = 503,
            ErrorCode = "EXTERNAL_SERVICE_ERROR",
            Message = "Error al comunicarse con servicios externos",
            Details = "Error al procesar el certificado",
            ShouldLog = true
        };
    }

    private ExceptionResult HandleGenericException(Exception ex)
    {
        return new ExceptionResult
        {
            StatusCode = 500,
            ErrorCode = "INTERNAL_ERROR",
            Message = "Error interno del servidor",
            Details = "Ocurrió un error al procesar la solicitud",
            ShouldLog = true
        };
    }

    private string MapPostgresErrorCode(string sqlState)
    {
        return sqlState switch
        {
            "23505" => "DUPLICATE_KEY",
            "23503" => "FOREIGN_KEY_VIOLATION",
            "23514" => "CHECK_VIOLATION",
            "42P01" => "TABLE_NOT_FOUND",
            "42703" => "COLUMN_NOT_FOUND",
            "28000" => "INVALID_AUTHORIZATION",
            "3D000" => "DATABASE_NOT_FOUND",
            "28P01" => "INVALID_PASSWORD",
            "42501" => "INSUFFICIENT_PRIVILEGE",
            "53100" => "DISK_FULL",
            "53200" => "OUT_OF_MEMORY",
            "53300" => "TOO_MANY_CONNECTIONS",
            "57014" => "QUERY_CANCELED",
            "57P01" => "ADMIN_SHUTDOWN",
            "57P02" => "CRASH_SHUTDOWN",
            "57P03" => "CANNOT_CONNECT_NOW",
            "58030" => "IO_ERROR",
            "58P01" => "UNDEFINED_FILE",
            "58P02" => "DUPLICATE_FILE",
            "F0000" => "CONFIG_FILE_ERROR",
            "HV000" => "FDW_ERROR",
            "P0000" => "PLPGSQL_ERROR",
            "XX000" => "INTERNAL_ERROR",
            "XX001" => "DATA_CORRUPTED",
            "XX002" => "INDEX_CORRUPTED",
            _ => "DATABASE_ERROR"
        };
    }

    private string GetPostgresErrorMessage(Npgsql.PostgresException ex)
    {
        return ex.SqlState switch
        {
            "23505" => "El certificado ya existe",
            "23503" => "Referencia a recurso no encontrada",
            "23514" => "Validación de datos falló",
            "42501" => "Permisos insuficientes",
            "53100" => "Espacio en disco insuficiente",
            "53200" => "Memoria insuficiente",
            "53300" => "Demasiadas conexiones",
            "57014" => "Consulta cancelada por timeout",
            "58030" => "Error de E/S",
            "XX000" => "Error interno de base de datos",
            "XX001" => "Datos corruptos",
            "XX002" => "Índice corrupto",
            _ => "Error de base de datos"
        };
    }

    private int GetPostgresStatusCode(string sqlState)
    {
        return sqlState switch
        {
            "23505" => 409, // Conflict
            "23503" => 404, // Not Found
            "23514" => 400, // Bad Request
            "42501" => 403, // Forbidden
            "53100" or "53200" or "53300" => 507, // Insufficient Storage
            "57014" => 504, // Gateway Timeout
            "58030" => 500, // Internal Server Error
            "XX000" or "XX001" or "XX002" => 500, // Internal Server Error
            _ => 500
        };
    }
}

/// <summary>
/// Resultado del manejo de excepción
/// </summary>
public class ExceptionResult
{
    public int StatusCode { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public bool ShouldLog { get; set; }

    /// <summary>
    /// Convertir a objeto de respuesta JSON
    /// </summary>
    public object ToResponseObject()
    {
        return new
        {
            error = new
            {
                code = ErrorCode,
                message = Message,
                details = Details,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }
        };
    }

    /// <summary>
    /// Convertir a JSON string
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(ToResponseObject());
    }
}

/// <summary>
/// Middleware global para manejo de excepciones del módulo de certificados
/// </summary>
public class CertificateExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CertificateExceptionHandler _exceptionHandler;
    private readonly ILogger<CertificateExceptionMiddleware> _logger;

    public CertificateExceptionMiddleware(
        RequestDelegate next, 
        CertificateExceptionHandler exceptionHandler,
        ILogger<CertificateExceptionMiddleware> logger)
    {
        _next = next;
        _exceptionHandler = exceptionHandler;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var result = await _exceptionHandler.HandleExceptionAsync(ex, $"{context.Request.Method} {context.Request.Path}");
            
            if (result.ShouldLog)
            {
                _logger.LogError(ex, "Excepción no manejada en certificados: {Path}", context.Request.Path);
            }

            context.Response.StatusCode = result.StatusCode;
            context.Response.ContentType = "application/json";
            
            await context.Response.WriteAsync(result.ToJson());
        }
    }
}

/// <summary>
/// Extensiones para el middleware de excepciones
/// </summary>
public static class CertificateExceptionMiddlewareExtensions
{
    /// <summary>
    /// Agregar el middleware de excepciones de certificados
    /// </summary>
    public static IApplicationBuilder UseCertificateExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CertificateExceptionMiddleware>();
    }
}