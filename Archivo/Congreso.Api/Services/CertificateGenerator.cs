using Congreso.Api.DTOs.Certificate;
using Congreso.Api.Infrastructure;
using System.Security.Cryptography;
using System.Text;

namespace Congreso.Api.Services;

/// <summary>
/// Generador de certificados PDF
/// </summary>
public class CertificateGenerator : ICertificateGenerator
{
    private readonly ICertificateTemplateProvider _templateProvider;
    private readonly ILogger<CertificateGenerator> _logger;

    public CertificateGenerator(
        ICertificateTemplateProvider templateProvider,
        ILogger<CertificateGenerator> logger)
    {
        _templateProvider = templateProvider;
        _logger = logger;
    }

    public async Task<byte[]> GenerateCertificatePdfAsync(
        GenerateCertificateRequest request, 
        UserCertificateData userData, 
        CertificateContext context)
    {
        _logger.LogInformation("Generando certificado PDF para usuario {UserId}, tipo {CertificateType}", 
            userData.UserId, request.Type);

        try
        {
            // Obtener la plantilla
            var template = await _templateProvider.GetTemplateAsync(request.Type);
            
            // Generar hash y código de verificación
            var timestamp = DateTime.UtcNow;
            var certificateHash = GenerateCertificateHash(userData.UserId, request.Type, timestamp);
            var verificationCode = GenerateVerificationCode();

            // Preparar datos para la plantilla
            var templateData = new Dictionary<string, object>
            {
                ["userName"] = userData.FullName,
                ["userEmail"] = userData.Email,
                ["eventName"] = context.EventName,
                ["eventDate"] = context.EventDate.HasValue ? context.EventDate.Value.ToString("dd/MM/yyyy") : string.Empty,
                ["certificateHash"] = certificateHash,
                ["verificationCode"] = verificationCode,
                ["issuedAt"] = timestamp.ToString("dd/MM/yyyy HH:mm"),
                ["validationUrl"] = $"https://congreso-digital.com/validate/{certificateHash}"
            };

            // Agregar datos específicos según el tipo
            AddCertificateSpecificData(templateData, request, userData, context);

            // Renderizar HTML
            var htmlContent = RenderTemplate(template.HtmlContent, templateData);
            
            // Generar PDF (simulado por ahora - en producción usar ReportGenerator)
            var pdfBytes = await GeneratePdfFromHtml(htmlContent, template.CssStyles);

            _logger.LogInformation("Certificado PDF generado exitosamente para usuario {UserId}", userData.UserId);
            
            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar certificado PDF para usuario {UserId}", userData.UserId);
            throw new InvalidOperationException("Error al generar el certificado PDF", ex);
        }
    }

    public string GenerateCertificateHash(Guid userId, CertificateType certificateType, DateTime timestamp)
    {
        var data = $"{userId}:{certificateType}:{timestamp:yyyyMMddHHmmss}:{Guid.NewGuid()}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public string GenerateVerificationCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public async Task<CertificateTemplate> GetTemplateAsync(CertificateType certificateType)
    {
        return await _templateProvider.GetTemplateAsync(certificateType);
    }

    public async Task<bool> ValidateCertificateIntegrityAsync(byte[] pdfBytes, string expectedHash)
    {
        // En una implementación real, esto verificaría la firma digital o hash del PDF
        // Por ahora, simulamos la validación
        await Task.Delay(1); // Evitar advertencia de async sin await
        return true;
    }

    private void AddCertificateSpecificData(
        Dictionary<string, object> templateData, 
        GenerateCertificateRequest request, 
        UserCertificateData userData, 
        CertificateContext context)
    {
        switch (request.Type)
        {
            case CertificateType.Attendance:
                templateData["durationHours"] = context.DurationHours ?? 0;
                templateData["additionalNotes"] = context.AdditionalNotes ?? string.Empty;
                break;

            case CertificateType.Participation:
                templateData["participantTitle"] = context.ParticipantTitle ?? "Participante";
                break;

            case CertificateType.Speaker:
                templateData["speakerName"] = context.SpeakerName ?? userData.FullName;
                templateData["speakerTopic"] = context.SpeakerTopic ?? string.Empty;
                break;

            case CertificateType.Organizer:
                templateData["organizerRole"] = context.OrganizerRole ?? "Organizador";
                break;

            case CertificateType.Winner:
                templateData["winnerCategory"] = context.WinnerCategory ?? string.Empty;
                templateData["winnerPosition"] = context.WinnerPosition ?? string.Empty;
                break;
        }
    }

    private string RenderTemplate(string template, Dictionary<string, object> data)
    {
        var result = template;
        foreach (var kvp in data)
        {
            result = result.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString() ?? string.Empty);
        }
        return result;
    }

    private async Task<byte[]> GeneratePdfFromHtml(string htmlContent, string cssStyles)
    {
        // En producción, esto usaría una biblioteca real como DinkToPdf, PdfSharp, o ReportGenerator
        // Por ahora, retornamos un PDF simulado
        await Task.Delay(1); // Evitar advertencia de async sin await
        
        // Simular un PDF básico con contenido HTML como metadatos
        var htmlWithMetadata = $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="UTF-8">
                <title>Certificado</title>
                <style>{cssStyles}</style>
            </head>
            <body>
                {htmlContent}
            </body>
            </html>
            """;

        // Convertir a bytes (simulando PDF)
        return Encoding.UTF8.GetBytes(htmlWithMetadata);
    }
}