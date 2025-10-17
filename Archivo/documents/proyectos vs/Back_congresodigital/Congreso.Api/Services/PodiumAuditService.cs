using System.Text.Json;
using Congreso.Api.DTOs;
using Congreso.Api.Repositories;
using Dapper;
using Npgsql;
using Serilog;

namespace Congreso.Api.Services;

/// <summary>
/// Servicio de auditoría específico para operaciones de podio
/// </summary>
public interface IPodiumAuditService
{
    /// <summary>
    /// Registra una acción de podio (crear, actualizar, eliminar)
    /// </summary>
    /// <param name="userId">ID del usuario que realizó la acción</param>
    /// <param name="year">Año del podio</param>
    /// <param name="place">Lugar del podio</param>
    /// <param name="action">Tipo de acción (CREATE, UPDATE, DELETE)</param>
    /// <param name="details">Detalles adicionales</param>
    Task LogPodiumActionAsync(int userId, int year, int place, string action, Dictionary<string, object>? details = null);
    
    /// <summary>
    /// Registra la visualización de podio público
    /// </summary>
    /// <param name="year">Año del podio consultado</param>
    /// <param name="userAgent">User agent del solicitante</param>
    /// <param name="ipAddress">Dirección IP del solicitante</param>
    /// <param name="cacheHit">Si fue hit de caché</param>
    /// <param name="details">Detalles adicionales</param>
    Task LogPodiumViewAsync(int year, string? userAgent, string? ipAddress, bool cacheHit, Dictionary<string, object>? details = null);
    
    /// <summary>
    /// Obtiene el historial de auditoría de podio
    /// </summary>
    /// <param name="limit">Límite de registros</param>
    /// <returns>Lista de eventos de auditoría</returns>
    Task<List<PodiumAuditLogEntry>> GetPodiumAuditHistoryAsync(int limit = 100);
    
    /// <summary>
    /// Obtiene estadísticas de auditoría de podio
    /// </summary>
    /// <param name="startDate">Fecha de inicio</param>
    /// <param name="endDate">Fecha de fin</param>
    /// <returns>Estadísticas</returns>
    Task<PodiumAuditStatistics> GetPodiumAuditStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
}

/// <summary>
/// Implementación del servicio de auditoría de podio
/// </summary>
public class PodiumAuditService : IPodiumAuditService
{
    private readonly string _connectionString;
    private readonly ILogger<PodiumAuditService> _logger;
    private readonly Congreso.Api.Infrastructure.MetricsCollector _metricsCollector;

