using System.Collections.Concurrent;
using System.Diagnostics;
using Congreso.Api.Models.Configuration;
using Congreso.Api.Models.Email;
using Microsoft.Extensions.Options;

namespace Congreso.Api.Services;

/// <summary>
/// Implementación del servicio de logging avanzado para emails
/// </summary>
public class EmailLogService : IEmailLogService
{
    private readonly ILogger<EmailLogService> _logger;
    private readonly EmailSettings _emailSettings;
    private readonly ConcurrentDictionary<string, EmailLogEntry> _emailLogs = new();
    private readonly ConcurrentDictionary<string, Stopwatch> _emailTimers = new();

    public EmailLogService(ILogger<EmailLogService> logger, IOptions<EmailSettings> emailSettings)
    {
        _logger = logger;
        _emailSettings = emailSettings.Value;
    }

    public string StartEmailTracking(EmailType emailType, string recipientEmail, string recipientName, 
        string subject, Dictionary<string, object>? metadata = null)
    {
        var emailId = Guid.NewGuid().ToString();
        var logEntry = new EmailLogEntry
        {
            EmailId = emailId,
            EmailType = emailType,
            RecipientEmail = recipientEmail,
            RecipientName = recipientName,
            Subject = subject,
            Status = EmailStatus.Pending,
            StartedAt = DateTime.UtcNow,
            ServerInfo = new EmailServerInfo
            {
                Host = _emailSettings.Host,
                Port = _emailSettings.Port,
                EnableSsl = _emailSettings.EnableSsl,
                TimeoutSeconds = _emailSettings.TimeoutSeconds,
                FromEmail = _emailSettings.FromEmail,
                FromName = _emailSettings.FromName
            },
            Metadata = metadata ?? new Dictionary<string, object>()
        };

        _emailLogs.TryAdd(emailId, logEntry);
        _emailTimers.TryAdd(emailId, Stopwatch.StartNew());

        _logger.LogInformation("📧 [EMAIL-START] ID: {EmailId} | Tipo: {EmailType} | Para: {RecipientEmail} | Asunto: {Subject}",
            emailId, emailType, recipientEmail, subject);

        return emailId;
    }

    public void LogAttempt(string emailId, int attemptNumber)
    {
        if (_emailLogs.TryGetValue(emailId, out var logEntry))
        {
            logEntry.AttemptCount = attemptNumber;
            logEntry.Status = EmailStatus.Sending;

            _logger.LogInformation("🔄 [EMAIL-ATTEMPT] ID: {EmailId} | Intento: {AttemptNumber} | Para: {RecipientEmail}",
                emailId, attemptNumber, logEntry.RecipientEmail);
        }
    }

