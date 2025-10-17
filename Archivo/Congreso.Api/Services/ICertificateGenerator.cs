using Congreso.Api.DTOs.Certificate;

namespace Congreso.Api.Services;

/// <summary>
/// Interfaz para generar certificados PDF
/// </summary>
public interface ICertificateGenerator
{
    /// <summary>
    /// Genera un certificado PDF basado en la solicitud
    /// </summary>
    /// <param name="request">Solicitud de generación</param>
    /// <param name="userData">Datos del usuario</param>\param name="context">Contexto del certificado</param>
    /// <returns>Bytes del PDF generado</returns>
    Task<byte[]> GenerateCertificatePdfAsync(GenerateCertificateRequest request, UserCertificateData userData, CertificateContext context);
    
    /// <summary>
    /// Genera un hash único para el certificado
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="certificateType">Tipo de certificado</param>
    /// <param name="timestamp">Marca de tiempo</param>
    /// <returns>Hash único</returns>
    string GenerateCertificateHash(Guid userId, CertificateType certificateType, DateTime timestamp);
    
    /// <summary>
    /// Genera un código de verificación único
    /// </summary>
    /// <returns>Código de verificación</returns>
    string GenerateVerificationCode();
    
    /// <summary>
    /// Obtiene la plantilla de certificado para el tipo especificado
    /// </summary>
    /// <param name="certificateType">Tipo de certificado</param>
    /// <returns>Plantilla de certificado</returns>
    Task<CertificateTemplate> GetTemplateAsync(CertificateType certificateType);
    
    /// <summary>
    /// Valida la integridad de un certificado generado
    /// </summary>
    /// <param name="pdfBytes">Bytes del PDF</param>
    /// <param name="expectedHash">Hash esperado</param>
    /// <returns>True si el certificado es válido</returns>
    Task<bool> ValidateCertificateIntegrityAsync(byte[] pdfBytes, string expectedHash);
}

/// <summary>
/// Datos del usuario para el certificado
/// </summary>
public class UserCertificateData
{
    /// <summary>
    /// Nombre completo del usuario
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Email del usuario
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Carnet del estudiante (si aplica)
    /// </summary>
    public string? StudentId { get; set; }
    
    /// <summary>
    /// Carrera o programa (si aplica)
    /// </summary>
    public string? Career { get; set; }
    
    /// <summary>
    /// Rol del usuario en el sistema
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// ID del usuario
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Fecha de registro del usuario
    /// </summary>
    public DateTime RegistrationDate { get; set; }
}

/// <summary>
/// Plantilla de certificado
/// </summary>
public class CertificateTemplate
{
    /// <summary>
    /// Nombre de la plantilla
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Contenido HTML de la plantilla
    /// </summary>
    public string HtmlContent { get; set; } = string.Empty;
    
    /// <summary>
    /// Estilos CSS de la plantilla
    /// </summary>
    public string CssStyles { get; set; } = string.Empty;
    
    /// <summary>
    /// Versión de la plantilla
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// Dimensiones del certificado (ancho, alto)
    /// </summary>
    public (int Width, int Height) Dimensions { get; set; }
    
    /// <summary>
    /// Indica si la plantilla está activa
    /// </summary>
    public bool IsActive { get; set; }
}