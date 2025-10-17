using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Congreso.Api.Models;

/// <summary>
/// Enum para tipos de actividad normalizados
/// </summary>
public enum ActivityType
{
    CHARLA,
    TALLER,
    COMPETENCIA
}

[Table("activities")]
public partial class Activity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    /// <summary>
    /// DEPRECATED: Usar ActivityType en su lugar
    /// </summary>
    [Column("type")]
    [StringLength(100)]
    [Obsolete("Use ActivityType property instead")]
    public string? Type { get; set; }

    [Column("location")]
    [StringLength(255)]
    public string? Location { get; set; }

    [Column("start_time")]
    public DateTime? StartTime { get; set; }

    [Column("end_time")]
    public DateTime? EndTime { get; set; }

    [Column("capacity")]
    public int? Capacity { get; set; }

    [Column("published")]
    public bool Published { get; set; } = false;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("requires_enrollment")]
    public bool RequiresEnrollment { get; set; } = true;

    [Column("activity_type")]
    public ActivityType ActivityType { get; set; } = ActivityType.TALLER;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public virtual ICollection<Winner> Winners { get; set; } = new List<Winner>();

    /// <summary>
    /// Obtiene el tipo de actividad como string para compatibilidad
    /// </summary>
    public string GetActivityTypeString() => ActivityType.ToString().ToLower();

    /// <summary>
    /// Obtiene la descripción del tipo de actividad
    /// </summary>
    public string GetActivityTypeDescription() => ActivityType switch
    {
        ActivityType.CHARLA => "Charla",
        ActivityType.TALLER => "Taller",
        ActivityType.COMPETENCIA => "Competencia",
        _ => "Desconocido"
    };

    /// <summary>
    /// Verifica si la actividad permite inscripciones
    /// </summary>
    public bool CanEnroll() => IsActive && Published && RequiresEnrollment;

    /// <summary>
    /// Verifica si la actividad está en curso
    /// </summary>
    public bool IsInProgress() => 
        StartTime.HasValue && EndTime.HasValue &&
        DateTime.UtcNow >= StartTime.Value && DateTime.UtcNow <= EndTime.Value;

    /// <summary>
    /// Verifica si la actividad ha terminado
    /// </summary>
    public bool HasEnded() => 
        EndTime.HasValue && DateTime.UtcNow > EndTime.Value;
}

/// <summary>
/// Extensiones para ActivityType
/// </summary>
public static class ActivityTypeExtensions
{
    /// <summary>
    /// Convierte string a ActivityType
    /// </summary>
    public static ActivityType ToActivityType(this string typeString) => typeString?.ToUpper() switch
    {
        "CHARLA" or "CONFERENCIA" or "CONFERENCE" => ActivityType.CHARLA,
        "TALLER" or "WORKSHOP" => ActivityType.TALLER,
        "COMPETENCIA" or "COMPETITION" => ActivityType.COMPETENCIA,
        _ => ActivityType.TALLER // Default
    };

    /// <summary>
    /// Obtiene el color asociado al tipo de actividad para UI
    /// </summary>
    public static string GetColor(this ActivityType type) => type switch
    {
        ActivityType.CHARLA => "#3B82F6", // Blue
        ActivityType.TALLER => "#10B981", // Green
        ActivityType.COMPETENCIA => "#F59E0B", // Amber
        _ => "#6B7280" // Gray
    };

    /// <summary>
    /// Obtiene el icono asociado al tipo de actividad
    /// </summary>
    public static string GetIcon(this ActivityType type) => type switch
    {
        ActivityType.CHARLA => "presentation",
        ActivityType.TALLER => "tools",
        ActivityType.COMPETENCIA => "trophy",
        _ => "calendar"
    };
}