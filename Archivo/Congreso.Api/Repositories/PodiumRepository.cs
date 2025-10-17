using System.Data;
using Congreso.Api.DTOs;
using Dapper;
using Npgsql;

namespace Congreso.Api.Repositories;

/// <summary>
/// Repositorio para operaciones de base de datos relacionadas con el podio
/// </summary>
public interface IPodiumRepository
{
    /// <summary>
    /// Obtiene el podio por año desde la vista vw_podium_by_year
    /// </summary>
    /// <param name="year">Año del podio</param>
    /// <returns>Lista de registros del podio</returns>
    Task<List<PodiumResponse>> GetPodiumByYearAsync(int year);
    
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
    /// <returns>ID del nuevo registro</returns>
    Task<int> CreatePodiumAsync(CreatePodiumRequest request);
    
    /// <summary>
    /// Actualiza un registro de podio
    /// </summary>
    /// <param name="id">ID del registro</param>
    /// <param name="request">Datos actualizados</param>
    /// <returns>True si se actualizó, false si no existe</returns>
    Task<bool> UpdatePodiumAsync(int id, UpdatePodiumRequest request);
    
    /// <summary>
    /// Elimina un registro de podio
    /// </summary>
    /// <param name="id">ID del registro</param>
    /// <returns>True si se eliminó, false si no existe</returns>
    Task<bool> DeletePodiumAsync(int id);
    
    /// <summary>
    /// Verifica si existe un podio para un año y lugar específico
    /// </summary>
    /// <param name="year">Año</param>
    /// <param name="place">Lugar (1-3)</param>
    /// <returns>True si existe</returns>
    Task<bool> ExistsPodiumForYearAndPlaceAsync(int year, int place);
    
    /// <summary>
    /// Verifica si existe un usuario por ID
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <returns>True si existe</returns>
    Task<bool> UserExistsAsync(int userId);
    
    /// <summary>
    /// Verifica si existe una actividad por ID
    /// </summary>
    /// <param name="activityId">ID de la actividad</param>
    /// <returns>True si existe</returns>
    Task<bool> ActivityExistsAsync(int activityId);
    
    /// <summary>
    /// Obtiene el rango de fechas de una edición por año
    /// </summary>
    /// <param name="year">Año de la edición</param>
    /// <returns>Tupla con fecha de inicio y fin</returns>
    Task<(DateTime StartDate, DateTime EndDate)?> GetEditionDateRangeAsync(int year);
}

/// <summary>
/// Implementación del repositorio de podio
/// </summary>
public class PodiumRepository : IPodiumRepository
{
    private readonly string _connectionString;
    private readonly ILogger<PodiumRepository> _logger;

    public PodiumRepository(IConfiguration configuration, ILogger<PodiumRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
            throw new InvalidOperationException("DefaultConnection not configured");
        _logger = logger;
    }

    public async Task<List<PodiumResponse>> GetPodiumByYearAsync(int year)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = @"
            SELECT 
                id,
                year,
                COALESCE(place, position) AS Place,
                activity_id AS ActivityId,
                activity_title AS ActivityTitle,
                user_id AS UserId,
                COALESCE(winner_name, user_full_name) AS WinnerName,
                award_date AS AwardDate,
                COALESCE(prize_description, prize) AS PrizeDescription
            FROM vw_podium_by_year 
            WHERE year = @Year 
            ORDER BY COALESCE(place, position) ASC, id ASC";

