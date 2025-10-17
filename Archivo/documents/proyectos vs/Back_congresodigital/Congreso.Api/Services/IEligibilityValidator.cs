using Congreso.Api.DTOs.Certificate;

namespace Congreso.Api.Services;

/// <summary>
/// Interfaz para validar la elegibilidad de usuarios para certificados
/// </summary>
public interface IEligibilityValidator
{
    /// <summary>
    /// Valida si un usuario es elegible para un tipo específico de certificado
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="certificateType">Tipo de certificado</param>
    /// <param name="activityId">ID de actividad (opcional)</param>
    /// <param name="enrollmentId">ID de inscripción (opcional)</param>
    /// <returns>Resultado de validación con detalles</returns>
    Task<EligibilityValidationResult> ValidateEligibilityAsync(Guid userId, CertificateType certificateType, int? activityId = null, int? enrollmentId = null);
    
    /// <summary>
    /// Valida si un usuario ya tiene un certificado del mismo tipo
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="certificateType">Tipo de certificado</param>
    /// <param name="activityId">ID de actividad (opcional)</param>
    /// <returns>True si ya existe un certificado similar</returns>
    Task<bool> HasExistingCertificateAsync(Guid userId, CertificateType certificateType, int? activityId = null);
    
    /// <summary>
    /// Obtiene los requisitos de elegibilidad para un tipo de certificado
    /// </summary>
    /// <param name="certificateType">Tipo de certificado</param>
    /// <returns>Detalles de requisitos</returns>
    CertificateRequirements GetRequirements(CertificateType certificateType);
}

/// <summary>
/// Resultado de validación de elegibilidad
/// </summary>
public class EligibilityValidationResult
{
    /// <summary>
    /// Indica si el usuario es elegible
    /// </summary>
    public bool IsEligible { get; set; }
    
    /// <summary>
    /// Mensaje de validación
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Detalles adicionales sobre la validación
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }
    
    /// <summary>
    /// Código de error si no es elegible
    /// </summary>
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Requisitos de certificado
/// </summary>
public class CertificateRequirements
{
    /// <summary>
    /// Requiere asistencia mínima (porcentaje)
    /// </summary>
    public int MinimumAttendancePercentage { get; set; }
    
    /// <summary>
    /// Requiere inscripción previa
    /// </summary>
    public bool RequiresEnrollment { get; set; }
    
    /// <summary>
    /// Requiere rol específico
    /// </summary>
    public string[]? RequiredRoles { get; set; }
    
    /// <summary>
    /// Requiere completar actividad específica
    /// </summary>
    public bool RequiresSpecificActivity { get; set; }
    
    /// <summary>
    /// Tiempo mínimo de participación en minutos
    /// </summary>
    public int? MinimumParticipationTimeMinutes { get; set; }
}