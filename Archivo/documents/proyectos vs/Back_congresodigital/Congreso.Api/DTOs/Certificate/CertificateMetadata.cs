namespace Congreso.Api.DTOs.Certificate;

/// <summary>
/// Metadatos adicionales del certificado
/// </summary>
public class CertificateMetadata
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
    /// Duración en horas
    /// </summary>
    public int? DurationHours { get; set; }
    
    /// <summary>
    /// Nombre del instructor/ponente
    /// </summary>
    public string? InstructorName { get; set; }
    
    /// <summary>
    /// Título del participante
    /// </summary>
    public string? ParticipantTitle { get; set; }
    
    /// <summary>
    /// ID de la actividad relacionada
    /// </summary>
    public int? ActivityId { get; set; }
    
    /// <summary>
    /// ID de la inscripción relacionada
    /// </summary>
    public int? EnrollmentId { get; set; }
    
    /// <summary>
    /// Número de versión del certificado
    /// </summary>
    public int Version { get; set; } = 1;
    
    /// <summary>
    /// Información adicional en formato JSON
    /// </summary>
    public string? AdditionalData { get; set; }
}