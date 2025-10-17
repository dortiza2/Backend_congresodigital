using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Npgsql;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Congreso.Api.Filters;

/// <summary>
/// Global exception filter that maps database and business logic exceptions to standardized HTTP responses
/// </summary>
public class DomainExceptionFilter : IExceptionFilter
{
    private readonly ILogger<DomainExceptionFilter> _logger;
    private readonly IWebHostEnvironment _environment;

    public DomainExceptionFilter(ILogger<DomainExceptionFilter> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        
        // Sanitize exception details for logging
        var sanitizedMessage = SanitizeLogMessage(exception.Message);
        var sanitizedStackTrace = SanitizeLogMessage(exception.StackTrace ?? "");

        // Map exception to domain error
        var errorResponse = MapExceptionToErrorResponse(exception, correlationId);
        
        // Log the exception with sanitized information
        LogException(exception, correlationId, sanitizedMessage, sanitizedStackTrace);

        // Set the response
        context.Result = new ObjectResult(errorResponse)
        {
            StatusCode = errorResponse.StatusCode
        };
        
        context.ExceptionHandled = true;
    }

    private ErrorResponse MapExceptionToErrorResponse(Exception exception, string correlationId)
    {
        return exception switch
        {
            // PostgreSQL/Npgsql exceptions
            PostgresException pgEx => MapPostgresException(pgEx, correlationId),
            NpgsqlException npgsqlEx => MapNpgsqlException(npgsqlEx, correlationId),
            
            // Business logic exceptions
            InvalidOperationException invalidOpEx => MapInvalidOperationException(invalidOpEx, correlationId),
            ArgumentException argEx => MapArgumentException(argEx, correlationId),
            
            // Generic exceptions
            _ => new ErrorResponse
            {
                StatusCode = 500,
                Code = "internal_server_error",
                Message = _environment.IsDevelopment() 
                    ? SanitizeErrorMessage(exception.Message)
                    : "An internal server error occurred",
                CorrelationId = correlationId
            }
        };
    }

    private ErrorResponse MapPostgresException(PostgresException pgEx, string correlationId)
    {
        var message = pgEx.MessageText?.ToLowerInvariant() ?? "";
        var detail = pgEx.Detail?.ToLowerInvariant() ?? "";
        var combinedMessage = $"{message} {detail}".Trim();

        // Map based on SQLSTATE and message content
        return combinedMessage switch
        {
            var msg when msg.Contains("already_registered") || pgEx.SqlState == "P0001" && msg.Contains("already_registered") => 
                new ErrorResponse { StatusCode = 409, Code = "already_registered", Message = "User is already registered for this activity", CorrelationId = correlationId },
                
            var msg when msg.Contains("no_seats_left") || msg.Contains("cupo_lleno") || msg.Contains("cupo_agotado") => 
                new ErrorResponse { StatusCode = 409, Code = "no_seats_left", Message = "No seats available for this activity", CorrelationId = correlationId },
                
            var msg when msg.Contains("time_conflict") || msg.Contains("conflicto_horario") => 
                new ErrorResponse { StatusCode = 409, Code = "time_conflict", Message = "Time conflict with existing enrollment", CorrelationId = correlationId },
                
            var msg when msg.Contains("email_not_found") => 
                new ErrorResponse { StatusCode = 404, Code = "email_not_found", Message = "Email address not found", CorrelationId = correlationId },
                
            var msg when msg.Contains("talk_requires_speaker") => 
                new ErrorResponse { StatusCode = 422, Code = "talk_requires_speaker", Message = "Talk activity requires a speaker assignment", CorrelationId = correlationId },
                
            var msg when msg.Contains("duplicate") || msg.Contains("unique") => 
                new ErrorResponse { StatusCode = 409, Code = "duplicate_entry", Message = "Duplicate entry detected", CorrelationId = correlationId },
                
            _ => new ErrorResponse 
            { 
                StatusCode = 500, 
                Code = "database_error", 
                Message = _environment.IsDevelopment() ? SanitizeErrorMessage(pgEx.MessageText ?? "Database error") : "A database error occurred", 
                CorrelationId = correlationId 
            }
        };
    }

    private ErrorResponse MapNpgsqlException(NpgsqlException npgsqlEx, string correlationId)
    {
        // Handle Npgsql-specific exceptions
        var message = npgsqlEx.Message?.ToLowerInvariant() ?? "";
        
        if (message.Contains("connection") || message.Contains("timeout"))
        {
            return new ErrorResponse
            {
                StatusCode = 503,
                Code = "service_unavailable",
                Message = "Database service temporarily unavailable",
                CorrelationId = correlationId
            };
        }

        return new ErrorResponse
        {
            StatusCode = 500,
            Code = "database_error",
            Message = _environment.IsDevelopment() ? SanitizeErrorMessage(npgsqlEx.Message) : "A database error occurred",
            CorrelationId = correlationId
        };
    }

