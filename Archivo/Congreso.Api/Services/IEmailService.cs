using Congreso.Api.Models.Email;

namespace Congreso.Api.Services;

/// <summary>
/// Servicio para envío de emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Enviar email de confirmación de inscripción
    /// </summary>
    /// <param name="confirmationData">Datos de la confirmación</param>
    /// <returns>True si se envió exitosamente</returns>
    Task<bool> SendEnrollmentConfirmationAsync(EnrollmentConfirmationEmail confirmationData);

    /// <summary>
    /// Enviar email de recordatorio de actividad
    /// </summary>
    /// <param name="reminderData">Datos del recordatorio</param>
    /// <returns>True si se envió exitosamente</returns>
    Task<bool> SendActivityReminderAsync(ActivityReminderEmail reminderData);

    /// <summary>
    /// Enviar email de confirmación de registro
    /// </summary>
    /// <param name="registrationData">Datos del registro</param>
    /// <returns>True si se envió exitosamente</returns>
    Task<bool> SendRegistrationConfirmationAsync(RegistrationConfirmationEmail registrationData);

    /// <summary>
    /// Enviar email de confirmación de múltiples inscripciones
    /// </summary>
    /// <param name="confirmationData">Datos de las confirmaciones</param>
    /// <returns>True si se envió exitosamente</returns>
    Task<bool> SendMultipleActivitiesConfirmationAsync(MultipleActivitiesConfirmationEmail confirmationData);

    /// <summary>
    /// Enviar email genérico
    /// </summary>
    /// <param name="emailRequest">Solicitud de email</param>
    /// <returns>True si se envió exitosamente</returns>
    Task<bool> SendEmailAsync(EmailRequest emailRequest);

    /// <summary>
    /// Verificar que la configuración de email esté correcta
    /// </summary>
    /// <returns>True si la configuración es válida</returns>
    bool IsConfigurationValid();
}