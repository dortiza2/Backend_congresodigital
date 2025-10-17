namespace Congreso.Api.Models.Email;

/// <summary>
/// Datos para el email de confirmación de múltiples inscripciones
/// </summary>
public class MultipleActivitiesConfirmationEmail
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
    /// Lista de actividades confirmadas
    /// </summary>
    public List<ActivityConfirmationDetail> Activities { get; set; } = new();

    /// <summary>
    /// URL del dashboard del usuario
    /// </summary>
    public string DashboardUrl { get; set; } = string.Empty;

    /// <summary>
    /// Información adicional general
    /// </summary>
    public string? AdditionalInfo { get; set; }

    /// <summary>
    /// Fecha de confirmación
    /// </summary>
    public DateTime ConfirmationDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Detalle de una actividad confirmada
/// </summary>
public class ActivityConfirmationDetail
{
    /// <summary>
    /// ID de la actividad
    /// </summary>
    public int ActivityId { get; set; }

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
    /// Tipo de actividad (Conferencia, Taller, etc.)
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// Duración estimada de la actividad
    /// </summary>
    public string Duration { get; set; } = string.Empty;

    /// <summary>
    /// Ponente(s) de la actividad
    /// </summary>
    public string Speaker { get; set; } = string.Empty;

    /// <summary>
    /// Notas especiales para esta actividad
    /// </summary>
    public string? SpecialNotes { get; set; }

    /// <summary>
    /// Indica si requiere confirmación adicional
    /// </summary>
    public bool RequiresAdditionalConfirmation { get; set; } = false;
}

/// <summary>
/// Resumen estadístico de las inscripciones
/// </summary>
public class EnrollmentSummary
{
    /// <summary>
    /// Total de actividades inscritas
    /// </summary>
    public int TotalActivities { get; set; }

    /// <summary>
    /// Actividades por tipo
    /// </summary>
    public Dictionary<string, int> ActivitiesByType { get; set; } = new();

    /// <summary>
    /// Próxima actividad
    /// </summary>
    public ActivityConfirmationDetail? NextActivity { get; set; }

    /// <summary>
    /// Días del congreso con actividades
    /// </summary>
    public List<DateTime> CongressDays { get; set; } = new();
}