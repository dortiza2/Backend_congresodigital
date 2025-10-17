using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Congreso.Api.Models;

/// <summary>
/// Modelo para almacenar información de imágenes gestionadas por Cloudinary
/// </summary>
[Table("images")]
public class Image
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("public_id")]
    [StringLength(255)]
    public string PublicId { get; set; } = string.Empty;

    [Required]
    [Column("url")]
    [StringLength(500)]
    public string Url { get; set; } = string.Empty;

    [Required]
    [Column("secure_url")]
    [StringLength(500)]
    public string SecureUrl { get; set; } = string.Empty;

    [Column("resource_type")]
    [StringLength(50)]
    public string ResourceType { get; set; } = "image";

    [Column("format")]
    [StringLength(20)]
    public string? Format { get; set; }

    [Column("width")]
    public int? Width { get; set; }

    [Column("height")]
    public int? Height { get; set; }

    [Column("bytes")]
    public int? Bytes { get; set; }

    [Required]
    [Column("entity_type")]
    [StringLength(50)]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    [Column("entity_id")]
    public Guid EntityId { get; set; }

    [Required]
    [Column("image_type")]
    [StringLength(50)]
    public string ImageType { get; set; } = string.Empty;

    [Column("uploaded_by")]
    public Guid? UploadedBy { get; set; }

    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [Column("is_primary")]
    public bool IsPrimary { get; set; } = false;

    [Column("metadata")]
    public string? Metadata { get; set; } // JSON para datos adicionales

    // Propiedades de navegación
    [ForeignKey("UploadedBy")]
    public virtual User? UploadedByUser { get; set; }

    /// <summary>
    /// Tipos de entidades válidas
    /// </summary>
    public static class EntityTypes
    {
        public const string Speaker = "speaker";
        public const string User = "user";
        public const string Team = "team";
        public const string Activity = "activity";
        public const string Winner = "winner";
        public const string Certificate = "certificate";
    }

    /// <summary>
    /// Tipos de imágenes válidas
    /// </summary>
    public static class ImageTypes
    {
        public const string Profile = "profile";
        public const string Banner = "banner";
        public const string Logo = "logo";
        public const string Certificate = "certificate";
        public const string Event = "event";
        public const string Gallery = "gallery";
    }
}