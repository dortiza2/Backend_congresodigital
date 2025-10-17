using System.Net;
using System.Net.Mail;
using System.Text;
using Congreso.Api.Models.Configuration;
using Congreso.Api.Models.Email;
using Microsoft.Extensions.Options;

namespace Congreso.Api.Services;

/// <summary>
/// Implementación del servicio de email usando SMTP
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly IEmailLogService _emailLogService;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger, IEmailLogService emailLogService)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
        _emailLogService = emailLogService;
    }

    public async Task<bool> SendEnrollmentConfirmationAsync(EnrollmentConfirmationEmail confirmationData)
    {
        var emailId = _emailLogService.StartEmailTracking(
            EmailType.SingleActivityConfirmation,
            confirmationData.StudentEmail,
            confirmationData.StudentName,
            $"Confirmación de Inscripción - {confirmationData.ActivityName}",
            new Dictionary<string, object>
            {
                ["EnrollmentId"] = confirmationData.EnrollmentId,
                ["ActivityName"] = confirmationData.ActivityName,
                ["ActivityDateTime"] = confirmationData.ActivityDateTime,
                ["ActivityLocation"] = confirmationData.ActivityLocation,
                ["QrToken"] = confirmationData.QrToken
            });

        try
        {
            var subject = $"Confirmación de Inscripción - {confirmationData.ActivityName}";
            var htmlContent = GenerateConfirmationEmailHtml(confirmationData);
            var textContent = GenerateConfirmationEmailText(confirmationData);

            var emailRequest = new EmailRequest
            {
                To = confirmationData.StudentEmail,
                ToName = confirmationData.StudentName,
                Subject = subject,
                HtmlContent = htmlContent,
                TextContent = textContent
            };

            _logger.LogInformation("Enviando email de confirmación de inscripción a {Email} para actividad {Activity}", 
                confirmationData.StudentEmail, confirmationData.ActivityName);

            return await SendEmailAsync(emailRequest, emailId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email de confirmación de inscripción a {Email}", 
                confirmationData.StudentEmail);
            return false;
        }
    }

    public async Task<bool> SendActivityReminderAsync(ActivityReminderEmail reminderData)
    {
        var emailId = _emailLogService.StartEmailTracking(
            EmailType.ActivityReminder,
            reminderData.StudentEmail,
            reminderData.StudentName,
            $"Recordatorio - {reminderData.ActivityName} en {reminderData.TimeUntilActivity}",
            new Dictionary<string, object>
            {
                ["ActivityName"] = reminderData.ActivityName,
                ["ActivityDateTime"] = reminderData.ActivityDateTime,
                ["ActivityLocation"] = reminderData.ActivityLocation,
                ["TimeUntilActivity"] = reminderData.TimeUntilActivity,
                ["QrToken"] = reminderData.QrToken
            });

        try
        {
            var subject = $"Recordatorio - {reminderData.ActivityName} en {reminderData.TimeUntilActivity}";
            var htmlContent = GenerateReminderEmailHtml(reminderData);
            var textContent = GenerateReminderEmailText(reminderData);

            var emailRequest = new EmailRequest
            {
                To = reminderData.StudentEmail,
                ToName = reminderData.StudentName,
                Subject = subject,
                HtmlContent = htmlContent,
                TextContent = textContent
            };

            _logger.LogInformation("Enviando email de recordatorio a {Email} para actividad {Activity}", 
                reminderData.StudentEmail, reminderData.ActivityName);

            return await SendEmailAsync(emailRequest, emailId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email de recordatorio a {Email}", 
                reminderData.StudentEmail);
            return false;
        }
    }

    public async Task<bool> SendRegistrationConfirmationAsync(RegistrationConfirmationEmail registrationData)
    {
        var emailId = _emailLogService.StartEmailTracking(
            EmailType.WelcomeEmail,
            registrationData.UserEmail,
            registrationData.FullName,
            "¡Bienvenido al Congreso Digital UMG! - Registro Exitoso",
            new Dictionary<string, object>
            {
                ["UserId"] = registrationData.UserId,
                ["FullName"] = registrationData.FullName,
                ["IsUmg"] = registrationData.IsUmg,
                ["OrgName"] = registrationData.OrgName,
                ["DashboardUrl"] = registrationData.DashboardUrl
            });

        try
        {
            var subject = "¡Bienvenido al Congreso Digital UMG! - Registro Exitoso";
            var htmlContent = GenerateRegistrationConfirmationEmailHtml(registrationData);
            var textContent = GenerateRegistrationConfirmationEmailText(registrationData);

            var emailRequest = new EmailRequest
            {
                To = registrationData.UserEmail,
                ToName = registrationData.FullName,
                Subject = subject,
                HtmlContent = htmlContent,
                TextContent = textContent
            };

            _logger.LogInformation("Enviando email de confirmación de registro a {Email}", 
                registrationData.UserEmail);

            return await SendEmailAsync(emailRequest, emailId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email de confirmación de registro a {Email}", 
                registrationData.UserEmail);
            return false;
        }
    }

    public async Task<bool> SendMultipleActivitiesConfirmationAsync(MultipleActivitiesConfirmationEmail confirmationData)
    {
        try
        {
            var activityCount = confirmationData.Activities.Count;
            var subject = activityCount == 1 
                ? $"Confirmación de Inscripción - {confirmationData.Activities.First().ActivityName}"
                : $"Confirmación de {activityCount} Inscripciones - Congreso Digital UMG";
            
            var htmlContent = GenerateMultipleActivitiesConfirmationEmailHtml(confirmationData);
            var textContent = GenerateMultipleActivitiesConfirmationEmailText(confirmationData);

            var emailRequest = new EmailRequest
            {
                To = confirmationData.StudentEmail,
                ToName = confirmationData.StudentName,
                Subject = subject,
                HtmlContent = htmlContent,
                TextContent = textContent
            };

            _logger.LogInformation("Enviando email de confirmación de {Count} actividades a {Email}", 
                activityCount, confirmationData.StudentEmail);

            return await SendEmailAsync(emailRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email de confirmación de múltiples actividades a {Email}", 
                confirmationData.StudentEmail);
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(EmailRequest emailRequest)
    {
        return await SendEmailAsync(emailRequest, null);
    }

    public async Task<bool> SendEmailAsync(EmailRequest emailRequest, string? emailId)
    {
        if (!IsConfigurationValid())
        {
            _logger.LogError("Configuración de email inválida. Verificar variables de entorno SMTP.");
            if (emailId != null)
            {
                var configError = new InvalidOperationException("Configuración de email inválida");
                _emailLogService.LogError(emailId, 1, configError, false);
                _emailLogService.LogFinalFailure(emailId, 1, configError);
            }
            return false;
        }

        var attempt = 0;
        var maxRetries = _emailSettings.MaxRetries;
        var startTime = DateTime.UtcNow;

        while (attempt <= maxRetries)
        {
            attempt++;
            
            if (emailId != null)
            {
                _emailLogService.LogAttempt(emailId, attempt);
            }

            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.Host, _emailSettings.Port)
                {
                    Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
                    EnableSsl = _emailSettings.EnableSsl,
                    Timeout = _emailSettings.TimeoutSeconds * 1000
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                    Subject = emailRequest.Subject,
                    Body = emailRequest.HtmlContent,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                mailMessage.To.Add(new MailAddress(emailRequest.To, emailRequest.ToName));

                // Agregar contenido de texto plano como vista alternativa
                if (!string.IsNullOrWhiteSpace(emailRequest.TextContent))
                {
                    var textView = AlternateView.CreateAlternateViewFromString(
                        emailRequest.TextContent, Encoding.UTF8, "text/plain");
                    mailMessage.AlternateViews.Add(textView);
                }

                // Agregar archivos adjuntos si existen
                if (emailRequest.Attachments?.Any() == true)
                {
                    foreach (var attachment in emailRequest.Attachments)
                    {
                        var stream = new MemoryStream(attachment.Content);
                        var mailAttachment = new Attachment(stream, attachment.FileName, attachment.ContentType);
                        mailMessage.Attachments.Add(mailAttachment);
                    }
                }

                await smtpClient.SendMailAsync(mailMessage);

                var processingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                
                _logger.LogInformation("Email enviado exitosamente a {Email} con asunto '{Subject}' en intento {Attempt}", 
                    emailRequest.To, emailRequest.Subject, attempt);

                if (emailId != null)
                {
                    _emailLogService.LogSuccess(emailId, processingTime);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al enviar email a {Email} en intento {Attempt}/{MaxRetries}: {Error}", 
                    emailRequest.To, attempt, maxRetries + 1, ex.Message);

                var isRetrying = attempt <= maxRetries;
                
                if (emailId != null)
                {
                    _emailLogService.LogError(emailId, attempt, ex, isRetrying);
                }

                if (!isRetrying)
                {
                    _logger.LogError("Falló el envío de email a {Email} después de {MaxRetries} intentos", 
                        emailRequest.To, maxRetries + 1);
                    
                    if (emailId != null)
                    {
                        _emailLogService.LogFinalFailure(emailId, attempt, ex);
                    }
                    
                    return false;
                }

                // Esperar antes del siguiente intento (backoff exponencial)
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                await Task.Delay(delay);
            }
        }

        return false;
    }

    public bool IsConfigurationValid()
    {
        return _emailSettings.IsValid();
    }

    private string GenerateConfirmationEmailHtml(EnrollmentConfirmationEmail data)
    {
        var qrImageUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(data.QrToken)}";
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Confirmación de Inscripción</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .qr-section {{ text-align: center; background: white; padding: 20px; border-radius: 10px; margin: 20px 0; border: 2px dashed #667eea; }}
        .activity-details {{ background: white; padding: 20px; border-radius: 10px; margin: 20px 0; }}
        .detail-row {{ display: flex; justify-content: space-between; margin: 10px 0; padding: 10px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: bold; color: #667eea; }}
        .footer {{ text-align: center; margin-top: 30px; padding: 20px; color: #666; font-size: 14px; }}
        .btn {{ display: inline-block; background: #667eea; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>¡Inscripción Confirmada!</h1>
        <p>Congreso Digital UMG</p>
    </div>
    
    <div class='content'>
        <h2>Hola {data.StudentName},</h2>
        <p>Tu inscripción ha sido confirmada exitosamente. A continuación encontrarás todos los detalles de tu actividad:</p>
        
        <div class='activity-details'>
            <h3>Detalles de la Actividad</h3>
            <div class='detail-row'>
                <span class='detail-label'>Actividad:</span>
                <span>{data.ActivityName}</span>
            </div>
            <div class='detail-row'>
                <span class='detail-label'>Fecha y Hora:</span>
                <span>{data.ActivityDateTime:dddd, dd MMMM yyyy 'a las' HH:mm}</span>
            </div>
            <div class='detail-row'>
                <span class='detail-label'>Ubicación:</span>
                <span>{data.ActivityLocation}</span>
            </div>
            {(!string.IsNullOrWhiteSpace(data.ActivityDescription) ? $@"
            <div class='detail-row'>
                <span class='detail-label'>Descripción:</span>
                <span>{data.ActivityDescription}</span>
            </div>" : "")}
        </div>
        
        <div class='qr-section'>
            <h3>Tu Código QR de Asistencia</h3>
            <p>Presenta este código QR al momento de ingresar a la actividad:</p>
            <img src='{qrImageUrl}' alt='Código QR' style='max-width: 200px; height: auto;' />
            <p><strong>Código:</strong> {data.QrToken}</p>
            <p style='font-size: 14px; color: #666;'>También puedes ingresar el código manualmente si tienes problemas con el QR.</p>
        </div>
        
        {(!string.IsNullOrWhiteSpace(data.AdditionalInfo) ? $@"
        <div style='background: #e8f4fd; padding: 15px; border-radius: 5px; margin: 20px 0;'>
            <h4>Información Adicional:</h4>
            <p>{data.AdditionalInfo}</p>
        </div>" : "")}
        
        <p>Te enviaremos un recordatorio antes de la actividad. ¡Nos vemos pronto!</p>
    </div>
    
    <div class='footer'>
        <p>Congreso Digital UMG<br>
        Universidad Mariano Gálvez de Guatemala<br>
        Este es un email automático, por favor no responder.</p>
    </div>
</body>
</html>";
    }

    private string GenerateConfirmationEmailText(EnrollmentConfirmationEmail data)
    {
        return $@"
CONFIRMACIÓN DE INSCRIPCIÓN - CONGRESO DIGITAL UMG

Hola {data.StudentName},

Tu inscripción ha sido confirmada exitosamente.

DETALLES DE LA ACTIVIDAD:
- Actividad: {data.ActivityName}
- Fecha y Hora: {data.ActivityDateTime:dddd, dd MMMM yyyy 'a las' HH:mm}
- Ubicación: {data.ActivityLocation}
{(!string.IsNullOrWhiteSpace(data.ActivityDescription) ? $"- Descripción: {data.ActivityDescription}" : "")}

CÓDIGO QR DE ASISTENCIA:
Código: {data.QrToken}

Presenta este código al momento de ingresar a la actividad.
También puedes generar el QR en: {data.QrCodeUrl}

{(!string.IsNullOrWhiteSpace(data.AdditionalInfo) ? $@"
INFORMACIÓN ADICIONAL:
{data.AdditionalInfo}
" : "")}

Te enviaremos un recordatorio antes de la actividad.

¡Nos vemos pronto!

---
Congreso Digital UMG
Universidad Mariano Gálvez de Guatemala
Este es un email automático, por favor no responder.
";
    }

    private string GenerateReminderEmailHtml(ActivityReminderEmail data)
    {
        var qrImageUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(data.QrToken)}";
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Recordatorio de Actividad</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .reminder-alert {{ background: #fff3cd; border: 1px solid #ffeaa7; color: #856404; padding: 15px; border-radius: 5px; margin: 20px 0; text-align: center; }}
        .qr-section {{ text-align: center; background: white; padding: 20px; border-radius: 10px; margin: 20px 0; border: 2px dashed #f5576c; }}
        .activity-details {{ background: white; padding: 20px; border-radius: 10px; margin: 20px 0; }}
        .detail-row {{ display: flex; justify-content: space-between; margin: 10px 0; padding: 10px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: bold; color: #f5576c; }}
        .footer {{ text-align: center; margin-top: 30px; padding: 20px; color: #666; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🔔 Recordatorio de Actividad</h1>
        <p>Congreso Digital UMG</p>
    </div>
    
    <div class='content'>
        <div class='reminder-alert'>
            <h3>⏰ Tu actividad comienza en {data.TimeUntilActivity}</h3>
        </div>
        
        <h2>Hola {data.StudentName},</h2>
        <p>Te recordamos que tienes una actividad próxima en el Congreso Digital UMG:</p>
        
        <div class='activity-details'>
            <h3>Detalles de la Actividad</h3>
            <div class='detail-row'>
                <span class='detail-label'>Actividad:</span>
                <span>{data.ActivityName}</span>
            </div>
            <div class='detail-row'>
                <span class='detail-label'>Fecha y Hora:</span>
                <span>{data.ActivityDateTime:dddd, dd MMMM yyyy 'a las' HH:mm}</span>
            </div>
            <div class='detail-row'>
                <span class='detail-label'>Ubicación:</span>
                <span>{data.ActivityLocation}</span>
            </div>
        </div>
        
        <div class='qr-section'>
            <h3>Tu Código QR de Asistencia</h3>
            <p>No olvides presentar este código QR al ingresar:</p>
            <img src='{qrImageUrl}' alt='Código QR' style='max-width: 200px; height: auto;' />
            <p><strong>Código:</strong> {data.QrToken}</p>
        </div>
        
        {(!string.IsNullOrWhiteSpace(data.EntryNotes) ? $@"
        <div style='background: #e8f4fd; padding: 15px; border-radius: 5px; margin: 20px 0;'>
            <h4>📝 Notas de Ingreso:</h4>
            <p>{data.EntryNotes}</p>
        </div>" : "")}
        
        <p><strong>¡Te esperamos!</strong> Llega unos minutos antes para facilitar tu ingreso.</p>
    </div>
    
    <div class='footer'>
        <p>Congreso Digital UMG<br>
        Universidad Mariano Gálvez de Guatemala<br>
        Este es un email automático, por favor no responder.</p>
    </div>
</body>
</html>";
    }

    private string GenerateReminderEmailText(ActivityReminderEmail data)
    {
        return $@"
RECORDATORIO DE ACTIVIDAD - CONGRESO DIGITAL UMG

🔔 Tu actividad comienza en {data.TimeUntilActivity}

Hola {data.StudentName},

Te recordamos que tienes una actividad próxima en el Congreso Digital UMG:

DETALLES DE LA ACTIVIDAD:
- Actividad: {data.ActivityName}
- Fecha y Hora: {data.ActivityDateTime:dddd, dd MMMM yyyy 'a las' HH:mm}
- Ubicación: {data.ActivityLocation}

CÓDIGO QR DE ASISTENCIA:
Código: {data.QrToken}

No olvides presentar este código al ingresar.
También puedes generar el QR en: {data.QrCodeUrl}

{(!string.IsNullOrWhiteSpace(data.EntryNotes) ? $@"
NOTAS DE INGRESO:
{data.EntryNotes}
" : "")}

¡Te esperamos! Llega unos minutos antes para facilitar tu ingreso.

---
Congreso Digital UMG
Universidad Mariano Gálvez de Guatemala
Este es un email automático, por favor no responder.
";
    }

    private string GenerateRegistrationConfirmationEmailHtml(RegistrationConfirmationEmail data)
    {
        var userTypeText = data.IsUmg ? "Participante UMG" : "Participante Externo";
        var institutionText = string.IsNullOrWhiteSpace(data.OrgName) ? "Ninguna" : data.OrgName;
        
        // URLs del frontend
        var homeUrl = $"{data.FrontBaseUrl}/";
        var accountUrl = $"{data.FrontBaseUrl}/mi-cuenta";
        var activitiesUrl = $"{data.FrontBaseUrl}/inscripcion";
        
        // Lógica condicional para CTAs
        var primaryCta = data.HasEnrollments 
            ? $"<a href='{accountUrl}' class='button primary'>🏠 Ir a mi cuenta</a>"
            : $"<a href='{activitiesUrl}' class='button primary'>📚 Ir a actividades</a>";
        
        var secondaryCta = data.HasEnrollments 
            ? $"<a href='{activitiesUrl}' class='button secondary'>📚 Ir a actividades</a>"
            : $"<a href='{accountUrl}' class='button secondary'>🏠 Ir a mi cuenta</a>";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; }}
        .header {{ background: linear-gradient(135deg, #1e3a8a, #3b82f6); color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .welcome-box {{ background: #f0f9ff; border-left: 4px solid #3b82f6; padding: 15px; margin: 20px 0; }}
        .info-section {{ background: #f8fafc; padding: 15px; margin: 15px 0; border-radius: 8px; }}
        .button {{ display: inline-block; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin: 5px; font-weight: bold; }}
        .button.primary {{ background: #3b82f6; }}
        .button.secondary {{ background: #6b7280; }}
        .cta-section {{ text-align: center; margin: 25px 0; }}
        .footer {{ background: #f1f5f9; padding: 15px; text-align: center; font-size: 12px; color: #64748b; }}
        .highlight {{ color: #1e40af; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🎓 CONGRESO DIGITAL UMG — ¡Bienvenido al Congreso Digital UMG!</h1>
    </div>
    
    <div class='content'>
        <div class='welcome-box'>
            <h3>¡Hola {data.FullName}!</h3>
            <p>Tu registro ha sido <span class='highlight'>exitoso</span>. Ya formas parte del Congreso Digital UMG.</p>
        </div>
        
        <div class='info-section'>
            <h4>📋 Información de tu cuenta:</h4>
            <p><strong>Email:</strong> {data.UserEmail}</p>
            <p><strong>Tipo de usuario:</strong> {userTypeText}</p>
            <p><strong>Institución:</strong> {institutionText}</p>
        </div>
        
        <div class='info-section'>
            <h4>🎯 Próximos pasos:</h4>
            <ol>
                <li>Ingresa a <strong>tu cuenta</strong> para gestionar tu participación.</li>
                <li>Explora las actividades disponibles.</li>
                <li>Inscríbete en los talleres/charlas de tu interés.</li>
                <li>Recibirás por correo los <strong>QR</strong> de acceso por cada actividad confirmada.</li>
            </ol>
        </div>
        
        <div class='cta-section'>
            {primaryCta}
            {secondaryCta}
            <br>
            <a href='{homeUrl}' class='button secondary'>🌐 Ver la página del congreso</a>
        </div>
        
        <div class='info-section'>
            <h4>📞 ¿Necesitas ayuda?</h4>
            <p>Si tienes alguna pregunta o necesitas asistencia, no dudes en contactarnos.</p>
            <p>Estamos aquí para hacer de tu experiencia en el congreso algo memorable.</p>
        </div>
        
        <p><strong>¡Gracias por ser parte del Congreso Digital UMG!</strong></p>
    </div>
    
    <div class='footer'>
        <p>Congreso Digital UMG<br>
        Universidad Mariano Gálvez de Guatemala<br>
        Este es un email automático, por favor no responder.</p>
    </div>
</body>
</html>";
    }

    private string GenerateRegistrationConfirmationEmailText(RegistrationConfirmationEmail data)
    {
        var userTypeText = data.IsUmg ? "Participante UMG" : "Participante Externo";
        var institutionText = string.IsNullOrWhiteSpace(data.OrgName) ? "Ninguna" : data.OrgName;
        
        // URLs del frontend
        var homeUrl = $"{data.FrontBaseUrl}/";
        var accountUrl = $"{data.FrontBaseUrl}/mi-cuenta";
        var activitiesUrl = $"{data.FrontBaseUrl}/inscripcion";

        return $@"
🎓 CONGRESO DIGITAL UMG — ¡Bienvenido al Congreso Digital UMG!

¡Hola {data.FullName}!

Tu registro ha sido exitoso. Ya formas parte del Congreso Digital UMG.

📋 INFORMACIÓN DE TU CUENTA:
• Email: {data.UserEmail}
• Tipo de usuario: {userTypeText}
• Institución: {institutionText}

🎯 PRÓXIMOS PASOS:
1. Ingresa a tu cuenta para gestionar tu participación.
2. Explora las actividades disponibles.
3. Inscríbete en los talleres/charlas de tu interés.
4. Recibirás por correo los QR de acceso por cada actividad confirmada.

🔗 ENLACES IMPORTANTES:
• Ir a mi cuenta: {accountUrl}
• Ir a actividades: {activitiesUrl}
• Ver la página del congreso: {homeUrl}

📞 ¿NECESITAS AYUDA?
Si tienes alguna pregunta o necesitas asistencia, no dudes en contactarnos.
Estamos aquí para hacer de tu experiencia en el congreso algo memorable.

¡Gracias por ser parte del Congreso Digital UMG!

---
Congreso Digital UMG
Universidad Mariano Gálvez de Guatemala
Este es un email automático, por favor no responder.
";
    }

    /// <summary>
    /// Genera el contenido HTML para el email de confirmación de múltiples actividades
    /// </summary>
    private string GenerateMultipleActivitiesConfirmationEmailHtml(MultipleActivitiesConfirmationEmail data)
    {
        var activitiesHtml = string.Join("", data.Activities.Select(activity => $@"
            <div style='border: 1px solid #e0e0e0; border-radius: 8px; padding: 20px; margin: 15px 0; background-color: #f9f9f9;'>
                <h3 style='color: #2c5aa0; margin: 0 0 10px 0; font-size: 18px;'>{activity.ActivityName}</h3>
                <p style='color: #666; margin: 5px 0; font-size: 14px;'><strong>Tipo:</strong> {activity.ActivityType}</p>
                <p style='color: #666; margin: 5px 0; font-size: 14px;'><strong>Fecha y Hora:</strong> {activity.ActivityDateTime:dddd, dd 'de' MMMM 'de' yyyy 'a las' HH:mm}</p>
                <p style='color: #666; margin: 5px 0; font-size: 14px;'><strong>Ubicación:</strong> {activity.ActivityLocation}</p>
                {(!string.IsNullOrWhiteSpace(activity.Speaker) ? $"<p style='color: #666; margin: 5px 0; font-size: 14px;'><strong>Ponente:</strong> {activity.Speaker}</p>" : "")}
                {(!string.IsNullOrWhiteSpace(activity.Duration) ? $"<p style='color: #666; margin: 5px 0; font-size: 14px;'><strong>Duración:</strong> {activity.Duration}</p>" : "")}
                <p style='color: #666; margin: 10px 0 5px 0; font-size: 14px;'><strong>Descripción:</strong></p>
                <p style='color: #666; margin: 5px 0; font-size: 14px; line-height: 1.4;'>{activity.ActivityDescription}</p>
                
                <div style='background-color: #fff; border: 2px dashed #2c5aa0; border-radius: 8px; padding: 15px; margin: 15px 0; text-align: center;'>
                    <h4 style='color: #2c5aa0; margin: 0 0 10px 0;'>🎫 Tu Código QR de Asistencia</h4>
                    <div style='background-color: #f0f4f8; padding: 10px; border-radius: 4px; margin: 10px 0;'>
                        <p style='font-family: monospace; font-size: 16px; font-weight: bold; color: #2c5aa0; margin: 0;'>{activity.QrToken}</p>
                    </div>
                    <p style='color: #666; font-size: 12px; margin: 5px 0;'>Presenta este código o escanea el QR para registrar tu asistencia</p>
                    {(!string.IsNullOrWhiteSpace(activity.QrCodeUrl) ? $"<p style='margin: 10px 0;'><a href='{activity.QrCodeUrl}' style='color: #2c5aa0; text-decoration: none; font-weight: bold;'>🔗 Ver Código QR</a></p>" : "")}
                </div>
                
                {(!string.IsNullOrWhiteSpace(activity.SpecialNotes) ? $@"
                <div style='background-color: #fff3cd; border: 1px solid #ffeaa7; border-radius: 4px; padding: 10px; margin: 10px 0;'>
                    <p style='color: #856404; margin: 0; font-size: 14px;'><strong>📝 Nota Especial:</strong> {activity.SpecialNotes}</p>
                </div>" : "")}
            </div>"));

        var summaryInfo = data.Activities.Count > 1 ? $@"
            <div style='background-color: #e8f4fd; border-radius: 8px; padding: 20px; margin: 20px 0;'>
                <h3 style='color: #2c5aa0; margin: 0 0 15px 0;'>📊 Resumen de tus Inscripciones</h3>
                <p style='color: #666; margin: 5px 0;'><strong>Total de actividades:</strong> {data.Activities.Count}</p>
                <p style='color: #666; margin: 5px 0;'><strong>Próxima actividad:</strong> {data.Activities.OrderBy(a => a.ActivityDateTime).First().ActivityName}</p>
                <p style='color: #666; margin: 5px 0;'><strong>Fecha de la próxima:</strong> {data.Activities.OrderBy(a => a.ActivityDateTime).First().ActivityDateTime:dddd, dd 'de' MMMM 'a las' HH:mm}</p>
            </div>" : "";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Confirmación de Inscripciones - Congreso Digital UMG</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #2c5aa0 0%, #1e3a8a 100%); color: white; padding: 30px; border-radius: 10px; text-align: center; margin-bottom: 30px;'>
        <h1 style='margin: 0; font-size: 28px; font-weight: bold;'>🎓 CONGRESO DIGITAL UMG</h1>
        <h2 style='margin: 10px 0 0 0; font-size: 20px; font-weight: normal;'>✅ Confirmación de Inscripciones</h2>
    </div>

    <div style='background-color: #f8f9fa; border-radius: 8px; padding: 25px; margin-bottom: 25px;'>
        <h2 style='color: #2c5aa0; margin: 0 0 15px 0;'>¡Hola {data.StudentName}!</h2>
        <p style='color: #666; margin: 0; font-size: 16px; line-height: 1.5;'>
            Hemos confirmado exitosamente {(data.Activities.Count == 1 ? "tu inscripción" : $"tus {data.Activities.Count} inscripciones")} 
            para el Congreso Digital UMG. A continuación encontrarás todos los detalles de {(data.Activities.Count == 1 ? "tu actividad" : "tus actividades")}.
        </p>
        <p style='color: #666; margin: 10px 0 0 0; font-size: 14px;'>
            <strong>Fecha de confirmación:</strong> {data.ConfirmationDate:dddd, dd 'de' MMMM 'de' yyyy 'a las' HH:mm}
        </p>
    </div>

    {summaryInfo}

    <div style='margin: 25px 0;'>
        <h2 style='color: #2c5aa0; margin: 0 0 20px 0; border-bottom: 2px solid #2c5aa0; padding-bottom: 10px;'>
            📅 {(data.Activities.Count == 1 ? "Tu Actividad Confirmada" : "Tus Actividades Confirmadas")}
        </h2>
        {activitiesHtml}
    </div>

    <div style='background-color: #e8f4fd; border-radius: 8px; padding: 20px; margin: 25px 0;'>
        <h3 style='color: #2c5aa0; margin: 0 0 15px 0;'>📱 Instrucciones Importantes</h3>
        <ul style='color: #666; margin: 0; padding-left: 20px;'>
            <li style='margin: 8px 0;'>Llega 15 minutos antes del inicio de cada actividad</li>
            <li style='margin: 8px 0;'>Ten tu código QR listo para el registro de asistencia</li>
            <li style='margin: 8px 0;'>Puedes acceder a tus códigos QR desde tu dashboard personal</li>
            <li style='margin: 8px 0;'>Si tienes problemas técnicos, contacta al soporte del evento</li>
        </ul>
    </div>

    {(!string.IsNullOrWhiteSpace(data.DashboardUrl) ? $@"
    <div style='text-align: center; margin: 25px 0;'>
        <a href='{data.DashboardUrl}' style='background-color: #2c5aa0; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>
            🏠 Acceder a Mi Dashboard
        </a>
    </div>" : "")}

    {(!string.IsNullOrWhiteSpace(data.AdditionalInfo) ? $@"
    <div style='background-color: #fff3cd; border: 1px solid #ffeaa7; border-radius: 8px; padding: 15px; margin: 20px 0;'>
        <h4 style='color: #856404; margin: 0 0 10px 0;'>ℹ️ Información Adicional</h4>
        <p style='color: #856404; margin: 0; line-height: 1.4;'>{data.AdditionalInfo}</p>
    </div>" : "")}

    <div style='background-color: #f8f9fa; border-radius: 8px; padding: 20px; margin: 25px 0; text-align: center;'>
        <p style='color: #666; margin: 0; font-size: 14px;'>
            ¿Necesitas ayuda? Contacta al equipo de soporte del Congreso Digital UMG
        </p>
    </div>

    <div style='text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #e0e0e0;'>
        <p style='color: #999; font-size: 12px; margin: 0;'>
            Congreso Digital UMG - Universidad Mariano Gálvez de Guatemala<br>
            Este es un email automático, por favor no responder.
        </p>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Genera el contenido de texto plano para el email de confirmación de múltiples actividades
    /// </summary>
    private string GenerateMultipleActivitiesConfirmationEmailText(MultipleActivitiesConfirmationEmail data)
    {
        var activitiesText = string.Join("\n\n", data.Activities.Select((activity, index) => $@"
=== ACTIVIDAD {index + 1}: {activity.ActivityName.ToUpper()} ===
Tipo: {activity.ActivityType}
Fecha y Hora: {activity.ActivityDateTime:dddd, dd 'de' MMMM 'de' yyyy 'a las' HH:mm}
Ubicación: {activity.ActivityLocation}
{(!string.IsNullOrWhiteSpace(activity.Speaker) ? $"Ponente: {activity.Speaker}" : "")}
{(!string.IsNullOrWhiteSpace(activity.Duration) ? $"Duración: {activity.Duration}" : "")}

Descripción:
{activity.ActivityDescription}

🎫 TU CÓDIGO QR DE ASISTENCIA:
Código: {activity.QrToken}
{(!string.IsNullOrWhiteSpace(activity.QrCodeUrl) ? $"Ver QR: {activity.QrCodeUrl}" : "")}

{(!string.IsNullOrWhiteSpace(activity.SpecialNotes) ? $"📝 NOTA ESPECIAL: {activity.SpecialNotes}" : "")}"));

        var summaryText = data.Activities.Count > 1 ? $@"

📊 RESUMEN DE TUS INSCRIPCIONES:
- Total de actividades: {data.Activities.Count}
- Próxima actividad: {data.Activities.OrderBy(a => a.ActivityDateTime).First().ActivityName}
- Fecha de la próxima: {data.Activities.OrderBy(a => a.ActivityDateTime).First().ActivityDateTime:dddd, dd 'de' MMMM 'a las' HH:mm}
" : "";

        return $@"
🎓 CONGRESO DIGITAL UMG
✅ CONFIRMACIÓN DE INSCRIPCIONES

¡Hola {data.StudentName}!

Hemos confirmado exitosamente {(data.Activities.Count == 1 ? "tu inscripción" : $"tus {data.Activities.Count} inscripciones")} para el Congreso Digital UMG.

Fecha de confirmación: {data.ConfirmationDate:dddd, dd 'de' MMMM 'de' yyyy 'a las' HH:mm}
{summaryText}

📅 {(data.Activities.Count == 1 ? "TU ACTIVIDAD CONFIRMADA" : "TUS ACTIVIDADES CONFIRMADAS")}:
{activitiesText}

📱 INSTRUCCIONES IMPORTANTES:
- Llega 15 minutos antes del inicio de cada actividad
- Ten tu código QR listo para el registro de asistencia
- Puedes acceder a tus códigos QR desde tu dashboard personal
- Si tienes problemas técnicos, contacta al soporte del evento

{(!string.IsNullOrWhiteSpace(data.DashboardUrl) ? $@"
🏠 ACCESO A TU DASHBOARD:
{data.DashboardUrl}
" : "")}

{(!string.IsNullOrWhiteSpace(data.AdditionalInfo) ? $@"
ℹ️ INFORMACIÓN ADICIONAL:
{data.AdditionalInfo}
" : "")}

¿NECESITAS AYUDA?
Contacta al equipo de soporte del Congreso Digital UMG

---
Congreso Digital UMG
Universidad Mariano Gálvez de Guatemala
Este es un email automático, por favor no responder.
";
    }
}