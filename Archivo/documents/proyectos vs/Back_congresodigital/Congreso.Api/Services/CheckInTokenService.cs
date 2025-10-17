using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Congreso.Api.Data;
using Congreso.Api.Models;

namespace Congreso.Api.Services;

public interface ICheckInTokenService
{
    Task<string> GenerateTokenAsync(Guid userId, Guid activityId, Guid enrollmentId);
    Task<CheckInTokenValidationResult> ValidateTokenAsync(string token);
    Task<CheckInResult> ProcessCheckInAsync(string token, Guid assistantUserId);
}

public class CheckInTokenValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public CheckInToken? Token { get; set; }
}

public class CheckInResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool WasAlreadyCheckedIn { get; set; }
    public DateTime? CheckInTime { get; set; }
}

public class CheckInTokenService : ICheckInTokenService
{
    private readonly CongresoDbContext _context;
    private readonly ILogger<CheckInTokenService> _logger;

    public CheckInTokenService(CongresoDbContext context, ILogger<CheckInTokenService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GenerateTokenAsync(Guid userId, Guid activityId, Guid enrollmentId)
    {
        // Generar token único y seguro
        var tokenData = $"{userId}:{activityId}:{enrollmentId}:{DateTime.UtcNow.Ticks}";
        var tokenBytes = SHA256.HashData(Encoding.UTF8.GetBytes(tokenData));
        var token = Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");

        // Crear registro en la base de datos
        var checkInToken = new CheckInToken
        {
            Id = Guid.NewGuid(),
            Token = token,
            UserId = userId,
            ActivityId = activityId,
            EnrollmentId = enrollmentId,
            ExpiresAt = DateTime.UtcNow.AddHours(24), // Token válido por 24 horas
            Used = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.CheckInTokens.Add(checkInToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated check-in token {TokenPrefix} for UserId: {UserId}, ActivityId: {ActivityId}, TokenLength: {TokenLength}",
            token[..8] + "...", userId, activityId, token.Length);

        return token;
    }

    public async Task<CheckInTokenValidationResult> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new CheckInTokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token no válido"
            };
        }

        var checkInToken = await _context.CheckInTokens
            .Include(t => t.User)
            .Include(t => t.Activity)
            .Include(t => t.Enrollment)
            .FirstOrDefaultAsync(t => t.Token == token);

        if (checkInToken == null)
        {
            return new CheckInTokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token no encontrado"
            };
        }

        if (checkInToken.ExpiresAt < DateTime.UtcNow)
        {
            return new CheckInTokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token expirado"
            };
        }

        return new CheckInTokenValidationResult
        {
            IsValid = true,
            Token = checkInToken
        };
    }

    public async Task<CheckInResult> ProcessCheckInAsync(string token, Guid assistantUserId)
    {
        var validation = await ValidateTokenAsync(token);
        
        if (!validation.IsValid)
        {
            return new CheckInResult
            {
                Success = false,
                Message = validation.ErrorMessage ?? "Token no válido"
            };
        }

        var checkInToken = validation.Token!;

        // Verificar si ya fue usado (idempotencia)
        if (checkInToken.Used)
        {
            return new CheckInResult
            {
                Success = true,
                Message = "Asistencia ya estaba registrada",
                WasAlreadyCheckedIn = true,
                CheckInTime = checkInToken.UsedAt
            };
        }

        // Marcar el token como usado
        checkInToken.Used = true;
        checkInToken.UsedAt = DateTime.UtcNow;
        checkInToken.UsedBy = assistantUserId;

        // Marcar la asistencia en el enrollment
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.Id == checkInToken.EnrollmentId);

        if (enrollment != null)
        {
            enrollment.Attended = true;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Check-in procesado exitosamente para token {TokenPrefix}, usuario {UserId}, actividad {ActivityId}", 
            token[..8] + "...", checkInToken.UserId, checkInToken.ActivityId);

        return new CheckInResult
        {
            Success = true,
            Message = "Asistencia registrada exitosamente",
            WasAlreadyCheckedIn = false,
            CheckInTime = checkInToken.UsedAt
        };
    }
}