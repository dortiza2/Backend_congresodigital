using System.ComponentModel.DataAnnotations;
using Congreso.Api.DTOs;
using Congreso.Api.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Congreso.Api.Services;

/// <summary>
/// Interfaz para el servicio de gestión de podio (ganadores)
/// </summary>
public interface IPodiumService
{
    /// <summary>
    /// Obtiene el podio por año con caché
    /// </summary>
    /// <param name="year">Año del podio</param>
    /// <param name="userAgent">User agent para auditoría</param>
    /// <param name="ipAddress">IP para auditoría</param>
    /// <returns>Lista de registros del podio</returns>
    Task<List<PodiumResponse>> GetPodiumByYearAsync(int year, string? userAgent = null, string? ipAddress = null);
    
    /// <summary>
    /// Obtiene un registro de podio por ID
    /// </summary>
    /// <param name="id">ID del registro</param>
    /// <returns>Registro del podio o null</returns>
    Task<PodiumResponse?> GetPodiumByIdAsync(int id);
    
    /// <summary>
    /// Obtiene todos los registros de podio con paginación
    /// </summary>
    /// <param name="page">Número de página</param>
    /// <param name="pageSize">Tamaño de página</param>
    /// <returns>Lista de registros del podio</returns>
    Task<List<PodiumResponse>> GetAllPodiumsAsync(int page, int pageSize);
    
    /// <summary>
    /// Cuenta el total de registros de podio
    /// </summary>
    /// <returns>Total de registros</returns>
    Task<int> CountPodiumsAsync();
    
    /// <summary>
    /// Crea un nuevo registro de podio
    /// </summary>
    /// <param name="request">Datos del podio</param>
    /// <param name="userId">ID del usuario que crea</param>
    /// <returns>ID del nuevo registro</returns>
    Task<int> CreatePodiumAsync(CreatePodiumRequest request, int userId);
    
    /// <summary>
    /// Actualiza un registro de podio
    /// </summary>
    /// <param name="id">ID del registro</param>
    /// <param name="request">Datos actualizados</param>
    /// <param name="userId">ID del usuario que actualiza</param>
    /// <returns>True si se actualizó, false si no existe</returns>
    Task<bool> UpdatePodiumAsync(int id, UpdatePodiumRequest request, int userId);
    
    /// <summary>
    /// Elimina un registro de podio
    /// </summary>
    /// <param name="id">ID del registro</param>
    /// <param name="userId">ID del usuario que elimina</param>
    /// <returns>True si se eliminó, false si no existe</returns>
    Task<bool> DeletePodiumAsync(int id, int userId);
    
    /// <summary>
    /// Invalida la caché para un año específico
    /// </summary>
    /// <param name="year">Año a invalidar</param>
    Task InvalidateCacheAsync(int year);
}

