namespace Congreso.Api.DTOs.Certificate;

/// <summary>
/// Tipos de certificados disponibles en el sistema
/// </summary>
public enum CertificateType
{
    /// <summary>
    /// Certificado de asistencia a actividades
    /// </summary>
    Attendance = 1,
    
    /// <summary>
    /// Certificado de participación en congreso
    /// </summary>
    Participation = 2,
    
    /// <summary>
    /// Certificado de ponente/presentador
    /// </summary>
    Speaker = 3,
    
    /// <summary>
    /// Certificado de organizador/staff
    /// </summary>
    Organizer = 4,
    
    /// <summary>
    /// Certificado de ganador/premiado
    /// </summary>
    Winner = 5
}