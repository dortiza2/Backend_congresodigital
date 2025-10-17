namespace Congreso.Api.DTOs.Certificate;

/// <summary>
/// Estadísticas de auditoría de certificados
/// </summary>
public class AuditStatistics
{
    /// <summary>
    /// Total de operaciones de auditoría
    /// </summary>
    public int TotalOperations { get; set; }

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
    /// Total de certificados reemitidos
    /// </summary>
    public int TotalReissued { get; set; }

    /// <summary>
    /// Total de fallos en generación
    /// </summary>
    public int TotalFailed { get; set; }

    /// <summary>
    /// Operaciones por tipo
    /// </summary>
    public Dictionary<string, int> OperationsByType { get; set; } = new();

    /// <summary>
    /// Operaciones en los últimos 30 días
    /// </summary>
    public int OperationsLast30Days { get; set; }

    /// <summary>
    /// Tasa de éxito (porcentaje)
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Fecha de generación de estadísticas
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}