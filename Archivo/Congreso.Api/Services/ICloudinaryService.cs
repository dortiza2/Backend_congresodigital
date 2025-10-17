using CloudinaryDotNet.Actions;

namespace Congreso.Api.Services;

/// <summary>
/// Interfaz para el servicio de gestión de imágenes con Cloudinary
/// </summary>
public interface ICloudinaryService
{
    /// <summary>
    /// Sube una imagen a Cloudinary
    /// </summary>
    /// <param name="file">Archivo de imagen</param>
    /// <param name="folder">Carpeta en Cloudinary</param>
    /// <param name="publicId">ID público opcional</param>
    /// <returns>Resultado de la carga</returns>
    Task<ImageUploadResult> UploadImageAsync(IFormFile file, string folder, string? publicId = null);

    /// <summary>
    /// Elimina una imagen de Cloudinary
    /// </summary>
    /// <param name="publicId">ID público de la imagen</param>
    /// <returns>Resultado de la eliminación</returns>
    Task<DeletionResult> DeleteImageAsync(string publicId);

    /// <summary>
    /// Actualiza una imagen existente
    /// </summary>
    /// <param name="file">Nuevo archivo</param>
    /// <param name="existingPublicId">ID público existente</param>
    /// <returns>Resultado de la actualización</returns>
    Task<ImageUploadResult> UpdateImageAsync(IFormFile file, string existingPublicId);

    /// <summary>
    /// Obtiene la URL de una imagen con transformaciones opcionales
    /// </summary>
    /// <param name="publicId">ID público</param>
    /// <param name="width">Ancho opcional</param>
    /// <param name="height">Alto opcional</param>
    /// <param name="crop">Tipo de recorte</param>
    /// <returns>URL de la imagen transformada</returns>
    string GetImageUrl(string publicId, int? width = null, int? height = null, string crop = "fill");

    /// <summary>
    /// Sube múltiples imágenes
    /// </summary>
    /// <param name="files">Lista de archivos</param>
    /// <param name="folder">Carpeta destino</param>
    /// <returns>Lista de resultados</returns>
    Task<List<ImageUploadResult>> UploadMultipleImagesAsync(List<IFormFile> files, string folder);

    /// <summary>
    /// Obtiene información de transformaciones disponibles para una imagen
    /// </summary>
    /// <param name="publicId">ID público</param>
    /// <returns>Diccionario con información de transformaciones</returns>
    Task<Dictionary<string, string>> GetImageTransformations(string publicId);
}