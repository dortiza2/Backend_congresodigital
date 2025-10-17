namespace Congreso.Api.DTOs.Certificate;

/// <summary>
/// Resultado de la validación de un certificado
/// </summary>
public class CertificateValidationResult
{
    /// <summary>
    /// Indica si el certificado es válido
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// Mensaje de validación
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// ID del certificado (si es válido)
    /// </summary>
    public long? CertificateId { get; set; }
    
    /// <summary>
    /// Tipo de certificado (si es válido)
    /// </summary>
    public CertificateType? Type { get; set; }
    
    /// <summary>
    /// ID del usuario propietario (si es válido)
    /// </summary>
    public Guid? UserId { get; set; }
    
    /// <summary>
    /// Fecha de emisión (si es válido)
    /// </summary>
    public DateTime? IssuedAt { get; set; }
    
    /// <summary>
    /// Fecha de expiración (si aplica)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Estado actual del certificado
    /// </summary>
    public CertificateStatus? Status { get; set; }
    
    /// <summary>
    /// Metadatos del certificado (si es válido)
    /// </summary>
    public CertificateMetadata? Metadata { get; set; }
    
    /// <summary>
    /// Detalles adicionales sobre la validación
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }
}