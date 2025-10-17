using Congreso.Api.DTOs.Certificate;
using Congreso.Api.Services;
using System.Security.Claims;

namespace Congreso.Api.Infrastructure;

/// <summary>
/// Proveedor de plantillas HTML para certificados
/// </summary>
public interface ICertificateTemplateProvider
{
    /// <summary>
    /// Obtener plantilla HTML para un tipo de certificado específico
    /// </summary>
    Task<CertificateTemplate> GetTemplateAsync(CertificateType type);
    
    /// <summary>
    /// Obtener todas las plantillas disponibles
    /// </summary>
    Task<IEnumerable<CertificateTemplate>> GetAllTemplatesAsync();
    
    /// <summary>
    /// Validar si existe una plantilla para el tipo de certificado
    /// </summary>
    bool HasTemplate(CertificateType type);
}

/// <summary>
/// Implementación del proveedor de plantillas de certificados
/// </summary>
public class CertificateTemplateProvider : ICertificateTemplateProvider
{
    private readonly Dictionary<CertificateType, CertificateTemplate> _templates;
    private readonly ILogger<CertificateTemplateProvider> _logger;

    public CertificateTemplateProvider(ILogger<CertificateTemplateProvider> logger)
    {
        _logger = logger;
        _templates = InitializeTemplates();
    }

    public async Task<CertificateTemplate> GetTemplateAsync(CertificateType type)
    {
        if (_templates.TryGetValue(type, out var template))
        {
            _logger.LogInformation("Plantilla obtenida para tipo: {CertificateType}", type);
            return await Task.FromResult(template);
        }

        _logger.LogWarning("Plantilla no encontrada para tipo: {CertificateType}", type);
        throw new KeyNotFoundException($"Plantilla no encontrada para el tipo de certificado: {type}");
    }

    public async Task<IEnumerable<CertificateTemplate>> GetAllTemplatesAsync()
    {
        return await Task.FromResult(_templates.Values.ToList());
    }

    public bool HasTemplate(CertificateType type)
    {
        return _templates.ContainsKey(type);
    }

    private Dictionary<CertificateType, CertificateTemplate> InitializeTemplates()
    {
        var templates = new Dictionary<CertificateType, CertificateTemplate>
        {
            [CertificateType.Attendance] = new CertificateTemplate
            {
                Name = "Attendance Certificate",
                HtmlContent = GetAttendanceTemplate(),
                CssStyles = GetDefaultStyles(),
                Version = 1,
                Dimensions = (800, 600),
                IsActive = true
            },
            [CertificateType.Participation] = new CertificateTemplate
            {
                Name = "Participation Certificate",
                HtmlContent = GetParticipationTemplate(),
                CssStyles = GetDefaultStyles(),
                Version = 1,
                Dimensions = (800, 600),
                IsActive = true
            },
            [CertificateType.Speaker] = new CertificateTemplate
            {
                Name = "Speaker Certificate",
                HtmlContent = GetSpeakerTemplate(),
                CssStyles = GetDefaultStyles(),
                Version = 1,
                Dimensions = (800, 600),
                IsActive = true
            },
            [CertificateType.Organizer] = new CertificateTemplate
            {
                Name = "Organizer Certificate",
                HtmlContent = GetOrganizerTemplate(),
                CssStyles = GetDefaultStyles(),
                Version = 1,
                Dimensions = (800, 600),
                IsActive = true
            },
            [CertificateType.Winner] = new CertificateTemplate
            {
                Name = "Winner Certificate",
                HtmlContent = GetWinnerTemplate(),
                CssStyles = GetWinnerStyles(),
                Version = 1,
                Dimensions = (800, 600),
                IsActive = true
            }
        };

        _logger.LogInformation("Plantillas de certificados inicializadas: {Count}", templates.Count);
        return templates;
    }

