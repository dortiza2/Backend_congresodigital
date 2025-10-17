namespace Congreso.Api.Models.Email;

/// <summary>
/// Datos para el email de confirmación de inscripción
/// </summary>
public class EnrollmentConfirmationEmail
{
    /// <summary>
    /// Nombre completo del estudiante
    /// </summary>
    public string StudentName { get; set; } = string.Empty;

    /// <summary>
    /// Email del estudiante
    /// </summary>
    public string StudentEmail { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la actividad
    /// </summary>
    public string ActivityName { get; set; } = string.Empty;

    /// <summary>
    /// Descripción de la actividad
    /// </summary>
    public string ActivityDescription { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora de la actividad
    /// </summary>
    public DateTime ActivityDateTime { get; set; }

    /// <summary>
    /// Ubicación/sede de la actividad
    /// </summary>
    public string ActivityLocation { get; set; } = string.Empty;

    /// <summary>
    /// Token/código QR para la asistencia
    /// </summary>
    public string QrToken { get; set; } = string.Empty;

    /// <summary>
    /// URL para generar el código QR
    /// </summary>
    public string QrCodeUrl { get; set; } = string.Empty;

    /// <summary>
    /// ID de la inscripción
    /// </summary>
    public int EnrollmentId { get; set; }

    /// <summary>
    /// Información adicional o instrucciones
    /// </summary>
    public string? AdditionalInfo { get; set; }
}

/// <summary>
/// Datos para el email de recordatorio de actividad
/// </summary>
public class ActivityReminderEmail
{
    /// <summary>
    /// Nombre completo del estudiante
    /// </summary>
    public string StudentName { get; set; } = string.Empty;

    /// <summary>
    /// Email del estudiante
    /// </summary>
    public string StudentEmail { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la actividad
    /// </summary>
    public string ActivityName { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora de la actividad
    /// </summary>
    public DateTime ActivityDateTime { get; set; }

    /// <summary>
    /// Ubicación/sede de la actividad
    /// </summary>
    public string ActivityLocation { get; set; } = string.Empty;

    /// <summary>
    /// Token/código QR para la asistencia
    /// </summary>
    public string QrToken { get; set; } = string.Empty;

    /// <summary>
    /// URL para generar el código QR
    /// </summary>
    public string QrCodeUrl { get; set; } = string.Empty;

    /// <summary>
    /// Tiempo restante hasta la actividad (ej: "2 horas")
    /// </summary>
    public string TimeUntilActivity { get; set; } = string.Empty;

    /// <summary>
    /// Notas especiales de ingreso o preparación
    /// </summary>
    public string? EntryNotes { get; set; }
}