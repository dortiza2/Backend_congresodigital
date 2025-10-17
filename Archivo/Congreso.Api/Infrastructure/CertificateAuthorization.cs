using Congreso.Api.DTOs.Certificate;
using System.Security.Claims;

namespace Congreso.Api.Infrastructure;

/// <summary>
/// Servicio de autorización para certificados
/// </summary>
public class CertificateAuthorization
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CertificateAuthorization> _logger;

    public CertificateAuthorization(IHttpContextAccessor httpContextAccessor, ILogger<CertificateAuthorization> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Verificar si el usuario actual puede generar certificados para otro usuario
    /// </summary>
    public bool CanGenerateCertificateForUser(int targetUserId)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
        {
            _logger.LogWarning("No hay usuario autenticado");
            return false;
        }

        var currentUserId = GetCurrentUserId(user);
        if (currentUserId == null)
        {
            _logger.LogWarning("No se pudo obtener el ID del usuario actual");
            return false;
        }

        // Usuario puede generar sus propios certificados
        if (currentUserId == targetUserId)
        {
            return true;
        }

        // Administradores pueden generar certificados para cualquier usuario
        if (IsAdmin(user))
        {
            return true;
        }

        // Staff/Organizadores pueden generar certificados bajo ciertas condiciones
        if (IsStaff(user))
        {
            // Verificar si el usuario objetivo está en el mismo evento/actividad
            return CanStaffManageUser(currentUserId.Value, targetUserId);
        }

        return false;
    }

    /// <summary>
    /// Verificar si el usuario actual puede ver certificados de otro usuario
    /// </summary>
    public bool CanViewUserCertificates(int targetUserId)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
        {
            return false;
        }

        var currentUserId = GetCurrentUserId(user);
        if (currentUserId == null)
        {
            return false;
        }

        // Usuario puede ver sus propios certificados
        if (currentUserId == targetUserId)
        {
            return true;
        }

        // Administradores y staff pueden ver certificados de otros usuarios
        return IsAdmin(user) || IsStaff(user);
    }

    /// <summary>
    /// Verificar si el usuario actual puede revocar certificados
    /// </summary>
    public bool CanRevokeCertificates()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
        {
            return false;
        }

        // Solo administradores pueden revocar certificados
        return IsAdmin(user);
    }

    /// <summary>
    /// Verificar si el usuario actual puede ver estadísticas
    /// </summary>
    public bool CanViewStatistics()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
        {
            return false;
        }

        // Solo administradores y staff pueden ver estadísticas
        return IsAdmin(user) || IsStaff(user);
    }

    /// <summary>
    /// Verificar si el usuario actual puede ver auditoría
    /// </summary>
    public bool CanViewAudit()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
        {
            return false;
        }

        // Solo administradores pueden ver auditoría completa
        return IsAdmin(user);
    }

    /// <summary>
    /// Verificar si el usuario actual puede validar certificados
    /// </summary>
    public bool CanValidateCertificates()
    {
        // Cualquier usuario autenticado puede validar certificados
        var user = _httpContextAccessor.HttpContext?.User;
        return user != null && user.Identity?.IsAuthenticated == true;
    }

    /// <summary>
    /// Obtener el ID del usuario actual desde los claims
    /// </summary>
    private int? GetCurrentUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Verificar si el usuario es administrador
    /// </summary>
    private bool IsAdmin(ClaimsPrincipal user)
    {
        return user.IsInRole("ADMIN") || user.IsInRole("DVADMIN") || user.IsInRole("SuperAdmin");
    }

    /// <summary>
    /// Verificar si el usuario es staff/organizador
    /// </summary>
    private bool IsStaff(ClaimsPrincipal user)
    {
        return user.IsInRole("STAFF") || user.IsInRole("ORGANIZADOR");
    }

    /// <summary>
    /// Verificar si un miembro del staff puede gestionar a un usuario específico
    /// </summary>
    private bool CanStaffManageUser(int staffUserId, int targetUserId)
    {
        // En una implementación real, esto verificaría si el usuario objetivo
        // está asociado a eventos/actividades donde el staff tiene permisos
        // Por ahora, permitimos que el staff gestione usuarios (con restricciones en el servicio)
        return true;
    }

    /// <summary>
    /// Verificar si el usuario tiene un rol específico
    /// </summary>
    public bool HasRole(string role)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.IsInRole(role) ?? false;
    }

    /// <summary>
    /// Verificar si el usuario tiene alguno de los roles especificados
    /// </summary>
    public bool HasAnyRole(params string[] roles)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        return roles.Any(role => user.IsInRole(role));
    }
}

/// <summary>
/// Extensiones para CertificateAuthorization
/// </summary>
public static class CertificateAuthorizationExtensions
{
    /// <summary>
    /// Verificar si el usuario actual puede generar certificados para otro usuario
    /// </summary>
    public static bool CanGenerateCertificateForUser(this CertificateAuthorization auth, int targetUserId)
    {
        return auth.CanGenerateCertificateForUser(targetUserId);
    }

    /// <summary>
    /// Verificar si el usuario actual puede ver certificados de otro usuario
    /// </summary>
    public static bool CanViewUserCertificates(this CertificateAuthorization auth, int targetUserId)
    {
        return auth.CanViewUserCertificates(targetUserId);
    }

    /// <summary>
    /// Verificar si el usuario actual puede revocar certificados
    /// </summary>
    public static bool CanRevokeCertificates(this CertificateAuthorization auth)
    {
        return auth.CanRevokeCertificates();
    }
}