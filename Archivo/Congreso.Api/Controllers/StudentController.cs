using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Congreso.Api.Data;
using Congreso.Api.Models;
using Congreso.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Congreso.Api.Controllers;

[ApiController]
[Route("api/student")]
[Authorize] // Requiere autenticación para todos los endpoints
public class StudentController : ControllerBase
{
    private readonly CongresoDbContext _db;
    private readonly ILogger<StudentController> _logger;
    private readonly ICheckInTokenService _checkInTokenService;
    private readonly ISecureQrService _secureQrService;

    public StudentController(CongresoDbContext db, ILogger<StudentController> logger, ICheckInTokenService checkInTokenService, ISecureQrService secureQrService)
    {
        _db = db;
        _logger = logger;
        _checkInTokenService = checkInTokenService;
        _secureQrService = secureQrService;
    }

    // DTOs para las respuestas
    public record StudentProfileDto(
        Guid Id,
        string Email,
        string FullName,
        string? OrgName,
        bool IsUmg,
        string? AvatarUrl,
        string[] Roles
    );

    public record StudentEnrollmentDto(
        Guid Id,
        Guid ActivityId,
        string ActivityTitle,
        string ActivityKind,
        string Location,
        DateTime StartTime,
        DateTime EndTime,
        string SeatNumber,
        string QrToken,
        DateTime EnrolledAt,
        bool? Attended,
        string? Instructor
    );

    public record StudentCertificateDto(
        Guid Id,
        Guid ActivityId,
        string ActivityTitle,
        string ActivityType,
        DateTime CompletedAt,
        bool IsGenerated,
        int Hours,
        string? Instructor,
        string? CertificateUrl
    );

    public record StudentQRDto(
        string Id,
        string QrCode,
        bool IsActive,
        DateTime GeneratedAt,
        DateTime? ExpiresAt
    );

    /// <summary>Obtiene el perfil del estudiante autenticado</summary>
    /// <response code="200">Perfil del usuario autenticado</response>
    /// <response code="401">Usuario no autenticado</response>
    // GET /api/student/profile
    [HttpGet("profile")]
    [SwaggerOperation(Summary = "Perfil del estudiante", Description = "Devuelve datos básicos del perfil del usuario autenticado")]
    [ProducesResponseType(typeof(StudentProfileDto), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return NotFound(new { message = "User not found" });

        var roles = user.UserRoles.Select(ur => ur.Role.Code).ToArray();

        var profile = new StudentProfileDto(
            user.Id,
            user.Email,
            user.FullName,
            user.OrgName,
            user.IsUmg,
            user.AvatarUrl,
            roles
        );

        return Ok(profile);
    }

    /// <summary>Actualiza campos permitidos del perfil del estudiante</summary>
    /// <remarks>Solo actualiza `FullName` y `OrgName`</remarks>
    /// <param name="dto">Datos de actualización del perfil</param>
    /// <response code="200">Perfil actualizado correctamente</response>
    /// <response code="400">Datos inválidos</response>
    /// <response code="401">Usuario no autenticado</response>
    /// <response code="404">Usuario no encontrado</response>
    // PUT /api/student/profile
    [HttpPut("profile")]
    [SwaggerOperation(Summary = "Actualizar perfil", Description = "Actualiza los campos permitidos del perfil (FullName, OrgName)")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        // Actualizar solo los campos permitidos
        if (!string.IsNullOrWhiteSpace(dto.FullName))
            user.FullName = dto.FullName;
        
        if (!string.IsNullOrWhiteSpace(dto.OrgName))
            user.OrgName = dto.OrgName;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Profile updated successfully" });
    }

    // GET /api/student/enrollments
    [HttpGet("enrollments")]
    public async Task<IActionResult> GetEnrollments()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var enrollmentData = await _db.Enrollments
            .Where(e => e.UserId == userId)
            .Include(e => e.Activity)
            .ToListAsync();

        var enrollments = new List<StudentEnrollmentDto>();
        
        foreach (var e in enrollmentData)
        {
            // Generate QR token for each enrollment
            var qrToken = await _checkInTokenService.GenerateTokenAsync(userId.Value, e.ActivityId, e.Id);
            
            enrollments.Add(new StudentEnrollmentDto(
                e.Id,
                e.ActivityId,
                e.Activity.Title,
                e.Activity.ActivityType.ToString(),
                e.Activity.Location ?? "Por definir",
                e.Activity.StartTime ?? DateTime.UtcNow,
                e.Activity.EndTime ?? DateTime.UtcNow,
                e.SeatNumber?.ToString() ?? "N/A",
                qrToken,
                DateTime.UtcNow,
                e.Attended,
                "Instructor por definir"
            ));
        }

        return Ok(enrollments);
    }

