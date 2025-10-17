using Congreso.Api.DTOs.Certificate;

namespace Congreso.Api.Services;

/// <summary>
/// Interfaz principal para el servicio de certificados
/// </summary>
public interface ICertificatesService
{
    /// <summary>
    /// Genera un nuevo certificado para un usuario
    /// </summary>
    /// <param name="request">Solicitud de generación</param>
    /// <param name="userId">ID del usuario que solicita (para auditoría)</param>
    /// <returns>Certificado generado</returns>
    Task<CertificateResponse> GenerateCertificateAsync(GenerateCertificateRequest request, Guid userId);
    
    /// <summary>
    /// Obtiene los certificados de un usuario específico
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="pageNumber">Número de página</param>
    /// <param name="pageSize">Tamaño de página</param>
    /// <param name="includeRevoked">Incluir certificados revocados</param>
    /// <returns>Resultado paginado de certificados</returns>
    Task<PaginatedResult<CertificateResponse>> GetUserCertificatesAsync(Guid userId, int pageNumber = 1, int pageSize = 10, bool includeRevoked = false);
    
    /// <summary>
    /// Valida un certificado por su hash
    /// </summary>
    /// <param name="hash">Hash del certificado</param>
    /// <param name="includeMetadata">Incluir metadatos completos</param>
    /// <returns>Resultado de validación</returns>
    Task<CertificateValidationResult> ValidateCertificateAsync(string hash, bool includeMetadata = true);
    
    /// <summary>
    /// Revoca un certificado específico
    /// </summary>
    /// <param name="certificateId">ID del certificado</param>
    /// <param name="reason">Razón de la revocación</param>
    /// <param name="userId">ID del usuario que revoca (para auditoría)</param>
    /// <returns>True si se revocó exitosamente</returns>
    Task<bool> RevokeCertificateAsync(long certificateId, string reason, Guid userId);
    
    /// <summary>
    /// Obtiene un certificado específico por ID
    /// </summary>
    /// <param name="certificateId">ID del certificado</param>
    /// <returns>Certificado o null si no existe</returns>
    Task<CertificateResponse?> GetCertificateByIdAsync(long certificateId);
    
    /// <summary>
    /// Reemite un certificado (genera uno nuevo con los mismos datos)
    /// </summary>
    /// <param name="originalCertificateId">ID del certificado original</param>
    /// <param name="userId">ID del usuario que solicita el reemision</param>
    /// <returns>Nuevo certificado reemitido</returns>
    Task<CertificateResponse> ReissueCertificateAsync(long originalCertificateId, Guid userId);

    /// <summary>
    /// Obtiene estadísticas agregadas de certificados
    /// </summary>
    /// <returns>Estadísticas de certificados</returns>
    Task<CertificateStatistics> GetStatisticsAsync();
}