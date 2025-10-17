namespace Congreso.Api.DTOs.Certificate;

/// <summary>
/// Estados de los certificados en el sistema
/// </summary>
public enum CertificateStatus
{
    /// <summary>
    /// Certificado activo y válido
    /// </summary>
    Active = 1,
    
    /// <summary>
    /// Certificado revocado
    /// </summary>
    Revoked = 2,
    
    /// <summary>
    /// Certificado expirado
    /// </summary>
    Expired = 3,
    
    /// <summary>
    /// Certificado en proceso de generación
    /// </summary>
    Processing = 4,
    
    /// <summary>
    /// Error en la generación del certificado
    /// </summary>
    Failed = 5
}