using System.ComponentModel.DataAnnotations;

namespace Congreso.Api.DTOs.Certificate;

/// <summary>
/// Respuesta de certificado generado
/// </summary>
public class CertificateResponse
{
    /// <summary>
    /// ID único del certificado
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// Hash único del certificado para validación
    /// </summary>
    [Required]
    public string Hash { get; set; } = string.Empty;
    
    /// <summary>
    /// Código de verificación del certificado
    /// </summary>
    [Required]
    public string VerificationCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Tipo de certificado
    /// </summary>
    [Required]
    public CertificateType Type { get; set; }
    
    /// <summary>
    /// Estado del certificado
    /// </summary>
    [Required]
    public CertificateStatus Status { get; set; }
    
    /// <summary>
    /// ID del usuario propietario
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// URL para descargar el certificado en PDF
    /// </summary>
    [Required]
    public string DownloadUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// URL para validar el certificado
    /// </summary>
    [Required]
    public string ValidationUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Fecha de emisión del certificado
    /// </summary>
    [Required]
    public DateTime IssuedAt { get; set; }
    
    /// <summary>
    /// Fecha de expiración del certificado (opcional)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Metadatos adicionales del certificado
    /// </summary>
    public CertificateMetadata? Metadata { get; set; }
}