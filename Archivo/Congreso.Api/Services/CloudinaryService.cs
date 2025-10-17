using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Congreso.Api.Models;

namespace Congreso.Api.Services;

/// <summary>
/// Implementación del servicio de gestión de imágenes con Cloudinary
/// </summary>
public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;
    private readonly IConfiguration _configuration;

    public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
    {
        _logger = logger;
        _configuration = configuration;
        
        var cloudName = _configuration["Cloudinary:CloudName"];
        var apiKey = _configuration["Cloudinary:ApiKey"];
        var apiSecret = _configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            _logger.LogWarning("Cloudinary configuration is incomplete. Image uploads will be disabled.");
            return;
        }

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<ImageUploadResult> UploadImageAsync(IFormFile file, string folder, string? publicId = null)
    {
        if (_cloudinary == null)
        {
            throw new InvalidOperationException("Cloudinary is not configured.");
        }

        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Archivo inválido", nameof(file));
        }

        // Validar tipo de archivo
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            throw new ArgumentException($"Tipo de archivo no permitido: {file.ContentType}. Solo se permiten: JPG, PNG, GIF, WebP", nameof(file));
        }

        // Validar tamaño (máximo 5MB)
        const int maxSizeInBytes = 5 * 1024 * 1024;
        if (file.Length > maxSizeInBytes)
        {
            throw new ArgumentException("El archivo excede el tamaño máximo de 5MB", nameof(file));
        }

        try
        {
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                Folder = $"congreso_digital/{folder}",
                PublicId = publicId ?? Guid.NewGuid().ToString(),
                Overwrite = true,
                Invalidate = true,
                Transformation = new Transformation()
                    .Quality("auto:good")
                    .FetchFormat("auto")
                    .Crop("limit")
                    .Width(1920)
                    .Height(1080),
                UseFilename = true,
                UniqueFilename = false
            };

            _logger.LogInformation($"Uploading image to Cloudinary: {file.FileName} in folder {folder}");
            
            var result = await _cloudinary.UploadAsync(uploadParams);
            
            if (result.Error != null)
            {
                _logger.LogError($"Error uploading image to Cloudinary: {result.Error.Message}");
                throw new Exception($"Error al subir imagen: {result.Error.Message}");
            }

            _logger.LogInformation($"Image uploaded successfully: {result.PublicId}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while uploading image to Cloudinary");
            throw new Exception($"Error al subir imagen: {ex.Message}", ex);
        }
    }

    public async Task<DeletionResult> DeleteImageAsync(string publicId)
    {
        if (_cloudinary == null)
        {
            throw new InvalidOperationException("Cloudinary is not configured.");
        }

        if (string.IsNullOrEmpty(publicId))
        {
            throw new ArgumentException("Public ID cannot be null or empty", nameof(publicId));
        }

        try
        {
            var deleteParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image
            };

            _logger.LogInformation($"Deleting image from Cloudinary: {publicId}");
            
            var result = await _cloudinary.DestroyAsync(deleteParams);
            
            if (result.Error != null)
            {
                _logger.LogError($"Error deleting image from Cloudinary: {result.Error.Message}");
                throw new Exception($"Error al eliminar imagen: {result.Error.Message}");
            }

            _logger.LogInformation($"Image deleted successfully: {publicId}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while deleting image from Cloudinary");
            throw new Exception($"Error al eliminar imagen: {ex.Message}", ex);
        }
    }

    public async Task<ImageUploadResult> UpdateImageAsync(IFormFile file, string existingPublicId)
    {
        if (string.IsNullOrEmpty(existingPublicId))
        {
            throw new ArgumentException("Existing public ID cannot be null or empty", nameof(existingPublicId));
        }

        // Primero eliminar la imagen existente
        await DeleteImageAsync(existingPublicId);

        // Luego subir la nueva imagen con el mismo public_id
        var folder = existingPublicId.Substring(0, existingPublicId.LastIndexOf('/'));
        var publicId = existingPublicId.Substring(existingPublicId.LastIndexOf('/') + 1);
        
        return await UploadImageAsync(file, folder, publicId);
    }

    public string GetImageUrl(string publicId, int? width = null, int? height = null, string crop = "fill")
    {
        if (_cloudinary == null || string.IsNullOrEmpty(publicId))
        {
            return string.Empty;
        }

        try
        {
            var transformation = new Transformation()
                .Quality("auto:good")
                .FetchFormat("auto");

            if (width.HasValue)
                transformation.Width(width.Value);
            
            if (height.HasValue)
                transformation.Height(height.Value);

            transformation.Crop(crop);

            var url = _cloudinary.Api.UrlImgUp
                .Transform(transformation)
                .Secure(true)
                .BuildUrl(publicId);

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generating URL for image: {publicId}");
            return string.Empty;
        }
    }

    public async Task<List<ImageUploadResult>> UploadMultipleImagesAsync(List<IFormFile> files, string folder)
    {
        if (files == null || !files.Any())
        {
            throw new ArgumentException("No files provided", nameof(files));
        }

        var results = new List<ImageUploadResult>();
        
        foreach (var file in files)
        {
            try
            {
                var result = await UploadImageAsync(file, folder);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading file: {file.FileName}");
                // Continuar con el siguiente archivo
            }
        }

        return results;
    }

    public async Task<Dictionary<string, string>> GetImageTransformations(string publicId)
    {
        if (_cloudinary == null || string.IsNullOrEmpty(publicId))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var transformations = new Dictionary<string, string>
            {
                ["original"] = GetImageUrl(publicId),
                ["thumbnail"] = GetImageUrl(publicId, 150, 150, "fill"),
                ["medium"] = GetImageUrl(publicId, 500, 500, "fill"),
                ["large"] = GetImageUrl(publicId, 800, 800, "fill"),
                ["avatar"] = GetImageUrl(publicId, 200, 200, "fill"),
                ["banner"] = GetImageUrl(publicId, 1200, 400, "fill")
            };

            return transformations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting transformations for image: {publicId}");
            return new Dictionary<string, string>();
        }
    }
}