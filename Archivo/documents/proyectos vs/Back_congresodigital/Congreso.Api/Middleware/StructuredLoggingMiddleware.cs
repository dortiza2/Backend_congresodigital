using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Congreso.Api.Middleware
{
    public class StructuredLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<StructuredLoggingMiddleware> _logger;

        public StructuredLoggingMiddleware(RequestDelegate next, ILogger<StructuredLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();
            var traceId = context.Items["TraceId"]?.ToString() ?? Guid.NewGuid().ToString();
            
            // Extraer información del request
            var logContext = new LogContext
            {
                RequestId = requestId,
                TraceId = traceId,
                Timestamp = DateTime.UtcNow,
                Service = "Congreso.Api",
                Method = context.Request.Method,
                Endpoint = context.Request.Path.Value ?? "/",
                UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault(),
                IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                UserId = GetUserId(context),
                QueryString = context.Request.QueryString.Value,
                Referrer = context.Request.Headers["Referer"].FirstOrDefault()
            };

            try
            {
                // Log del request entrante
                LogRequestStart(logContext);
                
                await _next(context);
                
                stopwatch.Stop();
                logContext.DurationMs = stopwatch.ElapsedMilliseconds;
                logContext.StatusCode = context.Response.StatusCode;
                
                // Log del request completado
                LogRequestComplete(logContext);
                
                // Log de eventos específicos
                LogSpecialEvents(context, logContext);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logContext.DurationMs = stopwatch.ElapsedMilliseconds;
                logContext.StatusCode = 500;
                
                // Log de error estructurado
                LogError(ex, logContext);
                throw;
            }
        }

        private void LogRequestStart(LogContext context)
        {
            var logData = new
            {
                timestamp = context.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                level = "info",
                service = context.Service,
                trace_id = context.TraceId,
                request_id = context.RequestId,
                event_type = "request_start",
                http = new
                {
                    method = context.Method,
                    endpoint = context.Endpoint,
                    user_agent = context.UserAgent,
                    ip_address = context.IpAddress,
                    user_id = context.UserId,
                    query_string = context.QueryString,
                    referrer = context.Referrer
                }
            };

            _logger.LogInformation("REQUEST_START {LogData}", JsonSerializer.Serialize(logData));
        }

        private void LogRequestComplete(LogContext context)
        {
            var logData = new
            {
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                level = GetLogLevel(context.StatusCode),
                service = context.Service,
                trace_id = context.TraceId,
                request_id = context.RequestId,
                event_type = "request_complete",
                duration_ms = context.DurationMs,
                http = new
                {
                    method = context.Method,
                    endpoint = context.Endpoint,
                    status_code = context.StatusCode,
                    user_id = context.UserId,
                    ip_address = context.IpAddress
                }
            };

            _logger.LogInformation("REQUEST_COMPLETE {LogData}", JsonSerializer.Serialize(logData));
        }

        private void LogError(Exception ex, LogContext context)
        {
            var logData = new
            {
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                level = "error",
                service = context.Service,
                trace_id = context.TraceId,
                request_id = context.RequestId,
                event_type = "error",
                duration_ms = context.DurationMs,
                error = new
                {
                    type = ex.GetType().Name,
                    message = ex.Message,
                    stack_trace = ex.StackTrace?.Split('\n').Take(5).ToArray(), // Limitar stack trace
                    inner_error = ex.InnerException?.Message
                },
                http = new
                {
                    method = context.Method,
                    endpoint = context.Endpoint,
                    status_code = context.StatusCode,
                    user_id = context.UserId,
                    ip_address = context.IpAddress
                }
            };

            _logger.LogError("ERROR {LogData}", JsonSerializer.Serialize(logData));
        }

        private void LogSpecialEvents(HttpContext context, LogContext logContext)
        {
            var endpoint = context.Request.Path.Value ?? "/";
            
            // Log intentos de acceso no autorizado
            if (context.Response.StatusCode == 401 || context.Response.StatusCode == 403)
            {
                var logData = new
                {
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    level = "warning",
                    service = logContext.Service,
                    trace_id = logContext.TraceId,
                    request_id = logContext.RequestId,
                    event_type = "unauthorized_access",
                    http = new
                    {
                        method = logContext.Method,
                        endpoint = logContext.Endpoint,
                        status_code = context.Response.StatusCode,
                        user_id = logContext.UserId,
                        ip_address = logContext.IpAddress
                    }
                };

                _logger.LogWarning("UNAUTHORIZED_ACCESS {LogData}", JsonSerializer.Serialize(logData));
            }
            
            // Log de rate limiting
            if (context.Response.StatusCode == 429)
            {
                var logData = new
                {
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    level = "warning",
                    service = logContext.Service,
                    trace_id = logContext.TraceId,
                    request_id = logContext.RequestId,
                    event_type = "rate_limited",
                    http = new
                    {
                        method = logContext.Method,
                        endpoint = logContext.Endpoint,
                        user_id = logContext.UserId,
                        ip_address = logContext.IpAddress
                    }
                };

                _logger.LogWarning("RATE_LIMITED {LogData}", JsonSerializer.Serialize(logData));
            }
        }

        private string GetLogLevel(int statusCode)
        {
            return statusCode switch
            {
                >= 500 => "error",
                >= 400 => "warning",
                _ => "info"
            };
        }

        private string GetUserId(HttpContext context)
        {
            try
            {
                if (context.User?.Identity?.IsAuthenticated == true)
                {
                    var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    return userId ?? "anonymous";
                }
                return "anonymous";
            }
            catch
            {
                return "unknown";
            }
        }
    }

    public class LogContext
    {
        public string RequestId { get; set; }
        public string TraceId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Service { get; set; }
        public string Method { get; set; }
        public string Endpoint { get; set; }
        public string UserAgent { get; set; }
        public string IpAddress { get; set; }
        public string UserId { get; set; }
        public string QueryString { get; set; }
        public string Referrer { get; set; }
        public long DurationMs { get; set; }
        public int StatusCode { get; set; }
    }

    public static class StructuredLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseStructuredLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<StructuredLoggingMiddleware>();
        }
    }
}