/// <summary>
/// Implementación del servicio de gestión de podio
/// </summary>
public class PodiumService : IPodiumService
{
    private readonly IPodiumRepository _podiumRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PodiumService> _logger;
    private readonly IPodiumAuditService _auditService;
    private readonly Congreso.Api.Infrastructure.MetricsCollector _metricsCollector;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);

    public PodiumService(
        IPodiumRepository podiumRepository,
        IMemoryCache cache,
        ILogger<PodiumService> logger,
        IPodiumAuditService auditService,
        Congreso.Api.Infrastructure.MetricsCollector metricsCollector)
    {
        _podiumRepository = podiumRepository;
        _cache = cache;
        _logger = logger;
        _auditService = auditService;
        _metricsCollector = metricsCollector;
    }

    public async Task<List<PodiumResponse>> GetPodiumByYearAsync(int year, string? userAgent = null, string? ipAddress = null)
    {
        using var activity = _metricsCollector.StartActivity("podium_query");
        
        try
        {
            var cacheKey = $"podium_year_{year}";
            
            if (_cache.TryGetValue(cacheKey, out List<PodiumResponse>? cachedPodium))
            {
                _logger.LogInformation("Podio encontrado en caché para año {Year}", year);
                await _auditService.LogPodiumViewAsync(year, userAgent, ipAddress, true);
                return cachedPodium ?? new List<PodiumResponse>();
            }

            _logger.LogInformation("Consultando podio desde base de datos para año {Year}", year);
            var podium = await _podiumRepository.GetPodiumByYearAsync(year);
            
            // Guardar en caché
            _cache.Set(cacheKey, podium, _cacheExpiration);
            
            await _auditService.LogPodiumViewAsync(year, userAgent, ipAddress, false);
            
            _metricsCollector.RecordCacheMiss("podium", year.ToString());
            // Activity is automatically completed when disposed
            
            return podium;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener podio para año {Year}", year);
            // Activity will record failure when disposed in catch block
            throw;
        }
    }

    public async Task<PodiumResponse?> GetPodiumByIdAsync(int id)
    {
        try
        {
            return await _podiumRepository.GetPodiumByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener podio con ID {Id}", id);
            throw;
        }
    }

    public async Task<List<PodiumResponse>> GetAllPodiumsAsync(int page, int pageSize)
    {
        try
        {
            return await _podiumRepository.GetAllPodiumsAsync(page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los podios - Página {Page}, Tamaño {PageSize}", page, pageSize);
            throw;
        }
    }

    public async Task<int> CountPodiumsAsync()
    {
        try
        {
            return await _podiumRepository.CountPodiumsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al contar podios");
            throw;
        }
    }

    public async Task<int> CreatePodiumAsync(CreatePodiumRequest request, int userId)
    {
        using var activity = _metricsCollector.StartActivity("podium_create");
        
        try
        {
            // Validaciones de negocio
            await ValidatePodiumRequestAsync(request);
            
            // Verificar que no exista otro podio para el mismo año y lugar
            if (await _podiumRepository.ExistsPodiumForYearAndPlaceAsync(request.Year, request.Place))
            {
                throw new InvalidOperationException($"Ya existe un podio para el año {request.Year} y lugar {request.Place}");
            }
            
            var podiumId = await _podiumRepository.CreatePodiumAsync(request);
            
            // Invalidar caché
            await InvalidateCacheAsync(request.Year);
            
            // Registrar auditoría
            await _auditService.LogPodiumActionAsync(userId, request.Year, request.Place, "CREATE", new Dictionary<string, object>
            {
                ["podiumId"] = podiumId,
                ["activityId"] = request.ActivityId,
                ["userId"] = request.UserId
            });
            
            _logger.LogInformation("Podio creado exitosamente - ID {PodiumId}, Año {Year}, Lugar {Place}", 
                podiumId, request.Year, request.Place);
            
            return podiumId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear podio - Año {Year}, Lugar {Place}", request.Year, request.Place);
            // Activity will record failure when disposed in catch block
            throw;
        }
    }

    public async Task<bool> UpdatePodiumAsync(int id, UpdatePodiumRequest request, int userId)
    {
        using var activity = _metricsCollector.StartActivity("podium_update");
        
        try
        {
            // Validaciones de negocio
            await ValidatePodiumRequestAsync(request);
            
            // Verificar que no exista otro podio para el mismo año y lugar (excluyendo el actual)
            var existingPodium = await _podiumRepository.GetPodiumByIdAsync(id);
            if (existingPodium == null)
            {
                return false;
            }
            
            if (existingPodium.Year != request.Year || existingPodium.Place != request.Place)
            {
                if (await _podiumRepository.ExistsPodiumForYearAndPlaceAsync(request.Year, request.Place))
                {
                    throw new InvalidOperationException($"Ya existe un podio para el año {request.Year} y lugar {request.Place}");
                }
            }
            
            var updated = await _podiumRepository.UpdatePodiumAsync(id, request);
            
            if (updated)
            {
                // Invalidar caché para ambos años (antiguo y nuevo)
                await InvalidateCacheAsync(existingPodium.Year);
                if (existingPodium.Year != request.Year)
                {
                    await InvalidateCacheAsync(request.Year);
                }
                
                // Registrar auditoría
                await _auditService.LogPodiumActionAsync(userId, request.Year, request.Place, "UPDATE", new Dictionary<string, object>
                {
                    ["podiumId"] = id,
                    ["activityId"] = request.ActivityId,
                    ["userId"] = request.UserId
                });
                
                _logger.LogInformation("Podio actualizado exitosamente - ID {PodiumId}, Año {Year}, Lugar {Place}", 
                    id, request.Year, request.Place);
            }
            
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar podio - ID {PodiumId}", id);
            // Activity will record failure when disposed in catch block
            throw;
        }
    }

    public async Task<bool> DeletePodiumAsync(int id, int userId)
    {
        using var activity = _metricsCollector.StartActivity("podium_delete");
        
        try
        {
            var podium = await _podiumRepository.GetPodiumByIdAsync(id);
            if (podium == null)
            {
                return false;
            }
            
            var deleted = await _podiumRepository.DeletePodiumAsync(id);
            
            if (deleted)
            {
                // Invalidar caché
                await InvalidateCacheAsync(podium.Year);
                
                // Registrar auditoría
                await _auditService.LogPodiumActionAsync(userId, podium.Year, podium.Place, "DELETE", new Dictionary<string, object>
                {
                    ["podiumId"] = id,
                    ["activityId"] = podium.ActivityId,
                    ["userId"] = podium.UserId
                });
                
                _logger.LogInformation("Podio eliminado exitosamente - ID {PodiumId}, Año {Year}, Lugar {Place}", 
                    id, podium.Year, podium.Place);
            }
            
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar podio - ID {PodiumId}", id);
            // Activity will record failure when disposed in catch block
            throw;
        }
    }

    public async Task InvalidateCacheAsync(int year)
    {
        try
        {
            var cacheKey = $"podium_year_{year}";
            _cache.Remove(cacheKey);
            _logger.LogInformation("Caché invalidada para podio año {Year}", year);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al invalidar caché para año {Year}", year);
        }
    }

    private async Task ValidatePodiumRequestAsync(CreatePodiumRequest request)
    {
        // Validar año (rango razonable)
        var currentYear = DateTime.UtcNow.Year;
        if (request.Year < 2020 || request.Year > currentYear + 2)
        {
            throw new ValidationException($"El año debe estar entre 2020 y {currentYear + 2}");
        }

        // Validar lugar (1-3)
        if (request.Place < 1 || request.Place > 3)
        {
            throw new ValidationException("El lugar debe ser 1, 2 o 3");
        }

        // Validar que exista el usuario
        if (!await _podiumRepository.UserExistsAsync(request.UserId))
        {
            throw new ValidationException($"El usuario con ID {request.UserId} no existe");
        }

        // Validar que exista la actividad
        if (!await _podiumRepository.ActivityExistsAsync(request.ActivityId))
        {
            throw new ValidationException($"La actividad con ID {request.ActivityId} no existe");
        }

        // Validar que la fecha de premiación esté dentro del rango de la edición
        var dateRange = await _podiumRepository.GetEditionDateRangeAsync(request.Year);
        if (dateRange.HasValue)
        {
            if (request.AwardDate < dateRange.Value.StartDate || request.AwardDate > dateRange.Value.EndDate)
            {
                throw new ValidationException($"La fecha de premiación debe estar entre {dateRange.Value.StartDate:yyyy-MM-dd} y {dateRange.Value.EndDate:yyyy-MM-dd}");
            }
        }
    }

    private async Task ValidatePodiumRequestAsync(UpdatePodiumRequest request)
    {
        // Reutilizar la validación de CreatePodiumRequest
        var createRequest = new CreatePodiumRequest
        {
            Year = request.Year,
            Place = request.Place,
            ActivityId = request.ActivityId,
            UserId = request.UserId,
            AwardDate = request.AwardDate,
            TeamId = request.TeamId,
            PrizeDescription = request.PrizeDescription
        };

        await ValidatePodiumRequestAsync(createRequest);
    }
}