using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Congreso.Api.Data;
using Congreso.Api.Models;

namespace Congreso.Api.Services;

public interface ISecureQrService
{
    Task<SecureQrGenerationResult> GenerateSecureQrAsync(Guid userId, Guid activityId);
    Task<SecureQrValidationResult> ValidateSecureQrAsync(string qrToken);
    Task<SecureQrProcessResult> ProcessSecureQrAsync(string qrToken, Guid assistantUserId);
}

public class SecureQrGenerationResult
{
    public bool Success { get; set; }
    public string? QrToken { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? JwtId { get; set; }
}

public class SecureQrValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public SecureQrPayload? Payload { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class SecureQrProcessResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool WasAlreadyProcessed { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public class SecureQrPayload
{
    public string Sub { get; set; } = string.Empty;  // Subject (user_id)
    public string Act { get; set; } = string.Empty;  // Activity ID
    public long Iat { get; set; }                    // Issued At (Unix timestamp)
    public string Jti { get; set; } = string.Empty;  // JWT ID (unique identifier)
    public long Exp { get; set; }                    // Expiration (Unix timestamp)
}

public class SecureQrService : ISecureQrService
{
    private readonly CongresoDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecureQrService> _logger;
    private readonly IMetricsCollector _metrics;

    public SecureQrService(
        CongresoDbContext context, 
        IConfiguration configuration, 
        ILogger<SecureQrService> logger,
        IMetricsCollector metrics)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<SecureQrGenerationResult> GenerateSecureQrAsync(Guid userId, Guid activityId)
    {
        try
        {
            // Verificar que el usuario y la actividad existen
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return new SecureQrGenerationResult
                {
                    Success = false,
                    ErrorMessage = "Usuario no encontrado"
                };
            }

            var activity = await _context.Activities
                .FirstOrDefaultAsync(a => a.Id == activityId && a.IsActive && a.Published);
            
            if (activity == null)
            {
                return new SecureQrGenerationResult
                {
                    Success = false,
                    ErrorMessage = "Actividad no encontrada o no disponible"
                };
            }

            // Verificar que el usuario está inscrito en la actividad
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.ActivityId == activityId);
            
            if (enrollment == null)
            {
                return new SecureQrGenerationResult
                {
                    Success = false,
                    ErrorMessage = "Usuario no inscrito en esta actividad"
                };
            }

            // Generar payload seguro
            var now = DateTimeOffset.UtcNow;
            var ttlSeconds = GetQrTtlSeconds();
            var expiresAt = now.AddSeconds(ttlSeconds);
            var jwtId = Guid.NewGuid().ToString("N");

            var payload = new SecureQrPayload
            {
                Sub = userId.ToString(),
                Act = activityId.ToString(),
                Iat = now.ToUnixTimeSeconds(),
                Jti = jwtId,
                Exp = expiresAt.ToUnixTimeSeconds()
            };

            // Generar token firmado con HMAC
            var qrToken = GenerateSignedToken(payload);

            // Guardar JTI para anti-replay
            await SaveJwtIdAsync(jwtId, userId, activityId, expiresAt.DateTime);

            _logger.LogInformation(
                "Generated secure QR token with JTI {JwtIdPrefix} for UserId: {UserId}, ActivityId: {ActivityId}, ExpiresAt: {ExpiresAt}",
                jwtId[..8] + "...", userId, activityId, expiresAt);

            var result = new SecureQrGenerationResult
            {
                Success = true,
                QrToken = qrToken,
                ExpiresAt = expiresAt.DateTime,
                JwtId = jwtId
            };
            _metrics.TrackQrGeneration(true, true);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating secure QR for UserId: {UserId}, ActivityId: {ActivityId}", 
                userId, activityId);
            
            _metrics.TrackQrGeneration(false, true);
            return new SecureQrGenerationResult
            {
                Success = false,
                ErrorMessage = "Error interno generando código QR"
            };
    }
    }

