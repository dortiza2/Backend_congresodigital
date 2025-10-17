namespace Congreso.Api.DTOs.Certificate;

/// <summary>
/// Estadísticas de certificados del sistema
/// </summary>
public class CertificateStatistics
{
    /// <summary>
    /// Total de certificados generados
    /// </summary>
    public int TotalCertificates { get; set; }

    /// <summary>
    /// Certificados activos
    /// </summary>
    public int ActiveCertificates { get; set; }

    /// <summary>
    /// Certificados revocados
    /// </summary>
    public int RevokedCertificates { get; set; }

    /// <summary>
    /// Certificados expirados
    /// </summary>
    public int ExpiredCertificates { get; set; }

    /// <summary>
    /// Certificados por tipo
    /// </summary>
    public Dictionary<CertificateType, int> CertificatesByType { get; set; } = new();

    /// <summary>
    /// Certificados generados en los últimos 30 días
    /// </summary>
    public int CertificatesLast30Days { get; set; }

    /// <summary>
    /// Tasa de revocación (porcentaje)
    /// </summary>
    public double RevocationRate { get; set; }

    /// <summary>
    /// Promedio de certificados por usuario
    /// </summary>
    public double AverageCertificatesPerUser { get; set; }

    /// <summary>
    /// Usuarios con al menos un certificado
    /// </summary>
    public int UsersWithCertificates { get; set; }

    /// <summary>
    /// Fecha de generación de estadísticas
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}