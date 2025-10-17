using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Congreso.Api.Data;
using Congreso.Api.Models;
using Congreso.Api.DTOs;

namespace Congreso.Api.Services;

public interface IQrAttendanceService
{
    Task<QrVerificationResponseDto> VerifyQrAndMarkAttendanceAsync(QrVerificationDto dto);
    Task<List<Enrollment>> GetAttendanceByActivityAsync(Guid activityId);
    Task<List<Enrollment>> GetAttendanceByUserAsync(Guid userId);
    Task<object> GetAttendanceStatsAsync(Guid activityId);
    Task<bool> GenerateQrCodeForEnrollmentAsync(Guid enrollmentId);
}

public class QrAttendanceService : IQrAttendanceService
{
    private readonly CongresoDbContext _context;

    public QrAttendanceService(CongresoDbContext context)
    {
        _context = context;
    }

    public async Task<QrVerificationResponseDto> VerifyQrAndMarkAttendanceAsync(QrVerificationDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Verificar que existe la inscripción
            var enrollment = await _context.Enrollments
                .Include(e => e.User)
                .Include(e => e.Activity)
                .FirstOrDefaultAsync(e => e.UserId == dto.UserId && e.ActivityId == dto.ActivityId);

            if (enrollment == null)
            {
                return new QrVerificationResponseDto
                {
                    Success = false,
                    Message = "No se encontró inscripción para este usuario y actividad"
                };
            }

            // 2. Verificar que la actividad está activa y publicada
            if (!enrollment.Activity.IsActive || !enrollment.Activity.Published)
            {
                return new QrVerificationResponseDto
                {
                    Success = false,
                    Message = "La actividad no está disponible para verificación de asistencia"
                };
            }

            // 3. Verificar/generar QR code
            if (string.IsNullOrEmpty(enrollment.QrCodeId))
            {
                enrollment.QrCodeId = GenerateQrCode();
            }

            // 4. Verificar que el QR code coincide (si se proporciona)
            if (!string.IsNullOrEmpty(dto.QrCodeId) && enrollment.QrCodeId != dto.QrCodeId)
            {
                return new QrVerificationResponseDto
                {
                    Success = false,
                    Message = "Código QR inválido"
                };
            }

            // 5. Verificar si ya estaba marcado como asistido
            var wasAlreadyAttended = enrollment.Attended;
            var attendanceTime = DateTime.UtcNow;

            // 6. Marcar asistencia
            enrollment.Attended = true;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new QrVerificationResponseDto
            {
                Success = true,
                Message = wasAlreadyAttended ? "Asistencia ya registrada previamente" : "Asistencia registrada exitosamente",
                EnrollmentId = enrollment.Id,
                WasAlreadyAttended = wasAlreadyAttended,
                AttendanceTime = attendanceTime,
                User = MapToUserBasicDto(enrollment.User),
                Activity = MapToActivityBasicDto(enrollment.Activity)
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new QrVerificationResponseDto
            {
                Success = false,
                Message = $"Error al procesar verificación: {ex.Message}"
            };
        }
    }

    public async Task<List<Enrollment>> GetAttendanceByActivityAsync(Guid activityId)
    {
        return await _context.Enrollments
            .Include(e => e.User)
            .Include(e => e.Activity)
            .Where(e => e.ActivityId == activityId)
            .OrderByDescending(e => e.Attended)
            .ThenBy(e => e.User.FullName)
            .ToListAsync();
    }

