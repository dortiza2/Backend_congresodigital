using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Congreso.Api.Models;

[Table("organizations")]
public partial class Organization
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("type")]
    [StringLength(100)]
    public string? Type { get; set; }

    [Column("domain")]
    [StringLength(255)]
    public string? Domain { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
}