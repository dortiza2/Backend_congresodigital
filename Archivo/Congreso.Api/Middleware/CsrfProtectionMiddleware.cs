using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Congreso.Api.Middleware
{
    public class CsrfProtectionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CsrfProtectionMiddleware> _logger;
        private readonly IAntiforgery _antiforgery;

        public CsrfProtectionMiddleware(RequestDelegate next, ILogger<CsrfProtectionMiddleware> logger, IAntiforgery antiforgery)
        {
            _next = next;
            _logger = logger;
            _antiforgery = antiforgery;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Rutas que no requieren protección CSRF
            var excludedPaths = new[]
            {
                "/api/auth/login",
                "/api/auth/register",
                "/api/auth/google",
                "/api/auth/callback",
                "/api/auth/logout",
                "/api/health",
                "/healthz",
                "/ready",
                "/metrics",
                "/api/public/",
                "/api/users/profile/",
                "/api/certificates/"
            };

            var path = context.Request.Path.Value?.ToLower() ?? "";
            var method = context.Request.Method.ToUpper();

            // Saltar protección para rutas excluidas
            if (excludedPaths.Any(excluded => path.StartsWith(excluded.ToLower())))
            {
                await _next(context);
                return;
            }

            // Saltar protección para métodos seguros
            if (method == "GET" || method == "HEAD" || method == "OPTIONS")
            {
                await _next(context);
                return;
            }

            // Si la petición usa Bearer puro (sin cookie de sesión), no requiere CSRF
            var authHeader = context.Request.Headers["Authorization"].ToString();
            var hasBearer = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
            var hasAuthCookie = context.Request.Cookies.ContainsKey("auth-session");
            if (hasBearer && !hasAuthCookie)
            {
                await _next(context);
                return;
            }

            // Validar token CSRF para métodos no seguros
            if (ShouldValidateCsrf(context))
            {
                try
                {
                    // Intentar validar el token CSRF
                    await _antiforgery.ValidateRequestAsync(context);
                }
                catch (AntiforgeryValidationException ex)
                {
                    _logger.LogWarning("Validación CSRF fallida: {Message}", ex.Message);
                    
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    
                    var errorResponse = new
                    {
                        error = "Token CSRF inválido",
                        message = "El token CSRF proporcionado no es válido o ha expirado. Por favor, actualice la página e intente nuevamente."
                    };
                    
                    await context.Response.WriteAsJsonAsync(errorResponse);
                    return;
                }
            }

            await _next(context);
        }

        private bool ShouldValidateCsrf(HttpContext context)
        {
            // Validar solo si hay un Content-Type que indica formulario o JSON
            var contentType = context.Request.ContentType?.ToLower() ?? "";
            
            // Validar para formularios y JSON
            return contentType.Contains("application/x-www-form-urlencoded") ||
                   contentType.Contains("multipart/form-data") ||
                   contentType.Contains("application/json");
        }
    }

    public static class CsrfProtectionMiddlewareExtensions
    {
        public static IApplicationBuilder UseCsrfProtection(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CsrfProtectionMiddleware>();
        }
    }
}