    private ErrorResponse MapInvalidOperationException(InvalidOperationException invalidOpEx, string correlationId)
    {
        var message = invalidOpEx.Message?.ToLowerInvariant() ?? "";
        
        return message switch
        {
            var msg when msg.Contains("conflicto_horario") || msg.Contains("time_conflict") => 
                new ErrorResponse { StatusCode = 409, Code = "time_conflict", Message = "Time conflict with existing enrollment", CorrelationId = correlationId },
                
            var msg when msg.Contains("cupo_lleno") || msg.Contains("cupo_agotado") || msg.Contains("no_seats_left") => 
                new ErrorResponse { StatusCode = 409, Code = "no_seats_left", Message = "No seats available for this activity", CorrelationId = correlationId },
                
            var msg when msg.Contains("already_registered") => 
                new ErrorResponse { StatusCode = 409, Code = "already_registered", Message = "User is already registered for this activity", CorrelationId = correlationId },
                
            _ => new ErrorResponse 
            { 
                StatusCode = 400, 
                Code = "invalid_operation", 
                Message = _environment.IsDevelopment() ? SanitizeErrorMessage(invalidOpEx.Message) : "Invalid operation", 
                CorrelationId = correlationId 
            }
        };
    }

    private ErrorResponse MapArgumentException(ArgumentException argEx, string correlationId)
    {
        return new ErrorResponse
        {
            StatusCode = 400,
            Code = "invalid_argument",
            Message = _environment.IsDevelopment() ? SanitizeErrorMessage(argEx.Message) : "Invalid argument provided",
            CorrelationId = correlationId
        };
    }

    private void LogException(Exception exception, string correlationId, string sanitizedMessage, string sanitizedStackTrace)
    {
        var logLevel = exception switch
        {
            ArgumentException => LogLevel.Warning,
            InvalidOperationException => LogLevel.Warning,
            PostgresException => LogLevel.Error,
            NpgsqlException => LogLevel.Error,
            _ => LogLevel.Error
        };

        _logger.Log(logLevel, exception, 
            "Exception occurred. CorrelationId: {CorrelationId}, Type: {ExceptionType}, Message: {SanitizedMessage}",
            correlationId, exception.GetType().Name, sanitizedMessage);

        // Log stack trace separately for debugging in development
        if (_environment.IsDevelopment() && !string.IsNullOrEmpty(sanitizedStackTrace))
        {
            _logger.LogDebug("Stack trace for CorrelationId {CorrelationId}: {SanitizedStackTrace}", 
                correlationId, sanitizedStackTrace);
        }
    }

    /// <summary>
    /// Sanitizes log messages by redacting sensitive information
    /// </summary>
    private string SanitizeLogMessage(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return "";

        var sanitized = message;

        // Redact common sensitive patterns
        var sensitivePatterns = new[]
        {
            (@"password['""\s]*[:=]['""\s]*[^'""\s,}]+", "[redacted]"),
            (@"password_hash['""\s]*[:=]['""\s]*[^'""\s,}]+", "[redacted]"),
            (@"token['""\s]*[:=]['""\s]*[^'""\s,}]+", "[redacted]"),
            (@"reset_token['""\s]*[:=]['""\s]*[^'""\s,}]+", "[redacted]"),
            (@"invite_token['""\s]*[:=]['""\s]*[^'""\s,}]+", "[redacted]"),
            (@"qr_token['""\s]*[:=]['""\s]*[^'""\s,}]+", "[redacted]"),
            (@"auth_token['""\s]*[:=]['""\s]*[^'""\s,}]+", "[redacted]"),
            (@"access_token['""\s]*[:=]['""\s]*[^'""\s,}]+", "[redacted]"),
            (@"refresh_token['""\s]*[:=]['""\s]*[^'""\s,}]+", "[redacted]"),
            (@"\{Token\}", "[redacted]"),
            (@"\{Password\}", "[redacted]"),
            (@"\{PasswordHash\}", "[redacted]"),
            (@"\{ResetToken\}", "[redacted]"),
            (@"\{InviteToken\}", "[redacted]"),
            (@"\{QrToken\}", "[redacted]"),
            (@"\{AuthToken\}", "[redacted]"),
            (@"\{AccessToken\}", "[redacted]"),
            (@"\{RefreshToken\}", "[redacted]")
        };

        foreach (var (pattern, replacement) in sensitivePatterns)
        {
            sanitized = Regex.Replace(sanitized, pattern, replacement, RegexOptions.IgnoreCase);
        }

        return sanitized;
    }

    /// <summary>
    /// Sanitizes error messages for public consumption
    /// </summary>
    private string SanitizeErrorMessage(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return "An error occurred";

        // Remove sensitive information from error messages
        var sanitized = SanitizeLogMessage(message);
        
        // Remove technical details that shouldn't be exposed
        sanitized = Regex.Replace(sanitized, @"at\s+[\w\.]+\([^)]*\)", "", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"in\s+[^\s]+\.cs:line\s+\d+", "", RegexOptions.IgnoreCase);
        
        return sanitized.Trim();
    }
}

/// <summary>
/// Standardized error response format
/// </summary>
public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";
    public string CorrelationId { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}