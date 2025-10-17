namespace Congreso.Api.Models.Configuration;

/// <summary>
/// Configuración para el servicio de email SMTP
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// Servidor SMTP (ej: smtp.gmail.com)
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Puerto del servidor SMTP (ej: 587 para TLS, 465 para SSL)
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Usuario/email para autenticación SMTP
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña para autenticación SMTP
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Email del remitente
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del remitente
    /// </summary>
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// Habilitar SSL/TLS
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Timeout en segundos para conexiones SMTP
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Número máximo de reintentos en caso de fallo
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Validar que la configuración esté completa
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Host) &&
               Port > 0 &&
               !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password) &&
               !string.IsNullOrWhiteSpace(FromEmail) &&
               !string.IsNullOrWhiteSpace(FromName);
    }
}