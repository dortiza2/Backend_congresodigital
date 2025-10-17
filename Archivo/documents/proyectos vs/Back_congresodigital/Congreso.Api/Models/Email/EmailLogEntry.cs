namespace Congreso.Api.Models.Email;

/// <summary>
/// Entrada de log estructurada para el sistema de emails
/// </summary>
public class EmailLogEntry
{
    /// <summary>
    /// ID único del intento de envío
    /// </summary>
    public string EmailId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Tipo de email enviado
    /// </summary>
    public EmailType EmailType { get; set; }

    /// <summary>
    /// Email del destinatario
    /// </summary>
    public string RecipientEmail { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del destinatario
    /// </summary>
    public string RecipientName { get; set; } = string.Empty;

    /// <summary>
    /// Asunto del email
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Estado del envío
    /// </summary>
    public EmailStatus Status { get; set; }

    /// <summary>
    /// Número de intentos realizados
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Tiempo total de procesamiento en milisegundos
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Mensaje de error si falló
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Detalles adicionales del error
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// Timestamp del primer intento
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp de finalización (éxito o fallo final)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Configuración SMTP utilizada (sin credenciales)
    /// </summary>
    public EmailServerInfo ServerInfo { get; set; } = new();

    /// <summary>
    /// Información adicional específica del tipo de email
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Tipos de email del sistema
/// </summary>
public enum EmailType
{
    WelcomeEmail,
    SingleActivityConfirmation,
    MultipleActivityConfirmation,
    ActivityReminder,
    TestEmail,
    PasswordReset,
    Other
}

/// <summary>
/// Estados posibles del envío de email
/// </summary>
public enum EmailStatus
{
    Pending,
    Sending,
    Sent,
    Failed,
    Retrying
}

/// <summary>
/// Información del servidor SMTP (sin credenciales sensibles)
/// </summary>
public class EmailServerInfo
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool EnableSsl { get; set; }
    public int TimeoutSeconds { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}