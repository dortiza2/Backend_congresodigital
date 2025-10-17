using Congreso.Api.DTOs.Certificate;

namespace Congreso.Api.Services;

/// <summary>
/// Interfaz para el repositorio de certificados
/// </summary>
public interface ICertificateRepository
{
    /// <summary>
    /// Crea un nuevo certificado en la base de datos
    /// </summary>
    /// <param name="certificate">Datos del certificado</param>
    /// <returns>Certificado creado con ID</returns>
    Task<CertificateRecord> CreateCertificateAsync(CertificateRecord certificate);
    
    /// <summary>
    /// Obtiene un certificado por su ID
    /// </summary>
    /// <param name="certificateId">ID del certificado</param>
    /// <returns>Certificado o null</returns>
    Task<CertificateRecord?> GetCertificateByIdAsync(long certificateId);
    
    /// <summary>
    /// Obtiene un certificado por su hash
    /// </summary>
    /// <param name="hash">Hash del certificado</param>
    /// <returns>Certificado o null</returns>
    Task<CertificateRecord?> GetCertificateByHashAsync(string hash);
    
    /// <summary>
    /// Obtiene los certificados de un usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="includeRevoked">Incluir revocados</param>
    /// <param name="pageNumber">Número de página</param>
    /// <param name="pageSize">Tamaño de página</param>
    /// <returns>Lista de certificados</returns>
    Task<List<CertificateRecord>> GetUserCertificatesAsync(Guid userId, bool includeRevoked = false, int pageNumber = 1, int pageSize = 10);
    
    /// <summary>
    /// Cuenta los certificados de un usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="includeRevoked">Incluir revocados</param>
    /// <returns>Número de certificados</returns>
    Task<int> CountUserCertificatesAsync(Guid userId, bool includeRevoked = false);
    
    /// <summary>
    /// Actualiza el estado de un certificado
    /// </summary>
    /// <param name="certificateId">ID del certificado</param>
    /// <param name="status">Nuevo estado</param>
    /// <param name="reason">Razón del cambio</param>
    /// <returns>True si se actualizó</returns>
    Task<bool> UpdateCertificateStatusAsync(long certificateId, CertificateStatus status, string? reason = null);
    
    /// <summary>
    /// Verifica si existe un certificado similar
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="certificateType">Tipo de certificado</param>
    /// <param name="activityId">ID de actividad (opcional)</param>
    /// <returns>True si existe</returns>
    Task<bool> HasExistingCertificateAsync(Guid userId, CertificateType certificateType, int? activityId = null);
    
    /// <summary>
    /// Obtiene certificados por tipo y estado
    /// </summary>
    /// <param name="certificateType">Tipo de certificado</param>
    /// <param name="status">Estado</param>
    /// <param name="pageNumber">Número de página</param>
    /// <param name="pageSize">Tamaño de página</param>
    /// <returns>Lista de certificados</returns>
    Task<List<CertificateRecord>> GetCertificatesByTypeAndStatusAsync(CertificateType certificateType, CertificateStatus? status = null, int pageNumber = 1, int pageSize = 10);
}

/// <summary>
/// Registro de certificado en base de datos
/// </summary>
public class CertificateRecord
{
    /// <summary>
    /// ID único del certificado
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// Hash único del certificado
    /// </summary>
    public string Hash { get; set; } = string.Empty;
    
    /// <summary>
    /// Código de verificación
    /// </summary>
    public string VerificationCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Tipo de certificado
    /// </summary>
    public CertificateType Type { get; set; }
    
    /// <summary>
    /// Estado del certificado
    /// </summary>
    public CertificateStatus Status { get; set; }
    
    /// <summary>
    /// ID del usuario propietario
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// URL de descarga del certificado
    /// </summary>
    public string? DownloadUrl { get; set; }
    
    /// <summary>
    /// URL de validación del certificado
    /// </summary>
    public string? ValidationUrl { get; set; }
    
    /// <summary>
    /// Fecha de emisión
    /// </summary>
    public DateTime IssuedAt { get; set; }
    
    /// <summary>
    /// Fecha de expiración (opcional)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// ID de actividad relacionada (opcional)
    /// </summary>
    public int? ActivityId { get; set; }
    
    /// <summary>
    /// ID de inscripción relacionada (opcional)
    /// </summary>
    public int? EnrollmentId { get; set; }
    
    /// <summary>
    /// Metadatos adicionales en JSON
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Fecha de creación
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Fecha de última actualización
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Razón de revocación (si aplica)
    /// </summary>
    public string? RevocationReason { get; set; }
    
    /// <summary>
    /// Fecha de revocación (si aplica)
    /// </summary>
    public DateTime? RevokedAt { get; set; }
}