using System.Security.Cryptography;
using System.Text;
using Congreso.Api.DTOs.Certificate;
using Npgsql;

namespace Congreso.Api.Services;

/// <summary>
/// Implementación del servicio de certificados
/// </summary>
public class CertificatesService : ICertificatesService
{
    private readonly ICertificateRepository _repository;
    private readonly IEligibilityValidator _eligibilityValidator;
    private readonly ICertificateGenerator _certificateGenerator;
    private readonly IAuditService _auditService;
    private readonly Congreso.Api.Infrastructure.MetricsCollector _metricsCollector;
    private readonly ILogger<CertificatesService> _logger;
    private readonly NpgsqlDataSource _dataSource;

    public CertificatesService(
        ICertificateRepository repository,
        IEligibilityValidator eligibilityValidator,
        ICertificateGenerator certificateGenerator,
        IAuditService auditService,
        Congreso.Api.Infrastructure.MetricsCollector metricsCollector,
        ILogger<CertificatesService> logger,
        NpgsqlDataSource dataSource)
    {
        _repository = repository;
        _eligibilityValidator = eligibilityValidator;
        _certificateGenerator = certificateGenerator;
        _auditService = auditService;
        _metricsCollector = metricsCollector;
        _logger = logger;
        _dataSource = dataSource;
    }

