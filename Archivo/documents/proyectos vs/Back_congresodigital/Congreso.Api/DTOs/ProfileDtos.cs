using System.ComponentModel.DataAnnotations;
using Congreso.Api.Models;
using Congreso.Api.Attributes;

namespace Congreso.Api.DTOs;

/// <summary>
/// DTO para crear perfil de staff
/// </summary>
public class CreateStaffAccountDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public StaffRole StaffRole { get; set; }

    [MaxLength(255)]
    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }

    public Dictionary<string, object>? ExtraData { get; set; }
}

/// <summary>
/// DTO para actualizar perfil de staff
/// </summary>
public class UpdateStaffAccountDto
{
    public StaffRole? StaffRole { get; set; }

    [MaxLength(255)]
    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }

    public Dictionary<string, object>? ExtraData { get; set; }
}

/// <summary>
/// DTO para respuesta de perfil de staff
/// </summary>
public class StaffAccountDto
{
    public Guid UserId { get; set; }
    public StaffRole StaffRole { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public Dictionary<string, object> ExtraData { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Propiedades calculadas
    public string RoleDescription { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsSuperAdmin { get; set; }

    // Información del usuario asociado
    public UserBasicDto? User { get; set; }
}

/// <summary>
/// DTO para crear perfil de estudiante
/// </summary>
public class CreateStudentAccountDto
{
    [Required]
    public Guid UserId { get; set; }

    [MaxLength(50)]
    public string? Carnet { get; set; }

    [MaxLength(255)]
    public string? Career { get; set; }

    public int? CohortYear { get; set; }

    public Dictionary<string, object>? ExtraData { get; set; }
}

/// <summary>
/// DTO para actualizar perfil de estudiante
/// </summary>
public class UpdateStudentAccountDto
{
    [MaxLength(50)]
    public string? Carnet { get; set; }

    [MaxLength(255)]
    public string? Career { get; set; }

    public int? CohortYear { get; set; }

    public Dictionary<string, object>? ExtraData { get; set; }
}

/// <summary>
/// DTO para respuesta de perfil de estudiante
/// </summary>
public class StudentAccountDto
{
    public Guid UserId { get; set; }
    public string? Carnet { get; set; }
    public string? Career { get; set; }
    public int? CohortYear { get; set; }
    public Dictionary<string, object> ExtraData { get; set; } = new();

    // Propiedades calculadas
    public bool IsUmgStudent { get; set; }
    public int CurrentAcademicYear { get; set; }

    // Información del usuario asociado
    public UserBasicDto? User { get; set; }
}

/// <summary>
/// DTO básico de usuario para incluir en perfiles
/// </summary>
public class UserBasicDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsUmg { get; set; }
    public string? OrgName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// DTO extendido de usuario con información de perfiles
/// </summary>
public class UserWithProfileDto : UserBasicDto
{
    public string ProfileType { get; set; } = "none"; // "staff", "student", "none"
    public StaffAccountDto? StaffProfile { get; set; }
    public StudentAccountDto? StudentProfile { get; set; }

    // Propiedades calculadas
    public string DisplayName { get; set; } = string.Empty;
    public string? EffectiveAvatarUrl { get; set; }
    public bool HasAdminPermissions { get; set; }
    public StaffRole? StaffRole { get; set; }
}

/// <summary>
/// DTO para crear usuario con perfil
/// </summary>
public class CreateUserWithProfileDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StrongPassword]
    public string Password { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    [Required]
    public string ProfileType { get; set; } = string.Empty; // "staff" or "student"

    // Para perfil de staff
    public StaffRole? StaffRole { get; set; }
    public string? DisplayName { get; set; }

    // Para perfil de estudiante
    public string? Carnet { get; set; }
    public string? Career { get; set; }
    public int? CohortYear { get; set; }

    public Dictionary<string, object>? ExtraData { get; set; }
}

/// <summary>
/// DTO para verificación QR con datos básicos del usuario
/// </summary>
public class QrUserVerificationDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string ProfileType { get; set; } = string.Empty; // "staff" | "student"
    public string? StaffRole { get; set; } // Solo si es staff
    public string? Carnet { get; set; } // Solo si es estudiante
    public string? Career { get; set; } // Solo si es estudiante
}

/// <summary>
/// DTO para solicitud de verificación QR
/// </summary>
public class QrVerificationDto
{
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public Guid ActivityId { get; set; }
    
    public string? QrCodeId { get; set; }
}

/// <summary>
/// DTO para respuesta de verificación QR
/// </summary>
public class QrVerificationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? EnrollmentId { get; set; }
    public bool WasAlreadyAttended { get; set; }
    public DateTime? AttendanceTime { get; set; }
    public UserBasicDto? User { get; set; }
    public ActivityBasicDto? Activity { get; set; }
}

/// <summary>
/// DTO básico de actividad para respuestas
/// </summary>
public class ActivityBasicDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public ActivityType ActivityType { get; set; }
    public string? Location { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}