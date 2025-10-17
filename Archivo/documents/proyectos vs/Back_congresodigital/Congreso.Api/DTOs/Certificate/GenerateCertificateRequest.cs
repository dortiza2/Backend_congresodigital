using System.ComponentModel.DataAnnotations;

namespace Congreso.Api.DTOs.Certificate;

/// <summary>
/// Solicitud para generar un nuevo certificado
/// </summary>
public class GenerateCertificateRequest
{
    /// <summary>
    /// ID del usuario para el que se genera el certificado
    /// </summary>
    [Required]
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Tipo de certificado a generar
    /// </summary>
    [Required]
    public CertificateType Type { get; set; }
    
    /// <summary>
    /// ID de la actividad relacionada (opcional, dependiendo del tipo)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "El ID de actividad debe ser mayor a 0")]
    public int? ActivityId { get; set; }
    
    /// <summary>
    /// Contexto adicional para la generación del certificado
    /// </summary>
    public CertificateContext? Context { get; set; }
    
    /// <summary>
    /// ID de la inscripción relacionada (para certificados de asistencia)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "El ID de inscripción debe ser mayor a 0")]
    public int? EnrollmentId { get; set; }
}