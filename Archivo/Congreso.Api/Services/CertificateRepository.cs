using Congreso.Api.DTOs.Certificate;
using Npgsql;

namespace Congreso.Api.Services;

/// <summary>
/// Implementación del repositorio de certificados
/// </summary>
public class CertificateRepository : ICertificateRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<CertificateRepository> _logger;

    public CertificateRepository(NpgsqlDataSource dataSource, ILogger<CertificateRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<CertificateRecord> CreateCertificateAsync(CertificateRecord certificate)
    {
        const string sql = @"
            INSERT INTO certificates (
                hash, verification_code, type, status, user_id, 
                download_url, validation_url, issued_at, expires_at, 
                metadata, activity_id, enrollment_id, created_at, updated_at
            ) VALUES (
                @hash, @verificationCode, @type, @status, @userId,
                @downloadUrl, @validationUrl, @issuedAt, @expiresAt,
                @metadata, @activityId, @enrollmentId, NOW(), NOW()
            ) RETURNING id;";

        try
        {
            await using var cmd = _dataSource.CreateCommand(sql);
            cmd.Parameters.AddWithValue("@hash", certificate.Hash);
            cmd.Parameters.AddWithValue("@verificationCode", certificate.VerificationCode);
            cmd.Parameters.AddWithValue("@type", certificate.Type.ToString());
            cmd.Parameters.AddWithValue("@status", certificate.Status.ToString());
            cmd.Parameters.AddWithValue("@userId", certificate.UserId);
            cmd.Parameters.AddWithValue("@downloadUrl", certificate.DownloadUrl ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@validationUrl", certificate.ValidationUrl ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@issuedAt", certificate.IssuedAt);
            cmd.Parameters.AddWithValue("@expiresAt", certificate.ExpiresAt ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@metadata", certificate.Metadata ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@activityId", certificate.ActivityId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@enrollmentId", certificate.EnrollmentId ?? (object)DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            certificate.Id = Convert.ToInt32(result);

            _logger.LogInformation("Certificado creado exitosamente con ID: {CertificateId}", certificate.Id);
            return certificate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear certificado en base de datos");
            throw new InvalidOperationException("Error al crear certificado", ex);
        }
    }

    public async Task<CertificateRecord?> GetCertificateByHashAsync(string hash)
    {
        const string sql = @"
            SELECT id, hash, verification_code, type, status, user_id,
                   download_url, validation_url, issued_at, expires_at,
                   metadata, activity_id, enrollment_id, created_at, updated_at
            FROM certificates 
            WHERE hash = @hash 
            LIMIT 1;";

        try
        {
            await using var cmd = _dataSource.CreateCommand(sql);
            cmd.Parameters.AddWithValue("@hash", hash);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapCertificateRecord(reader);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener certificado por hash: {Hash}", hash);
            throw new InvalidOperationException("Error al obtener certificado", ex);
        }
    }

    public async Task<CertificateRecord?> GetCertificateByIdAsync(long id)
    {
        const string sql = @"
            SELECT id, hash, verification_code, type, status, user_id,
                   download_url, validation_url, issued_at, expires_at,
                   metadata, activity_id, enrollment_id, created_at, updated_at
            FROM certificates 
            WHERE id = @id 
            LIMIT 1;";

        try
        {
            await using var cmd = _dataSource.CreateCommand(sql);
            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapCertificateRecord(reader);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener certificado por ID: {CertificateId}", id);
            throw new InvalidOperationException("Error al obtener certificado", ex);
        }
    }

    public async Task<List<CertificateRecord>> GetUserCertificatesAsync(Guid userId, bool includeRevoked = false, int pageNumber = 1, int pageSize = 10)
    {
        var sql = @"
            SELECT id, hash, verification_code, type, status, user_id,
                   download_url, validation_url, issued_at, expires_at,
                   metadata, activity_id, enrollment_id, created_at, updated_at
            FROM certificates 
            WHERE user_id = @userId";

        if (!includeRevoked)
        {
            sql += " AND status != 'Revoked'";
        }

        sql += " ORDER BY issued_at DESC LIMIT @pageSize OFFSET @offset;";

        try
        {
            await using var cmd = _dataSource.CreateCommand(sql);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@pageSize", pageSize);
            cmd.Parameters.AddWithValue("@offset", (pageNumber - 1) * pageSize);

            var certificates = new List<CertificateRecord>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                certificates.Add(MapCertificateRecord(reader));
            }

            return certificates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener certificados por usuario ID: {UserId}", userId);
            throw new InvalidOperationException("Error al obtener certificados", ex);
        }
    }

    public async Task<int> CountUserCertificatesAsync(Guid userId, bool includeRevoked = false)
    {
        var sql = "SELECT COUNT(1) FROM certificates WHERE user_id = @userId";
        
        if (!includeRevoked)
        {
            sql += " AND status != 'Revoked'";
        }
        sql += ";";

        try
        {
            await using var cmd = _dataSource.CreateCommand(sql);
            cmd.Parameters.AddWithValue("@userId", userId);

            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al contar certificados del usuario: {UserId}", userId);
            throw new InvalidOperationException("Error al contar certificados", ex);
        }
    }

    public async Task<bool> UpdateCertificateStatusAsync(long certificateId, CertificateStatus status, string? reason = null)
    {
        var sql = @"
            UPDATE certificates 
            SET status = @status, updated_at = NOW()";

        if (!string.IsNullOrEmpty(reason))
        {
            sql += ", revocation_reason = @reason, revoked_at = NOW()";
        }
        
        sql += " WHERE id = @id;";

        try
        {
            await using var cmd = _dataSource.CreateCommand(sql);
            cmd.Parameters.AddWithValue("@id", certificateId);
            cmd.Parameters.AddWithValue("@status", status.ToString());
            
            if (!string.IsNullOrEmpty(reason))
            {
                cmd.Parameters.AddWithValue("@reason", reason);
            }

            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar estado del certificado: {CertificateId}", certificateId);
            throw new InvalidOperationException("Error al actualizar certificado", ex);
        }
    }

    public async Task<bool> HasExistingCertificateAsync(Guid userId, CertificateType certificateType, int? activityId = null)
    {
        var sql = @"
            SELECT COUNT(1) 
            FROM certificates 
            WHERE user_id = @userId 
            AND type = @type 
            AND status = 'Active'";

        if (activityId.HasValue)
        {
            sql += " AND activity_id = @activityId";
        }
        
        sql += ";";

        try
        {
            await using var cmd = _dataSource.CreateCommand(sql);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@type", certificateType.ToString());
            
            if (activityId.HasValue)
            {
                cmd.Parameters.AddWithValue("@activityId", activityId.Value);
            }

            return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar certificado existente para usuario: {UserId}, tipo: {CertificateType}", userId, certificateType);
            throw new InvalidOperationException("Error al verificar certificado existente", ex);
        }
    }

    public async Task<List<CertificateRecord>> GetCertificatesByTypeAndStatusAsync(CertificateType certificateType, CertificateStatus? status = null, int pageNumber = 1, int pageSize = 10)
    {
        var sql = @"
            SELECT id, hash, verification_code, type, status, user_id,
                   download_url, validation_url, issued_at, expires_at,
                   metadata, activity_id, enrollment_id, created_at, updated_at
            FROM certificates 
            WHERE type = @type";

        if (status.HasValue)
        {
            sql += " AND status = @status";
        }
        
        sql += " ORDER BY issued_at DESC LIMIT @pageSize OFFSET @offset;";

        try
        {
            await using var cmd = _dataSource.CreateCommand(sql);
            cmd.Parameters.AddWithValue("@type", certificateType.ToString());
            
            if (status.HasValue)
            {
                cmd.Parameters.AddWithValue("@status", status.Value.ToString());
            }
            
            cmd.Parameters.AddWithValue("@pageSize", pageSize);
            cmd.Parameters.AddWithValue("@offset", (pageNumber - 1) * pageSize);

            var certificates = new List<CertificateRecord>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                certificates.Add(MapCertificateRecord(reader));
            }

            return certificates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener certificados por tipo y estado: {CertificateType}, {Status}", certificateType, status);
            throw new InvalidOperationException("Error al obtener certificados", ex);
        }
    }

    private CertificateRecord MapCertificateRecord(NpgsqlDataReader reader)
    {
        return new CertificateRecord
        {
            Id = reader.GetInt64(0),
            Hash = reader.GetString(1),
            VerificationCode = reader.GetString(2),
            Type = Enum.Parse<CertificateType>(reader.GetString(3)),
            Status = Enum.Parse<CertificateStatus>(reader.GetString(4)),
            UserId = reader.GetGuid(5),
            DownloadUrl = reader.IsDBNull(6) ? null : reader.GetString(6),
            ValidationUrl = reader.IsDBNull(7) ? null : reader.GetString(7),
            IssuedAt = reader.GetDateTime(8),
            ExpiresAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
            Metadata = reader.IsDBNull(10) ? null : reader.GetString(10),
            ActivityId = reader.IsDBNull(11) ? null : reader.GetInt32(11),
            EnrollmentId = reader.IsDBNull(12) ? null : reader.GetInt32(12),
            CreatedAt = reader.GetDateTime(13),
            UpdatedAt = reader.GetDateTime(14)
        };
    }
}