    /// <summary>Lista certificados generados para actividades asistidas</summary>
    /// <response code="200">Listado de certificados del estudiante</response>
    /// <response code="401">Usuario no autenticado</response>
    // GET /api/student/certificates
    [HttpGet("certificates")]
    [SwaggerOperation(Summary = "Certificados del estudiante", Description = "Devuelve certificados asociados a inscripciones marcadas como asistidas")]
    [ProducesResponseType(typeof(List<StudentCertificateDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetCertificates()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        // Obtener inscripciones completadas (asistidas)
        var completedEnrollments = await _db.Enrollments
            .Where(e => e.UserId == userId && e.Attended == true)
            .Include(e => e.Activity)
            .AsNoTracking()
            .ToListAsync();

        var certificates = completedEnrollments.Select(e => {
            var id = Guid.NewGuid();
            return new StudentCertificateDto(
                id,
                e.ActivityId,
                e.Activity.Title,
                e.Activity.ActivityType.ToString(),
                DateTime.UtcNow,
                true,
                GetActivityHours(e.Activity.StartTime ?? DateTime.UtcNow, e.Activity.EndTime ?? DateTime.UtcNow),
                "Instructor por definir",
                $"/certificates/{id}.html"
            );
        }).ToList();

        return Ok(certificates);
    }

    // GET /api/student/qr
    [HttpGet("qr")]
    public async Task<IActionResult> GetQRCode()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return NotFound(new { message = "User not found" });

        // Intentar generar QR seguro para la próxima actividad disponible
        try
        {
            var nextEnrollment = await _db.Enrollments
                .Include(e => e.Activity)
                .Where(e => e.UserId == userId && e.Activity != null && e.Activity.IsActive && e.Activity.Published)
                .OrderBy(e => e.Activity.StartTime ?? DateTime.MaxValue)
                .FirstOrDefaultAsync();

            if (nextEnrollment != null)
            {
                var secure = await _secureQrService.GenerateSecureQrAsync(userId.Value, nextEnrollment.ActivityId);
                if (secure.Success && secure.QrToken != null && secure.ExpiresAt != null)
                {
                    var qrData = new StudentQRDto(
                        $"qr-main-{userId}",
                        secure.QrToken,
                        true,
                        DateTime.UtcNow,
                        secure.ExpiresAt.Value
                    );
                    return Ok(qrData);
                }

                _logger.LogWarning("Fallo generando QR seguro: {Error}", secure.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generando QR seguro, se usará formato legado");
        }

        // Fallback: formato legado predecible (mantener compatibilidad)
        var legacyQr = new StudentQRDto(
            $"qr-main-{userId}",
            $"CONGRESO2024-{userId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
            true,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7)
        );

        return Ok(legacyQr);
    }

    // POST /api/student/qr/generate
    [HttpPost("qr/generate")]
    public async Task<IActionResult> GenerateNewQR()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        // Generar nuevo código QR seguro si es posible
        try
        {
            var nextEnrollment = await _db.Enrollments
                .Include(e => e.Activity)
                .Where(e => e.UserId == userId && e.Activity != null && e.Activity.IsActive && e.Activity.Published)
                .OrderBy(e => e.Activity.StartTime ?? DateTime.MaxValue)
                .FirstOrDefaultAsync();

            if (nextEnrollment != null)
            {
                var secure = await _secureQrService.GenerateSecureQrAsync(userId.Value, nextEnrollment.ActivityId);
                if (secure.Success && secure.QrToken != null && secure.ExpiresAt != null)
                {
                    var qrData = new StudentQRDto(
                        $"qr-main-{userId}",
                        secure.QrToken,
                        true,
                        DateTime.UtcNow,
                        secure.ExpiresAt.Value
                    );
                    return Ok(qrData);
                }

                _logger.LogWarning("Fallo generando QR seguro: {Error}", secure.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generando nuevo QR seguro, se usará formato legado");
        }

        // Fallback legado
        var legacyQr = new StudentQRDto(
            $"qr-main-{userId}",
            $"CONGRESO2024-{userId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
            true,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7)
        );

        return Ok(legacyQr);
    }

    // POST /api/student/certificates/{certificateId}/generate
    [HttpPost("certificates/{certificateId}/generate")]
    public async Task<IActionResult> GenerateCertificate(Guid certificateId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            // Buscar la inscripción correspondiente al certificado
            var enrollment = await _db.Enrollments
                .Include(e => e.Activity)
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.UserId == userId && e.Attended == true);

            if (enrollment == null)
            {
                return NotFound(new { message = "No se encontró una inscripción válida para generar el certificado" });
            }

            // Verificar que la actividad haya terminado
            if (enrollment.Activity.EndTime > DateTime.UtcNow)
            {
                return BadRequest(new { message = "No se puede generar el certificado hasta que la actividad haya terminado" });
            }

            // Generar el contenido del certificado
            var certificateContent = GenerateCertificateContent(enrollment);
            
            // Crear directorio de certificados si no existe
            var certificatesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "certificates");
            Directory.CreateDirectory(certificatesDir);

            // Guardar el certificado como HTML (simulando PDF)
            var fileName = $"{certificateId}.html";
            var filePath = Path.Combine(certificatesDir, fileName);
            await System.IO.File.WriteAllTextAsync(filePath, certificateContent);

            return Ok(new { 
                message = "Certificado generado exitosamente",
                certificateUrl = $"/certificates/{fileName}",
                certificateId = certificateId,
                activityTitle = enrollment.Activity.Title,
                generatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando certificado para usuario {UserId}", userId);
            return StatusCode(500, new { message = "Error interno del servidor al generar el certificado" });
        }
    }

    private string GenerateCertificateContent(Enrollment enrollment)
    {
        var hours = GetActivityHours(enrollment.Activity.StartTime ?? DateTime.UtcNow, enrollment.Activity.EndTime ?? DateTime.UtcNow);
        var currentDate = DateTime.UtcNow.ToString("dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"));

        return $@"<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Certificado de Participación</title>
    <style>
        body {{
            font-family: 'Times New Roman', serif;
            margin: 0;
            padding: 40px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
        }}
        .certificate {{
            background: white;
            padding: 60px;
            border-radius: 15px;
            box-shadow: 0 20px 40px rgba(0,0,0,0.1);
            max-width: 800px;
            text-align: center;
            border: 8px solid #2c3e50;
        }}
        .header {{
            border-bottom: 3px solid #3498db;
            padding-bottom: 20px;
            margin-bottom: 40px;
        }}
        .title {{
            font-size: 36px;
            color: #2c3e50;
            margin: 0;
            font-weight: bold;
            text-transform: uppercase;
            letter-spacing: 2px;
        }}
        .subtitle {{
            font-size: 18px;
            color: #7f8c8d;
            margin: 10px 0 0 0;
        }}
        .recipient {{
            font-size: 24px;
            color: #2c3e50;
            margin: 30px 0;
            font-style: italic;
        }}
        .name {{
            font-size: 32px;
            color: #e74c3c;
            font-weight: bold;
            text-decoration: underline;
            margin: 20px 0;
        }}
        .activity {{
            font-size: 20px;
            color: #2c3e50;
            margin: 30px 0;
            line-height: 1.6;
        }}
        .details {{
            font-size: 16px;
            color: #7f8c8d;
            margin: 20px 0;
        }}
        .footer {{
            margin-top: 50px;
            border-top: 2px solid #ecf0f1;
            padding-top: 30px;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }}
        .signature {{
            text-align: center;
            flex: 1;
        }}
        .signature-line {{
            border-top: 2px solid #2c3e50;
            width: 200px;
            margin: 0 auto 10px auto;
        }}
        .date {{
            font-size: 14px;
            color: #7f8c8d;
        }}
        .logo {{
            font-size: 24px;
            color: #3498db;
            font-weight: bold;
        }}
    </style>
</head>
<body>
    <div class='certificate'>
        <div class='header'>
            <h1 class='title'>Certificado de Participación</h1>
            <p class='subtitle'>Congreso Digital 2024</p>
        </div>
        
        <p class='recipient'>Se otorga el presente certificado a:</p>
        
        <h2 class='name'>{enrollment.User.FullName}</h2>
        
        <div class='activity'>
            Por su participación en la actividad:<br>
            <strong>""{enrollment.Activity.Title}""</strong>
        </div>
        
        <div class='details'>
            <p>Tipo de actividad: <strong>{enrollment.Activity.ActivityType}</strong></p>
            <p>Duración: <strong>{hours} hora{(hours != 1 ? "s" : "")}</strong></p>
            <p>Ubicación: <strong>{enrollment.Activity.Location}</strong></p>
        </div>
        
        <div class='footer'>
            <div class='signature'>
                <div class='signature-line'></div>
                <p><strong>Director del Congreso</strong></p>
            </div>
            <div class='date'>
                <div class='logo'>UMG</div>
                <p>Guatemala, {currentDate}</p>
                <p>Certificado ID: {enrollment.Id}</p>
            </div>
        </div>
    </div>
</body>
</html>";
    }

    // Métodos auxiliares
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static int GetActivityHours(DateTime startTime, DateTime endTime)
    {
        var duration = endTime - startTime;
        return Math.Max(1, (int)Math.Ceiling(duration.TotalHours));
    }

    // DTO para actualización de perfil
    public record UpdateProfileDto(
        string? FullName,
        string? OrgName
    );
}