using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Congreso.Api.Models;

[Table("users")]
public partial class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("email")]
    [Required]
    public string Email { get; set; } = null!;

    [Column("password_hash")]
    public string? PasswordHash { get; set; }

    [Column("full_name")]
    public string? FullName { get; set; }

    [Column("is_umg")]
    public bool IsUmg { get; set; } = false;

    [Column("last_login")]
    public DateTime? LastLogin { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("org_id")]
    public Guid? OrgId { get; set; }

    [Column("org_name")]
    [StringLength(255)]
    public string? OrgName { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string? Status { get; set; } = "active";

    [ForeignKey("OrgId")]
    public virtual Organization? Organization { get; set; }

    // Navegaciones a perfiles (1:1 opcional)
    public virtual StaffAccount? StaffAccount { get; set; }
    public virtual StudentAccount? StudentAccount { get; set; }

    // Navegaciones existentes
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
    public virtual ICollection<Winner> Winners { get; set; } = new List<Winner>();

    /// <summary>
    /// Verifica si el usuario tiene perfil de staff
    /// </summary>
    public bool IsStaff => StaffAccount != null;

    /// <summary>
    /// Verifica si el usuario tiene perfil de estudiante
    /// </summary>
    public bool IsStudent => StudentAccount != null;

    /// <summary>
    /// Obtiene el rol de staff si existe
    /// </summary>
    public StaffRole? GetStaffRole() => StaffAccount?.StaffRole;

    /// <summary>
    /// Verifica si el usuario tiene permisos administrativos
    /// </summary>
    public bool HasAdminPermissions() => StaffAccount?.IsAdmin == true;

    /// <summary>
    /// Obtiene el nombre para mostrar (prioriza display_name de perfiles)
    /// </summary>
    public string GetDisplayName() => 
        StaffAccount?.DisplayName ?? 
        StudentAccount?.User?.FullName ?? 
        FullName;

    /// <summary>
    /// Obtiene la URL del avatar
    /// </summary>
    public string? GetAvatarUrl() => AvatarUrl;

    /// <summary>
    /// Obtiene el tipo de perfil como string
    /// </summary>
    public string GetProfileType() => IsStaff ? "staff" : IsStudent ? "student" : "none";
}