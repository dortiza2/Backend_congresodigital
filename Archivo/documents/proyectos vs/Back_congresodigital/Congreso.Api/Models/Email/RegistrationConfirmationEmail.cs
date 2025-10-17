namespace Congreso.Api.Models.Email;

/// <summary>
/// Datos para el email de confirmación de registro
/// </summary>
public class RegistrationConfirmationEmail
{
    /// <summary>
    /// ID del usuario registrado
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Email del usuario
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Nombre completo del usuario
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Indica si es usuario UMG
    /// </summary>
    public bool IsUmg { get; set; }

    /// <summary>
    /// Nombre de la organización
    /// </summary>
    public string OrgName { get; set; } = string.Empty;

    /// <summary>
    /// URL del dashboard/portal del usuario
    /// </summary>
    public string DashboardUrl { get; set; } = string.Empty;

    /// <summary>
    /// Token de confirmación (opcional para verificación futura)
    /// </summary>
    public string? ConfirmationToken { get; set; }

    /// <summary>
    /// Indica si el usuario ya tiene actividades confirmadas
    /// </summary>
    public bool HasEnrollments { get; set; }

    /// <summary>
    /// URL base del frontend para construir enlaces
    /// </summary>
    public string FrontBaseUrl { get; set; } = string.Empty;
}