using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Congreso.Api.Infrastructure;

namespace Congreso.Api.Middleware
{
    public class JwtValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtValidationMiddleware> _logger;
        private readonly IMetricsCollector _metricsCollector;
        private readonly IConfiguration _configuration;

        public JwtValidationMiddleware(RequestDelegate next, ILogger<JwtValidationMiddleware> logger, IMetricsCollector metricsCollector, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _metricsCollector = metricsCollector;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Rutas que no requieren validación JWT
            var excludedPaths = new[]
            {
                "/api/auth/login",
                "/api/auth/google",
                "/api/auth/callback",
                "/api/auth/logout",
                "/api/health",
                "/healthz",
                "/ready",
                "/metrics",
                "/api/public/"
            };

            var path = context.Request.Path.Value?.ToLower() ?? "";
            
            // Saltar validación para rutas excluidas
            if (excludedPaths.Any(excluded => path.StartsWith(excluded.ToLower())))
            {
                await _next(context);
                return;
            }

            // Saltar validación para métodos OPTIONS (CORS preflight)
            if (context.Request.Method == "OPTIONS")
            {
                await _next(context);
                return;
            }

            // Validar JWT si está presente
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                
                if (!await ValidateJwtToken(context, token))
                {
                    _metricsCollector.RecordJwtAuthFailure();
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    
                    var errorResponse = new
                    {
                        error = "Token inválido o expirado",
                        message = "El token de autenticación no es válido o ha expirado. Por favor, inicie sesión nuevamente."
                    };
                    
                    await context.Response.WriteAsJsonAsync(errorResponse);
                    return;
                }
            }
            else
            {
                // Si no hay token, pero el usuario está autenticado por cookies, permitir
                if (context.User?.Identity?.IsAuthenticated == true)
                {
                    await _next(context);
                    return;
                }
                
                // Si no hay autenticación de ningún tipo para rutas protegidas
                if (IsProtectedPath(path))
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    
                    var errorResponse = new
                    {
                        error = "No autorizado",
                        message = "Se requiere autenticación para acceder a este recurso."
                    };
                    
                    await context.Response.WriteAsJsonAsync(errorResponse);
                    return;
                }
            }

            await _next(context);
        }

        private async Task<bool> ValidateJwtToken(HttpContext context, string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(GetJwtSecret());

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                SecurityToken validatedToken;
                var principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);

                // Verificar que el token no esté en la lista negra (si existe)
                if (await IsTokenBlacklisted(token))
                {
                    return false;
                }

                // Establecer el usuario en el contexto
                context.User = principal;
                
                // Agregar información del token al contexto
                if (validatedToken is JwtSecurityToken jwtToken)
                {
                    context.Items["JwtToken"] = token;
                    context.Items["JwtTokenId"] = jwtToken.Id;
                    context.Items["JwtExpiration"] = jwtToken.ValidTo;
                }

                return true;
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("JWT token expirado");
                return false;
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                _logger.LogWarning("Firma JWT inválida");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando JWT");
                return false;
            }
        }

        private bool IsProtectedPath(string path)
        {
            var protectedPrefixes = new[]
            {
                "/api/admin/",
                "/api/users/",
                "/api/staff/",
                "/api/certificates/"
            };

            return protectedPrefixes.Any(prefix => path.StartsWith(prefix.ToLower()));
        }

        private async Task<bool> IsTokenBlacklisted(string token)
        {
            // Implementar lógica de lista negra si es necesario
            // Por ahora, asumimos que no hay lista negra
            await Task.CompletedTask;
            return false;
        }

        private string GetJwtSecret()
        {
            // Priorizar configuración appsettings: Jwt:Key; fallback a env JWT_SECRET
            var keyFromConfig = _configuration["Jwt:Key"];
            if (!string.IsNullOrWhiteSpace(keyFromConfig)) return keyFromConfig;
            var envKey = Environment.GetEnvironmentVariable("JWT_SECRET");
            if (!string.IsNullOrWhiteSpace(envKey)) return envKey;
            return "your-super-secret-jwt-key-that-should-be-at-least-32-characters-long";
        }
    }

    public static class JwtValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtValidationMiddleware>();
        }
    }
}