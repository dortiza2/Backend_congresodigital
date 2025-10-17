using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Congreso.Api.DTOs;

/// <summary>
/// DTO para subir una imagen
/// </summary>
public class UploadImageDto
{
    /// <summary>
    /// Archivo de imagen a subir
    /// </summary>
    [Required]
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// Carpeta destino (speakers, winners, teams, activities, workshops)
    /// </summary>
    [Required]
    public string Folder { get; set; } = string.Empty;

    /// <summary>
    /// ID de la entidad relacionada (opcional)
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Tipo de imagen (opcional)
    /// </summary>
    public string? ImageType { get; set; }
}