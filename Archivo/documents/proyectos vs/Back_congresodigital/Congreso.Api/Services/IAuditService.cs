using System.ComponentModel.DataAnnotations;

namespace Congreso.Api.Services;

/// <summary>
/// Interfaz para el servicio de auditoría de certificados
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Registra la generación de un certificado
    /// </summary>
    /// <param name="certificateId">ID del certificado</param>
    /// <param name="userId">ID del usuario que generó</param>
    /// <param name="details">Detalles adicionales</param>
    Task LogCertificateGenerationAsync(long certificateId, Guid userId, Dictionary<string, object>? details = null);
    
    /// <summary>
    /// Registra la validación de un certificado
    /// </summary>
    /// <param name="certificateId">ID del certificado</param>
    /// <param name="userId">ID del usuario que validó (opcional)</param>
    /// <param name="isValid">Si la validación fue exitosa</param>
    /// <param name="details">Detalles adicionales</param>
    Task LogCertificateValidationAsync(long certificateId, Guid? userId, bool isValid, Dictionary<string, object>? details = null);
    
    /// <summary>
    /// Registra la revocación de un certificado
    /// </summary>
    /// <param name="certificateId">ID del certificado</param>
    /// <param name="userId">ID del usuario que revocó</param>
    /// <param name="reason">Razón de revocación</param>
    /// <param name="details">Detalles adicionales</param>
    Task LogCertificateRevocationAsync(long certificateId, Guid userId, string reason, Dictionary<string, object>? details = null);
    
    /// <summary>
    /// Registra el reemision de un certificado
    /// </summary>
    /// <param name="originalCertificateId">ID del certificado original</param>
    /// <param name="newCertificateId">ID del nuevo certificado</param>
    /// <param name="userId">ID del usuario que reemitió</param>
    /// <param name="reason">Razón del reemision</param>
    Task LogCertificateReissueAsync(long originalCertificateId, long newCertificateId, Guid userId, string? reason = null);
    
    /// <summary>
    /// Registra un intento fallido de generación
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="certificateType">Tipo de certificado</param>
    /// <param name="reason">Razón del fallo</param>
    /// <param name="details">Detalles adicionales</param>
    Task LogFailedGenerationAttemptAsync(Guid userId, string certificateType, string reason, Dictionary<string, object>? details = null);
    
    /// <summary>
    /// Registra una acción de podio (crear, actualizar, eliminar)
    /// </summary>
    /// <param name="userId">ID del usuario que realizó la acción</param>
    /// <param name="year">Año del podio</param>
    /// <param name="place">Lugar del podio</param>
    /// <param name="action">Tipo de acción (CREATE, UPDATE, DELETE)</param>
    /// <param name="details">Detalles adicionales</param>
    Task LogPodiumActionAsync(Guid userId, int year, int place, string action, Dictionary<string, object>? details = null);
    
    /// <summary>
    /// Registra la visualización de podio público
    /// </summary>
    /// <param name="year">Año del podio consultado</param>
    /// <param name="userAgent">User agent del solicitante</param>
    /// <param name="ipAddress">Dirección IP del solicitante</param>
    /// <param name="cacheHit">Si fue hit de caché</param>
    /// <param name="details">Detalles adicionales</param>
    Task LogPodiumViewAsync(int year, string? userAgent, string? ipAddress, bool cacheHit, Dictionary<string, object>? details = null);
    
    /// <summary>
    /// Obtiene el historial de auditoría de un certificado
    /// </summary>
    /// <param name="certificateId">ID del certificado</param>
    /// <param name="limit">Límite de registros</param>
    /// <returns>Lista de eventos de auditoría</returns>
    Task<List<AuditLogEntry>> GetCertificateAuditHistoryAsync(long certificateId, int limit = 50);
    
    /// <summary>
    /// Obtiene el historial de auditoría con filtros opcionales
    /// </summary>
    /// <param name="userId">ID de usuario (opcional)</param>
    /// <param name="certificateId">ID de certificado (opcional)</param>
    /// <param name="limit">Límite de registros</param>
    /// <returns>Lista de eventos de auditoría</returns>
    Task<List<AuditLogEntry>> GetAuditHistoryAsync(Guid? userId = null, long? certificateId = null, int limit = 100);
    
    /// <summary>
    /// Obtiene estadísticas de auditoría
    /// </summary>
    /// <param name="startDate">Fecha de inicio</param>
    /// <param name="endDate">Fecha de fin</param>
    /// <returns>Estadísticas</returns>
    Task<AuditStatistics> GetAuditStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
}

/// <summary>
/// Entrada de registro de auditoría
/// </summary>
public class AuditLogEntry
{
    /// <summary>
    /// ID del registro
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// ID del certificado relacionado
    /// </summary>
    public long? CertificateId { get; set; }
    
    /// <summary>
    /// ID del usuario que realizó la acción
    /// </summary>
    public Guid? UserId { get; set; }
    
    /// <summary>
    /// Tipo de acción
    /// </summary>
    public string ActionType { get; set; } = string.Empty;
    
    /// <summary>
    /// Descripción de la acción
    /// </summary>
    public string ActionDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Fecha y hora de la acción
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Dirección IP del usuario
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// User agent del usuario
    /// </summary>
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// Datos adicionales en JSON
    /// </summary>
    public string? AdditionalData { get; set; }
    
    /// <summary>
    /// Resultado de la acción (éxito/fracaso)
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Mensaje de error (si aplica)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Estadísticas de auditoría
/// </summary>
public class AuditStatistics
{
    /// <summary>
    /// Total de certificados generados
    /// </summary>
    public int TotalGenerated { get; set; }
    
    /// <summary>
    /// Total de certificados validados
    /// </summary>
    public int TotalValidated { get; set; }
    
    /// <summary>
    /// Total de certificados revocados
    /// </summary>
    public int TotalRevoked { get; set; }
    
    /// <summary>
    /// Total de reemisiones
    /// </summary>
    public int TotalReissued { get; set; }
    
    /// <summary>
    /// Total de intentos fallidos
    /// </summary>
    public int TotalFailedAttempts { get; set; }
    
    /// <summary>
    /// Validaciones exitosas
    /// </summary>
    public int SuccessfulValidations { get; set; }
    
    /// <summary>
    /// Validaciones fallidas
    /// </summary>
    public int FailedValidations { get; set; }
    
    /// <summary>
    /// Período de las estadísticas
    /// </summary>
    public DateTime StatisticsPeriodStart { get; set; }
    
    /// <summary>
    /// Fecha de generación de estadísticas
    /// </summary>
    public DateTime StatisticsGeneratedAt { get; set; }
}