    public void LogSuccess(string emailId, long processingTimeMs)
    {
        if (_emailLogs.TryGetValue(emailId, out var logEntry))
        {
            logEntry.Status = EmailStatus.Sent;
            logEntry.ProcessingTimeMs = processingTimeMs;
            logEntry.CompletedAt = DateTime.UtcNow;

            _emailTimers.TryRemove(emailId, out var timer);
            timer?.Stop();

            _logger.LogInformation("✅ [EMAIL-SUCCESS] ID: {EmailId} | Para: {RecipientEmail} | Tiempo: {ProcessingTimeMs}ms | Intentos: {AttemptCount}",
                emailId, logEntry.RecipientEmail, processingTimeMs, logEntry.AttemptCount);

            // Log estructurado para métricas
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["EmailId"] = emailId,
                ["EmailType"] = logEntry.EmailType.ToString(),
                ["RecipientEmail"] = logEntry.RecipientEmail,
                ["ProcessingTimeMs"] = processingTimeMs,
                ["AttemptCount"] = logEntry.AttemptCount,
                ["Status"] = "Success"
            }))
            {
                _logger.LogInformation("Email enviado exitosamente");
            }
        }
    }

    public void LogError(string emailId, int attemptNumber, Exception error, bool isRetrying)
    {
        if (_emailLogs.TryGetValue(emailId, out var logEntry))
        {
            logEntry.AttemptCount = attemptNumber;
            logEntry.Status = isRetrying ? EmailStatus.Retrying : EmailStatus.Failed;
            logEntry.ErrorMessage = error.Message;
            logEntry.ErrorDetails = error.ToString();

            var logLevel = isRetrying ? LogLevel.Warning : LogLevel.Error;
            var icon = isRetrying ? "⚠️" : "❌";
            var action = isRetrying ? "RETRY" : "ERROR";

            _logger.Log(logLevel, error, 
                "{Icon} [EMAIL-{Action}] ID: {EmailId} | Para: {RecipientEmail} | Intento: {AttemptNumber} | Error: {ErrorMessage}",
                icon, action, emailId, logEntry.RecipientEmail, attemptNumber, error.Message);

            // Log estructurado para análisis de errores
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["EmailId"] = emailId,
                ["EmailType"] = logEntry.EmailType.ToString(),
                ["RecipientEmail"] = logEntry.RecipientEmail,
                ["AttemptNumber"] = attemptNumber,
                ["ErrorType"] = error.GetType().Name,
                ["ErrorMessage"] = error.Message,
                ["IsRetrying"] = isRetrying,
                ["Status"] = isRetrying ? "Retrying" : "Failed"
            }))
            {
                _logger.Log(logLevel, error, "Error en envío de email");
            }
        }
    }

    public void LogFinalFailure(string emailId, int totalAttempts, Exception finalError)
    {
        if (_emailLogs.TryGetValue(emailId, out var logEntry))
        {
            logEntry.Status = EmailStatus.Failed;
            logEntry.AttemptCount = totalAttempts;
            logEntry.ErrorMessage = finalError.Message;
            logEntry.ErrorDetails = finalError.ToString();
            logEntry.CompletedAt = DateTime.UtcNow;

            _emailTimers.TryRemove(emailId, out var timer);
            var totalTime = timer?.ElapsedMilliseconds ?? 0;
            logEntry.ProcessingTimeMs = totalTime;
            timer?.Stop();

            _logger.LogError(finalError,
                "💥 [EMAIL-FAILED] ID: {EmailId} | Para: {RecipientEmail} | Intentos totales: {TotalAttempts} | Tiempo total: {TotalTimeMs}ms | Error final: {ErrorMessage}",
                emailId, logEntry.RecipientEmail, totalAttempts, totalTime, finalError.Message);

            // Log estructurado para análisis de fallos
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["EmailId"] = emailId,
                ["EmailType"] = logEntry.EmailType.ToString(),
                ["RecipientEmail"] = logEntry.RecipientEmail,
                ["TotalAttempts"] = totalAttempts,
                ["TotalProcessingTimeMs"] = totalTime,
                ["FinalErrorType"] = finalError.GetType().Name,
                ["FinalErrorMessage"] = finalError.Message,
                ["Status"] = "FinalFailure"
            }))
            {
                _logger.LogError(finalError, "Fallo final en envío de email después de todos los reintentos");
            }
        }
    }

    public async Task<EmailMetrics> GetEmailMetricsAsync(DateTime fromDate, DateTime toDate)
    {
        await Task.CompletedTask; // Para mantener la interfaz async

        var logsInRange = _emailLogs.Values
            .Where(log => log.StartedAt >= fromDate && log.StartedAt <= toDate)
            .ToList();

        var metrics = new EmailMetrics
        {
            TotalEmails = logsInRange.Count,
            SuccessfulEmails = logsInRange.Count(log => log.Status == EmailStatus.Sent),
            FailedEmails = logsInRange.Count(log => log.Status == EmailStatus.Failed),
            AverageProcessingTimeMs = logsInRange.Where(log => log.ProcessingTimeMs > 0)
                .Select(log => (double)log.ProcessingTimeMs)
                .DefaultIfEmpty(0)
                .Average(),
            TotalRetries = logsInRange.Sum(log => Math.Max(0, log.AttemptCount - 1))
        };

        // Emails por tipo
        metrics.EmailsByType = logsInRange
            .GroupBy(log => log.EmailType)
            .ToDictionary(g => g.Key, g => g.Count());

        // Errores por tipo
        metrics.ErrorsByType = logsInRange
            .Where(log => !string.IsNullOrEmpty(log.ErrorMessage))
            .GroupBy(log => log.ErrorMessage?.Split(':')[0] ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count());

        return metrics;
    }

    public async Task<EmailLogEntry?> GetEmailLogAsync(string emailId)
    {
        await Task.CompletedTask; // Para mantener la interfaz async
        _emailLogs.TryGetValue(emailId, out var logEntry);
        return logEntry;
    }

    public async Task<List<EmailLogEntry>> GetFailedEmailsAsync(DateTime fromDate, int limit = 100)
    {
        await Task.CompletedTask; // Para mantener la interfaz async

        return _emailLogs.Values
            .Where(log => log.Status == EmailStatus.Failed && log.StartedAt >= fromDate)
            .OrderByDescending(log => log.StartedAt)
            .Take(limit)
            .ToList();
    }
}