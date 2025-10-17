using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Congreso.Api.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityHeadersMiddleware> _logger;

        public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Agregar headers de seguridad
            AddSecurityHeaders(context);
            
            // Remover headers que revelan información del servidor
            RemoveServerHeaders(context);
            
            await _next(context);
        }

        private void AddSecurityHeaders(HttpContext context)
        {
            var response = context.Response;
            
            // Content Security Policy
            response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; img-src 'self' data: https:; connect-src 'self' https: http:;";
            
            // X-Content-Type-Options
            response.Headers["X-Content-Type-Options"] = "nosniff";
            
            // X-Frame-Options
            response.Headers["X-Frame-Options"] = "DENY";
            
            // X-XSS-Protection
            response.Headers["X-XSS-Protection"] = "1; mode=block";
            
            // Referrer Policy
            response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            
            // Permissions Policy
            response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            
            // Strict Transport Security (solo en HTTPS)
            if (context.Request.IsHttps)
            {
                response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
            }
            
            // Cross-Origin Resource Sharing
            response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";
            response.Headers["Cross-Origin-Opener-Policy"] = "same-origin-allow-popups";
            
            _logger.LogDebug("Security headers added for {Path}", context.Request.Path);
        }

        private void RemoveServerHeaders(HttpContext context)
        {
            // Remover headers que revelan información del servidor
            if (context.Response.Headers.ContainsKey("Server"))
            {
                context.Response.Headers.Remove("Server");
            }
            
            if (context.Response.Headers.ContainsKey("X-Powered-By"))
            {
                context.Response.Headers.Remove("X-Powered-By");
            }
            
            if (context.Response.Headers.ContainsKey("X-AspNet-Version"))
            {
                context.Response.Headers.Remove("X-AspNet-Version");
            }
            
            _logger.LogDebug("Server headers removed for {Path}", context.Request.Path);
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}