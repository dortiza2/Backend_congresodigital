using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Congreso.Api.DTOs;

/// <summary>
/// DTO para subir múltiples imágenes
/// </summary>
public class UploadMultipleImagesDto
{
    /// <summary>
    /// Archivos de imagen a subir
    /// </summary>
    [Required]
    public List<IFormFile> Files { get; set; } = new();

    /// <summary>
    /// Carpeta destino (speakers, winners, teams, activities, workshops)
    /// </summary>
    [Required]
    public string Folder { get; set; } = string.Empty;
}