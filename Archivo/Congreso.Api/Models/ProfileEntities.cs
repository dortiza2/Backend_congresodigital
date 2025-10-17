using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Congreso.Api.Models;

/// <summary>
/// Enum para roles de staff en el sistema
/// </summary>
public enum StaffRole
{
    AdminDev,    // Desarrollador/Super Admin
    Admin,       // Administrador
    Asistente    // Asistente
}

/// <summary>
/// Perfil de staff/administrador - relación 1:1 opcional con User
/// </summary>
[Table("staff_accounts")]
public class StaffAccount
{
    [Key]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("staff_role")]
    public StaffRole StaffRole { get; set; }

    [Column("display_name")]
    [MaxLength(255)]
    public string? DisplayName { get; set; }

    [Column("extra", TypeName = "jsonb")]
    public string Extra { get; set; } = "{}";

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navegación
    public User User { get; set; } = null!;

    // Propiedades auxiliares para JSON
    [NotMapped]
    public Dictionary<string, object> ExtraData
    {
        get => string.IsNullOrEmpty(Extra) 
            ? new Dictionary<string, object>() 
            : JsonSerializer.Deserialize<Dictionary<string, object>>(Extra) ?? new Dictionary<string, object>();
        set => Extra = JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// Verifica si el staff tiene permisos de administrador
    /// </summary>
    public bool IsAdmin => StaffRole == StaffRole.AdminDev || StaffRole == StaffRole.Admin;

    /// <summary>
    /// Verifica si el staff es super administrador
    /// </summary>
    public bool IsSuperAdmin => StaffRole == StaffRole.AdminDev;

    /// <summary>
    /// Obtiene la descripción del rol
    /// </summary>
    public string RoleDescription => StaffRole switch
    {
        StaffRole.AdminDev => "Desarrollador/Super Admin",
        StaffRole.Admin => "Administrador",
        StaffRole.Asistente => "Asistente",
        _ => "Desconocido"
    };
}

/// <summary>
/// Perfil de estudiante - relación 1:1 opcional con User
/// </summary>
[Table("student_accounts")]
public class StudentAccount
{
    [Key]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("carnet")]
    [MaxLength(50)]
    public string? Carnet { get; set; }

    [Column("career")]
    [MaxLength(255)]
    public string? Career { get; set; }

    [Column("cohort_year")]
    public int? CohortYear { get; set; }

    [Column("organization")]
    public string? Organization { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("is_enabled")]
    public bool IsEnabled { get; set; } = true;

    [Column("enabled_at")]
    public DateTime? EnabledAt { get; set; }

    // Navegación
    public User User { get; set; } = null!;

    /// <summary>
    /// Verifica si es estudiante UMG basado en el carnet o email
    /// </summary>
    public bool IsUmgStudent => !string.IsNullOrEmpty(Carnet) || 
                               (User?.Email?.EndsWith("@umg.edu.gt") == true);

    /// <summary>
    /// Obtiene el año académico actual o de cohorte
    /// </summary>
    public int CurrentAcademicYear => CohortYear ?? DateTime.Now.Year;
}

/// <summary>
/// Extensiones para trabajar con perfiles
/// </summary>
public static class ProfileExtensions
{
    /// <summary>
    /// Convierte StaffRole a string para compatibilidad con JWT
    /// </summary>
    public static string ToRoleString(this StaffRole role) => role switch
    {
        StaffRole.AdminDev => "adminDev",
        StaffRole.Admin => "admin", 
        StaffRole.Asistente => "asistente",
        _ => "unknown"
    };

    /// <summary>
    /// Convierte string a StaffRole
    /// </summary>
    public static StaffRole ToStaffRole(this string roleString) => roleString?.ToLower() switch
    {
        "admindev" or "mgadmin" => StaffRole.AdminDev,
        "admin" => StaffRole.Admin,
        "asistente" => StaffRole.Asistente,
        _ => StaffRole.Asistente // Default
    };

    /// <summary>
    /// Verifica si un rol tiene permisos administrativos
    /// </summary>
    public static bool HasAdminPermissions(this StaffRole role) => 
        role == StaffRole.AdminDev || role == StaffRole.Admin;
}