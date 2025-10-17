using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Congreso.Api.Infrastructure;

namespace Congreso.Api.Middleware
{
    public class RequestMetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestMetricsMiddleware> _logger;
        private readonly IMetricsCollector _metricsCollector;

        public RequestMetricsMiddleware(RequestDelegate next, ILogger<RequestMetricsMiddleware> logger, IMetricsCollector metricsCollector)
        {
            _next = next;
            _logger = logger;
            _metricsCollector = metricsCollector;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();
            var traceId = context.Request.Headers["X-Trace-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
            
            // Agregar IDs para trazabilidad
            context.Items["RequestId"] = requestId;
            context.Items["TraceId"] = traceId;
            context.Response.Headers["X-Request-Id"] = requestId;
            context.Response.Headers["X-Trace-Id"] = traceId;
            
            var method = context.Request.Method;
            var endpoint = context.Request.Path.Value ?? "/";
            var userId = context.User?.Identity?.IsAuthenticated == true 
                ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                : "anonymous";
            
            try
            {
                await _next(context);
                
                stopwatch.Stop();
                var duration = stopwatch.Elapsed.TotalSeconds;
                var statusCode = context.Response.StatusCode;
                
                // Métricas de HTTP
                _metricsCollector.RecordHttpRequest(method, endpoint, statusCode, duration);
                
                // Log de request exitoso
                _logger.LogInformation("HTTP Request: {Method} {Endpoint} {StatusCode} {DurationMs}ms {RequestId} {UserId}",
                    method, endpoint, statusCode, (int)(duration * 1000), requestId, userId);
                
                // Métricas específicas de errores
                if (statusCode >= 400)
                {
                    var errorType = GetErrorType(statusCode);
                    _metricsCollector.RecordApiError(errorType, endpoint);
                    
                    _logger.LogWarning("HTTP Error: {Method} {Endpoint} {StatusCode} {ErrorType} {RequestId} {UserId}",
                        method, endpoint, statusCode, errorType, requestId, userId);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var duration = stopwatch.Elapsed.TotalSeconds;
                
                // Métricas de error
                _metricsCollector.RecordApiError("exception", endpoint);
                _metricsCollector.RecordHttpRequest(method, endpoint, 500, duration);
                
                // Log de error con detalles
                _logger.LogError(ex, "HTTP Exception: {Method} {Endpoint} {DurationMs}ms {RequestId} {UserId} {ErrorMessage}",
                    method, endpoint, (int)(duration * 1000), requestId, userId, ex.Message);
                
                // Asegurar que nunca retornemos 500 en endpoints públicos
                if (IsPublicEndpoint(endpoint))
                {
                    context.Response.StatusCode = 503;
                    context.Response.ContentType = "application/json";
                    
                    var errorResponse = new
                    {
                        error = "Servicio temporalmente no disponible",
                        message = "Estamos experimentando dificultades técnicas. Por favor, intenta más tarde.",
                        requestId = requestId,
                        timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    };
                    
                    await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
                    return;
                }
                
                throw;
            }
        }

        private string GetErrorType(int statusCode)
        {
            return statusCode switch
            {
                400 => "bad_request",
                401 => "unauthorized",
                403 => "forbidden",
                404 => "not_found",
                429 => "rate_limited",
                >= 500 => "server_error",
                _ => "unknown_error"
            };
        }

        private bool IsPublicEndpoint(string endpoint)
        {
            return endpoint.Contains("/api/public/") || 
                   endpoint.Contains("/healthz") || 
                   endpoint.Contains("/ready") ||
                   endpoint.Contains("/metrics");
        }
    }

    public static class RequestMetricsMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestMetrics(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestMetricsMiddleware>();
        }
    }
}