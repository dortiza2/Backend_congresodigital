namespace Congreso.Api.Models.Email;

/// <summary>
/// Solicitud base para envío de email
/// </summary>
public class EmailRequest
{
    /// <summary>
    /// Email del destinatario
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del destinatario
    /// </summary>
    public string ToName { get; set; } = string.Empty;

    /// <summary>
    /// Asunto del email
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Contenido HTML del email
    /// </summary>
    public string HtmlContent { get; set; } = string.Empty;

    /// <summary>
    /// Contenido de texto plano del email (opcional)
    /// </summary>
    public string? TextContent { get; set; }

    /// <summary>
    /// Archivos adjuntos (opcional)
    /// </summary>
    public List<EmailAttachment>? Attachments { get; set; }
}

/// <summary>
/// Archivo adjunto para email
/// </summary>
public class EmailAttachment
{
    /// <summary>
    /// Nombre del archivo
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Contenido del archivo en bytes
    /// </summary>
    public byte[] Content { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Tipo MIME del archivo
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";
}