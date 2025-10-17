using Congreso.Api.DTOs.Certificate;
using Npgsql;

namespace Congreso.Api.Services;

/// <summary>
/// Implementación del validador de elegibilidad
/// </summary>
public class EligibilityValidator : IEligibilityValidator
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ICertificateRepository _certificateRepository;
    private readonly ILogger<EligibilityValidator> _logger;

    public EligibilityValidator(
        NpgsqlDataSource dataSource,
        ICertificateRepository certificateRepository,
        ILogger<EligibilityValidator> logger)
    {
        _dataSource = dataSource;
        _certificateRepository = certificateRepository;
        _logger = logger;
    }

    public async Task<EligibilityValidationResult> ValidateEligibilityAsync(Guid userId, CertificateType certificateType, int? activityId = null, int? enrollmentId = null)
    {
        try
        {
            _logger.LogInformation("Validando elegibilidad para usuario {UserId}, tipo {CertificateType}", userId, certificateType);

            // Validar que el usuario existe
            if (!await UserExistsAsync(userId))
            {
                return new EligibilityValidationResult
                {
                    IsEligible = false,
                    Message = "Usuario no encontrado",
                    ErrorCode = "USER_NOT_FOUND"
                };
            }

            // Obtener requisitos para el tipo de certificado
            var requirements = GetRequirements(certificateType);

            // Validar según el tipo de certificado
            switch (certificateType)
            {
                case CertificateType.Attendance:
                    return await ValidateAttendanceEligibilityAsync(userId, activityId, enrollmentId, requirements);
                    
                case CertificateType.Participation:
                    return await ValidateParticipationEligibilityAsync(userId, requirements);
                    
                case CertificateType.Speaker:
                    return await ValidateSpeakerEligibilityAsync(userId, activityId, requirements);
                    
                case CertificateType.Organizer:
                    return await ValidateOrganizerEligibilityAsync(userId, requirements);
                    
                case CertificateType.Winner:
                    return await ValidateWinnerEligibilityAsync(userId, activityId, requirements);
                    
                default:
                    return new EligibilityValidationResult
                    {
                        IsEligible = false,
                        Message = "Tipo de certificado no válido",
                        ErrorCode = "INVALID_CERTIFICATE_TYPE"
                    };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar elegibilidad para usuario {UserId}", userId);
            return new EligibilityValidationResult
            {
                IsEligible = false,
                Message = "Error al validar elegibilidad",
                ErrorCode = "VALIDATION_ERROR"
            };
        }
    }

    public async Task<bool> HasExistingCertificateAsync(Guid userId, CertificateType certificateType, int? activityId = null)
    {
        return await _certificateRepository.HasExistingCertificateAsync(userId, certificateType, activityId);
    }

    public CertificateRequirements GetRequirements(CertificateType certificateType)
    {
        return certificateType switch
        {
            CertificateType.Attendance => new CertificateRequirements
            {
                MinimumAttendancePercentage = 80,
                RequiresEnrollment = true,
                RequiresSpecificActivity = true,
                MinimumParticipationTimeMinutes = 60
            },
            CertificateType.Participation => new CertificateRequirements
            {
                MinimumAttendancePercentage = 60,
                RequiresEnrollment = true,
                RequiresSpecificActivity = false,
                MinimumParticipationTimeMinutes = 120
            },
            CertificateType.Speaker => new CertificateRequirements
            {
                RequiresSpecificActivity = true,
                RequiredRoles = new[] { "PONENTE", "SPEAKER" }
            },
            CertificateType.Organizer => new CertificateRequirements
            {
                RequiredRoles = new[] { "ORGANIZADOR", "STAFF", "ADMIN", "DVADMIN" }
            },
            CertificateType.Winner => new CertificateRequirements
            {
                RequiresSpecificActivity = true
            },
            _ => new CertificateRequirements()
        };
    }

    private async Task<bool> UserExistsAsync(Guid userId)
    {
        const string sql = "SELECT COUNT(1) FROM users WHERE id = @userId;";
        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("@userId", userId);
        
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    private async Task<EligibilityValidationResult> ValidateAttendanceEligibilityAsync(Guid userId, int? activityId, int? enrollmentId, CertificateRequirements requirements)
    {
        // Validar que tenga enrollment o actividad específica
        if (requirements.RequiresEnrollment && !enrollmentId.HasValue && !activityId.HasValue)
        {
            return new EligibilityValidationResult
            {
                IsEligible = false,
                Message = "Se requiere inscripción o actividad específica para certificado de asistencia",
                ErrorCode = "REQUIRES_ENROLLMENT_OR_ACTIVITY"
            };
        }

        // Validar asistencia mínima
        if (enrollmentId.HasValue)
        {
            var attendancePercentage = await GetEnrollmentAttendancePercentageAsync(enrollmentId.Value);
            if (attendancePercentage < requirements.MinimumAttendancePercentage)
            {
                return new EligibilityValidationResult
                {
                    IsEligible = false,
                    Message = $"Asistencia insuficiente: {attendancePercentage}% (mínimo requerido: {requirements.MinimumAttendancePercentage}%)",
                    ErrorCode = "INSUFFICIENT_ATTENDANCE",
                    Details = new Dictionary<string, object> { ["currentAttendance"] = attendancePercentage, ["requiredAttendance"] = requirements.MinimumAttendancePercentage }
                };
            }
        }

        // Validar tiempo de participación
        if (enrollmentId.HasValue && requirements.MinimumParticipationTimeMinutes.HasValue)
        {
            var participationTime = await GetEnrollmentParticipationTimeAsync(enrollmentId.Value);
            if (participationTime < requirements.MinimumParticipationTimeMinutes.Value)
            {
                return new EligibilityValidationResult
                {
                    IsEligible = false,
                    Message = $"Tiempo de participación insuficiente: {participationTime} minutos (mínimo requerido: {requirements.MinimumParticipationTimeMinutes} minutos)",
                    ErrorCode = "INSUFFICIENT_PARTICIPATION_TIME",
                    Details = new Dictionary<string, object> { ["currentTime"] = participationTime, ["requiredTime"] = requirements.MinimumParticipationTimeMinutes }
                };
            }
        }

        return new EligibilityValidationResult
        {
            IsEligible = true,
            Message = "Elegible para certificado de asistencia",
            Details = new Dictionary<string, object> { ["attendancePercentage"] = 100, ["participationTime"] = 120 }
        };
    }

    private async Task<EligibilityValidationResult> ValidateParticipationEligibilityAsync(Guid userId, CertificateRequirements requirements)
    {
        // Validar participación general en el congreso
        var totalParticipationTime = await GetUserTotalParticipationTimeAsync(userId);
        
        if (totalParticipationTime < requirements.MinimumParticipationTimeMinutes)
        {
            return new EligibilityValidationResult
            {
                IsEligible = false,
                Message = $"Tiempo de participación total insuficiente: {totalParticipationTime} minutos (mínimo requerido: {requirements.MinimumParticipationTimeMinutes} minutos)",
                ErrorCode = "INSUFFICIENT_TOTAL_PARTICIPATION",
                Details = new Dictionary<string, object> { ["currentTime"] = totalParticipationTime, ["requiredTime"] = requirements.MinimumParticipationTimeMinutes }
            };
        }

        // Validar número mínimo de actividades
        var activityCount = await GetUserActivityCountAsync(userId);
        if (activityCount < 3) // Mínimo 3 actividades
        {
            return new EligibilityValidationResult
            {
                IsEligible = false,
                Message = $"Número de actividades insuficiente: {activityCount} (mínimo requerido: 3)",
                ErrorCode = "INSUFFICIENT_ACTIVITIES",
                Details = new Dictionary<string, object> { ["currentActivities"] = activityCount, ["requiredActivities"] = 3 }
            };
        }

        return new EligibilityValidationResult
        {
            IsEligible = true,
            Message = "Elegible para certificado de participación",
            Details = new Dictionary<string, object> { ["totalParticipationTime"] = totalParticipationTime, ["activityCount"] = activityCount }
        };
    }

    private async Task<EligibilityValidationResult> ValidateSpeakerEligibilityAsync(Guid userId, int? activityId, CertificateRequirements requirements)
    {
        // Validar rol de speaker
        if (requirements.RequiredRoles != null && requirements.RequiredRoles.Length > 0)
        {
            var userRoles = await GetUserRolesAsync(userId);
            var hasRequiredRole = requirements.RequiredRoles.Any(role => userRoles.Contains(role));
            
            if (!hasRequiredRole)
            {
                return new EligibilityValidationResult
                {
                    IsEligible = false,
                    Message = "Se requiere rol de ponente/speaker",
                    ErrorCode = "REQUIRES_SPEAKER_ROLE",
                    Details = new Dictionary<string, object> { ["requiredRoles"] = requirements.RequiredRoles, ["userRoles"] = userRoles }
                };
            }
        }

        // Validar actividad específica si se proporciona
        if (activityId.HasValue)
        {
            var isSpeakerInActivity = await IsUserSpeakerInActivityAsync(userId, activityId.Value);
            if (!isSpeakerInActivity)
            {
                return new EligibilityValidationResult
                {
                    IsEligible = false,
                    Message = "El usuario no es ponente en la actividad especificada",
                    ErrorCode = "NOT_SPEAKER_IN_ACTIVITY",
                    Details = new Dictionary<string, object> { ["activityId"] = activityId.Value }
                };
            }
        }

        return new EligibilityValidationResult
        {
            IsEligible = true,
            Message = "Elegible para certificado de ponente",
            Details = new Dictionary<string, object> { ["isSpeaker"] = true }
        };
    }

    private async Task<EligibilityValidationResult> ValidateOrganizerEligibilityAsync(Guid userId, CertificateRequirements requirements)
    {
        // Validar rol de organizador
        if (requirements.RequiredRoles != null && requirements.RequiredRoles.Length > 0)
        {
            var userRoles = await GetUserRolesAsync(userId);
            var hasRequiredRole = requirements.RequiredRoles.Any(role => userRoles.Contains(role));
            
            if (!hasRequiredRole)
            {
                return new EligibilityValidationResult
                {
                    IsEligible = false,
                    Message = "Se requiere rol de organizador/staff",
                    ErrorCode = "REQUIRES_ORGANIZER_ROLE",
                    Details = new Dictionary<string, object> { ["requiredRoles"] = requirements.RequiredRoles, ["userRoles"] = userRoles }
                };
            }
        }

        // Validar participación como organizador
        var isOrganizer = await IsUserOrganizerAsync(userId);
        if (!isOrganizer)
        {
            return new EligibilityValidationResult
            {
                IsEligible = false,
                Message = "El usuario no ha participado como organizador",
                ErrorCode = "NOT_ORGANIZER"
            };
        }

        return new EligibilityValidationResult
        {
            IsEligible = true,
            Message = "Elegible para certificado de organizador",
            Details = new Dictionary<string, object> { ["isOrganizer"] = true }
        };
    }

    private async Task<EligibilityValidationResult> ValidateWinnerEligibilityAsync(Guid userId, int? activityId, CertificateRequirements requirements)
    {
        // Validar que sea ganador
        var isWinner = await IsUserWinnerAsync(userId, activityId);
        if (!isWinner)
        {
            return new EligibilityValidationResult
            {
                IsEligible = false,
                Message = "El usuario no es ganador/premiado",
                ErrorCode = "NOT_WINNER"
            };
        }

        return new EligibilityValidationResult
        {
            IsEligible = true,
            Message = "Elegible para certificado de ganador",
            Details = new Dictionary<string, object> { ["isWinner"] = true }
        };
    }

    // Métodos auxiliares para obtener datos
    private async Task<int> GetEnrollmentAttendancePercentageAsync(int enrollmentId)
    {
        // Implementar lógica real para obtener porcentaje de asistencia
        // Por ahora retornar valor simulado
        return 85; // 85% de asistencia
    }

    private async Task<int> GetEnrollmentParticipationTimeAsync(int enrollmentId)
    {
        // Implementar lógica real para obtener tiempo de participación
        // Por ahora retornar valor simulado
        return 120; // 120 minutos
    }

    private async Task<int> GetUserTotalParticipationTimeAsync(Guid userId)
    {
        // Implementar lógica real para obtener tiempo total de participación
        // Por ahora retornar valor simulado
        return 300; // 300 minutos total
    }

    private async Task<int> GetUserActivityCountAsync(Guid userId)
    {
        // Implementar lógica real para obtener número de actividades
        // Por ahora retornar valor simulado
        return 5; // 5 actividades
    }

    private async Task<string[]> GetUserRolesAsync(Guid userId)
    {
        const string sql = "SELECT role_name FROM user_roles WHERE user_id = @userId;";
        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("@userId", userId);
        
        var roles = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            roles.Add(reader.GetString(0));
        }
        
        return roles.ToArray();
    }

    private async Task<bool> IsUserSpeakerInActivityAsync(Guid userId, int activityId)
    {
        // Implementar lógica real para verificar si es speaker en actividad
        // Por ahora retornar valor simulado
        return true;
    }

    private async Task<bool> IsUserOrganizerAsync(Guid userId)
    {
        // Implementar lógica real para verificar si es organizador
        // Por ahora retornar valor simulado
        var roles = await GetUserRolesAsync(userId);
        return roles.Any(r => r.Contains("ORGANIZADOR") || r.Contains("STAFF") || r.Contains("ADMIN"));
    }

    private async Task<bool> IsUserWinnerAsync(Guid userId, int? activityId)
    {
        // Implementar lógica real para verificar si es ganador
        // Por ahora retornar valor simulado
        return true;
    }
}