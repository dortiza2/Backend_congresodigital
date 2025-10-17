using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Congreso.Api.Middleware;

/// <summary>
/// Middleware para autorización basada en roles con soporte para claims JWT
/// </summary>
public class RoleAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RoleAuthorizationMiddleware> _logger;

    public RoleAuthorizationMiddleware(RequestDelegate next, ILogger<RoleAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Bypass explícito para endpoints públicos de health/metrics
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (path == "/healthz" || path == "/ready" || path == "/metrics" || path == "/api/health" || path == "/health/db" || path == "/internal/metrics")
        {
            await _next(context);
            return;
        }

        var endpoint = context.GetEndpoint();
        var authorizeAttributes = endpoint?.Metadata.GetOrderedMetadata<AuthorizeAttribute>() ?? Array.Empty<AuthorizeAttribute>();
        
        if (!authorizeAttributes.Any())
        {
            await _next(context);
            return;
        }

        var user = context.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("{\"error\":\"No autorizado\"}");
            return;
        }

        // Leer claims JWT (compatibilidad con "roleLevel" y "role_level")
        var roleLevelClaim = user.FindFirst("roleLevel")?.Value ?? user.FindFirst("role_level")?.Value;
        var rolesClaim = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        
        // Mapear roles con permisos
        var userPermissions = GetUserPermissions(roleLevelClaim, rolesClaim);
        
        // Verificar políticas requeridas
        foreach (var attribute in authorizeAttributes)
        {
            if (!HasRequiredPermission(attribute, userPermissions))
            {
                _logger.LogWarning("Acceso denegado para usuario {UserId}. Rol: {RoleLevel}, Permisos: {Permissions}", 
                    user.FindFirst(ClaimTypes.NameIdentifier)?.Value, roleLevelClaim, string.Join(",", userPermissions));
                
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("{\"error\":\"Sin permisos suficientes\"}");
                return;
            }
        }

        await _next(context);
    }

    private List<string> GetUserPermissions(string roleLevel, List<string> roles)
    {
        var permissions = new List<string>();
        
        if (int.TryParse(roleLevel, out int level))
        {
            switch (level)
            {
                case 3: // DevAdmin
                    permissions.AddRange(new[] { "devadmin", "admin", "staff", "user" });
                    break;
                case 2: // Admin
                    permissions.AddRange(new[] { "admin", "staff", "user" });
                    break;
                case 1: // Staff
                    permissions.AddRange(new[] { "staff", "user" });
                    break;
                default:
                    permissions.Add("user");
                    break;
            }
        }

        // También considerar roles tradicionales
        if (roles.Contains("DevAdmin")) permissions.AddRange(new[] { "devadmin", "admin", "staff", "user" });
        if (roles.Contains("Admin")) permissions.AddRange(new[] { "admin", "staff", "user" });
        if (roles.Contains("Staff")) permissions.AddRange(new[] { "staff", "user" });
        
        return permissions.Distinct().ToList();
    }

    private bool HasRequiredPermission(AuthorizeAttribute attribute, List<string> userPermissions)
    {
        if (string.IsNullOrEmpty(attribute.Policy))
        {
            if (attribute.Roles != null)
            {
                var requiredRoles = attribute.Roles.Split(',').Select(r => r.Trim()).ToList();
                return requiredRoles.Any(role => userPermissions.Contains(role.ToLower()));
            }
            return true; // No hay requisitos específicos
        }

        // Verificar políticas personalizadas
        return attribute.Policy switch
        {
            "RequireAdmin" => userPermissions.Contains("admin"),
            "RequireSuperAdmin" => userPermissions.Contains("devadmin"),
            "RequireStaffOrHigher" => userPermissions.Contains("staff") || userPermissions.Contains("admin") || userPermissions.Contains("devadmin"),
            _ => false
        };
    }
}

/// <summary>
/// Extensión para registrar el middleware
/// </summary>
public static class RoleAuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseRoleAuthorization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RoleAuthorizationMiddleware>();
    }
}