        try
        {
            var result = await connection.QueryAsync<PodiumResponse>(sql, new { Year = year });
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener podio para el año {Year}", year);
            throw;
        }
    }

    public async Task<PodiumResponse?> GetPodiumByIdAsync(int id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = @"
            SELECT 
                id,
                year,
                COALESCE(place, position) AS Place,
                activity_id AS ActivityId,
                activity_title AS ActivityTitle,
                user_id AS UserId,
                COALESCE(winner_name, user_full_name) AS WinnerName,
                award_date AS AwardDate,
                COALESCE(prize_description, prize) AS PrizeDescription
            FROM vw_podium_by_year 
            WHERE id = @Id";

        try
        {
            return await connection.QueryFirstOrDefaultAsync<PodiumResponse>(sql, new { Id = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener podio con ID {Id}", id);
            throw;
        }
    }

    public async Task<List<PodiumResponse>> GetAllPodiumsAsync(int page, int pageSize)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = @"
            SELECT 
                id,
                year,
                COALESCE(place, position) AS Place,
                activity_id AS ActivityId,
                activity_title AS ActivityTitle,
                user_id AS UserId,
                COALESCE(winner_name, user_full_name) AS WinnerName,
                award_date AS AwardDate,
                COALESCE(prize_description, prize) AS PrizeDescription
            FROM vw_podium_by_year 
            ORDER BY year DESC, COALESCE(place, position) ASC, id ASC
            LIMIT @PageSize OFFSET @Offset";

        try
        {
            var offset = (page - 1) * pageSize;
            var result = await connection.QueryAsync<PodiumResponse>(sql, new { PageSize = pageSize, Offset = offset });
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los podios - Página {Page}, Tamaño {PageSize}", page, pageSize);
            throw;
        }
    }

    public async Task<int> CountPodiumsAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = "SELECT COUNT(*) FROM vw_podium_by_year";

        try
        {
            return await connection.ExecuteScalarAsync<int>(sql);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al contar podios");
            throw;
        }
    }

    public async Task<int> CreatePodiumAsync(CreatePodiumRequest request)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = @"
            INSERT INTO podiums (year, place, activity_id, user_id, award_date, team_id, prize_description, created_at, updated_at)
            VALUES (@Year, @Place, @ActivityId, @UserId, @AwardDate, @TeamId, @PrizeDescription, NOW(), NOW())
            RETURNING id";

        try
        {
            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                request.Year,
                request.Place,
                request.ActivityId,
                request.UserId,
                request.AwardDate,
                request.TeamId,
                request.PrizeDescription
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear podio - Año {Year}, Lugar {Place}", request.Year, request.Place);
            throw;
        }
    }

    public async Task<bool> UpdatePodiumAsync(int id, UpdatePodiumRequest request)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = @"
            UPDATE podiums 
            SET year = @Year,
                place = @Place,
                activity_id = @ActivityId,
                user_id = @UserId,
                award_date = @AwardDate,
                team_id = @TeamId,
                prize_description = @PrizeDescription,
                updated_at = NOW()
            WHERE id = @Id";

        try
        {
            var affectedRows = await connection.ExecuteAsync(sql, new
            {
                id,
                request.Year,
                request.Place,
                request.ActivityId,
                request.UserId,
                request.AwardDate,
                request.TeamId,
                request.PrizeDescription
            });
            
            return affectedRows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar podio con ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeletePodiumAsync(int id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = "DELETE FROM podiums WHERE id = @Id";

        try
        {
            var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });
            return affectedRows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar podio con ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> ExistsPodiumForYearAndPlaceAsync(int year, int place)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = "SELECT COUNT(*) FROM podiums WHERE year = @Year AND place = @Place";

        try
        {
            var count = await connection.ExecuteScalarAsync<int>(sql, new { Year = year, Place = place });
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar existencia de podio - Año {Year}, Lugar {Place}", year, place);
            throw;
        }
    }

    public async Task<bool> UserExistsAsync(int userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = "SELECT COUNT(*) FROM users WHERE id = @UserId";

        try
        {
            var count = await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar existencia de usuario con ID {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ActivityExistsAsync(int activityId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = "SELECT COUNT(*) FROM activities WHERE id = @ActivityId";

        try
        {
            var count = await connection.ExecuteScalarAsync<int>(sql, new { ActivityId = activityId });
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar existencia de actividad con ID {ActivityId}", activityId);
            throw;
        }
    }

    public async Task<(DateTime StartDate, DateTime EndDate)?> GetEditionDateRangeAsync(int year)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = @"
            SELECT start_date, end_date 
            FROM editions 
            WHERE year = @Year 
            LIMIT 1";

        try
        {
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Year = year });
            if (result != null)
            {
                return ((DateTime)result.start_date, (DateTime)result.end_date);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener rango de fechas de edición para el año {Year}", year);
            throw;
        }
    }
}