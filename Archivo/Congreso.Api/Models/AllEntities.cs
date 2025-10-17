using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Congreso.Api.Models;

/// <summary>
/// Modelo de invitación de usuario
/// </summary>
[Table("user_invitations")]
[Index(nameof(Token), IsUnique = true)]
public class UserInvitation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("token")]
    public string Token { get; set; } = string.Empty;

    [Required]
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("accepted_at")]
    public DateTime? AcceptedAt { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}

[Table("roles")]
public partial class Role
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("code")]
    [StringLength(50)]
    public string Code { get; set; } = null!;

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("label")]
    [StringLength(100)]
    public string Label { get; set; } = null!;

    [Column("level")]
    public int? Level { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

[Table("user_roles")]
public partial class UserRole
{
    [Key, Column("user_id", Order = 0)]
    public Guid UserId { get; set; }

    [Key, Column("role_id", Order = 1)]
    public int RoleId { get; set; }

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("RoleId")]
    public virtual Role Role { get; set; } = null!;
}

[Table("speakers")]
public partial class Speaker
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("full_name")]
    [Required]
    public string FullName { get; set; } = null!;

    [Column("email")]
    public string? Email { get; set; }

    [Column("org_name")]
    public string? OrgName { get; set; }

    [Column("bio")]
    public string? Bio { get; set; }

    [Column("photo_url")]
    public string? PhotoUrl { get; set; }

    [Column("contact_email")]
    [Required]
    public string ContactEmail { get; set; } = null!;

    [Column("social", TypeName = "jsonb")]
    public string Social { get; set; } = "{}";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}

[Table("teams")]
public partial class Team
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("org_id")]
    public Guid? OrgId { get; set; }

    // [ForeignKey("OrgId")]
    // public virtual Organization? Organization { get; set; }

    public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
    public virtual ICollection<Winner> Winners { get; set; } = new List<Winner>();
}

[Table("team_members")]
public partial class TeamMember
{
    [Key, Column("team_id", Order = 0)]
    public Guid TeamId { get; set; }

    [Key, Column("user_id", Order = 1)]
    public Guid UserId { get; set; }

    [ForeignKey("TeamId")]
    public virtual Team Team { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}

[Table("enrollments")]
public partial class Enrollment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("activity_id")]
    public Guid ActivityId { get; set; }

    [Column("seat_number")]
    public int? SeatNumber { get; set; }

    [Column("qr_code_id")]
    [StringLength(100)]
    public string? QrCodeId { get; set; }

    [Column("attended")]
    public bool Attended { get; set; } = false;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("ActivityId")]
    public virtual Activity Activity { get; set; } = null!;
}

[Table("winners")]
public partial class Winner
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("edition_year")]
    public int EditionYear { get; set; }

    [Column("activity_id")]
    public Guid ActivityId { get; set; }

    [Column("place")]
    public short Place { get; set; }

    [Column("team_id")]
    public Guid? TeamId { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [ForeignKey("ActivityId")]
    public virtual Activity Activity { get; set; } = null!;

    [ForeignKey("TeamId")]
    public virtual Team? Team { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}

[Table("faq_items")]
public partial class FaqItem
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("question")]
    public string Question { get; set; } = null!;

    [Column("answer")]
    public string Answer { get; set; } = null!;

    [Column("published")]
    public bool Published { get; set; } = true;

    [Column("position")]
    public int Position { get; set; } = 0;
}

[Table("checkin_tokens")]
public partial class CheckInToken
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("token")]
    [StringLength(255)]
    public string Token { get; set; } = null!;

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("activity_id")]
    public Guid ActivityId { get; set; }

    [Column("enrollment_id")]
    public Guid EnrollmentId { get; set; }

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("used")]
    public bool Used { get; set; } = false;

    [Column("used_at")]
    public DateTime? UsedAt { get; set; }

    [Column("used_by")]
    public Guid? UsedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("ActivityId")]
    public virtual Activity Activity { get; set; } = null!;

    [ForeignKey("EnrollmentId")]
    public virtual Enrollment Enrollment { get; set; } = null!;

    [ForeignKey("UsedBy")]
    public virtual User? UsedByUser { get; set; }
}

[Table("qr_jwt_ids")]
public class QrJwtId
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("jwt_id")]
    [MaxLength(50)]
    public string JwtId { get; set; } = string.Empty;

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("activity_id")]
    public Guid ActivityId { get; set; }

    [Required]
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Required]
    [Column("used")]
    public bool Used { get; set; } = false;

    [Column("used_at")]
    public DateTime? UsedAt { get; set; }

    [Column("used_by")]
    public Guid? UsedBy { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("ActivityId")]
    public virtual Activity Activity { get; set; } = null!;

    [ForeignKey("UsedBy")]
    public virtual User? UsedByUser { get; set; }
}