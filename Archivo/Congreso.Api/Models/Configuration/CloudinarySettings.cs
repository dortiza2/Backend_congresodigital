namespace Congreso.Api.Models.Configuration;

/// <summary>
/// Configuración de Cloudinary para el almacenamiento de imágenes
/// </summary>
public class CloudinarySettings
{
    /// <summary>
    /// Nombre del cloud en Cloudinary
    /// </summary>
    public string CloudName { get; set; } = string.Empty;

    /// <summary>
    /// API Key de Cloudinary
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API Secret de Cloudinary
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>
    /// Tamaño máximo de archivo en bytes (por defecto 5MB)
    /// </summary>
    public long MaxFileSize { get; set; } = 5 * 1024 * 1024; // 5MB

    /// <summary>
    /// Tipos de archivo permitidos
    /// </summary>
    public string[] AllowedFormats { get; set; } = { "jpg", "jpeg", "png", "gif", "webp" };

    /// <summary>
    /// Carpeta base para todas las imágenes
    /// </summary>
    public string BaseFolder { get; set; } = "congreso_digital";

    /// <summary>
    /// Subcarpetas disponibles
    /// </summary>
    public Dictionary<string, string> Folders { get; set; } = new()
    {
        ["speakers"] = "speakers",
        ["winners"] = "winners",
        ["teams"] = "teams",
        ["activities"] = "activities",
        ["workshops"] = "workshops"
    };

    /// <summary>
    /// Configuraciones de transformación predeterminadas
    /// </summary>
    public TransformSettings Transforms { get; set; } = new();
}

/// <summary>
/// Configuraciones de transformación de imágenes
/// </summary>
public class TransformSettings
{
    /// <summary>
    /// Configuración para thumbnails (miniaturas)
    /// </summary>
    public TransformConfig Thumbnail { get; set; } = new() { Width = 150, Height = 150, Crop = "fill" };

    /// <summary>
    /// Configuración para imágenes medianas
    /// </summary>
    public TransformConfig Medium { get; set; } = new() { Width = 500, Height = 500, Crop = "fill" };

    /// <summary>
    /// Configuración para imágenes grandes
    /// </summary>
    public TransformConfig Large { get; set; } = new() { Width = 800, Height = 800, Crop = "fill" };

    /// <summary>
    /// Configuración para avatares
    /// </summary>
    public TransformConfig Avatar { get; set; } = new() { Width = 200, Height = 200, Crop = "fill" };

    /// <summary>
    /// Configuración para banners
    /// </summary>
    public TransformConfig Banner { get; set; } = new() { Width = 1200, Height = 400, Crop = "fill" };
}

/// <summary>
/// Configuración individual de transformación
/// </summary>
public class TransformConfig
{
    /// <summary>
    /// Ancho de la imagen
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Alto de la imagen
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Tipo de recorte (fill, fit, scale, etc.)
    /// </summary>
    public string Crop { get; set; } = "fill";

    /// <summary>
    /// Calidad de la imagen (auto, good, best, etc.)
    /// </summary>
    public string Quality { get; set; } = "auto:good";

    /// <summary>
    /// Formato de salida (auto, jpg, png, etc.)
    /// </summary>
    public string Format { get; set; } = "auto";
}