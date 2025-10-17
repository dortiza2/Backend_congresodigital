using Microsoft.AspNetCore.Mvc;
using Congreso.Api.DTOs.Certificate;
using Congreso.Api.Services;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace Congreso.Api.Controllers;

/// <summary>
/// Controlador para la gestión de certificados
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CertificatesController : ControllerBase
{
    private readonly ICertificatesService _certificatesService;
    private readonly IAuditService _auditService;
    private readonly ILogger<CertificatesController> _logger;

    public CertificatesController(
        ICertificatesService certificatesService,
        IAuditService auditService,
        ILogger<CertificatesController> logger)
    {
        _certificatesService = certificatesService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Generar un nuevo certificado
    /// </summary>
    /// <param name="request">Datos para generar el certificado</param>
    /// <returns>Certificado generado</returns>
    [SwaggerOperation(Summary = "Generar certificado", Description = "Genera un certificado para el usuario especificado")]
    [ProducesResponseType(typeof(CertificateResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(500)]
    [HttpPost("generate")]
    public async Task<ActionResult<CertificateResponse>> GenerateCertificate([FromBody] GenerateCertificateRequest request)
    {
        try
        {
            _logger.LogInformation("Solicitando generación de certificado para usuario {UserId}, tipo {CertificateType}", 
                request.UserId, request.Type);

            // Validar usuario autenticado
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized(new { error = "Usuario no autenticado" });
            }

            // Verificar permisos: usuario puede generar su propio certificado o admin puede generar para otros
            if (currentUserId != request.UserId && !IsAdmin())
            {
                return Forbid("No tiene permisos para generar certificados para este usuario");
            }

            try
            {
                var certificate = await _certificatesService.GenerateCertificateAsync(request, currentUserId.Value);
                
                _logger.LogInformation("Certificado generado exitosamente para usuario {UserId}, ID: {CertificateId}", 
                    request.UserId, certificate.Id);
                
                return Ok(certificate);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Falló generación de certificado para usuario {UserId}: {Error}", 
                    request.UserId, ex.Message);
                
                return BadRequest(new { error = ex.Message });
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Acceso no autorizado al generar certificado");
            return Forbid("Acceso no autorizado");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error de operación al generar certificado");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al generar certificado");
            return StatusCode(500, new { error = "Error interno al procesar la solicitud" });
        }
    }

    /// <summary>
    /// Obtener certificados de un usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="page">Número de página (por defecto 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto 10)</param>
    /// <returns>Lista paginada de certificados del usuario</returns>
    [SwaggerOperation(Summary = "Certificados de usuario", Description = "Obtiene certificados del usuario con paginación")]
    [ProducesResponseType(typeof(PaginatedResult<CertificateResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(500)]
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<PaginatedResult<CertificateResponse>>> GetUserCertificates(
        Guid userId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        try
        {
            _logger.LogInformation("Obteniendo certificados para usuario {UserId}, página {Page}", userId, page);

            // Validar usuario autenticado
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized(new { error = "Usuario no autenticado" });
            }

            // Verificar permisos: usuario puede ver sus propios certificados o admin puede ver de otros
            if (currentUserId != userId && !IsAdmin())
            {
                return Forbid("No tiene permisos para ver certificados de este usuario");
            }

            var result = await _certificatesService.GetUserCertificatesAsync(userId, page, pageSize, false);
            
            _logger.LogInformation("Obtenidos {Count} certificados para usuario {UserId}", 
                result.Items.Count, userId);
            
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Acceso no autorizado al obtener certificados de usuario");
            return Forbid("Acceso no autorizado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener certificados de usuario {UserId}", userId);
            return StatusCode(500, new { error = "Error interno al procesar la solicitud" });
        }
    }

    /// <summary>
    /// Validar un certificado por su hash
    /// </summary>
    /// <param name="hash">Hash del certificado</param>
    /// <returns>Resultado de la validación</returns>
    [SwaggerOperation(Summary = "Validar certificado", Description = "Valida un certificado por su hash")]
    [ProducesResponseType(typeof(CertificateValidationResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    [HttpGet("validate/{hash}")]
    public async Task<ActionResult<CertificateValidationResult>> ValidateCertificate(string hash)
    {
        try
        {
            _logger.LogInformation("Validando certificado con hash: {Hash}", hash);

            if (string.IsNullOrWhiteSpace(hash))
            {
                return BadRequest(new { error = "Hash de certificado requerido" });
            }

            var result = await _certificatesService.ValidateCertificateAsync(hash, true);
            
            // Registrar auditoría de validación
            if (result.CertificateId.HasValue)
            {
                await _auditService.LogCertificateValidationAsync(result.CertificateId.Value, GetCurrentUserId(), result.IsValid, new Dictionary<string, object>
                {
                    ["Message"] = result.Message,
                    ["Hash"] = hash
                });
            }
            
            if (result.IsValid)
            {
                _logger.LogInformation("Certificado válido: {Hash}", hash);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Certificado inválido: {Hash}, razón: {Reason}", hash, result.Message);
                return Ok(result); // Retornar 200 con resultado de validación fallida
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar certificado con hash: {Hash}", hash);
            return StatusCode(500, new { error = "Error interno al procesar la solicitud" });
        }
    }

    /// <summary>
    /// Revocar un certificado
    /// </summary>
    /// <param name="id">ID del certificado</param>
    /// <param name="request">Datos de revocación</param>
    /// <returns>Resultado de la revocación</returns>
    [SwaggerOperation(Summary = "Revocar certificado", Description = "Revoca un certificado por ID")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(500)]
    [HttpPost("revoke/{id}")]
    public async Task<ActionResult> RevokeCertificate(int id, [FromBody] RevokeCertificateRequest request)
    {
        try
        {
            _logger.LogInformation("Solicitando revocación de certificado ID: {CertificateId}", id);

            // Validar usuario autenticado
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized(new { error = "Usuario no autenticado" });
            }

            // Verificar permisos: solo administradores pueden revocar certificados
            if (!IsAdmin())
            {
                return Forbid("No tiene permisos para revocar certificados");
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new { error = "Se requiere una razón para la revocación" });
            }

            var revoked = await _certificatesService.RevokeCertificateAsync(id, request.Reason, currentUserId.Value);
            
            if (revoked)
            {
                _logger.LogInformation("Certificado ID: {CertificateId} revocado exitosamente por usuario {UserId}", 
                    id, currentUserId);
                
                return Ok(new { success = true, message = "Certificado revocado exitosamente" });
            }
            else
            {
                _logger.LogWarning("Falló revocación de certificado ID: {CertificateId}", id);
                
                return Ok(new { success = false, error = "No se pudo revocar el certificado" });
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Acceso no autorizado al revocar certificado");
            return Forbid("Acceso no autorizado");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error de operación al revocar certificado");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al revocar certificado ID: {CertificateId}", id);
            return StatusCode(500, new { error = "Error interno al procesar la solicitud" });
        }
    }

    /// <summary>
    /// Obtener estadísticas de certificados
    /// </summary>
    /// <returns>Estadísticas de certificados</returns>
    [SwaggerOperation(Summary = "Estadísticas de certificados", Description = "Obtiene métricas y conteos de certificados")]
    [ProducesResponseType(typeof(CertificateStatistics), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(500)]
    [HttpGet("statistics")]
    public async Task<ActionResult<CertificateStatistics>> GetStatistics()
    {
        try
        {
            // Validar usuario autenticado
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized(new { error = "Usuario no autenticado" });
            }

            // Verificar permisos: solo administradores pueden ver estadísticas
            if (!IsAdmin())
            {
                return Forbid("No tiene permisos para ver estadísticas");
            }

            var statistics = await _certificatesService.GetStatisticsAsync();
            
            return Ok(statistics);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Acceso no autorizado al obtener estadísticas");
            return Forbid("Acceso no autorizado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas de certificados");
            return StatusCode(500, new { error = "Error interno al procesar la solicitud" });
        }
    }

    /// <summary>
    /// Obtener auditoría de certificados
    /// </summary>
    /// <param name="userId">ID de usuario (opcional)</param>
    /// <param name="certificateId">ID de certificado (opcional)</param>
    /// <param name="limit">Límite de registros (por defecto 100)</param>
    /// <returns>Historial de auditoría</returns>
    [SwaggerOperation(Summary = "Auditoría de certificados", Description = "Historial de eventos relacionados con certificados")]
    [ProducesResponseType(typeof(List<AuditLogEntry>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(500)]
    [HttpGet("audit")]
    public async Task<ActionResult<List<AuditLogEntry>>> GetAuditHistory(
        [FromQuery] Guid? userId = null,
        [FromQuery] int? certificateId = null,
        [FromQuery] int limit = 100)
    {
        try
        {
            // Validar usuario autenticado
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized(new { error = "Usuario no autenticado" });
            }

            // Verificar permisos: solo administradores pueden ver auditoría completa
            if (!IsAdmin())
            {
                return Forbid("No tiene permisos para ver auditoría");
            }

            var auditHistory = await _auditService.GetAuditHistoryAsync(userId, certificateId, limit: limit);
            
            return Ok(auditHistory);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Acceso no autorizado al obtener auditoría");
            return Forbid("Acceso no autorizado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener auditoría de certificados");
            return StatusCode(500, new { error = "Error interno al procesar la solicitud" });
        }
    }

    #region Métodos auxiliares

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out Guid userId))
        {
            return userId;
        }
        return null;
    }

    private bool IsAdmin()
    {
        return User.IsInRole("ADMIN") || User.IsInRole("DVADMIN") || User.IsInRole("SuperAdmin");
    }

    #endregion
}

/// <summary>
/// Solicitud de revocación de certificado
/// </summary>
public class RevokeCertificateRequest
{
    public string Reason { get; set; } = string.Empty;
}