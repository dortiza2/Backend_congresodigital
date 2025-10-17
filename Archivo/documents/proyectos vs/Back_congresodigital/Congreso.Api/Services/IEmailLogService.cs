using Congreso.Api.Models.Email;

namespace Congreso.Api.Services;

/// <summary>
/// Servicio para logging avanzado del sistema de emails
/// </summary>
public interface IEmailLogService
{
    /// <summary>
    /// Inicia el tracking de un nuevo envío de email
    /// </summary>
    /// <param name="emailType">Tipo de email</param>
    /// <param name="recipientEmail">Email del destinatario</param>
    /// <param name="recipientName">Nombre del destinatario</param>
    /// <param name="subject">Asunto del email</param>
    /// <param name="metadata">Información adicional</param>
    /// <returns>ID único del email para tracking</returns>
    string StartEmailTracking(EmailType emailType, string recipientEmail, string recipientName, 
        string subject, Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Registra un intento de envío
    /// </summary>
    /// <param name="emailId">ID del email</param>
    /// <param name="attemptNumber">Número del intento</param>
    void LogAttempt(string emailId, int attemptNumber);

    /// <summary>
    /// Registra un envío exitoso
    /// </summary>
    /// <param name="emailId">ID del email</param>
    /// <param name="processingTimeMs">Tiempo de procesamiento</param>
    void LogSuccess(string emailId, long processingTimeMs);

    /// <summary>
    /// Registra un error en el envío
    /// </summary>
    /// <param name="emailId">ID del email</param>
    /// <param name="attemptNumber">Número del intento</param>
    /// <param name="error">Excepción ocurrida</param>
    /// <param name="isRetrying">Si se va a reintentar</param>
    void LogError(string emailId, int attemptNumber, Exception error, bool isRetrying);

    /// <summary>
    /// Registra un fallo final (después de todos los reintentos)
    /// </summary>
    /// <param name="emailId">ID del email</param>
    /// <param name="totalAttempts">Total de intentos realizados</param>
    /// <param name="finalError">Error final</param>
    void LogFinalFailure(string emailId, int totalAttempts, Exception finalError);

    /// <summary>
    /// Obtiene métricas de envío de emails
    /// </summary>
    /// <param name="fromDate">Fecha desde</param>
    /// <param name="toDate">Fecha hasta</param>
    /// <returns>Métricas del período</returns>
    Task<EmailMetrics> GetEmailMetricsAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Obtiene el historial de un email específico
    /// </summary>
    /// <param name="emailId">ID del email</param>
    /// <returns>Entrada de log completa</returns>
    Task<EmailLogEntry?> GetEmailLogAsync(string emailId);

    /// <summary>
    /// Obtiene emails fallidos para análisis
    /// </summary>
    /// <param name="fromDate">Fecha desde</param>
    /// <param name="limit">Límite de resultados</param>
    /// <returns>Lista de emails fallidos</returns>
    Task<List<EmailLogEntry>> GetFailedEmailsAsync(DateTime fromDate, int limit = 100);
}

/// <summary>
/// Métricas del sistema de emails
/// </summary>
public class EmailMetrics
{
    public int TotalEmails { get; set; }
    public int SuccessfulEmails { get; set; }
    public int FailedEmails { get; set; }
    public double SuccessRate => TotalEmails > 0 ? (double)SuccessfulEmails / TotalEmails * 100 : 0;
    public double AverageProcessingTimeMs { get; set; }
    public Dictionary<EmailType, int> EmailsByType { get; set; } = new();
    public Dictionary<string, int> ErrorsByType { get; set; } = new();
    public int TotalRetries { get; set; }
}