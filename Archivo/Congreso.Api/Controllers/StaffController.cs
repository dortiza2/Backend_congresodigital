using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Congreso.Api.Data;
using Congreso.Api.Services;
using Congreso.Api.DTOs;
using System.Security.Claims;
using System.Text.RegularExpressions;
using StaffRole = Congreso.Api.Models.StaffRole;

namespace Congreso.Api.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize]
public class StaffController : ControllerBase
{
    private readonly CongresoDbContext _db;
    private readonly ILogger<StaffController> _logger;
    private readonly ISecureQrService _secureQrService;
    private readonly IMetricsCollector _metrics;
    private readonly IStaffService _staffService;

    public StaffController(
        CongresoDbContext db, 
        ILogger<StaffController> logger, 
        ISecureQrService secureQrService, 
        IMetricsCollector metrics,
        IStaffService staffService)
    {
        _db = db;
        _logger = logger;
        _secureQrService = secureQrService;
        _metrics = metrics;
        _staffService = staffService;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    // DTOs
    public record QRScanRequest(string QrCode, Guid? ActivityId = null);
    
    public record QRScanResult(
        bool Success,
        string Message,
        string? ParticipantName = null,
        string? ActivityTitle = null,
        DateTime? Timestamp = null,
        Guid? EnrollmentId = null
    );

    public record StaffStatsDto(
        int TotalParticipants,
        int TotalActivities,
        int TodayAttendance,
        int PendingTasks
    );

    // POST /api/staff/scan-qr
    [HttpPost("scan-qr")]
    public async Task<IActionResult> ScanQR([FromBody] QRScanRequest request)
    {
        var staffUserId = GetCurrentUserId();
        if (staffUserId == null)
            return Unauthorized();

        // Verificar que el usuario actual es staff
        var staffUser = await _db.Users
            .Include(u => u.StaffAccount)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == staffUserId && u.StaffAccount != null);

        if (staffUser == null)
            return Forbid("Access denied. Staff role required.");

        try
        {
            // Primero intentar procesar como token QR seguro (HMAC, anti-replay)
            try
            {
                var validation = await _secureQrService.ValidateSecureQrAsync(request.QrCode);
                if (validation.IsValid && validation.Payload != null)
                {
                    var secureParticipantUserId = Guid.Parse(validation.Payload.Sub);
                    var secureActivityId = Guid.Parse(validation.Payload.Act);

                    var enrollment = await _db.Enrollments
                        .Include(e => e.Activity)
                        .Include(e => e.User)
                        .FirstOrDefaultAsync(e => e.UserId == secureParticipantUserId && e.ActivityId == secureActivityId);

                    if (enrollment == null)
                    {
                        return Ok(new QRScanResult(
                            false,
                            "Inscripción no encontrada para token QR"
                        ));
                    }

                    var process = await _secureQrService.ProcessSecureQrAsync(request.QrCode, staffUserId.Value);
                    if (!process.Success)
                    {
                        _metrics.TrackQrScan(false, true);
                        return Ok(new QRScanResult(
                            false,
                            process.Message
                        ));
                    }

                    _logger.LogInformation("Secure QR attendance for user {UserId} in activity {ActivityId} by staff {StaffUserId}", secureParticipantUserId, secureActivityId, staffUserId);
                    _metrics.TrackQrScan(true, true);

                    return Ok(new QRScanResult(
                        true,
                        "Asistencia registrada exitosamente",
                        enrollment.User.FullName,
                        enrollment.Activity?.Title,
                        process.ProcessedAt ?? DateTime.UtcNow,
                        enrollment.Id
                    ));
                }
            }
            catch (Exception ex)
            {
                // Si falla validación/procesamiento seguro, continuar con flujo legado
                _logger.LogWarning(ex, "Fallo validación/procesamiento de QR seguro; se intenta con formato legado");
                _metrics.TrackQrValidation(false);
            }

            // Flujo legado: Parsear el código QR para extraer el userId
            // Formato esperado: "CONGRESO2024-{userId}-{timestamp}"
            var qrPattern = @"CONGRESO2024-([a-fA-F0-9\-]+)-(\d+)";
            var match = Regex.Match(request.QrCode, qrPattern);

            if (!match.Success)
            {
                _metrics.TrackQrScan(false, false);
                return Ok(new QRScanResult(
                    false,
                    "Código QR inválido o formato no reconocido"
                ));
            }

            var userIdString = match.Groups[1].Value;
            var timestamp = long.Parse(match.Groups[2].Value);

            if (!Guid.TryParse(userIdString, out var participantUserId))
            {
                _metrics.TrackQrScan(false, false);
                return Ok(new QRScanResult(
                    false,
                    "ID de usuario inválido en el código QR"
                ));
            }

            // Verificar que el QR no haya expirado (7 días)
            var qrGeneratedTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            if (DateTime.UtcNow > qrGeneratedTime.AddDays(7))
            {
                _metrics.TrackQrScan(false, false);
                return Ok(new QRScanResult(
                    false,
                    "Código QR expirado"
                ));
            }

            // Buscar al participante
            var participant = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == participantUserId);

            if (participant == null)
            {
                _metrics.TrackQrScan(false, false);
                return Ok(new QRScanResult(
                    false,
                    "Participante no encontrado"
                ));
            }

            // Si se especifica una actividad, buscar inscripción específica
            if (request.ActivityId.HasValue)
            {
                var enrollment = await _db.Enrollments
                    .Include(e => e.Activity)
                    .FirstOrDefaultAsync(e => e.UserId == participantUserId && 
                                            e.ActivityId == request.ActivityId.Value);

                if (enrollment == null)
                {
                    return Ok(new QRScanResult(
                        false,
                        $"El participante {participant.FullName} no está inscrito en esta actividad"
                    ));
                }

                // Verificar si ya tiene asistencia registrada
                if (enrollment.Attended == true)
                {
                    return Ok(new QRScanResult(
                        false,
                        $"El participante {participant.FullName} ya tiene asistencia registrada para {enrollment.Activity?.Title}",
                        participant.FullName,
                        enrollment.Activity?.Title
                    ));
                }

                // Registrar asistencia
                enrollment.Attended = true;
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Attendance registered for user {participantUserId} in activity {request.ActivityId} by staff {staffUserId}");

                return Ok(new QRScanResult(
                    true,
                    "Asistencia registrada exitosamente",
                    participant.FullName,
                    enrollment.Activity?.Title,
                    DateTime.UtcNow,
                    enrollment.Id
                ));
            }
            else
            {
                // Buscar inscripciones activas del día para el participante
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                var todayEnrollments = await _db.Enrollments
                    .Include(e => e.Activity)
                    .Where(e => e.UserId == participantUserId && 
                               e.Activity != null &&
                               e.Activity.StartTime >= today && 
                               e.Activity.StartTime < tomorrow)
                    .ToListAsync();

                if (!todayEnrollments.Any())
                {
                    return Ok(new QRScanResult(
                        false,
                        $"El participante {participant.FullName} no tiene actividades programadas para hoy"
                    ));
                }

                // Buscar la primera actividad sin asistencia registrada
                var pendingEnrollment = todayEnrollments.FirstOrDefault(e => e.Attended != true);
                
                if (pendingEnrollment == null)
                {
                    return Ok(new QRScanResult(
                        false,
                        $"El participante {participant.FullName} ya tiene asistencia registrada en todas sus actividades de hoy"
                    ));
                }

                // Registrar asistencia en la primera actividad pendiente
                pendingEnrollment.Attended = true;
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Attendance registered for user {participantUserId} in activity {pendingEnrollment.ActivityId} by staff {staffUserId}");

                _metrics.TrackQrScan(true, false);
                return Ok(new QRScanResult(
                    true,
                    "Asistencia registrada exitosamente",
                    participant.FullName,
                    pendingEnrollment.Activity?.Title,
                    DateTime.UtcNow,
                    pendingEnrollment.Id
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing QR scan");
            _metrics.TrackQrScan(false, false);
            return Ok(new QRScanResult(
                false,
                "Error interno del servidor al procesar el código QR"
            ));
        }
    }

    // GET /api/staff/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStaffStats()
    {
        var staffUserId = GetCurrentUserId();
        if (staffUserId == null)
            return Unauthorized();

        // Verificar que el usuario actual es staff
        var staffUser = await _db.Users
            .Include(u => u.StaffAccount)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == staffUserId && u.StaffAccount != null);

        if (staffUser == null)
            return Forbid("Access denied. Staff role required.");

        try
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Contar participantes totales
            var totalParticipants = await _db.Users
                .CountAsync(u => u.StudentAccount != null);

            // Contar actividades totales
            var totalActivities = await _db.Activities.CountAsync(a => a.IsActive);

            // Contar asistencias de hoy
            var todayAttendance = await _db.Enrollments
                .Include(e => e.Activity)
                .CountAsync(e => e.Attended == true && 
                               e.Activity != null &&
                               e.Activity.StartTime >= today &&
                               e.Activity.StartTime < tomorrow);

            // Contar tareas pendientes (actividades de hoy sin completar)
            var pendingTasks = await _db.Activities
                .Where(a => a.IsActive && 
                           a.StartTime >= today &&
                           a.StartTime < tomorrow)
                .CountAsync();

            var stats = new StaffStatsDto(
                totalParticipants,
                totalActivities,
                todayAttendance,
                pendingTasks
            );

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting staff stats");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    // GET /api/staff/attendance-history
    [HttpGet("attendance-history")]
    public async Task<IActionResult> GetAttendanceHistory([FromQuery] DateTime? date = null)
    {
        var staffUserId = GetCurrentUserId();
        if (staffUserId == null)
            return Unauthorized();

        // Verificar que el usuario actual es staff
        var staffUser = await _db.Users
            .Include(u => u.StaffAccount)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == staffUserId && u.StaffAccount != null);

        if (staffUser == null)
            return Forbid("Access denied. Staff role required.");

        try
        {
            var targetDate = date ?? DateTime.Today;
            var nextDay = targetDate.AddDays(1);
            
            var attendanceHistory = await _db.Enrollments
                .Include(e => e.User)
                .Include(e => e.Activity)
                .Where(e => e.Attended == true && 
                           e.Activity != null &&
                           e.Activity.StartTime >= targetDate &&
                           e.Activity.StartTime < nextDay)
                .OrderByDescending(e => e.Activity.StartTime)
                .Select(e => new
                {
                    Id = e.Id,
                    ParticipantName = e.User!.FullName,
                    ParticipantEmail = e.User.Email,
                    ActivityTitle = e.Activity!.Title,
                    ActivityStartTime = e.Activity.StartTime,
                    SeatNumber = e.SeatNumber
                })
                .ToListAsync();

            return Ok(attendanceHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance history");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    // POST /api/staff/invite
    [HttpPost("invite")]
    [Authorize(Policy = "RequireStaffOrHigher")]
    public async Task<ActionResult<ApiResponse<StaffInviteResponse>>> InviteStaff([FromBody] StaffInviteRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var invitation = await _staffService.InviteStaffAsync(request, currentUserId.Value);
            return Ok(new ApiResponse<StaffInviteResponseDto>
            {
                Success = true,
                Data = invitation,
                Message = "Staff invitation sent successfully"
            });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new ApiResponse<StaffInviteResponseDto> 
            { 
                Success = false, 
                Message = "insufficient_permissions" 
            });
        }
        catch (InvalidOperationException ex) when (ex.Message == "user_already_staff")
        {
            return Conflict(new ApiResponse<StaffInviteResponseDto> 
            { 
                Success = false, 
                Message = "user_already_staff" 
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Error inviting staff"))
        {
            return StatusCode(500, new ApiResponse<StaffInviteResponseDto> 
            { 
                Success = false, 
                Message = "error_creating_invitation" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inviting staff");
            return StatusCode(500, new ApiResponse<StaffInviteResponseDto> 
            { 
                Success = false, 
                Message = "error_creating_invitation" 
            });
        }
    }

    // GET /api/staff/invitations
    [HttpGet("invitations")]
    [Authorize(Policy = "RequireStaffOrHigher")]
    public async Task<ActionResult<ApiResponse<List<StaffInviteResponseDto>>>> GetStaffInvitations()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            var invitations = await _staffService.GetStaffInvitationsAsync();
            return Ok(new ApiResponse<List<StaffInviteResponseDto>>
            {
                Success = true,
                Data = invitations,
                Message = "Staff invitations retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting staff invitations");
            return StatusCode(500, new ApiResponse<List<StaffInviteResponseDto>>
            {
                Success = false,
                Message = "error_retrieving_invitations"
            });
        }
    }

    // GET /api/staff/all
    [HttpGet("all")]
    [Authorize(Policy = "RequireStaffOrHigher")]
    public async Task<ActionResult<ApiResponse<List<StaffDto>>>> GetAllStaff()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            var staff = await _staffService.GetAllStaffAsync();
            return Ok(ApiResponse<List<StaffDto>>.CreateSuccess(staff, "Staff members retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all staff");
            return StatusCode(500, new ApiResponse<List<StaffDto>>
            {
                Success = false,
                Message = "error_retrieving_staff"
            });
        }
    }

    // GET /api/staff/search
    [HttpGet("search")]
    [Authorize(Policy = "RequireStaffOrHigher")]
    public async Task<ActionResult<ApiResponse<List<StaffDto>>>> SearchStaff([FromQuery] string q)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(q))
                return Ok(ApiResponse<List<StaffDto>>.CreateSuccess(new List<StaffDto>(), "No search query provided"));

            var staff = await _staffService.SearchStaffAsync(q);
            return Ok(ApiResponse<List<StaffDto>>.CreateSuccess(staff, "Staff search completed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching staff");
            return StatusCode(500, ApiResponse<List<StaffDto>>.CreateError("error_searching_staff"));
        }
    }

    // GET /api/staff/role/{role}
    [HttpGet("role/{role}")]
    [Authorize(Policy = "RequireStaffOrHigher")]
    public async Task<ActionResult<ApiResponse<List<StaffDto>>>> GetStaffByRole([FromRoute] StaffRole role)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            var staff = await _staffService.GetStaffByRoleAsync(role);
            return Ok(new ApiResponse<List<StaffDto>>
            {
                Success = true,
                Data = staff,
                Message = "Staff members retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting staff by role");
            return StatusCode(500, ApiResponse<List<StaffDto>>.CreateError("error_retrieving_staff_by_role"));
        }
    }

    // DELETE /api/staff/invitations/{invitationId}
    [HttpDelete("invitations/{invitationId:guid}")]
    [Authorize(Policy = "RequireStaffOrHigher")]
    public async Task<ActionResult<ApiResponse<bool>>> RevokeInvitation([FromRoute] Guid invitationId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            var success = await _staffService.RevokeInvitationAsync(invitationId);
            if (!success)
                return NotFound(ApiResponse<bool>.CreateError("invitation_not_found"));

            return Ok(ApiResponse<bool>.CreateSuccess(true, "Invitation revoked successfully"));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse<bool>.CreateError("insufficient_permissions"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking invitation");
            return StatusCode(500, ApiResponse<bool>.CreateError("error_revoking_invitation"));
        }
    }

    // PATCH /api/staff/{staffId}/role
    [HttpPatch("{staffId:guid}/role")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateStaffRole([FromRoute] Guid staffId, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _staffService.UpdateStaffRoleAsync(staffId, request.RoleId, currentUserId.Value);
            if (!success)
                return NotFound(ApiResponse<bool>.CreateError("staff_not_found"));

            return Ok(ApiResponse<bool>.CreateSuccess(true, "Staff role updated successfully"));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse<bool>.CreateError("insufficient_permissions"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating staff role");
            return StatusCode(500, ApiResponse<bool>.CreateError("error_updating_staff_role"));
        }
    }

    // PATCH /api/staff/{staffId}/status
    [HttpPatch("{staffId:guid}/status")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateStaffStatus([FromRoute] Guid staffId, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _staffService.UpdateStaffStatusAsync(staffId, request.IsActive, currentUserId.Value);
            if (!success)
                return NotFound(ApiResponse<bool>.CreateError("staff_not_found"));

            return Ok(ApiResponse<bool>.CreateSuccess(true, "Staff status updated successfully"));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse<bool>.CreateError("insufficient_permissions"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating staff status");
            return StatusCode(500, ApiResponse<bool>.CreateError("error_updating_staff_status"));
        }
    }
}