    public async Task<CertificateResponse> GenerateCertificateAsync(GenerateCertificateRequest request, Guid userId)
    {
        try
        {
            _logger.LogInformation("Iniciando generación de certificado para usuario {UserId}, tipo {CertificateType}", 
                request.UserId, request.Type);

            // Validar elegibilidad
            var eligibilityResult = await _eligibilityValidator.ValidateEligibilityAsync(
                request.UserId, request.Type, request.ActivityId, request.EnrollmentId);

            if (!eligibilityResult.IsEligible)
            {
                await _auditService.LogFailedGenerationAttemptAsync(request.UserId, request.Type.ToString(), 
                    eligibilityResult.Message, eligibilityResult.Details);
                
                throw new InvalidOperationException($"Usuario no elegible para certificado: {eligibilityResult.Message}");
            }

            // Verificar certificado duplicado
            var hasExisting = await _eligibilityValidator.HasExistingCertificateAsync(
                request.UserId, request.Type, request.ActivityId);

            if (hasExisting)
            {
                await _auditService.LogFailedGenerationAttemptAsync(request.UserId, request.Type.ToString(), 
                    "Certificado duplicado");
                
                throw new InvalidOperationException("El usuario ya posee un certificado de este tipo");
            }

            // Obtener datos del usuario
            var userData = await GetUserCertificateDataAsync(request.UserId);
            if (userData == null)
            {
                throw new InvalidOperationException($"Usuario con ID {request.UserId} no encontrado");
            }

            // Generar contexto si no existe
            var context = request.Context ?? new CertificateContext
            {
                EventName = await GetActivityNameAsync(request.ActivityId),
                VerificationCode = _certificateGenerator.GenerateVerificationCode()
            };

            // Generar PDF
            var pdfBytes = await _certificateGenerator.GenerateCertificatePdfAsync(request, userData, context);
            
            // Generar hash único
            var hash = _certificateGenerator.GenerateCertificateHash(request.UserId, request.Type, DateTime.UtcNow);
            
            // Guardar archivo
            var filePath = await SaveCertificateFileAsync(pdfBytes, hash);

            // Crear registro en base de datos
            var certificate = new CertificateRecord
            {
                Hash = hash,
                VerificationCode = context.VerificationCode ?? _certificateGenerator.GenerateVerificationCode(),
                Type = request.Type,
                Status = CertificateStatus.Active,
                UserId = request.UserId,
                DownloadUrl = filePath,
                IssuedAt = DateTime.UtcNow,
                ActivityId = request.ActivityId,
                EnrollmentId = request.EnrollmentId,
                Metadata = SerializeMetadata(context, request),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdCertificate = await _repository.CreateCertificateAsync(certificate);

            // Auditoría
            await _auditService.LogCertificateGenerationAsync(createdCertificate.Id, userId, new Dictionary<string, object>
            {
                ["Type"] = request.Type.ToString(),
                ["ActivityId"] = request.ActivityId,
                ["EnrollmentId"] = request.EnrollmentId
            });

            _metricsCollector.RecordCertificateGeneration(0, "certificate", true);

            return MapToResponse(createdCertificate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar certificado para usuario {UserId}", request.UserId);
            _metricsCollector.RecordCertificateGeneration(0, "certificate", false);
            throw;
        }
    }

    public async Task<PaginatedResult<CertificateResponse>> GetUserCertificatesAsync(Guid userId, int pageNumber = 1, int pageSize = 10, bool includeRevoked = false)
    {
        try
        {
            var certificates = await _repository.GetUserCertificatesAsync(userId, includeRevoked, pageNumber, pageSize);
            var totalCount = await _repository.CountUserCertificatesAsync(userId, includeRevoked);

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new PaginatedResult<CertificateResponse>
            {
                Items = certificates.Select(MapToResponse).ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = pageNumber < totalPages,
                HasPreviousPage = pageNumber > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener certificados del usuario {UserId}", userId);
            throw;
        }
    }

    public async Task<CertificateValidationResult> ValidateCertificateAsync(string hash, bool includeMetadata = true)
    {
        try
        {
            var certificate = await _repository.GetCertificateByHashAsync(hash);
            
            if (certificate == null)
            {
                await _auditService.LogCertificateValidationAsync(0, null, false, new Dictionary<string, object>
                {
                    ["Hash"] = hash,
                    ["Reason"] = "Certificado no encontrado"
                });

                return new CertificateValidationResult
                {
                    IsValid = false,
                    Message = "Certificado no encontrado",
                    Details = new Dictionary<string, object> { ["hash"] = hash }
                };
            }

            var isValid = certificate.Status == CertificateStatus.Active &&
                         (!certificate.ExpiresAt.HasValue || certificate.ExpiresAt.Value > DateTime.UtcNow);

            var result = new CertificateValidationResult
            {
                IsValid = isValid,
                CertificateId = certificate.Id,
                Type = certificate.Type,
                UserId = certificate.UserId,
                IssuedAt = certificate.IssuedAt,
                ExpiresAt = certificate.ExpiresAt,
                Status = certificate.Status,
                Message = isValid ? "Certificado válido" : GetInvalidCertificateMessage(certificate),
                Details = new Dictionary<string, object>
                {
                    ["Hash"] = certificate.Hash,
                    ["VerificationCode"] = certificate.VerificationCode,
                    ["DownloadUrl"] = certificate.DownloadUrl
                }
            };

            if (includeMetadata && !string.IsNullOrEmpty(certificate.Metadata))
            {
                result.Metadata = DeserializeMetadata(certificate.Metadata);
            }

            await _auditService.LogCertificateValidationAsync(certificate.Id, null, isValid, new Dictionary<string, object>
            {
                ["Status"] = certificate.Status.ToString(),
                ["IsExpired"] = certificate.ExpiresAt.HasValue && certificate.ExpiresAt.Value <= DateTime.UtcNow
            });

            _metricsCollector.RecordCertificateValidation(isValid, "certificate");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar certificado con hash {Hash}", hash);
            _metricsCollector.RecordCertificateValidation(false, "certificate");
            throw;
        }
    }

    public async Task<bool> RevokeCertificateAsync(long certificateId, string reason, Guid userId)
    {
        try
        {
            var certificate = await _repository.GetCertificateByIdAsync(certificateId);
            if (certificate == null)
            {
                throw new InvalidOperationException($"Certificado con ID {certificateId} no encontrado");
            }

            if (certificate.Status == CertificateStatus.Revoked)
            {
                throw new InvalidOperationException("El certificado ya está revocado");
            }

            var updated = await _repository.UpdateCertificateStatusAsync(certificateId, CertificateStatus.Revoked, reason);
            
            if (updated)
            {
                await _auditService.LogCertificateRevocationAsync(certificateId, userId, reason);
            }

            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al revocar certificado {CertificateId}", certificateId);
            throw;
        }
    }

    public async Task<CertificateResponse?> GetCertificateByIdAsync(long certificateId)
    {
        try
        {
            var certificate = await _repository.GetCertificateByIdAsync(certificateId);
            return certificate != null ? MapToResponse(certificate) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener certificado por ID {CertificateId}", certificateId);
            throw;
        }
    }

    public async Task<CertificateResponse> ReissueCertificateAsync(long originalCertificateId, Guid userId)
    {
        try
        {
            var originalCertificate = await _repository.GetCertificateByIdAsync(originalCertificateId);
            if (originalCertificate == null)
            {
                throw new InvalidOperationException($"Certificado original con ID {originalCertificateId} no encontrado");
            }

            if (originalCertificate.Status != CertificateStatus.Revoked && originalCertificate.Status != CertificateStatus.Expired)
            {
                throw new InvalidOperationException("Solo se pueden reemitir certificados revocados o expirados");
            }

            // Crear nueva solicitud con los mismos datos
            var request = new GenerateCertificateRequest
            {
                UserId = originalCertificate.UserId,
                Type = originalCertificate.Type,
                ActivityId = originalCertificate.ActivityId,
                EnrollmentId = originalCertificate.EnrollmentId
            };

            // Obtener contexto del certificado original
            if (!string.IsNullOrEmpty(originalCertificate.Metadata))
            {
                var originalMetadata = DeserializeMetadata(originalCertificate.Metadata);
                request.Context = new CertificateContext
                {
                    EventName = originalMetadata?.EventName,
                    EventDate = originalMetadata?.EventDate,
                    DurationHours = originalMetadata?.DurationHours,
                    SpeakerName = originalMetadata?.InstructorName,
                    ParticipantTitle = originalMetadata?.ParticipantTitle
                };
            }

            // Generar nuevo certificado
            var newCertificate = await GenerateCertificateAsync(request, userId);

            // Registrar auditoría de reemisión
            await _auditService.LogCertificateReissueAsync(originalCertificateId, newCertificate.Id, userId, 
                "Reemisión automática");

            return newCertificate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reemitir certificado {OriginalCertificateId}", originalCertificateId);
            throw;
        }
    }

    private async Task<UserCertificateData?> GetUserCertificateDataAsync(Guid userId)
    {
        const string sql = @"
            SELECT u.id, u.email, p.full_name, p.student_id, p.career, ur.role_name
            FROM users u
            LEFT JOIN profiles p ON u.id = p.user_id
            LEFT JOIN user_roles ur ON u.id = ur.user_id
            WHERE u.id = @userId
            LIMIT 1;";

        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("@userId", userId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new UserCertificateData
        {
            UserId = userId,
            FullName = reader.IsDBNull(2) ? "Usuario" : reader.GetString(2),
            Email = reader.GetString(1),
            StudentId = reader.IsDBNull(3) ? null : reader.GetString(3),
            Career = reader.IsDBNull(4) ? null : reader.GetString(4),
            Role = reader.IsDBNull(5) ? "ESTUDIANTE" : reader.GetString(5),
            RegistrationDate = DateTime.UtcNow // TODO: Obtener fecha real de registro
        };
    }

    private async Task<string?> GetActivityNameAsync(int? activityId)
    {
        if (!activityId.HasValue) return null;

        const string sql = "SELECT name FROM activities WHERE id = @activityId LIMIT 1;";
        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("@activityId", activityId.Value);

        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString();
    }

    private async Task<string> SaveCertificateFileAsync(byte[] pdfBytes, string hash)
    {
        var fileName = $"certificate_{hash}.pdf";
        var filePath = Path.Combine("wwwroot", "certificates", fileName);
        
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllBytesAsync(filePath, pdfBytes);
        
        return $"/certificates/{fileName}";
    }

    private CertificateResponse MapToResponse(CertificateRecord certificate)
    {
        return new CertificateResponse
        {
            Id = certificate.Id,
            Hash = certificate.Hash,
            VerificationCode = certificate.VerificationCode,
            Type = certificate.Type,
            Status = certificate.Status,
            UserId = certificate.UserId,
            DownloadUrl = certificate.DownloadUrl,
            ValidationUrl = $"/api/certificates/validate/{certificate.Hash}",
            IssuedAt = certificate.IssuedAt,
            ExpiresAt = certificate.ExpiresAt,
            Metadata = !string.IsNullOrEmpty(certificate.Metadata) ? 
                DeserializeMetadata(certificate.Metadata) : null
        };
    }

    private string SerializeMetadata(CertificateContext context, GenerateCertificateRequest request)
    {
        var metadata = new CertificateMetadata
        {
            EventName = context?.EventName,
            EventDate = context?.EventDate,
            DurationHours = context?.DurationHours,
            InstructorName = context?.SpeakerName,
            ParticipantTitle = context?.ParticipantTitle,
            ActivityId = request.ActivityId,
            EnrollmentId = request.EnrollmentId
        };

        return System.Text.Json.JsonSerializer.Serialize(metadata);
    }
    private CertificateMetadata? DeserializeMetadata(string json)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<CertificateMetadata>(json);
        }
        catch
        {
            return null;
        }
    }

    private string GetInvalidCertificateMessage(CertificateRecord certificate)
    {
        return certificate.Status switch
        {
            CertificateStatus.Revoked => "Certificado revocado",
            CertificateStatus.Expired => "Certificado expirado",
            _ => certificate.ExpiresAt.HasValue && certificate.ExpiresAt.Value <= DateTime.UtcNow 
                ? "Certificado expirado" 
                : "Certificado inválido"
        };
    }

    public async Task<CertificateStatistics> GetStatisticsAsync()
    {
        try
        {
            const string sql = @"
                SELECT 
                    COUNT(*) AS total,
                    COUNT(CASE WHEN status = 'Active' THEN 1 END) AS active,
                    COUNT(CASE WHEN status = 'Revoked' THEN 1 END) AS revoked,
                    COUNT(CASE WHEN status = 'Expired' THEN 1 END) AS expired,
                    COUNT(CASE WHEN issued_at >= NOW() - INTERVAL '30 days' THEN 1 END) AS last30,
                    COUNT(DISTINCT user_id) AS users_with
                FROM certificates;";

            await using var cmd = _dataSource.CreateCommand(sql);
            await using var reader = await cmd.ExecuteReaderAsync();

            var stats = new CertificateStatistics();
            if (await reader.ReadAsync())
            {
                stats.TotalCertificates = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                stats.ActiveCertificates = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                stats.RevokedCertificates = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                stats.ExpiredCertificates = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                stats.CertificatesLast30Days = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                stats.UsersWithCertificates = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
            }

            // Por tipo
            const string byTypeSql = @"SELECT type, COUNT(*) FROM certificates GROUP BY type;";
            await using var byTypeCmd = _dataSource.CreateCommand(byTypeSql);
            await using var byTypeReader = await byTypeCmd.ExecuteReaderAsync();
            while (await byTypeReader.ReadAsync())
            {
                var typeStr = byTypeReader.GetString(0);
                var count = byTypeReader.GetInt32(1);
                if (Enum.TryParse<CertificateType>(typeStr, out var type))
                {
                    stats.CertificatesByType[type] = count;
                }
            }

            // Promedio por usuario
            stats.AverageCertificatesPerUser = stats.UsersWithCertificates > 0
                ? Math.Round((double)stats.TotalCertificates / stats.UsersWithCertificates, 2)
                : 0.0;

            stats.RevocationRate = stats.TotalCertificates > 0
                ? Math.Round((double)stats.RevokedCertificates / stats.TotalCertificates * 100, 2)
                : 0.0;

            stats.GeneratedAt = DateTime.UtcNow;
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas de certificados");
            throw;
        }
    }
}