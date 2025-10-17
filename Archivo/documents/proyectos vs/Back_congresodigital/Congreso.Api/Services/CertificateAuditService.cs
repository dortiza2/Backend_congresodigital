using Congreso.Api.DTOs.Certificate;
using Npgsql;
using System.Text.Json;

namespace Congreso.Api.Services;

/// <summary>
/// Implementación del servicio de auditoría para certificados
/// </summary>
public class CertificateAuditService : IAuditService
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly Microsoft.Extensions.Logging.ILogger<CertificateAuditService> _logger;

    public CertificateAuditService(NpgsqlDataSource dataSource, Microsoft.Extensions.Logging.ILogger<CertificateAuditService> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task LogCertificateGenerationAsync(long certificateId, Guid userId, Dictionary<string, object>? details = null)
    {
        await LogAuditEntryAsync("CERTIFICATE_GENERATION", userId, certificateId, null, details ?? new Dictionary<string, object>());
    }

    public async Task LogCertificateValidationAsync(long certificateId, Guid? userId, bool isValid, Dictionary<string, object>? details = null)
    {
        var auditDetails = details ?? new Dictionary<string, object>();
        auditDetails["isValid"] = isValid;
        
        await LogAuditEntryAsync("CERTIFICATE_VALIDATION", userId, certificateId, null, auditDetails);
    }

    public async Task LogCertificateRevocationAsync(long certificateId, Guid userId, string reason, Dictionary<string, object>? details = null)
    {
        var auditDetails = details ?? new Dictionary<string, object>();
        auditDetails["reason"] = reason;
        
        await LogAuditEntryAsync("CERTIFICATE_REVOCATION", userId, certificateId, null, auditDetails);
    }

    public async Task LogCertificateReissueAsync(long originalCertificateId, long newCertificateId, Guid userId, string? reason = null)
    {
        var details = new Dictionary<string, object>
        {
            ["originalCertificateId"] = originalCertificateId,
            ["reason"] = reason ?? string.Empty
        };
        
        await LogAuditEntryAsync("CERTIFICATE_REISSUANCE", userId, newCertificateId, null, details);
    }

    public async Task LogFailedGenerationAttemptAsync(Guid userId, string certificateType, string reason, Dictionary<string, object>? details = null)
    {
        var auditDetails = details ?? new Dictionary<string, object>();
        auditDetails["certificateType"] = certificateType;
        auditDetails["reason"] = reason;
        
        await LogAuditEntryAsync("FAILED_GENERATION_ATTEMPT", userId, null, null, auditDetails);
    }

    public async Task LogPodiumActionAsync(Guid userId, int year, int place, string action, Dictionary<string, object>? details = null)
    {
        var auditDetails = details ?? new Dictionary<string, object>();
        auditDetails["year"] = year;
        auditDetails["place"] = place;
        auditDetails["action"] = action;
        
        await LogAuditEntryAsync("PODIUM_ACTION", userId, null, null, auditDetails);
    }

    public async Task LogPodiumViewAsync(int year, string? userAgent, string? ipAddress, bool cacheHit, Dictionary<string, object>? details = null)
    {
        var auditDetails = details ?? new Dictionary<string, object>();
        auditDetails["year"] = year;
        auditDetails["cacheHit"] = cacheHit;
        auditDetails["userAgent"] = userAgent ?? string.Empty;
        auditDetails["ipAddress"] = ipAddress ?? string.Empty;
        
        await LogAuditEntryAsync("PODIUM_VIEW", null, null, null, auditDetails);
    }

    public async Task<List<AuditLogEntry>> GetCertificateAuditHistoryAsync(long certificateId, int limit = 50)
    {
        var sql = @"
            SELECT id, action_type, user_id, certificate_id, certificate_type, 
                   details, created_at, ip_address, user_agent
            FROM certificate_audit_log 
            WHERE certificate_id = @certificateId
            ORDER BY created_at DESC LIMIT @limit;";

        var parameters = new Dictionary<string, object>();
        parameters["@certificateId"] = certificateId;
        parameters["@limit"] = limit;

        try
        {
            await using var cmd = _dataSource.CreateCommand(sql);
            foreach (var param in parameters)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value);
            }

            var auditEntries = new List<AuditLogEntry>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                auditEntries.Add(MapAuditLogEntry(reader));
            }

            return auditEntries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener historial de auditoría");
            throw new InvalidOperationException("Error al obtener historial de auditoría", ex);
        }
    }

    public async Task<List<AuditLogEntry>> GetAuditHistoryAsync(Guid? userId = null, long? certificateId = null, int limit = 100)
    {
        var sql = @"
            SELECT id, action_type, user_id, certificate_id, certificate_type, 
                   details, created_at, ip_address, user_agent
            FROM certificate_audit_log 
            WHERE 1=1";

        var parameters = new Dictionary<string, object>();
        parameters["@limit"] = limit;

        if (userId.HasValue)
        {
            sql += " AND user_id = @userId";
            parameters["@userId"] = userId.Value;
        }

        if (certificateId.HasValue)
        {
            sql += " AND certificate_id = @certificateId";
            parameters["@certificateId"] = certificateId.Value;
        }

        sql += " ORDER BY created_at DESC LIMIT @limit;";

        try
        {
            await using var cmd = _dataSource.CreateCommand(sql);
            foreach (var param in parameters)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value);
            }

            var auditEntries = new List<AuditLogEntry>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                auditEntries.Add(MapAuditLogEntry(reader));
            }

            return auditEntries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener historial de auditoría");
            throw new InvalidOperationException("Error al obtener historial de auditoría", ex);
        }
    }

    public async Task<AuditStatistics> GetAuditStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var sql = @"
            SELECT 
                COUNT(CASE WHEN action_type = 'CERTIFICATE_GENERATION' THEN 1 END) as total_generated,
                COUNT(CASE WHEN action_type = 'CERTIFICATE_VALIDATION' THEN 1 END) as total_validated,
                COUNT(CASE WHEN action_type = 'CERTIFICATE_REVOCATION' THEN 1 END) as total_revoked,
                COUNT(CASE WHEN action_type = 'CERTIFICATE_REISSUANCE' THEN 1 END) as total_reissued,
                COUNT(CASE WHEN action_type = 'FAILED_GENERATION_ATTEMPT' THEN 1 END) as total_failed_attempts,
                COUNT(CASE WHEN action_type = 'CERTIFICATE_VALIDATION' AND (details::jsonb->>'isValid') = 'true' THEN 1 END) as successful_validations,
                COUNT(CASE WHEN action_type = 'CERTIFICATE_VALIDATION' AND (details::jsonb->>'isValid') = 'false' THEN 1 END) as failed_validations
            FROM certificate_audit_log
            WHERE created_at >= @startDate AND created_at <= @endDate;";

        var parameters = new Dictionary<string, object>();
        parameters["@startDate"] = startDate ?? DateTime.MinValue;
        parameters["@endDate"] = endDate ?? DateTime.MaxValue;

        try
        {
            await using var cmd = _dataSource.CreateCommand(sql);
            foreach (var param in parameters)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            var statisticsStart = parameters["@startDate"] is DateTime dt ? dt : DateTime.MinValue;
            if (await reader.ReadAsync())
            {
                return new AuditStatistics
                {
                    TotalGenerated = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                    TotalValidated = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                    TotalRevoked = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                    TotalReissued = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                    TotalFailedAttempts = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                    SuccessfulValidations = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                    FailedValidations = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                    StatisticsPeriodStart = statisticsStart,
                    StatisticsGeneratedAt = DateTime.UtcNow
                };
            }

            return new AuditStatistics
            {
                StatisticsPeriodStart = statisticsStart,
                StatisticsGeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas de auditoría");
            throw new InvalidOperationException("Error al obtener estadísticas de auditoría", ex);
        }
    }

    private async Task LogAuditEntryAsync(string actionType, Guid? userId, long? certificateId, CertificateType? certificateType, Dictionary<string, object> details)
    {
        const string sql = @"
            INSERT INTO certificate_audit_log (
                action_type, user_id, certificate_id, certificate_type, 
                details, created_at, ip_address, user_agent
            ) VALUES (
                @actionType, @userId, @certificateId, @certificateType,
                @details, NOW(), @ipAddress, @userAgent
            );";

        try
        {
            await using var cmd = _dataSource.CreateCommand(sql);
            cmd.Parameters.AddWithValue("@actionType", actionType);
            cmd.Parameters.AddWithValue("@userId", userId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@certificateId", certificateId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@certificateType", certificateType?.ToString() ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@details", System.Text.Json.JsonSerializer.Serialize(details));
            cmd.Parameters.AddWithValue("@ipAddress", GetClientIpAddress());
            cmd.Parameters.AddWithValue("@userAgent", GetUserAgent());

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar entrada de auditoría: {ActionType}", actionType);
            // No lanzar excepción para no interrumpir el flujo principal
        }
    }

    private string GetClientIpAddress()
    {
        // En producción, obtener IP real del cliente
        // Por ahora retornar IP simulada
        return "127.0.0.1";
    }

    private string GetUserAgent()
    {
        // En producción, obtener User-Agent real
        // Por ahora retornar valor simulado
        return "CertificateService/1.0";
    }

    private AuditLogEntry MapAuditLogEntry(NpgsqlDataReader reader)
    {
        // Expected columns in SELECT:
        // 0: id, 1: action_type, 2: user_id, 3: certificate_id, 4: certificate_type, 5: details, 6: created_at, 7: ip_address, 8: user_agent
        string? detailsJson = reader.IsDBNull(5) ? null : reader.GetString(5);
        bool inferredSuccess = false;
        string? errorMessage = null;
        try
        {
            if (!string.IsNullOrEmpty(detailsJson))
            {
                var doc = JsonDocument.Parse(detailsJson);
                if (doc.RootElement.TryGetProperty("isValid", out var isValidProp) && isValidProp.ValueKind == JsonValueKind.True)
                {
                    inferredSuccess = true;
                }
                else if (doc.RootElement.TryGetProperty("isValid", out var isValidProp2) && isValidProp2.ValueKind == JsonValueKind.False)
                {
                    inferredSuccess = false;
                }
                if (doc.RootElement.TryGetProperty("reason", out var reasonProp) && reasonProp.ValueKind == JsonValueKind.String)
                {
                    errorMessage = reasonProp.GetString();
                }
            }
            else
            {
                // Infer success from action type when no details
                var actionType = reader.GetString(1);
                inferredSuccess = !actionType.StartsWith("FAILED", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // Ignore JSON parse errors; keep defaults
        }

        return new AuditLogEntry
        {
            Id = reader.GetInt32(0),
            ActionType = reader.GetString(1),
            UserId = reader.IsDBNull(2) ? null : reader.GetGuid(2),
            CertificateId = reader.IsDBNull(3) ? null : reader.GetInt64(3),
            ActionDescription = string.Empty,
            Timestamp = reader.GetDateTime(6),
            IpAddress = reader.IsDBNull(7) ? null : reader.GetString(7),
            UserAgent = reader.IsDBNull(8) ? null : reader.GetString(8),
            AdditionalData = detailsJson,
            Success = inferredSuccess,
            ErrorMessage = errorMessage
        };
    }
}