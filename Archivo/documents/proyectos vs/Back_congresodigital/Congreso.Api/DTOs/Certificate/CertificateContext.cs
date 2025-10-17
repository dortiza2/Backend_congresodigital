namespace Congreso.Api.DTOs.Certificate;

/// <summary>
/// Contexto adicional para la generación de certificados
/// </summary>
public class CertificateContext
{
    /// <summary>
    /// Nombre del evento o actividad
    /// </summary>
    public string? EventName { get; set; }
    
    /// <summary>
    /// Fecha del evento
    /// </summary>
    public DateTime? EventDate { get; set; }
    
    /// <summary>
    /// Duración en horas del evento
    /// </summary>
    public int? DurationHours { get; set; }
    
    /// <summary>
    /// Nombre del ponente o instructor
    /// </summary>
    public string? SpeakerName { get; set; }
    
    /// <summary>
    /// Título o rol del participante
    /// </summary>
    public string? ParticipantTitle { get; set; }
    
    /// <summary>
    /// Notas adicionales para el certificado
    /// </summary>
    public string? AdditionalNotes { get; set; }
    
    /// <summary>
    /// URL de la firma digital (opcional)
    /// </summary>
    public string? DigitalSignatureUrl { get; set; }
    
    /// <summary>
    /// Código de verificación único
    /// </summary>
    public string? VerificationCode { get; set; }
    
    public string? SpeakerTopic { get; set; }
    public string? OrganizerRole { get; set; }
    public string? WinnerCategory { get; set; }
    public string? WinnerPosition { get; set; }
}