    private string GetAttendanceTemplate()
    {
        return @"
        <!DOCTYPE html>
        <html lang='es'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Certificado de Asistencia</title>
            <style>
                {{CSS_STYLES}}
            </style>
        </head>
        <body>
            <div class='certificate-container'>
                <div class='certificate-header'>
                    <img src='https://trae-api-us.mchost.guru/api/ide/v1/text_to_image?prompt=Congreso%20Digital%20Logo%20-%20Modern%20Professional%20Technology%20Conference%20Emblem%20with%20Digital%20Elements&image_size=square' alt='Logo Congreso Digital' class='logo'>
                    <h1 class='certificate-title'>CERTIFICADO DE ASISTENCIA</h1>
                </div>
                
                <div class='certificate-body'>
                    <p class='certificate-text'>Se certifica que:</p>
                    <h2 class='recipient-name'>{{userName}}</h2>
                    <p class='certificate-text'>Asistió al evento:</p>
                    <h3 class='event-name'>{{eventName}}</h3>
                    <p class='certificate-text'>Realizado el día:</p>
                    <p class='event-date'>{{eventDate}}</p>
                    <p class='certificate-text'>Con una duración de:</p>
                    <p class='duration'>{{durationHours}} horas</p>
                    
                    {{#if additionalNotes}}
                    <div class='additional-notes'>
                        <p>{{additionalNotes}}</p>
                    </div>
                    {{/if}}
                </div>
                
                <div class='certificate-footer'>
                    <div class='signature-section'>
                        <div class='signature-line'></div>
                        <p class='signature-text'>Firma Autorizada</p>
                    </div>
                    <div class='verification-section'>
                        <p class='verification-code'>Código de Verificación: {{verificationCode}}</p>
                        <p class='validation-url'>Validar en: {{validationUrl}}</p>
                    </div>
                </div>
                
                <div class='certificate-hash'>
                    <p class='hash-text'>Hash: {{certificateHash}}</p>
                </div>
            </div>
        </body>
        </html>";
    }

    private string GetParticipationTemplate()
    {
        return @"
        <!DOCTYPE html>
        <html lang='es'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Certificado de Participación</title>
            <style>
                {{CSS_STYLES}}
            </style>
        </head>
        <body>
            <div class='certificate-container'>
                <div class='certificate-header'>
                    <img src='https://trae-api-us.mchost.guru/api/ide/v1/text_to_image?prompt=Congreso%20Digital%20Logo%20-%20Modern%20Professional%20Technology%20Conference%20Emblem%20with%20Digital%20Elements&image_size=square' alt='Logo Congreso Digital' class='logo'>
                    <h1 class='certificate-title'>CERTIFICADO DE PARTICIPACIÓN</h1>
                </div>
                
                <div class='certificate-body'>
                    <p class='certificate-text'>Se certifica que:</p>
                    <h2 class='recipient-name'>{{userName}}</h2>
                    <p class='certificate-text'>Participó como:</p>
                    <h3 class='participant-title'>{{participantTitle}}</h3>
                    <p class='certificate-text'>En el evento:</p>
                    <h3 class='event-name'>{{eventName}}</h3>
                    <p class='certificate-text'>Realizado el día:</p>
                    <p class='event-date'>{{eventDate}}</p>
                    
                    {{#if speakerName}}
                    <p class='certificate-text'>Ponente principal:</p>
                    <p class='speaker-name'>{{speakerName}}</p>
                    {{/if}}
                    
                    {{#if additionalNotes}}
                    <div class='additional-notes'>
                        <p>{{additionalNotes}}</p>
                    </div>
                    {{/if}}
                </div>
                
                <div class='certificate-footer'>
                    <div class='signature-section'>
                        <div class='signature-line'></div>
                        <p class='signature-text'>Firma Autorizada</p>
                    </div>
                    <div class='verification-section'>
                        <p class='verification-code'>Código de Verificación: {{verificationCode}}</p>
                        <p class='validation-url'>Validar en: {{validationUrl}}</p>
                    </div>
                </div>
                
                <div class='certificate-hash'>
                    <p class='hash-text'>Hash: {{certificateHash}}</p>
                </div>
            </div>
        </body>
        </html>";
    }

    private string GetSpeakerTemplate()
    {
        return @"
        <!DOCTYPE html>
        <html lang='es'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Certificado de Ponente</title>
            <style>
                {{CSS_STYLES}}
            </style>
        </head>
        <body>
            <div class='certificate-container'>
                <div class='certificate-header'>
                    <img src='https://trae-api-us.mchost.guru/api/ide/v1/text_to_image?prompt=Congreso%20Digital%20Logo%20-%20Modern%20Professional%20Technology%20Conference%20Emblem%20with%20Digital%20Elements&image_size=square' alt='Logo Congreso Digital' class='logo'>
                    <h1 class='certificate-title'>CERTIFICADO DE PONENTE</h1>
                </div>
                
                <div class='certificate-body'>
                    <p class='certificate-text'>Se certifica que:</p>
                    <h2 class='recipient-name'>{{userName}}</h2>
                    <p class='certificate-text'>Participó como ponente en:</p>
                    <h3 class='event-name'>{{eventName}}</h3>
                    <p class='certificate-text'>En calidad de:</p>
                    <h3 class='participant-title'>{{participantTitle}}</h3>
                    <p class='certificate-text'>Realizado el día:</p>
                    <p class='event-date'>{{eventDate}}</p>
                    
                    {{#if durationHours}}
                    <p class='certificate-text'>Con una duración de:</p>
                    <p class='duration'>{{durationHours}} horas</p>
                    {{/if}}
                    
                    {{#if additionalNotes}}
                    <div class='additional-notes'>
                        <p>{{additionalNotes}}</p>
                    </div>
                    {{/if}}
                </div>
                
                <div class='certificate-footer'>
                    <div class='signature-section'>
                        <div class='signature-line'></div>
                        <p class='signature-text'>Firma Autorizada</p>
                    </div>
                    <div class='verification-section'>
                        <p class='verification-code'>Código de Verificación: {{verificationCode}}</p>
                        <p class='validation-url'>Validar en: {{validationUrl}}</p>
                    </div>
                </div>
                
                <div class='certificate-hash'>
                    <p class='hash-text'>Hash: {{certificateHash}}</p>
                </div>
            </div>
        </body>
        </html>";
    }

    private string GetOrganizerTemplate()
    {
        return @"
        <!DOCTYPE html>
        <html lang='es'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Certificado de Organizador</title>
            <style>
                {{CSS_STYLES}}
            </style>
        </head>
        <body>
            <div class='certificate-container'>
                <div class='certificate-header'>
                    <img src='https://trae-api-us.mchost.guru/api/ide/v1/text_to_image?prompt=Congreso%20Digital%20Logo%20-%20Modern%20Professional%20Technology%20Conference%20Emblem%20with%20Digital%20Elements&image_size=square' alt='Logo Congreso Digital' class='logo'>
                    <h1 class='certificate-title'>CERTIFICADO DE ORGANIZADOR</h1>
                </div>
                
                <div class='certificate-body'>
                    <p class='certificate-text'>Se certifica que:</p>
                    <h2 class='recipient-name'>{{userName}}</h2>
                    <p class='certificate-text'>Participó como organizador en:</p>
                    <h3 class='event-name'>{{eventName}}</h3>
                    <p class='certificate-text'>En calidad de:</p>
                    <h3 class='participant-title'>{{participantTitle}}</h3>
                    <p class='certificate-text'>Realizado el día:</p>
                    <p class='event-date'>{{eventDate}}</p>
                    
                    {{#if durationHours}}
                    <p class='certificate-text'>Con una duración de:</p>
                    <p class='duration'>{{durationHours}} horas</p>
                    {{/if}}
                    
                    {{#if additionalNotes}}
                    <div class='additional-notes'>
                        <p>{{additionalNotes}}</p>
                    </div>
                    {{/if}}
                </div>
                
                <div class='certificate-footer'>
                    <div class='signature-section'>
                        <div class='signature-line'></div>
                        <p class='signature-text'>Firma Autorizada</p>
                    </div>
                    <div class='verification-section'>
                        <p class='verification-code'>Código de Verificación: {{verificationCode}}</p>
                        <p class='validation-url'>Validar en: {{validationUrl}}</p>
                    </div>
                </div>
                
                <div class='certificate-hash'>
                    <p class='hash-text'>Hash: {{certificateHash}}</p>
                </div>
            </div>
        </body>
        </html>";
    }

    private string GetWinnerTemplate()
    {
        return @"
        <!DOCTYPE html>
        <html lang='es'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Certificado de Ganador</title>
            <style>
                {{CSS_STYLES}}
            </style>
        </head>
        <body>
            <div class='certificate-container winner-certificate'>
                <div class='certificate-header'>
                    <img src='https://trae-api-us.mchost.guru/api/ide/v1/text_to_image?prompt=Congreso%20Digital%20Logo%20-%20Modern%20Professional%20Technology%20Conference%20Emblem%20with%20Digital%20Elements&image_size=square' alt='Logo Congreso Digital' class='logo'>
                    <h1 class='certificate-title'>CERTIFICADO DE GANADOR</h1>
                    <div class='winner-badge'>🏆</div>
                </div>
                
                <div class='certificate-body'>
                    <p class='certificate-text'>Se certifica que:</p>
                    <h2 class='recipient-name'>{{userName}}</h2>
                    <p class='certificate-text'>Ha sido ganador en:</p>
                    <h3 class='event-name'>{{eventName}}</h3>
                    <p class='certificate-text'>En la categoría:</p>
                    <h3 class='participant-title'>{{participantTitle}}</h3>
                    <p class='certificate-text'>Realizado el día:</p>
                    <p class='event-date'>{{eventDate}}</p>
                    
                    {{#if additionalNotes}}
                    <div class='additional-notes winner-notes'>
                        <p>{{additionalNotes}}</p>
                    </div>
                    {{/if}}
                </div>
                
                <div class='certificate-footer'>
                    <div class='signature-section'>
                        <div class='signature-line'></div>
                        <p class='signature-text'>Firma Autorizada</p>
                    </div>
                    <div class='verification-section'>
                        <p class='verification-code'>Código de Verificación: {{verificationCode}}</p>
                        <p class='validation-url'>Validar en: {{validationUrl}}</p>
                    </div>
                </div>
                
                <div class='certificate-hash'>
                    <p class='hash-text'>Hash: {{certificateHash}}</p>
                </div>
            </div>
        </body>
        </html>";
    }

    private string GetDefaultStyles()
    {
        return @"
            * {
                margin: 0;
                padding: 0;
                box-sizing: border-box;
            }
            
            body {
                font-family: 'Arial', sans-serif;
                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                min-height: 100vh;
                padding: 20px;
            }
            
            .certificate-container {
                max-width: 800px;
                margin: 0 auto;
                background: white;
                border-radius: 15px;
                box-shadow: 0 20px 40px rgba(0,0,0,0.1);
                overflow: hidden;
                position: relative;
            }
            
            .certificate-container::before {
                content: '';
                position: absolute;
                top: 0;
                left: 0;
                right: 0;
                height: 5px;
                background: linear-gradient(90deg, #667eea, #764ba2);
            }
            
            .certificate-header {
                text-align: center;
                padding: 40px 20px 20px;
                background: linear-gradient(135deg, #f8f9ff 0%, #e8f0ff 100%);
                position: relative;
            }
            
            .logo {
                width: 80px;
                height: 80px;
                margin-bottom: 20px;
                border-radius: 50%;
                object-fit: cover;
            }
            
            .certificate-title {
                color: #2c3e50;
                font-size: 2.5em;
                font-weight: bold;
                margin-bottom: 10px;
                text-shadow: 2px 2px 4px rgba(0,0,0,0.1);
            }
            
            .certificate-body {
                padding: 40px;
                text-align: center;
            }
            
            .certificate-text {
                font-size: 1.2em;
                color: #555;
                margin-bottom: 15px;
                line-height: 1.6;
            }
            
            .recipient-name {
                color: #2c3e50;
                font-size: 2.2em;
                font-weight: bold;
                margin: 20px 0;
                padding: 15px;
                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                color: white;
                border-radius: 10px;
                text-shadow: 1px 1px 2px rgba(0,0,0,0.3);
            }
            
            .event-name {
                color: #34495e;
                font-size: 1.8em;
                font-weight: 600;
                margin: 15px 0;
                padding: 10px;
                border-left: 4px solid #667eea;
                background: #f8f9ff;
                border-radius: 0 10px 10px 0;
            }
            
            .participant-title {
                color: #e74c3c;
                font-size: 1.5em;
                font-weight: 600;
                margin: 15px 0;
                padding: 10px;
                background: #fff5f5;
                border-radius: 10px;
                border: 2px solid #e74c3c;
            }
            
            .event-date, .duration, .speaker-name {
                font-size: 1.3em;
                color: #2c3e50;
                font-weight: 500;
                margin: 10px 0;
                padding: 8px;
                background: #f0f8ff;
                border-radius: 8px;
                display: inline-block;
            }
            
            .additional-notes {
                margin-top: 30px;
                padding: 20px;
                background: #f8f9fa;
                border-radius: 10px;
                border-left: 4px solid #28a745;
            }
            
            .additional-notes p {
                color: #495057;
                font-style: italic;
                line-height: 1.5;
            }
            
            .certificate-footer {
                display: flex;
                justify-content: space-between;
                align-items: flex-end;
                padding: 30px 40px;
                background: #f8f9ff;
                border-top: 1px solid #e9ecef;
            }
            
            .signature-section {
                text-align: center;
                flex: 1;
            }
            
            .signature-line {
                width: 200px;
                height: 2px;
                background: #333;
                margin: 0 auto 10px;
            }
            
            .signature-text {
                font-size: 0.9em;
                color: #666;
                font-weight: 500;
            }
            
            .verification-section {
                text-align: right;
                flex: 1;
            }
            
            .verification-code {
                font-family: 'Courier New', monospace;
                font-size: 1em;
                color: #2c3e50;
                font-weight: bold;
                background: #e8f4fd;
                padding: 8px 12px;
                border-radius: 6px;
                display: inline-block;
                margin-bottom: 5px;
            }
            
            .validation-url {
                font-size: 0.8em;
                color: #666;
                margin-top: 5px;
            }
            
            .certificate-hash {
                text-align: center;
                padding: 15px;
                background: #f1f3f4;
                border-top: 1px solid #dee2e6;
            }
            
            .hash-text {
                font-family: 'Courier New', monospace;
                font-size: 0.7em;
                color: #6c757d;
                word-break: break-all;
            }
            
            @media print {
                body {
                    background: white;
                    padding: 0;
                }
                
                .certificate-container {
                    box-shadow: none;
                    border: 2px solid #333;
                }
            }
            
            @media (max-width: 768px) {
                .certificate-title {
                    font-size: 2em;
                }
                
                .recipient-name {
                    font-size: 1.8em;
                }
                
                .event-name {
                    font-size: 1.5em;
                }
                
                .certificate-footer {
                    flex-direction: column;
                    gap: 20px;
                }
                
                .verification-section {
                    text-align: center;
                }
            }";
    }

    private string GetWinnerStyles()
    {
        var defaultStyles = GetDefaultStyles();
        var winnerStyles = @"
            .winner-certificate {
                background: linear-gradient(135deg, #ffd700 0%, #ffed4e 100%);
            }
            
            .winner-certificate .certificate-header {
                background: linear-gradient(135deg, #fff8dc 0%, #fffacd 100%);
                position: relative;
            }
            
            .winner-badge {
                position: absolute;
                top: 20px;
                right: 30px;
                font-size: 3em;
                animation: bounce 2s infinite;
            }
            
            @keyframes bounce {
                0%, 20%, 50%, 80%, 100% {
                    transform: translateY(0);
                }
                40% {
                    transform: translateY(-10px);
                }
                60% {
                    transform: translateY(-5px);
                }
            }
            
            .winner-certificate .recipient-name {
                background: linear-gradient(135deg, #ffd700 0%, #ffed4e 100%);
                color: #8b4513;
                text-shadow: 2px 2px 4px rgba(0,0,0,0.2);
            }
            
            .winner-certificate .participant-title {
                background: #fff8dc;
                border-color: #ffd700;
                color: #b8860b;
            }
            
            .winner-certificate .event-name {
                border-left-color: #ffd700;
                background: #fffef7;
            }
            
            .winner-certificate .event-date {
                background: #fffacd;
            }
            
            .winner-notes {
                background: linear-gradient(135deg, #fff8dc 0%, #fffacd 100%) !important;
                border-left-color: #ffd700 !important;
            }
            
            .winner-certificate .certificate-footer {
                background: linear-gradient(135deg, #fff8dc 0%, #fffacd 100%);
            }
            
            .winner-certificate .verification-code {
                background: #fffacd;
                color: #b8860b;
            }";
        
        return defaultStyles + "\n" + winnerStyles;
    }
}