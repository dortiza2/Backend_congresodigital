using System.ComponentModel.DataAnnotations;
using Congreso.Api.Attributes;
using Congreso.Api.Models;

namespace Congreso.Api.DTOs;

/// <summary>
/// DTO para invitación de staff
/// </summary>
public class StaffInviteDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public StaffRole Role { get; set; }

    [MaxLength(255)]
    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }
}

/// <summary>
/// DTO para respuesta de invitación de staff
/// </summary>
public class StaffInviteResponseDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public StaffRole Role { get; set; }
    public string Status { get; set; } = string.Empty; // "invited", "existing"
    public string? InvitationToken { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO para gestión de usuarios
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsUmg { get; set; }
    public string? OrgName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
    public string ProfileType { get; set; } = "none"; // "staff", "student", "none"
}

/// <summary>
/// DTO para actualización de usuario
/// </summary>
public class UpdateUserDto
{
    [MaxLength(255)]
    public string? FullName { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Status { get; set; } // "active", "inactive", "suspended"

    public string[]? Roles { get; set; }
}

/// <summary>
/// DTO para creación de usuario por admin
/// </summary>
public class CreateUserDto
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

    public bool IsUmg { get; set; } = false;

    public string? OrgName { get; set; }

    public string Status { get; set; } = "active";

    public string[]? Roles { get; set; }
}

/// <summary>
/// DTO para administración de actividades
/// </summary>
public class ActivityAdminDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public ActivityType ActivityType { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int MaxCapacity { get; set; }
    public int CurrentEnrollment { get; set; }
    public bool IsPublished { get; set; }
    public bool RequiresSpeaker { get; set; }
    public string? SpeakerName { get; set; }
    public string? SpeakerBio { get; set; }
    public string? SpeakerAvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Status { get; set; } = string.Empty; // "draft", "published", "cancelled"
}

/// <summary>
/// DTO para creación de actividad
/// </summary>
public class CreateActivityAdminDto
{
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public ActivityType ActivityType { get; set; }

    public string? Description { get; set; }

    [MaxLength(255)]
    public string? Location { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [Range(1, 1000)]
    public int MaxCapacity { get; set; } = 50;

    public bool RequiresSpeaker { get; set; } = false;

    public string? SpeakerName { get; set; }

    public string? SpeakerBio { get; set; }

    public string? SpeakerAvatarUrl { get; set; }
}

/// <summary>
/// DTO para actualización de actividad
/// </summary>
public class UpdateActivityAdminDto
{
    [MaxLength(255)]
    public string? Title { get; set; }

    public ActivityType? ActivityType { get; set; }

    public string? Description { get; set; }

    [MaxLength(255)]
    public string? Location { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    [Range(1, 1000)]
    public int? MaxCapacity { get; set; }

    public bool? RequiresSpeaker { get; set; }

    public string? SpeakerName { get; set; }

    public string? SpeakerBio { get; set; }

    public string? SpeakerAvatarUrl { get; set; }

    public bool? IsPublished { get; set; }
}

/// <summary>
/// DTO para publicación de actividad
/// </summary>
public class PublishActivityDto
{
    public bool Publish { get; set; } = true;
}

/// <summary>
/// DTO para respuesta de operación genérica
/// </summary>
public class OperationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public object? Data { get; set; }
}

/// <summary>
/// DTO para listado paginado
/// </summary>
public class PagedResponseDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// DTO para invitación de staff (según especificación técnica)
/// </summary>
public class StaffInviteRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string FullName { get; set; } = string.Empty;
    
    [Required]
    public int RoleId { get; set; }
    
    public string? Department { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// DTO para respuesta de invitación de staff (según especificación técnica)
/// </summary>
public class StaffInviteResponse
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string InvitationToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// DTO para información de staff (según especificación técnica)
/// </summary>
public class StaffDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string? Department { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// DTO para actualización de rol (según especificación técnica)
/// </summary>
public class UpdateRoleRequest
{
    [Required]
    public int RoleId { get; set; }
}

/// <summary>
/// DTO para actualización de estado (según especificación técnica)
/// </summary>
public class UpdateStatusRequest
{
    [Required]
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO para actividad administrativa (según especificación técnica)
/// </summary>
public class AdminActivityDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public string? Location { get; set; }
    public int? MaxParticipants { get; set; }
    public bool RequiresRegistration { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO para creación de actividad administrativa (según especificación técnica)
/// </summary>
public class CreateActivityRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string Type { get; set; } = string.Empty;
    
    [Required]
    public DateTime ScheduledAt { get; set; }
    
    public string? Location { get; set; }
    public int? MaxParticipants { get; set; }
    public bool RequiresRegistration { get; set; }
}

/// <summary>
/// DTO para actualización de actividad administrativa (según especificación técnica)
/// </summary>
public class UpdateActivityRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? Location { get; set; }
    public int? MaxParticipants { get; set; }
    public bool? RequiresRegistration { get; set; }
    public string? Status { get; set; }
}

/// <summary>
/// DTO genérico para respuesta de operaciones (según especificación técnica)
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> CreateSuccess(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message ?? "Operación exitosa"
        };
    }

    public static ApiResponse<T> CreateError(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}

/// <summary>
/// DTO para resultados paginados (según especificación técnica)
/// </summary>
public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// DTO para creación de actividad administrativa (según especificación técnica)
/// </summary>
public class CreateAdminActivityDto
{
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string Type { get; set; } = string.Empty;
    
    [Required]
    public DateTime ScheduledAt { get; set; }
    
    public string? Location { get; set; }
    public int? MaxParticipants { get; set; }
    public bool RequiresRegistration { get; set; }
}

/// <summary>
/// DTO para actualización de actividad administrativa (según especificación técnica)
/// </summary>
public class UpdateAdminActivityDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? Location { get; set; }
    public int? MaxParticipants { get; set; }
    public bool? RequiresRegistration { get; set; }
    public string? Status { get; set; }
}

/// <summary>
/// DTO para actualización de estado de actividad administrativa
/// </summary>
public class UpdateAdminActivityStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// DTO para estadísticas de actividades administrativas
/// </summary>
public class AdminActivityStatsDto
{
    public int TotalActivities { get; set; }
    public int PublishedActivities { get; set; }
    public int DraftActivities { get; set; }
    public int UpcomingActivities { get; set; }
    public int PastActivities { get; set; }
    public int TotalParticipants { get; set; }
    public double AverageParticipantsPerActivity { get; set; }
    public Dictionary<string, int> ActivitiesByType { get; set; } = new();
    public Dictionary<string, int> ActivitiesByStatus { get; set; } = new();
}