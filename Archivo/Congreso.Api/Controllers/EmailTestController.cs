using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Congreso.Api.Services;
using Congreso.Api.Models.Email;
using System.ComponentModel.DataAnnotations;

namespace Congreso.Api.Controllers;

/// <summary>
/// Controlador para testing del sistema de emails
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class EmailTestController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailTestController> _logger;

    public EmailTestController(IEmailService emailService, ILogger<EmailTestController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Verificar configuración SMTP
    /// </summary>
    [HttpGet("config")]
    public IActionResult CheckConfiguration()
    {
        try
        {
            var isValid = _emailService.IsConfigurationValid();
            
            return Ok(new
            {
                isValid,
                message = isValid 
                    ? "Configuración SMTP válida" 
                    : "Configuración SMTP inválida. Verificar variables de entorno.",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar configuración SMTP");
            return StatusCode(500, new { message = "Error interno al verificar configuración", error = ex.Message });
        }
    }

    /// <summary>
    /// Enviar email de prueba
    /// </summary>
    [HttpPost("send-test")]
    public async Task<IActionResult> SendTestEmail([FromBody] TestEmailRequest request)
    {
        try
        {
            if (!_emailService.IsConfigurationValid())
            {
                return BadRequest(new { message = "Configuración SMTP inválida" });
            }

            var emailRequest = new EmailRequest
            {
                To = request.ToEmail,
                ToName = request.ToName ?? "Usuario de Prueba",
                Subject = "🧪 Email de Prueba - Congreso Digital UMG",
                HtmlContent = GenerateTestEmailHtml(request.ToName ?? "Usuario"),
                TextContent = GenerateTestEmailText(request.ToName ?? "Usuario")
            };

            var success = await _emailService.SendEmailAsync(emailRequest);

            if (success)
            {
                _logger.LogInformation("Email de prueba enviado exitosamente a {Email}", request.ToEmail);
                return Ok(new
                {
                    success = true,
                    message = "Email de prueba enviado exitosamente",
                    sentTo = request.ToEmail,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogWarning("Falló el envío del email de prueba a {Email}", request.ToEmail);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al enviar email de prueba",
                    sentTo = request.ToEmail,
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email de prueba a {Email}", request.ToEmail);
            return StatusCode(500, new { message = "Error interno al enviar email", error = ex.Message });
        }
    }

    /// <summary>
    /// Enviar email de bienvenida de prueba
    /// </summary>
    [HttpPost("send-welcome-test")]
    public async Task<IActionResult> SendWelcomeTestEmail([FromBody] TestEmailRequest request)
    {
        try
        {
            if (!_emailService.IsConfigurationValid())
            {
                return BadRequest(new { message = "Configuración SMTP inválida" });
            }

            var registrationData = new RegistrationConfirmationEmail
            {
                UserId = Guid.NewGuid(),
                UserEmail = request.ToEmail,
                FullName = request.ToName ?? "Usuario de Prueba",
                IsUmg = true,
                OrgName = "Universidad Mariano Gálvez",
                DashboardUrl = "https://congreso.umg.edu.gt/portal",
                ConfirmationToken = Guid.NewGuid().ToString()
            };

            var success = await _emailService.SendRegistrationConfirmationAsync(registrationData);

            if (success)
            {
                _logger.LogInformation("Email de bienvenida de prueba enviado exitosamente a {Email}", request.ToEmail);
                return Ok(new
                {
                    success = true,
                    message = "Email de bienvenida de prueba enviado exitosamente",
                    sentTo = request.ToEmail,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al enviar email de bienvenida de prueba",
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email de bienvenida de prueba a {Email}", request.ToEmail);
            return StatusCode(500, new { message = "Error interno al enviar email", error = ex.Message });
        }
    }

    /// <summary>
    /// Enviar email de confirmación de inscripción de prueba
    /// </summary>
    [HttpPost("send-enrollment-test")]
    public async Task<IActionResult> SendEnrollmentTestEmail([FromBody] TestEmailRequest request)
    {
        try
        {
            if (!_emailService.IsConfigurationValid())
            {
                return BadRequest(new { message = "Configuración SMTP inválida" });
            }

            var confirmationData = new EnrollmentConfirmationEmail
            {
                StudentName = request.ToName ?? "Usuario de Prueba",
                StudentEmail = request.ToEmail,
                ActivityName = "Conferencia Magistral: Innovación Tecnológica",
                ActivityDescription = "Una conferencia sobre las últimas tendencias en tecnología e innovación.",
                ActivityDateTime = DateTime.Now.AddDays(7).AddHours(10),
                ActivityLocation = "Auditorio Principal - Campus Central UMG",
                QrToken = $"TEST_{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                QrCodeUrl = "https://congreso.umg.edu.gt/qr",
                EnrollmentId = new Random().Next(1000, 9999),
                AdditionalInfo = "Por favor llegar 15 minutos antes del inicio de la actividad."
            };

            var success = await _emailService.SendEnrollmentConfirmationAsync(confirmationData);

            if (success)
            {
                _logger.LogInformation("Email de confirmación de inscripción de prueba enviado exitosamente a {Email}", request.ToEmail);
                return Ok(new
                {
                    success = true,
                    message = "Email de confirmación de inscripción de prueba enviado exitosamente",
                    sentTo = request.ToEmail,
                    qrToken = confirmationData.QrToken,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al enviar email de confirmación de inscripción de prueba",
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email de confirmación de inscripción de prueba a {Email}", request.ToEmail);
            return StatusCode(500, new { message = "Error interno al enviar email", error = ex.Message });
        }
    }

    private string GenerateTestEmailHtml(string name)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Email de Prueba</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .test-info {{ background: #e8f4fd; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #667eea; }}
        .footer {{ text-align: center; margin-top: 30px; padding: 20px; color: #666; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🧪 Email de Prueba</h1>
        <p>Congreso Digital UMG</p>
    </div>
    
    <div class='content'>
        <h2>Hola {name},</h2>
        <p>Este es un email de prueba para verificar que la configuración SMTP está funcionando correctamente.</p>
        
        <div class='test-info'>
            <h3>✅ Información de la Prueba</h3>
            <p><strong>Fecha y Hora:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
            <p><strong>Sistema:</strong> Congreso Digital UMG</p>
            <p><strong>Propósito:</strong> Verificación de configuración SMTP</p>
        </div>
        
        <p>Si recibes este email, significa que:</p>
        <ul>
            <li>✅ La configuración SMTP está correcta</li>
            <li>✅ El servidor puede enviar emails</li>
            <li>✅ Los templates HTML funcionan correctamente</li>
            <li>✅ El sistema de logging está operativo</li>
        </ul>
        
        <p>¡El sistema de emails está listo para producción!</p>
    </div>
    
    <div class='footer'>
        <p>Congreso Digital UMG<br>
        Universidad Mariano Gálvez de Guatemala<br>
        Este es un email automático de prueba.</p>
    </div>
</body>
</html>";
    }

    private string GenerateTestEmailText(string name)
    {
        return $@"
EMAIL DE PRUEBA - CONGRESO DIGITAL UMG

Hola {name},

Este es un email de prueba para verificar que la configuración SMTP está funcionando correctamente.

INFORMACIÓN DE LA PRUEBA:
- Fecha y Hora: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
- Sistema: Congreso Digital UMG
- Propósito: Verificación de configuración SMTP

Si recibes este email, significa que:
✅ La configuración SMTP está correcta
✅ El servidor puede enviar emails
✅ Los templates HTML funcionan correctamente
✅ El sistema de logging está operativo

¡El sistema de emails está listo para producción!

---
Congreso Digital UMG
Universidad Mariano Gálvez de Guatemala
Este es un email automático de prueba.
";
    }

    /// <summary>
    /// Enviar email de prueba de confirmación de inscripción
    /// </summary>
    [HttpPost("test-enrollment-confirmation")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SendTestEnrollmentConfirmation()
    {
        try
        {
            var testData = new EnrollmentConfirmationEmail
            {
                StudentName = "Juan Pérez",
                StudentEmail = "juan.perez@test.com",
                ActivityName = "Conferencia de Inteligencia Artificial",
                ActivityDescription = "Una conferencia magistral sobre los últimos avances en IA y machine learning.",
                ActivityDateTime = DateTime.Now.AddDays(7),
                ActivityLocation = "Auditorio Principal - Campus Central",
                QrToken = "TEST-QR-" + Random.Shared.Next(100000, 999999),
                QrCodeUrl = "https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=TEST-QR-123456",
                EnrollmentId = Random.Shared.Next(1, 1000)
            };

            var result = await _emailService.SendEnrollmentConfirmationAsync(testData);

            if (result)
            {
                return Ok(new { 
                    message = "Email de confirmación de inscripción enviado exitosamente",
                    recipient = testData.StudentEmail,
                    activity = testData.ActivityName
                });
            }

            return BadRequest(new { message = "Error al enviar el email de confirmación de inscripción" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en test de email de confirmación de inscripción");
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    /// <summary>
    /// Enviar email de prueba de confirmación de múltiples actividades
    /// </summary>
    [HttpPost("test-multiple-activities-confirmation")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SendTestMultipleActivitiesConfirmation()
    {
        try
        {
            var testData = new MultipleActivitiesConfirmationEmail
            {
                StudentName = "María González",
                StudentEmail = "maria.gonzalez@test.com",
                DashboardUrl = "https://congreso.umg.edu.gt/dashboard",
                AdditionalInfo = "Recuerda traer tu identificación estudiantil para el registro.",
                ConfirmationDate = DateTime.UtcNow,
                Activities = new List<ActivityConfirmationDetail>
                {
                    new ActivityConfirmationDetail
                    {
                        ActivityId = 1,
                        ActivityName = "Conferencia Magistral: El Futuro de la Tecnología",
                        ActivityDescription = "Una visión integral sobre las tendencias tecnológicas que definirán la próxima década.",
                        ActivityDateTime = DateTime.Now.AddDays(5).AddHours(9),
                        ActivityLocation = "Auditorio Principal - Campus Central",
                        ActivityType = "Conferencia",
                        Duration = "2 horas",
                        Speaker = "Dr. Carlos Mendoza",
                        QrToken = "CONF-QR-" + Random.Shared.Next(100000, 999999),
                        QrCodeUrl = "https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=CONF-QR-123456",
                        EnrollmentId = Random.Shared.Next(1, 1000),
                        SpecialNotes = "Se requiere registro 15 minutos antes del inicio."
                    },
                    new ActivityConfirmationDetail
                    {
                        ActivityId = 2,
                        ActivityName = "Taller Práctico: Desarrollo con IA",
                        ActivityDescription = "Taller hands-on para aprender a integrar inteligencia artificial en aplicaciones web.",
                        ActivityDateTime = DateTime.Now.AddDays(6).AddHours(14),
                        ActivityLocation = "Laboratorio de Computación 3 - Edificio B",
                        ActivityType = "Taller",
                        Duration = "3 horas",
                        Speaker = "Ing. Ana Rodríguez",
                        QrToken = "TALL-QR-" + Random.Shared.Next(100000, 999999),
                        QrCodeUrl = "https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=TALL-QR-789012",
                        EnrollmentId = Random.Shared.Next(1, 1000),
                        RequiresAdditionalConfirmation = true
                    },
                    new ActivityConfirmationDetail
                    {
                        ActivityId = 3,
                        ActivityName = "Mesa Redonda: Innovación en Guatemala",
                        ActivityDescription = "Discusión sobre el ecosistema de innovación y emprendimiento en Guatemala.",
                        ActivityDateTime = DateTime.Now.AddDays(7).AddHours(16),
                        ActivityLocation = "Sala de Conferencias - Campus Central",
                        ActivityType = "Mesa Redonda",
                        Duration = "1.5 horas",
                        Speaker = "Panel de Expertos",
                        QrToken = "MESA-QR-" + Random.Shared.Next(100000, 999999),
                        QrCodeUrl = "https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=MESA-QR-345678",
                        EnrollmentId = Random.Shared.Next(1, 1000)
                    }
                }
            };

            var result = await _emailService.SendMultipleActivitiesConfirmationAsync(testData);

            if (result)
            {
                return Ok(new { 
                    message = "Email de confirmación de múltiples actividades enviado exitosamente",
                    recipient = testData.StudentEmail,
                    activitiesCount = testData.Activities.Count,
                    activities = testData.Activities.Select(a => a.ActivityName).ToList()
                });
            }

            return BadRequest(new { message = "Error al enviar el email de confirmación de múltiples actividades" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en test de email de confirmación de múltiples actividades");
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }
}

/// <summary>
/// Modelo para solicitud de email de prueba
/// </summary>
public class TestEmailRequest
{
    /// <summary>
    /// Email del destinatario
    /// </summary>
    [Required]
    [EmailAddress]
    public string ToEmail { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del destinatario (opcional)
    /// </summary>
    public string? ToName { get; set; }
}