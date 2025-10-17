using Microsoft.AspNetCore.Mvc;
using Congreso.Api.Services;
using Congreso.Api.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Congreso.Api.Controllers;

/// <summary>
/// Controlador para gestionar imágenes del congreso
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<ImagesController> _logger;

    public ImagesController(ICloudinaryService cloudinaryService, ILogger<ImagesController> logger)
    {
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    /// <summary>
    /// Subir una imagen
    /// </summary>
    /// <param name="file">Archivo de imagen</param>
    /// <param name="folder">Carpeta destino (speakers, winners, teams, activities, workshops)</param>
    /// <param name="entityId">ID de la entidad relacionada</param>
    /// <param name="imageType">Tipo de imagen</param>
    /// <returns>Información de la imagen subida</returns>
    [HttpPost("upload")]
    [Authorize]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromForm] string folder, [FromForm] string? entityId = null, [FromForm] string? imageType = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No se proporcionó archivo" });
            }

            // Validar carpeta
            var validFolders = new[] { "speakers", "winners", "teams", "activities", "workshops" };
            if (!validFolders.Contains(folder.ToLower()))
            {
                return BadRequest(new { error = "Carpeta inválida. Use: speakers, winners, teams, activities, workshops" });
            }

            var result = await _cloudinaryService.UploadImageAsync(file, folder);

            // Aquí podrías guardar la información en la base de datos
            // Por ahora, solo devolvemos la información de Cloudinary
            var response = new
            {
                publicId = result.PublicId,
                url = result.SecureUrl.ToString(),
                originalUrl = result.Url.ToString(),
                format = result.Format,
                width = result.Width,
                height = result.Height,
                size = result.Bytes,
                folder = result.Folder,
                createdAt = result.CreatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Subir múltiples imágenes
    /// </summary>
    /// <param name="files">Archivos de imagen</param>
    /// <param name="folder">Carpeta destino</param>
    /// <returns>Lista de imágenes subidas</returns>
    [HttpPost("upload-multiple")]
    [Authorize]
    public async Task<IActionResult> UploadMultipleImages([FromForm] List<IFormFile> files, [FromForm] string folder)
    {
        try
        {
            if (files == null || !files.Any())
            {
                return BadRequest(new { error = "No se proporcionaron archivos" });
            }

            // Validar carpeta
            var validFolders = new[] { "speakers", "winners", "teams", "activities", "workshops" };
            if (!validFolders.Contains(folder.ToLower()))
            {
                return BadRequest(new { error = "Carpeta inválida. Use: speakers, winners, teams, activities, workshops" });
            }

            var results = await _cloudinaryService.UploadMultipleImagesAsync(files, folder);

            var response = results.Select(result => new
            {
                publicId = result.PublicId,
                url = result.SecureUrl.ToString(),
                format = result.Format,
                width = result.Width,
                height = result.Height,
                size = result.Bytes,
                folder = result.Folder,
                createdAt = result.CreatedAt
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading multiple images");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Eliminar una imagen
    /// </summary>
    /// <param name="publicId">ID público de la imagen</param>
    /// <returns>Resultado de la eliminación</returns>
    [HttpDelete("delete/{publicId}")]
    [Authorize]
    public async Task<IActionResult> DeleteImage(string publicId)
    {
        try
        {
            var result = await _cloudinaryService.DeleteImageAsync(publicId);
            
            return Ok(new
            {
                result = result.Result,
                message = "Imagen eliminada exitosamente"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting image: {publicId}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Actualizar una imagen existente
    /// </summary>
    /// <param name="file">Nuevo archivo</param>
    /// <param name="existingPublicId">ID público existente</param>
    /// <returns>Información de la imagen actualizada</returns>
    [HttpPut("update/{existingPublicId}")]
    [Authorize]
    public async Task<IActionResult> UpdateImage([FromForm] IFormFile file, string existingPublicId)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No se proporcionó archivo" });
            }

            var result = await _cloudinaryService.UpdateImageAsync(file, existingPublicId);

            var response = new
            {
                publicId = result.PublicId,
                url = result.SecureUrl.ToString(),
                format = result.Format,
                width = result.Width,
                height = result.Height,
                size = result.Bytes,
                updatedAt = result.CreatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating image: {existingPublicId}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener URL de imagen con transformaciones
    /// </summary>
    /// <param name="publicId">ID público</param>
    /// <param name="width">Ancho opcional</param>
    /// <param name="height">Alto opcional</param>
    /// <param name="crop">Tipo de recorte</param>
    /// <returns>URL de la imagen</returns>
    [HttpGet("url/{publicId}")]
    public IActionResult GetImageUrl(string publicId, [FromQuery] int? width = null, [FromQuery] int? height = null, [FromQuery] string crop = "fill")
    {
        try
        {
            var url = _cloudinaryService.GetImageUrl(publicId, width, height, crop);
            
            if (string.IsNullOrEmpty(url))
            {
                return NotFound(new { error = "No se pudo generar la URL de la imagen" });
            }

            return Ok(new { url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting image URL: {publicId}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener URLs de imagen con diferentes transformaciones
    /// </summary>
    /// <param name="publicId">ID público</param>
    /// <returns>Diccionario con URLs de diferentes tamaños</returns>
    [HttpGet("transformations/{publicId}")]
    public async Task<IActionResult> GetImageTransformations(string publicId)
    {
        try
        {
            var transformations = await _cloudinaryService.GetImageTransformations(publicId);
            
            return Ok(transformations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting image transformations: {publicId}");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}