    public PodiumAuditService(IConfiguration configuration, ILogger<PodiumAuditService> logger, Congreso.Api.Infrastructure.MetricsCollector metricsCollector)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
            throw new InvalidOperationException("DefaultConnection not configured");
        _logger = logger;
        _metricsCollector = metricsCollector;
    }

    public async Task LogPodiumActionAsync(int userId, int year, int place, string action, Dictionary<string, object>? details = null)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            
            const string sql = @"
                INSERT INTO podium_audit_logs (user_id, year, place, action_type, action_description, timestamp, additional_data)
                VALUES (@UserId, @Year, @Place, @ActionType, @ActionDescription, NOW(), @AdditionalData)";

            var actionDescription = action switch
            {
                "CREATE" => $"Creó podio para año {year}, lugar {place}",
                "UPDATE" => $"Actualizó podio para año {year}, lugar {place}",
                "DELETE" => $"Eliminó podio para año {year}, lugar {place}",
                _ => $"Acción {action} en podio año {year}, lugar {place}"
            };

            await connection.ExecuteAsync(sql, new
            {
                UserId = userId,
                Year = year,
                Place = place,
                ActionType = action,
                ActionDescription = actionDescription,
                AdditionalData = details != null ? JsonSerializer.Serialize(details) : null
            });

            _logger.LogInformation("Podio audit: Usuario {UserId} realizó {Action} en podio año {Year}, lugar {Place}", 
                userId, action, year, place);
            
            _metricsCollector.RecordPodiumOperation(action, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar auditoría de acción de podio - Usuario {UserId}, Año {Year}, Lugar {Place}, Acción {Action}", 
                userId, year, place, action);
            throw;
        }
    }

    public async Task LogPodiumViewAsync(int year, string? userAgent, string? ipAddress, bool cacheHit, Dictionary<string, object>? details = null)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            
            const string sql = @"
                INSERT INTO podium_view_logs (year, user_agent, ip_address, cache_hit, timestamp, additional_data)
                VALUES (@Year, @UserAgent, @IpAddress, @CacheHit, NOW(), @AdditionalData)";

            await connection.ExecuteAsync(sql, new
            {
                Year = year,
                UserAgent = userAgent,
                IpAddress = ipAddress,
                CacheHit = cacheHit,
                AdditionalData = details != null ? JsonSerializer.Serialize(details) : null
            });

            if (cacheHit)
            {
                _logger.LogInformation("Podio view: Caché hit para año {Year} desde IP {IpAddress}", year, ipAddress);
                _metricsCollector.RecordCacheHit("podium", year.ToString());
            }
            else
            {
                _logger.LogInformation("Podio view: Caché miss para año {Year} desde IP {IpAddress}", year, ipAddress);
                _metricsCollector.RecordCacheMiss("podium", year.ToString());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar auditoría de visualización de podio - Año {Year}", year);
            // No lanzamos la excepción para no interrumpir el flujo principal
        }
    }

    public async Task<List<PodiumAuditLogEntry>> GetPodiumAuditHistoryAsync(int limit = 100)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            
            const string sql = @"
                SELECT 
                    id,
                    user_id as UserId,
                    year,
                    place,
                    action_type as ActionType,
                    action_description as ActionDescription,
                    timestamp,
                    ip_address as IpAddress,
                    user_agent as UserAgent,
                    additional_data as AdditionalData,
                    cache_hit as CacheHit
                FROM podium_audit_logs 
                ORDER BY timestamp DESC 
                LIMIT @Limit";

            var result = await connection.QueryAsync<PodiumAuditLogEntry>(sql, new { Limit = limit });
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener historial de auditoría de podio");
            throw;
        }
    }

    public async Task<PodiumAuditStatistics> GetPodiumAuditStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            const string sql = @"
                SELECT 
                    COUNT(*) as TotalViews,
                    COUNT(CASE WHEN cache_hit = true THEN 1 END) as CacheHits,
                    COUNT(CASE WHEN cache_hit = false THEN 1 END) as CacheMisses,
                    COUNT(DISTINCT year) as UniqueYearsViewed,
                    COUNT(DISTINCT ip_address) as UniqueIpAddresses
                FROM podium_view_logs 
                WHERE timestamp BETWEEN @StartDate AND @EndDate";

            var stats = await connection.QueryFirstAsync<PodiumAuditStatistics>(sql, new 
            { 
                StartDate = startDate, 
                EndDate = endDate 
            });

            stats.StatisticsPeriodStart = startDate.Value;
            stats.StatisticsGeneratedAt = DateTime.UtcNow;

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas de auditoría de podio");
            throw;
        }
    }
}

/// <summary>
/// Entrada de registro de auditoría de podio
/// </summary>
public class PodiumAuditLogEntry
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public int Year { get; set; }
    public int Place { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ActionDescription { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? AdditionalData { get; set; }
    public bool? CacheHit { get; set; }
}

/// <summary>
/// Estadísticas de auditoría de podio
/// </summary>
public class PodiumAuditStatistics
{
    public int TotalViews { get; set; }
    public int CacheHits { get; set; }
    public int CacheMisses { get; set; }
    public int UniqueYearsViewed { get; set; }
    public int UniqueIpAddresses { get; set; }
    public DateTime StatisticsPeriodStart { get; set; }
    public DateTime StatisticsGeneratedAt { get; set; }
    
    public double CacheHitRate => TotalViews > 0 ? (double)CacheHits / TotalViews * 100 : 0;
}