    public async Task<SecureQrValidationResult> ValidateSecureQrAsync(string qrToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(qrToken))
            {
                return new SecureQrValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Token QR vacío o inválido"
                };
            }

            // Verificar y extraer payload
            var payload = VerifySignedToken(qrToken);
            if (payload == null)
            {
                _logger.LogWarning("Invalid QR token signature or format");
                _metrics.TrackQrValidation(false);
                return new SecureQrValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Firma de token QR inválida"
                };
            }

            // Verificar expiración
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (payload.Exp < now)
            {
                _logger.LogWarning("Expired QR token with JTI {JwtIdPrefix}, expired at {ExpiredAt}", 
                    payload.Jti[..8] + "...", DateTimeOffset.FromUnixTimeSeconds(payload.Exp));
                _metrics.TrackQrValidation(false);
                
                return new SecureQrValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Código QR expirado"
                };
            }

            // Verificar anti-replay (JTI único)
            var jtiExists = await _context.QrJwtIds
                .AnyAsync(j => j.JwtId == payload.Jti);
            
            if (!jtiExists)
            {
                _logger.LogWarning("QR token with unknown JTI {JwtIdPrefix}", payload.Jti[..8] + "...");
                _metrics.TrackQrValidation(false);
                return new SecureQrValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Token QR no reconocido"
                };
            }

            // Verificar que el JTI no ha sido usado
            var jtiRecord = await _context.QrJwtIds
                .FirstOrDefaultAsync(j => j.JwtId == payload.Jti);
            
            if (jtiRecord?.Used == true)
            {
                _logger.LogWarning("Replay attempt detected for JTI {JwtIdPrefix}", payload.Jti[..8] + "...");
                _metrics.TrackQrValidation(false);
                return new SecureQrValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Código QR ya utilizado"
                };
            }

            var validationResult = new SecureQrValidationResult
            {
                IsValid = true,
                Payload = payload,
                ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(payload.Exp).DateTime
            };
            _metrics.TrackQrValidation(true);
            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating secure QR token");
            _metrics.TrackQrValidation(false);
            return new SecureQrValidationResult
            {
                IsValid = false,
                ErrorMessage = "Error interno validando código QR"
            };
        }
    }

    public async Task<SecureQrProcessResult> ProcessSecureQrAsync(string qrToken, Guid assistantUserId)
    {
        try
        {
            // Validar token
            var validation = await ValidateSecureQrAsync(qrToken);
            if (!validation.IsValid)
            {
                _metrics.TrackQrScan(false, true);
                return new SecureQrProcessResult
                {
                    Success = false,
                    Message = validation.ErrorMessage ?? "Token QR inválido"
                };
            }

            var payload = validation.Payload!;
            var userId = Guid.Parse(payload.Sub);
            var activityId = Guid.Parse(payload.Act);

            // Verificar que el enrollment existe
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.ActivityId == activityId);
            
            if (enrollment == null)
            {
                _metrics.TrackQrScan(false, true);
                return new SecureQrProcessResult
                {
                    Success = false,
                    Message = "Inscripción no encontrada"
                };
            }

            // Verificar si ya fue procesado (idempotencia)
            var jtiRecord = await _context.QrJwtIds
                .FirstOrDefaultAsync(j => j.JwtId == payload.Jti);
            
            if (jtiRecord?.Used == true)
            {
                _metrics.TrackQrScan(true, true);
                return new SecureQrProcessResult
                {
                    Success = true,
                    Message = "Asistencia ya estaba registrada",
                    WasAlreadyProcessed = true,
                    ProcessedAt = jtiRecord.UsedAt
                };
            }

            // Marcar como usado (anti-replay)
            if (jtiRecord != null)
            {
                jtiRecord.Used = true;
                jtiRecord.UsedAt = DateTime.UtcNow;
                jtiRecord.UsedBy = assistantUserId;
            }

            // Marcar asistencia
            enrollment.Attended = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Secure QR processed successfully for JTI {JwtIdPrefix}, UserId: {UserId}, ActivityId: {ActivityId}, AssistantId: {AssistantId}",
                payload.Jti[..8] + "...", userId, activityId, assistantUserId);
            _metrics.TrackQrScan(true, true);

            return new SecureQrProcessResult
            {
                Success = true,
                Message = "Asistencia registrada exitosamente",
                WasAlreadyProcessed = false,
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing secure QR token");
            _metrics.TrackQrScan(false, true);
            return new SecureQrProcessResult
            {
                Success = false,
                Message = "Error interno procesando código QR"
            };
        }
    }

    private string GenerateSignedToken(SecureQrPayload payload)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var jsonBase64 = Convert.ToBase64String(jsonBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        // Generar firma HMAC
        var secret = GetHmacSecret();
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signatureBytes = hmac.ComputeHash(jsonBytes);
        var signatureBase64 = Convert.ToBase64String(signatureBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        return $"{jsonBase64}.{signatureBase64}";
    }

    private SecureQrPayload? VerifySignedToken(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 2)
                return null;

            var jsonBase64 = parts[0];
            var signatureBase64 = parts[1];

            // Restaurar padding de Base64
            jsonBase64 = RestoreBase64Padding(jsonBase64.Replace("-", "+").Replace("_", "/"));
            signatureBase64 = RestoreBase64Padding(signatureBase64.Replace("-", "+").Replace("_", "/"));

            var jsonBytes = Convert.FromBase64String(jsonBase64);
            var providedSignature = Convert.FromBase64String(signatureBase64);

            // Verificar firma
            var secret = GetHmacSecret();
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var expectedSignature = hmac.ComputeHash(jsonBytes);

            if (!providedSignature.SequenceEqual(expectedSignature))
                return null;

            // Deserializar payload
            var json = Encoding.UTF8.GetString(jsonBytes);
            return JsonSerializer.Deserialize<SecureQrPayload>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveJwtIdAsync(string jwtId, Guid userId, Guid activityId, DateTime expiresAt)
    {
        var qrJwtId = new QrJwtId
        {
            Id = Guid.NewGuid(),
            JwtId = jwtId,
            UserId = userId,
            ActivityId = activityId,
            ExpiresAt = expiresAt,
            Used = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.QrJwtIds.Add(qrJwtId);
        await _context.SaveChangesAsync();
    }

    private string GetHmacSecret()
    {
        var secret = _configuration["QR_HMAC_SECRET"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogCritical("QR_HMAC_SECRET is missing. Secure QR will be disabled.");
            throw new InvalidOperationException("QR_HMAC_SECRET environment variable is not configured");
        }

        // Validar longitud mínima y caracteres permitidos
        if (secret.Length < 32)
        {
            _logger.LogCritical("QR_HMAC_SECRET is too short (length: {Length}).", secret.Length);
            throw new InvalidOperationException("QR_HMAC_SECRET is too short; must be at least 32 characters.");
        }
        if (!secret.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.'))
        {
            _logger.LogWarning("QR_HMAC_SECRET contains unexpected characters. Continuing, but consider rotating.");
        }

        return secret;
    }

    private int GetQrTtlSeconds()
    {
        var ttlString = _configuration["QR_TTL_SECONDS"];
        if (int.TryParse(ttlString, out var ttl) && ttl > 0)
        {
            return ttl;
        }
        return 604800; // Default: 7 días
    }

    private static string RestoreBase64Padding(string base64)
    {
        var padding = base64.Length % 4;
        if (padding > 0)
        {
            base64 += new string('=', 4 - padding);
        }
        return base64;
    }
}