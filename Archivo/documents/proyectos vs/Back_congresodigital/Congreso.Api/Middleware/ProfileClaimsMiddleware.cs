using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Congreso.Api.Data;

namespace Congreso.Api.Middleware;

/// <summary>
/// Middleware para enriquecer los claims del usuario con información de perfiles
/// </summary>
public class ProfileClaimsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProfileClaimsMiddleware> _logger;

    public ProfileClaimsMiddleware(RequestDelegate next, ILogger<ProfileClaimsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, CongresoDbContext dbContext)
    {
        // Solo procesar si el usuario está autenticado
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await EnrichUserClaimsAsync(context, dbContext);
        }

        await _next(context);
    }

    private async Task EnrichUserClaimsAsync(HttpContext context, CongresoDbContext dbContext)
    {
        try
        {
            _logger.LogDebug("Starting EnrichUserClaimsAsync");
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogDebug("UserIdClaim: {UserIdClaim}", userIdClaim);
            
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogDebug("Invalid or missing userId, returning");
                return;
            }

            if (context.User.HasClaim("profile_type", "staff") || context.User.HasClaim("profile_type", "student"))
            {
                _logger.LogDebug("User already has profile claims, returning");
                return;
            }

            _logger.LogDebug("Querying database for user: {UserId}", userId);
            var user = await dbContext.Users
                .Include(u => u.StaffAccount)
                .Include(u => u.StudentAccount)
                .FirstOrDefaultAsync(u => u.Id == userId);
            _logger.LogDebug("Database query completed, user found: {UserFound}", user != null);

            if (user == null)
                return;

            var identity = context.User.Identity as ClaimsIdentity;
            if (identity == null)
                return;

            if (user.StaffAccount != null)
            {
                identity.AddClaim(new Claim("profile_type", "staff"));
                identity.AddClaim(new Claim("staff_role", user.StaffAccount.StaffRole.ToString()));
                identity.AddClaim(new Claim("staff_display_name", user.StaffAccount.DisplayName ?? ""));
                var roleDescription = user.StaffAccount.StaffRole switch
                {
                    Models.StaffRole.AdminDev => "Desarrollador/Super Admin",
                    Models.StaffRole.Admin => "Administrador", 
                    Models.StaffRole.Asistente => "Asistente",
                    _ => "Desconocido"
                };
                identity.AddClaim(new Claim("staff_role_description", roleDescription));
            }
            else if (user.StudentAccount != null)
            {
                identity.AddClaim(new Claim("profile_type", "student"));
                identity.AddClaim(new Claim("student_carnet", user.StudentAccount.Carnet ?? ""));
                identity.AddClaim(new Claim("student_career", user.StudentAccount.Career ?? ""));
                identity.AddClaim(new Claim("student_cohort", user.StudentAccount.CohortYear?.ToString() ?? ""));
            }
            else
            {
                identity.AddClaim(new Claim("profile_type", "student"));
            }

            identity.AddClaim(new Claim("org_name", user.OrgName ?? ""));
            identity.AddClaim(new Claim("is_umg", user.IsUmg.ToString()));
            identity.AddClaim(new Claim("full_name", user.FullName));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al enriquecer claims del usuario {UserId}", 
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }
    }
}

/// <summary>
/// Extensión para registrar el middleware
/// </summary>
public static class ProfileClaimsMiddlewareExtensions
{
    public static IApplicationBuilder UseProfileClaims(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ProfileClaimsMiddleware>();
    }
}