    public async Task<List<Enrollment>> GetAttendanceByUserAsync(Guid userId)
    {
        return await _context.Enrollments
            .Include(e => e.Activity)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Activity.StartTime)
            .ToListAsync();
    }

    public async Task<object> GetAttendanceStatsAsync(Guid activityId)
    {
        var enrollments = await _context.Enrollments
            .Where(e => e.ActivityId == activityId)
            .ToListAsync();

        var activity = await _context.Activities
            .FirstOrDefaultAsync(a => a.Id == activityId);

        var totalEnrolled = enrollments.Count;
        var totalAttended = enrollments.Count(e => e.Attended);
        var attendanceRate = totalEnrolled > 0 ? (double)totalAttended / totalEnrolled * 100 : 0;

        return new
        {
            ActivityId = activityId,
            ActivityTitle = activity?.Title ?? "Actividad no encontrada",
            ActivityType = activity?.ActivityType.ToString() ?? "N/A",
            TotalEnrolled = totalEnrolled,
            TotalAttended = totalAttended,
            AttendanceRate = Math.Round(attendanceRate, 2),
            Capacity = activity?.Capacity,
            CapacityUtilization = activity?.Capacity > 0 ? Math.Round((double)totalEnrolled / activity.Capacity.Value * 100, 2) : (double?)null,
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task<bool> GenerateQrCodeForEnrollmentAsync(Guid enrollmentId)
    {
        var enrollment = await _context.Enrollments.FindAsync(enrollmentId);
        if (enrollment == null)
            return false;

        if (string.IsNullOrEmpty(enrollment.QrCodeId))
        {
            enrollment.QrCodeId = GenerateQrCode();
            await _context.SaveChangesAsync();
        }

        return true;
    }

    #region Helper Methods

    private static string GenerateQrCode()
    {
        // Generar un código QR único
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var random = new Random().Next(1000, 9999);
        return $"QR_{timestamp}_{random}";
    }

    private static UserBasicDto MapToUserBasicDto(User user)
    {
        return new UserBasicDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName ?? string.Empty,
            AvatarUrl = user.AvatarUrl,
            Status = user.Status ?? "active",
            IsUmg = user.IsUmg,
            OrgName = user.OrgName
        };
    }

    private static ActivityBasicDto MapToActivityBasicDto(Activity activity)
    {
        // Convert Models.ActivityType to DTOs.ActivityType
        var dtoActivityType = activity.ActivityType switch
        {
            Models.ActivityType.TALLER => DTOs.ActivityType.Workshop,
            Models.ActivityType.CHARLA => DTOs.ActivityType.Talk,
            Models.ActivityType.COMPETENCIA => DTOs.ActivityType.Competition,
            _ => DTOs.ActivityType.Other
        };
        
        return new ActivityBasicDto
        {
            Id = activity.Id,
            Title = activity.Title,
            ActivityType = dtoActivityType,
            Location = activity.Location,
            StartTime = activity.StartTime ?? DateTime.MinValue,
            EndTime = activity.EndTime ?? DateTime.MinValue
        };
    }

    #endregion
}

/// <summary>
/// Extensiones para el controlador de QR/Asistencia
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QrAttendanceController : ControllerBase
{
    private readonly IQrAttendanceService _qrService;

    public QrAttendanceController(IQrAttendanceService qrService)
    {
        _qrService = qrService;
    }

    /// <summary>
    /// Verifica código QR y marca asistencia
    /// </summary>
    [HttpPost("verify")]
    [Authorize(Policy = "StaffOnly")]
    public async Task<ActionResult<QrVerificationResponseDto>> VerifyQrCode([FromBody] QrVerificationDto dto)
    {
        var result = await _qrService.VerifyQrAndMarkAttendanceAsync(dto);
        
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtiene asistencia por actividad
    /// </summary>
    [HttpGet("activity/{activityId:guid}")]
    [Authorize(Policy = "StaffOnly")]
    public async Task<ActionResult<List<Enrollment>>> GetAttendanceByActivity(Guid activityId)
    {
        var attendance = await _qrService.GetAttendanceByActivityAsync(activityId);
        return Ok(attendance);
    }

    /// <summary>
    /// Obtiene asistencia por usuario
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<List<Enrollment>>> GetAttendanceByUser(Guid userId)
    {
        // Verificar permisos: staff o el mismo usuario
        var currentUserId = GetCurrentUserId();
        if (currentUserId != userId && !IsCurrentUserStaff())
            return Forbid();

        var attendance = await _qrService.GetAttendanceByUserAsync(userId);
        return Ok(attendance);
    }

    /// <summary>
    /// Obtiene estadísticas de asistencia para una actividad
    /// </summary>
    [HttpGet("stats/{activityId:guid}")]
    [Authorize(Policy = "StaffOnly")]
    public async Task<ActionResult<object>> GetAttendanceStats(Guid activityId)
    {
        var stats = await _qrService.GetAttendanceStatsAsync(activityId);
        return Ok(stats);
    }

    /// <summary>
    /// Genera código QR para una inscripción
    /// </summary>
    [HttpPost("generate/{enrollmentId:guid}")]
    [Authorize(Policy = "StaffOnly")]
    public async Task<ActionResult> GenerateQrCode(Guid enrollmentId)
    {
        var success = await _qrService.GenerateQrCodeForEnrollmentAsync(enrollmentId);
        
        if (!success)
            return NotFound("Inscripción no encontrada");

        return Ok(new { Message = "Código QR generado exitosamente" });
    }

    /// <summary>
    /// Obtiene mi asistencia (usuario actual)
    /// </summary>
    [HttpGet("my-attendance")]
    public async Task<ActionResult<List<Enrollment>>> GetMyAttendance()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var attendance = await _qrService.GetAttendanceByUserAsync(userId.Value);
        return Ok(attendance);
    }

    #region Helper Methods

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private bool IsCurrentUserStaff()
    {
        var role = User.FindFirst("role")?.Value;
        return role == "adminDev" || role == "admin" || role == "asistente";
    }

    #endregion
}